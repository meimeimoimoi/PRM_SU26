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

    // ===== Auth Success Messages =====
    public const string LOGIN_SUCCESS = "Đăng nhập thành công";
    public const string REGISTER_SUCCESS = "Đăng ký thành công";
    public const string REFRESH_TOKEN_SUCCESS = "Làm mới token thành công";
    public const string RESET_PASSWORD_SUCCESS = "Đặt lại mật khẩu thành công";
    public const string GUEST_LOGIN_SUCCESS = "Đăng nhập khách thành công";
    public const string LOGOUT_SUCCESS = "Đăng xuất thành công, token đã được thu hồi.";

    // ===== Guest =====
    public const string TABLE_NOT_FOUND = "TABLE_NOT_FOUND";
    public const string TABLE_NOT_AVAILABLE = "TABLE_NOT_AVAILABLE";

    // ===== Table =====
    public const string TABLE_STATUS_INVALID = "Trạng thái bàn không hợp lệ. Các trạng thái hợp lệ: {0}";
    public const string TABLE_MAINTENANCE_CANNOT_SERVE = "Bàn số {0} đang bảo trì, không thể phục vụ.";
    public const string TABLE_RESERVED = "Bàn số {0} đã được đặt trước.";
    public const string TABLE_MAINTENANCE_CANNOT_OCCUPIED = "Bàn số {0} đang bảo trì, không thể chuyển sang OCCUPIED.";
    public const string TABLE_OCCUPIED_CANNOT_CHECKIN = "Bàn số {0} đang có khách, không thể check-in.";
    public const string TABLE_MAINTENANCE_CANNOT_CHECKIN = "Bàn số {0} đang bảo trì, không thể check-in.";

    // ===== Reservation =====
    public const string RESERVATION_NOT_FOUND = "RESERVATION_NOT_FOUND";
    public const string RESERVATION_STATUS_INVALID = "Trạng thái đặt bàn không hợp lệ. Các trạng thái hợp lệ: {0}";
    public const string RESERVATION_PARTY_SIZE_INVALID = "Số lượng khách phải lớn hơn 0.";
    public const string RESERVATION_PARTY_SIZE_EXCEED = "Bàn số {0} chỉ có sức chứa {1} người, không đủ cho {2} khách.";
    public const string RESERVATION_TIME_PAST = "Thời gian đặt bàn phải ở tương lai.";
    public const string RESERVATION_TIME_CONFLICT = "Bàn số {0} đã có lịch đặt trước trong khung giờ này.";
    public const string RESERVATION_GUEST_INFO_REQUIRED = "Phải cung cấp customer_id hoặc guest_name.";
    public const string RESERVATION_TABLE_MAINTENANCE = "Bàn số {0} đang bảo trì, không thể đặt trước.";
    public const string RESERVATION_ALREADY_CHECKED_IN = "Lịch đặt bàn đã check-in, không thể thay đổi trạng thái.";
    public const string RESERVATION_ALREADY_CANCELLED = "Lịch đặt bàn đã bị hủy, không thể thay đổi trạng thái.";
    public const string RESERVATION_ALREADY_NO_SHOW = "Lịch đặt bàn đã đánh dấu NO_SHOW, không thể thay đổi trạng thái.";

    // ===== Scan =====
    public const string SCAN_JOINED_SESSION = "Đã tham gia vào nhóm gọi món của bàn số {0}.";
    public const string SCAN_NEW_SESSION = "Đã tạo phiên ăn mới tại bàn số {0}.";

    // ===== General =====
    public const string NOT_FOUND = "NOT_FOUND";
    public const string CUSTOMER_NOT_FOUND = "CUSTOMER_NOT_FOUND";

    // ===== Menu =====
    public const string MENU_ITEM_NOT_FOUND = "MENU_ITEM_NOT_FOUND";
    public const string NO_MENU_ITEMS = "NO_MENU_ITEMS";

    // ===== DiningSession =====
    public const string DINING_SESSION_NOT_FOUND = "DINING_SESSION_NOT_FOUND";
    public const string DINING_SESSION_NOT_ACTIVE = "Phiên ăn này đã kết thúc, không thể thực hiện thao tác.";
    public const string DINING_SESSION_PARTICIPANT_NOT_FOUND = "Bạn không thuộc phiên ăn này.";
    public const string DINING_SESSION_ALREADY_LEFT = "Bạn đã rời khỏi phiên ăn này trước đó.";
    public const string DINING_SESSION_LEAVE_SUCCESS = "Đã rời khỏi nhóm gọi món tại bàn số {0} thành công.";
    public const string DINING_SESSION_ACCESS_DENIED = "Bạn không có quyền xem thông tin của phiên ăn này.";

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
