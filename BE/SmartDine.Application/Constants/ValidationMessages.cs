namespace SmartDine.Application.Constants;

public static class ValidationMessages
{
    // ── Common ───────────────────────────────────────────────────────────────
    public const string NOT_FOUND = "NOT_FOUND";

    // ── Order ────────────────────────────────────────────────────────────────
    public const string MENU_ITEM_NOT_FOUND = "MENU_ITEM_NOT_FOUND";
    public const string ORDER_ITEM_NOT_FOUND = "ORDER_ITEM_NOT_FOUND";
    public const string ORDER_NOT_FOUND = "ORDER_NOT_FOUND";

    public const string NO_MENU_ITEMS = "NO_MENU_ITEMS";
    public const string NO_ORDER_ITEMS = "NO_ORDER_ITEMS";
    public const string NO_ORDERS = "NO_ORDERS";

    public const string ORDER_ITEM_INVALID = "ORDER_ITEM_INVALID";
    public const string ORDER_ITEM_OUT_OF_STOCK = "ORDER_ITEM_OUT_OF_STOCK";
    public const string ORDER_ITEM_DUPLICATE = "ORDER_ITEM_DUPLICATE";

    public const string ORDER_STATUS_INVALID = "ORDER_STATUS_INVALID";
    public const string ORDER_STATUS_OUT_OF_STOCK = "ORDER_STATUS_OUT_OF_STOCK";
    public const string ORDER_STATUS_DUPLICATE = "ORDER_STATUS_DUPLICATE";

    public const string ORDER_TOTAL_AMOUNT_INVALID = "ORDER_TOTAL_AMOUNT_INVALID";
    public const string ORDER_TOTAL_AMOUNT_OUT_OF_STOCK = "ORDER_TOTAL_AMOUNT_OUT_OF_STOCK";
    public const string ORDER_TOTAL_AMOUNT_DUPLICATE = "ORDER_TOTAL_AMOUNT_DUPLICATE";

    public const string ORDER_CREATED_AT_INVALID = "ORDER_CREATED_AT_INVALID";
    public const string ORDER_CREATED_AT_OUT_OF_STOCK = "ORDER_CREATED_AT_OUT_OF_STOCK";
    public const string ORDER_CREATED_AT_DUPLICATE = "ORDER_CREATED_AT_DUPLICATE";

    // ── Authentication ───────────────────────────────────────────────────────
    public const string EMAIL_OR_PASSSWORD_INVALID   = "EMAIL_OR_PASSSWORD_INVALID";
    public const string AUTH_INVALID_CREDENTIALS     = "Email hoặc mật khẩu không đúng.";
    public const string AUTH_EMAIL_ALREADY_EXISTS    = "Email đã được sử dụng.";
    public const string AUTH_PHONE_ALREADY_EXISTS    = "Số điện thoại đã được sử dụng.";

    // ── Change Password ──────────────────────────────────────────────────────
    public const string AUTH_PASSWORD_CONFIRM_MISMATCH  = "Mật khẩu mới xác nhận không khớp.";
    public const string AUTH_CURRENT_PASSWORD_INCORRECT = "Mật khẩu hiện tại không đúng.";

    // ── Forgot / Reset Password ──────────────────────────────────────────────
    public const string AUTH_FORGOT_PASSWORD_SENT    = "Hướng dẫn đặt lại mật khẩu đã được gửi đến email của bạn.";
    public const string AUTH_FORGOT_PASSWORD_GENERIC = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
    public const string AUTH_RESET_TOKEN_INVALID     = "Token không hợp lệ hoặc đã hết hạn.";

    // ── Success Messages ─────────────────────────────────────────────────────
    public const string AUTH_LOGIN_SUCCESS           = "Đăng nhập thành công";
    public const string AUTH_REGISTER_SUCCESS        = "Đăng ký thành công";
    public const string AUTH_CHANGE_PASSWORD_SUCCESS = "Đổi mật khẩu thành công.";
    public const string AUTH_RESET_PASSWORD_SUCCESS  = "Đặt lại mật khẩu thành công.";
}
