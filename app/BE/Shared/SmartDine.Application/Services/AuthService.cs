using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý toàn bộ nghiệp vụ xác thực và quản lý phiên đăng nhập.
///
/// Chịu trách nhiệm:
///   - Login cho User (nhân viên) và Customer (khách hàng) — cùng 1 endpoint.
///   - Register cho Customer (nhân viên được tạo bởi Manager qua kênh khác).
///   - Refresh token rotation (revoke cũ, cấp mới).
///   - Forgot/Reset password flow.
///   - Guest login (khách vãng lai vào bàn không cần tài khoản).
///   - Logout (revoke tất cả refresh token).
///
/// Dependencies:
///   - IUnitOfWork: truy cập Users, Customers, RefreshTokens, PasswordResetTokens, Tables, DiningSessions.
///   - IJwtTokenService: tạo/validate JWT (RSA256).
///   - IPasswordHasher: hash/verify mật khẩu (BCrypt).
/// </summary>
public class AuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUnitOfWork uow, IJwtTokenService jwtService, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/login
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xác thực email + password, trả về cặp AccessToken + RefreshToken.
    ///
    /// Luồng:
    ///   1. Tìm email trong bảng Users (nhân viên) trước.
    ///      - Tìm thấy + IsActive → verify password BCrypt → cấp token với role (STAFF/CHEF/MANAGER).
    ///      - Tìm thấy nhưng IsActive = false → rơi xuống bước 2 (không tiết lộ tài khoản bị khóa).
    ///   2. Tìm email trong bảng Customers (khách hàng).
    ///      - Tìm thấy → verify password → cấp token với role = CUSTOMER.
    ///   3. Không tìm thấy ở cả 2 bảng → throw lỗi chung (chống user enumeration).
    ///
    /// Bảo mật: luôn trả cùng 1 error message dù email sai hay password sai.
    ///
    /// Error cases:
    ///   - Email không tồn tại → BusinessRuleViolationException (422).
    ///   - Password sai → BusinessRuleViolationException (422).
    ///   - Tài khoản User bị vô hiệu hóa → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        // Ưu tiên tìm trong Users (nhân viên) trước
        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user != null && user.IsActive)
        {
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.EMAIL_OR_PASSSWORD_INVALID);

            return await GenerateTokenResponseAsync(user.Id, user.Email, user.FullName, user.Role.ToString(), UserType.USER);
        }

        // Fallback: tìm trong Customers (khách hàng)
        var customer = await _uow.Customers.GetByEmailAsync(request.Email);
        if (customer != null)
        {
            if (customer.PasswordHash == null || !_passwordHasher.VerifyPassword(request.Password, customer.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.EMAIL_OR_PASSSWORD_INVALID);

            return await GenerateTokenResponseAsync(customer.Id, customer.Email ?? string.Empty, customer.FullName ?? "Customer", UserRole.CUSTOMER.ToString(), UserType.CUSTOMER, customer.Phone, customer.LoyaltyPoints, customer.MembershipLevel.ToString());
        }

        // Không tìm thấy → trả lỗi chung (chống enumeration)
        throw new BusinessRuleViolationException(ValidationMessages.EMAIL_OR_PASSSWORD_INVALID);
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/register
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Đăng ký tài khoản Customer mới, tự động login sau khi tạo thành công.
    ///
    /// Luồng:
    ///   1. Check email trùng trong cả bảng Users lẫn Customers (toàn hệ thống).
    ///   2. Check phone trùng (nếu có) trong Customers.
    ///   3. Tạo Customer với PasswordHash (BCrypt), MembershipLevel = BRONZE.
    ///   4. SaveChanges → sinh cặp token → trả về TokenResponse (tự động login).
    ///
    /// Error cases:
    ///   - Email đã tồn tại (User hoặc Customer) → BusinessRuleViolationException (422).
    ///   - Phone đã tồn tại → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        // Check trùng email trên cả 2 bảng
        if (await _uow.Users.ExistsAsync(request.Email) || await _uow.Customers.GetByEmailAsync(request.Email) != null)
            throw new BusinessRuleViolationException(ValidationMessages.EMAIL_ALREADY_EXISTS);

        if (!string.IsNullOrEmpty(request.PhoneNumber) && await _uow.Customers.GetByPhoneAsync(request.PhoneNumber) != null)
            throw new BusinessRuleViolationException(ValidationMessages.PHONE_ALREADY_EXISTS);

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.PhoneNumber,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            LoyaltyPoints = 0,
            MembershipLevel = LoyaltyTier.BRONZE,
            TotalSpent = 0.00m,
            VisitCount = 0
        };

        await _uow.Customers.AddAsync(customer);
        await _uow.SaveChangesAsync();

        // Tự động login: tạo token ngay sau register
        return await GenerateTokenResponseAsync(customer.Id, customer.Email, customer.FullName, UserRole.CUSTOMER.ToString(), UserType.CUSTOMER, customer.Phone, customer.LoyaltyPoints, customer.MembershipLevel.ToString());
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/refresh-token
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Làm mới cặp token khi AccessToken hết hạn (Token Rotation).
    ///
    /// Luồng:
    ///   1. Parse expired AccessToken → lấy ClaimsPrincipal (ValidateLifetime=false, vẫn verify RSA256).
    ///   2. Lấy jti claim từ token → dùng làm key liên kết với RefreshToken trong DB.
    ///   3. Tìm RefreshToken trong DB theo token string → validate:
    ///      - Token tồn tại? (chống dùng token bịa).
    ///      - Chưa hết hạn? (7 ngày).
    ///      - JwtId khớp? (đảm bảo đúng cặp token, chống mix-match).
    ///   4. Revoke RefreshToken cũ (IsRevoked=true, RevokedAt, ReplacedByToken).
    ///   5. Tạo cặp AccessToken + RefreshToken mới → lưu RefreshToken mới vào DB.
    ///   6. Trả về TokenResponse mới cho client.
    ///
    /// Error cases:
    ///   - AccessToken không parse được / bị tamper → BusinessRuleViolationException (422).
    ///   - RefreshToken không tồn tại trong DB → BusinessRuleViolationException (422).
    ///   - RefreshToken hết hạn → BusinessRuleViolationException (422).
    ///   - JwtId không khớp (sai cặp) → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Bước 1-2: Parse expired token, lấy jti
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken)
            ?? throw new BusinessRuleViolationException(ValidationMessages.ACCESS_TOKEN_INVALID);

        var jwtId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
            ?? throw new BusinessRuleViolationException(ValidationMessages.ACCESS_TOKEN_INVALID);

        // Bước 3: Tìm và validate RefreshToken trong DB
        var storedToken = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken)
            ?? throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_NOT_FOUND);

        if (storedToken.IsRevoked)
            throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_NOT_FOUND);

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_EXPIRED);

        if (storedToken.JwtId != jwtId)
            throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_MISMATCH);

        // Bước 4: Revoke token cũ
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Bước 5: Tạo cặp token mới
        var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var email = principal.FindFirst(ClaimTypes.Email)!.Value;
        var fullName = principal.FindFirst(ClaimTypes.Name)!.Value;
        var role = principal.FindFirst(ClaimTypes.Role)!.Value;

        var (accessToken, newJwtId) = _jwtService.GenerateAccessToken(userId, email, fullName, role);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            JwtId = newJwtId,
            UserType = storedToken.UserType,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        // Ghi nhận token chain: cũ → mới
        storedToken.ReplacedByToken = newRefreshToken;

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity);
        await _uow.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600,
            User = new UserInfoResponse
            {
                Id = userId,
                FullName = fullName,
                Email = email,
                Role = role
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/forgot-password
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Khởi tạo flow đặt lại mật khẩu — tạo reset token có thời hạn 15 phút.
    ///
    /// Luồng:
    ///   1. Tìm email trong Users, rồi Customers.
    ///   2. Nếu không tìm thấy → vẫn trả response thành công (chống email enumeration).
    ///   3. Invalidate tất cả reset token cũ của user này (tránh token cũ còn dùng được).
    ///   4. Tạo token mới (32 bytes random, base64) + lưu vào DB với expiry 15 phút.
    ///   5. Trả về ResetToken trong response (dev mode — production sẽ gửi qua email).
    ///
    /// Bảo mật: Response luôn giống nhau dù email có tồn tại hay không.
    /// </summary>
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        int userId;
        UserType userType;

        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user != null)
        {
            userId = user.Id;
            userType = UserType.USER;
        }
        else
        {
            var customer = await _uow.Customers.GetByEmailAsync(request.Email);
            if (customer == null)
            {
                // Email không tồn tại → trả message giống hệt (chống enumeration)
                return new ForgotPasswordResponse { Message = ValidationMessages.FORGOT_PASSWORD_MESSAGE };
            }
            userId = customer.Id;
            userType = UserType.CUSTOMER;
        }

        // Vô hiệu hóa tất cả reset token cũ
        await _uow.PasswordResetTokens.InvalidateAllByUserAsync(userId, userType);

        var role = userType == UserType.USER ? "STAFF" : "CUSTOMER";
        var resetToken = _jwtService.GeneratePasswordResetToken(userId, request.Email, role);
        var tokenEntity = new PasswordResetToken
        {
            Token = resetToken,
            Email = request.Email,
            UserType = userType,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };

        await _uow.PasswordResetTokens.AddAsync(tokenEntity);
        await _uow.SaveChangesAsync();

        return new ForgotPasswordResponse
        {
            Message = ValidationMessages.FORGOT_PASSWORD_MESSAGE,
            ResetToken = resetToken
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/reset-password
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Đặt lại mật khẩu bằng reset token nhận từ forgot-password flow.
    ///
    /// Luồng:
    ///   1. Validate NewPassword == ConfirmPassword.
    ///   2. Tìm PasswordResetToken trong DB → validate chưa hết hạn + chưa dùng.
    ///   3. Hash mật khẩu mới (BCrypt) → cập nhật vào User/Customer.PasswordHash.
    ///   4. Đánh dấu token IsUsed = true (single-use).
    ///   5. Revoke tất cả RefreshToken của user → force re-login trên mọi thiết bị.
    ///      Lý do: nếu mật khẩu cũ bị lộ, attacker có thể đang giữ refresh token hợp lệ.
    ///
    /// Error cases:
    ///   - Password không khớp ConfirmPassword → BusinessRuleViolationException (422).
    ///   - Token không tồn tại / hết hạn / đã dùng → BusinessRuleViolationException (422).
    ///   - User/Customer không tồn tại (bị xóa) → EntityNotFoundException (404).
    /// </summary>
    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new BusinessRuleViolationException(ValidationMessages.PASSWORD_CONFIRM_MISMATCH);

        var tokenEntity = await _uow.PasswordResetTokens.GetByTokenAsync(request.Token)
            ?? throw new BusinessRuleViolationException(ValidationMessages.RESET_TOKEN_INVALID);

        if (tokenEntity.IsUsed)
            throw new BusinessRuleViolationException(ValidationMessages.RESET_TOKEN_INVALID);

        if (tokenEntity.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleViolationException(ValidationMessages.RESET_TOKEN_INVALID);

        var newHash = _passwordHasher.HashPassword(request.NewPassword);

        if (tokenEntity.UserType == UserType.USER)
        {
            var user = await _uow.Users.GetByIdAsync(tokenEntity.UserId)
                ?? throw new EntityNotFoundException("User", tokenEntity.UserId);
            user.PasswordHash = newHash;
        }
        else
        {
            var customer = await _uow.Customers.GetByIdAsync(tokenEntity.UserId)
                ?? throw new EntityNotFoundException("Customer", tokenEntity.UserId);
            customer.PasswordHash = newHash;
        }

        tokenEntity.IsUsed = true;

        // Force re-login: revoke tất cả session cũ
        await _uow.RefreshTokens.RevokeAllByUserAsync(tokenEntity.UserId, tokenEntity.UserType);
        await _uow.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/auth/me
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy thông tin user hiện tại từ JWT claims (userId + role).
    ///
    /// Luồng:
    ///   1. Controller parse JWT → lấy id + role. Với CUSTOMER/STAFF/CHEF/MANAGER, id lấy từ
    ///      claim NameIdentifier (sub). Với GUEST, sub là UUID định danh phiên đăng nhập nên
    ///      Controller phải lấy id từ claim "session_id" thay vì sub.
    ///   2. Nếu role = CUSTOMER → tìm trong bảng Customers.
    ///   3. Nếu role = GUEST → không có DB, trả thẳng id (sessionId) + tên mặc định "Guest".
    ///   4. Ngược lại (STAFF/CHEF/MANAGER) → tìm trong bảng Users.
    ///   5. Trả về UserInfoResponse (id, fullName, email, role, avatarUrl).
    ///
    /// Error cases:
    ///   - User/Customer bị xóa hoặc không tồn tại → EntityNotFoundException (404).
    /// </summary>
    public async Task<UserInfoResponse> GetCurrentUserAsync(int id, string role)
    {
        if (role == UserRole.CUSTOMER.ToString())
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer != null)
            {
                return new UserInfoResponse
                {
                    Id = customer.Id,
                    FullName = customer.FullName ?? "Customer",
                    Email = customer.Email ?? string.Empty,
                    Role = UserRole.CUSTOMER.ToString(),
                    PhoneNumber = customer.Phone,
                    LoyaltyPoints = customer.LoyaltyPoints,
                    MembershipLevel = customer.MembershipLevel.ToString()
                };
            }
        }
        else if (role == UserRole.GUEST.ToString())
        {
            // GUEST không có row trong DB — id truyền vào đây là sessionId (lấy từ claim
            // "session_id" ở Controller, không phải sub/NameIdentifier vốn là UUID) → trả info tối thiểu
            return new UserInfoResponse
            {
                Id = id,
                FullName = "Guest",
                Email = string.Empty,
                Role = UserRole.GUEST.ToString()
            };
        }
        else
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user != null)
            {
                return new UserInfoResponse
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                };
            }
        }

        throw new EntityNotFoundException("User/Customer", id);
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/login-guest
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Đăng nhập cho khách vãng lai — không cần tài khoản, chỉ cần biết bàn nào.
    ///
    /// Luồng:
    ///   1. Tìm bàn theo TableId → 404 nếu không tồn tại.
    ///   2. Kiểm tra bàn đã có DiningSession ACTIVE chưa:
    ///      a. Có → dùng session hiện tại (khách mới join bàn đang ăn).
    ///      b. Không → chuyển bàn OCCUPIED + tạo DiningSession mới.
    ///   3. Tạo JWT với role=GUEST, userId=sessionId (không phải customerId).
    ///      Token GUEST có quyền hạn giới hạn: chỉ xem menu + gọi món tại bàn.
    ///
    /// Error cases:
    ///   - Bàn không tồn tại → EntityNotFoundException (404).
    /// </summary>
    public async Task<GuestLoginResponse> LoginGuestAsync(GuestLoginRequest request)
    {
        var table = await _uow.Tables.GetByIdAsync(request.TableId)
            ?? throw new EntityNotFoundException("Table", request.TableId);

        var existingSession = await _uow.DiningSessions.GetActiveByTableIdAsync(request.TableId);
        DiningSession session;

        var guestName = request.GuestName ?? "Guest";

        if (existingSession != null)
        {
            // Bàn đã có khách → join session hiện tại
            session = existingSession;
        }
        else
        {
            // Bàn trống → mở bàn + tạo session mới
            table.Status = TableStatus.OCCUPIED;

            session = new DiningSession
            {
                TableId = table.Id,
                GuestName = guestName,
                GuestPhone = request.GuestPhone,
                Status = DiningSessionStatus.ACTIVE,
                StartedAt = DateTime.UtcNow
            };

            await _uow.DiningSessions.AddAsync(session);
            await _uow.SaveChangesAsync();
        }

        // Mỗi GUEST nhận một UUID riêng làm identity — tránh nhầm lẫn khi nhiều khách cùng bàn
        var guestUniqueId = Guid.NewGuid().ToString("N");
        var (accessToken, _) = _jwtService.GenerateGuestToken(guestUniqueId, session.Id, guestName);

        // Tạo participant cho GUEST này (UUID là key để phân biệt)
        var isFirstParticipant = !session.Participants.Any(p => p.LeftAt == null);
        await _uow.SessionParticipants.AddAsync(new SessionParticipant
        {
            SessionId = session.Id,
            GuestSessionId = guestUniqueId,
            Role = isFirstParticipant ? ParticipantRole.HOST : ParticipantRole.MEMBER,
            JoinedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();

        return new GuestLoginResponse
        {
            Token = accessToken,
            SessionId = session.Id,
            TableId = table.Id,
            TableNumber = table.TableNumber,
            Role = UserRole.GUEST.ToString()
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/auth/logout
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Đăng xuất — revoke tất cả RefreshToken của user.
    ///
    /// Luồng:
    ///   1. Controller parse JWT → lấy userId + userType (USER/CUSTOMER/GUEST).
    ///   2. Revoke tất cả RefreshToken trong DB (IsRevoked=true).
    ///   3. AccessToken hiện tại vẫn valid cho đến khi hết hạn (stateless JWT).
    ///      Nhưng client sẽ không thể refresh khi token hết hạn → buộc phải login lại.
    /// </summary>
    public async Task<LogoutResponse> LogoutAsync(int userId, UserType userType)
    {
        await _uow.RefreshTokens.RevokeAllByUserAsync(userId, userType);
        await _uow.SaveChangesAsync();

        return new LogoutResponse
        {
            Message = ValidationMessages.LOGOUT_SUCCESS
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Helper
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Helper tạo cặp AccessToken + RefreshToken và lưu RefreshToken vào DB.
    /// Dùng chung cho Login và Register (tránh duplicate code).
    ///
    /// Luồng:
    ///   1. JwtTokenService tạo AccessToken (JWT RSA256) → trả (token, jwtId).
    ///   2. JwtTokenService tạo RefreshToken (random 64 bytes → base64).
    ///   3. Lưu RefreshToken vào DB kèm JwtId (liên kết cặp token), ExpiresAt = 7 ngày.
    ///   4. Trả TokenResponse cho client.
    /// </summary>
    private async Task<TokenResponse> GenerateTokenResponseAsync(int id, string email, string fullName, string role, UserType userType, string? phone = null, int? loyaltyPoints = null, string? membershipLevel = null)
    {
        var (accessToken, jwtId) = _jwtService.GenerateAccessToken(id, email, fullName, role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            JwtId = jwtId,
            UserType = userType,
            UserId = id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity);
        await _uow.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            User = new UserInfoResponse
            {
                Id = id,
                FullName = fullName,
                Email = email,
                Role = role,
                PhoneNumber = phone,
                LoyaltyPoints = loyaltyPoints,
                MembershipLevel = membershipLevel
            }
        };
    }
}