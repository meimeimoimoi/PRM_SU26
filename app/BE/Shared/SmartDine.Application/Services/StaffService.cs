using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Staff;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ quản lý tài khoản nhân viên nội bộ (STAFF/CHEF/MANAGER).
///
/// Chịu trách nhiệm:
///   - Danh sách nhân viên có phân trang, lọc theo role/trạng thái.
///   - Tạo tài khoản nhân viên mới (Manager đặt mật khẩu trực tiếp, không có luồng mời qua email).
///   - Cập nhật thông tin/role nhân viên.
///   - Vô hiệu hóa tài khoản (IsActive = false), chặn tự vô hiệu hóa chính mình.
///
/// Dependency: IUnitOfWork (Users, Customers), IPasswordHasher (BCrypt).
/// </summary>
public class StaffService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    // Dashboard chỉ quản lý 2 role: Nhân viên (order/bếp/thanh toán chung 1 tài khoản) và Quản lý.
    private static readonly UserRole[] AssignableRoles = { UserRole.STAFF, UserRole.MANAGER };

    public StaffService(IUnitOfWork uow, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<(List<StaffResponse> Items, int TotalCount, int TotalPages)> GetAllAsync(
        string? role, bool? isActive, int page, int pageSize)
    {
        if (role != null && !Enum.TryParse<UserRole>(role, true, out _))
            throw new BusinessRuleViolationException(ValidationMessages.STAFF_ROLE_INVALID);

        var (items, totalCount) = await _uow.Users.GetPagedFilteredAsync(role, isActive, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return (items.Select(MapToResponse).ToList(), totalCount, totalPages);
    }

    public async Task<StaffResponse> GetByIdAsync(int id)
    {
        var user = await _uow.Users.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("Staff", id);

        return MapToResponse(user);
    }

    public async Task<StaffResponse> CreateAsync(CreateStaffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BusinessRuleViolationException(ValidationMessages.STAFF_FULLNAME_REQUIRED);

        if (request.Password.Length < 6)
            throw new BusinessRuleViolationException(ValidationMessages.STAFF_PASSWORD_TOO_SHORT);

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role) || !AssignableRoles.Contains(role))
            throw new BusinessRuleViolationException(ValidationMessages.STAFF_ROLE_INVALID);

        if (await _uow.Users.ExistsAsync(request.Email) || await _uow.Customers.GetByEmailAsync(request.Email) != null)
            throw new BusinessRuleViolationException(ValidationMessages.EMAIL_ALREADY_EXISTS);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = role,
            IsActive = true
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task<StaffResponse> UpdateAsync(int id, UpdateStaffRequest request)
    {
        var user = await _uow.Users.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("Staff", id);

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            if (await _uow.Users.ExistsAsync(request.Email) || await _uow.Customers.GetByEmailAsync(request.Email) != null)
                throw new BusinessRuleViolationException(ValidationMessages.EMAIL_ALREADY_EXISTS);
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role) || !AssignableRoles.Contains(role))
                throw new BusinessRuleViolationException(ValidationMessages.STAFF_ROLE_INVALID);
            user.Role = role;
        }

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        await _uow.SaveChangesAsync();

        return MapToResponse(user);
    }

    public async Task DeactivateAsync(int id, int callerId)
    {
        if (id == callerId)
            throw new BusinessRuleViolationException(ValidationMessages.STAFF_CANNOT_DEACTIVATE_SELF);

        var user = await _uow.Users.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("Staff", id);

        user.IsActive = false;
        await _uow.SaveChangesAsync();
    }

    private static StaffResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
