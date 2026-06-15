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
    public const string EMAIL_OR_PASSSWORD_INVALID    = "EMAIL_OR_PASSSWORD_INVALID";
    public const string AUTH_INVALID_CREDENTIALS      = "Invalid email or password.";
    public const string AUTH_EMAIL_ALREADY_EXISTS     = "Email is already in use.";
    public const string AUTH_PHONE_ALREADY_EXISTS     = "Phone number is already in use.";

    // ── Change Password ──────────────────────────────────────────────────────
    public const string AUTH_PASSWORD_CONFIRM_MISMATCH  = "Password confirmation does not match.";
    public const string AUTH_CURRENT_PASSWORD_INCORRECT = "Current password is incorrect.";

    // ── Forgot / Reset Password ──────────────────────────────────────────────
    public const string AUTH_FORGOT_PASSWORD_SENT    = "Password reset instructions have been sent to your email.";
    public const string AUTH_FORGOT_PASSWORD_GENERIC = "If the email exists in our system, you will receive password reset instructions.";
    public const string AUTH_RESET_TOKEN_INVALID     = "Reset token is invalid or has expired.";

    // ── Success Messages ─────────────────────────────────────────────────────
    public const string AUTH_LOGIN_SUCCESS           = "Login successful.";
    public const string AUTH_REGISTER_SUCCESS        = "Registration successful.";
    public const string AUTH_CHANGE_PASSWORD_SUCCESS = "Password changed successfully.";
    public const string AUTH_RESET_PASSWORD_SUCCESS  = "Password reset successfully.";
}
