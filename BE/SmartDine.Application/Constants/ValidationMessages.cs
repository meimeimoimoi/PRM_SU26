namespace SmartDine.Application.Constants;

public static class ValidationMessages
{
    // ===== Auth =====
    public const string EMAIL_OR_PASSSWORD_INVALID = "EMAIL_OR_PASSWORD_INVALID";
    public const string EMAIL_ALREADY_EXISTS = "EMAIL_ALREADY_EXISTS";
    public const string PHONE_ALREADY_EXISTS = "PHONE_ALREADY_EXISTS";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string ACCOUNT_INACTIVE = "ACCOUNT_INACTIVE";

    // ===== JWT / Token =====
    public const string ACCESS_TOKEN_INVALID = "ACCESS_TOKEN_INVALID";
    public const string REFRESH_TOKEN_NOT_FOUND = "REFRESH_TOKEN_NOT_FOUND";
    public const string REFRESH_TOKEN_EXPIRED = "REFRESH_TOKEN_EXPIRED";
    public const string REFRESH_TOKEN_MISMATCH = "REFRESH_TOKEN_MISMATCH";

    // ===== Password Reset =====
    public const string PASSWORD_CONFIRM_MISMATCH = "PASSWORD_CONFIRM_MISMATCH";
    public const string RESET_TOKEN_INVALID = "RESET_TOKEN_INVALID";
    public const string FORGOT_PASSWORD_MESSAGE = "FORGOT_PASSWORD_EMAIL_SENT";

    // ===== General =====
    public const string NOT_FOUND = "NOT_FOUND";

    // ===== Menu =====
    public const string MENU_ITEM_NOT_FOUND = "MENU_ITEM_NOT_FOUND";
    public const string NO_MENU_ITEMS = "NO_MENU_ITEMS";

    // ===== Order =====
    public const string ORDER_NOT_FOUND = "ORDER_NOT_FOUND";
    public const string ORDER_ITEM_NOT_FOUND = "ORDER_ITEM_NOT_FOUND";
    public const string NO_ORDER_ITEMS = "NO_ORDER_ITEMS";
    public const string NO_ORDERS = "NO_ORDERS";
    public const string ORDER_ITEM_INVALID = "ORDER_ITEM_INVALID";
    public const string ORDER_ITEM_OUT_OF_STOCK = "ORDER_ITEM_OUT_OF_STOCK";
    public const string ORDER_ITEM_DUPLICATE = "ORDER_ITEM_DUPLICATE";
    public const string ORDER_STATUS_INVALID = "ORDER_STATUS_INVALID";
}
