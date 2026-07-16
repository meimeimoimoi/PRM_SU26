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
    public const string TABLE_CAPACITY_INVALID = "Sức chứa bàn phải lớn hơn 0.";
    public const string TABLE_NUMBER_ALREADY_EXISTS = "Số bàn {0} đã tồn tại trong hệ thống.";
    public const string TABLE_CANNOT_DELETE_OCCUPIED = "Bàn số {0} đang có khách, không thể xóa.";
    public const string LOCATION_NOT_FOUND = "LOCATION_NOT_FOUND";
    public const string LOCATION_NAME_REQUIRED = "Tên khu vực không được để trống.";

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
    public const string MENU_ITEM_CREATED_SUCCESS = "Tạo món ăn mới thành công.";
    public const string MENU_ITEM_UPDATED_SUCCESS = "Cập nhật thông tin món ăn thành công.";
    public const string MENU_ITEM_DELETED_SUCCESS = "Món ăn đã được đưa vào danh mục ngừng kinh doanh (Soft Delete).";
    public const string MENU_ITEM_NAME_REQUIRED = "Tên món ăn không được để trống.";
    public const string MENU_ITEM_PRICE_INVALID = "Giá món ăn phải lớn hơn 0.";
    public const string MENU_CATEGORY_NOT_FOUND = "MENU_CATEGORY_NOT_FOUND";
    public const string MENU_ITEM_DUPLICATE_NAME = "Tên món ăn '{0}' đã tồn tại trong danh mục này.";
    public const string MENU_ITEM_PATCH_EMPTY = "Phải cung cấp ít nhất một trường để cập nhật.";
    public const string MENU_ITEM_IMAGE_REQUIRED = "Vui lòng chọn 1 file ảnh để tải lên.";
    public const string MENU_ITEM_IMAGE_INVALID_TYPE = "File tải lên phải là ảnh (JPG, PNG, WEBP...).";
    public const string MENU_ITEM_IMAGE_TOO_LARGE = "Ảnh không được vượt quá 5MB.";
    public const string AI_RECOMMENDATION_NO_CONTEXT = "Không có dữ liệu ngữ cảnh kinh doanh để đề xuất.";

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
    public const string ORDER_SESSION_ACCESS_DENIED = "Bạn không thuộc phiên ăn này, không thể đặt món hoặc xem đơn.";

    // ===== Coupon =====
    public const string COUPON_NOT_FOUND = "Mã giảm giá không tồn tại hoặc đã ngừng áp dụng.";
    public const string COUPON_EXPIRED = "Mã giảm giá đã hết hạn hoặc chưa đến thời gian áp dụng.";
    public const string COUPON_NOT_OWNED = "Bạn không sở hữu mã giảm giá này.";
    public const string COUPON_ALREADY_USED = "Mã giảm giá này đã được sử dụng.";
    public const string COUPON_NOT_SUPPORTED_TYPE = "Loại khuyến mãi này hiện chưa được hỗ trợ áp dụng tự động.";

    // ===== Payment =====
    public const string PAYMENT_SESSION_NOT_FOUND = "PAYMENT_SESSION_NOT_FOUND";
    public const string PAYMENT_SESSION_CLOSED = "Phiên ăn này đã kết thúc, không thể tạo hóa đơn.";
    public const string PAYMENT_SESSION_CHECKOUT_IN_PROGRESS = "Phiên ăn đang trong quá trình thanh toán, vui lòng hoàn tất giao dịch hiện tại.";
    public const string PAYMENT_NO_ORDERS = "Phiên ăn chưa có đơn hàng nào để thanh toán.";
    public const string PAYMENT_ALREADY_PENDING = "Đang có giao dịch đang xử lý cho phiên ăn này. Vui lòng hoàn tất hoặc chờ hết hạn trước khi thử lại.";
    public const string PAYMENT_ALREADY_COMPLETED = "Phiên ăn này đã được thanh toán thành công.";
    public const string PAYMENT_METHOD_INVALID = "Phương thức thanh toán không hợp lệ. Hỗ trợ: VNPAY, MOMO, QR, CASH.";
    public const string PAYMENT_GATEWAY_ERROR = "Không thể kết nối cổng thanh toán. Vui lòng thử lại sau.";
    public const string PAYMENT_WEBHOOK_INVALID_SIGNATURE = "PAYMENT_WEBHOOK_INVALID_SIGNATURE";
    public const string PAYMENT_NOT_FOUND = "PAYMENT_NOT_FOUND";
    public const string PAYMENT_ACCESS_DENIED = "Bạn không có quyền thực hiện thanh toán cho phiên ăn này.";

    // ===== Order — block khi CHECKOUT =====
    public const string ORDER_BLOCKED_CHECKOUT = "Phiên ăn đang trong quá trình thanh toán, không thể đặt thêm món.";

    // ===== Order — item validation =====
    public const string ORDER_ITEM_NOT_IN_MENU = "Một hoặc nhiều món không tồn tại trong menu.";
    public const string ORDER_ITEM_UNAVAILABLE = "Các món sau đang hết: {0}";

    // ===== PayOS config =====
    public const string PAYOS_CONFIG_CLIENTID_MISSING = "PayOS:ClientId chưa được cấu hình.";
    public const string PAYOS_CONFIG_APIKEY_MISSING = "PayOS:ApiKey chưa được cấu hình.";
    public const string PAYOS_CONFIG_CHECKSUMKEY_MISSING = "PayOS:ChecksumKey chưa được cấu hình.";

    // ===== Order — success responses =====
    public const string ORDER_PLACED_SUCCESS = "Đặt món thành công";
    public const string ORDER_STATUS_UPDATED_SUCCESS = "Cập nhật trạng thái thành công";

    // ===== Table — success responses =====
    public const string TABLE_STATUS_UPDATED_SUCCESS = "Cập nhật trạng thái bàn thành công";
    public const string RESERVATION_CREATED_SUCCESS = "Đặt bàn thành công";
    public const string RESERVATION_STATUS_UPDATED_SUCCESS = "Cập nhật trạng thái đặt bàn thành công";
    public const string TABLE_CREATED_SUCCESS = "Tạo bàn mới thành công";
    public const string TABLE_UPDATED_SUCCESS = "Cập nhật thông tin bàn thành công";
    public const string TABLE_DELETED_SUCCESS = "Xóa bàn thành công";
    public const string LOCATION_CREATED_SUCCESS = "Tạo khu vực mới thành công";

    // ===== Staff =====
    public const string STAFF_NOT_FOUND = "STAFF_NOT_FOUND";
    public const string STAFF_ROLE_INVALID = "Vai trò không hợp lệ. Chỉ chấp nhận: STAFF, CHEF, MANAGER.";
    public const string STAFF_PASSWORD_TOO_SHORT = "Mật khẩu phải có ít nhất 6 ký tự.";
    public const string STAFF_FULLNAME_REQUIRED = "Họ tên không được để trống.";
    public const string STAFF_CANNOT_DEACTIVATE_SELF = "Không thể tự vô hiệu hóa tài khoản của chính mình.";
    public const string STAFF_CREATED_SUCCESS = "Tạo tài khoản nhân viên thành công";
    public const string STAFF_UPDATED_SUCCESS = "Cập nhật thông tin nhân viên thành công";
    public const string STAFF_DEACTIVATED_SUCCESS = "Vô hiệu hóa tài khoản nhân viên thành công";

    // ===== Menu Category =====
    public const string CATEGORY_NOT_FOUND = "CATEGORY_NOT_FOUND";
    public const string CATEGORY_NAME_REQUIRED = "Tên danh mục không được để trống.";
    public const string CATEGORY_NAME_ALREADY_EXISTS = "Tên danh mục '{0}' đã tồn tại.";
    public const string CATEGORY_HAS_MENU_ITEMS = "Danh mục này vẫn còn món ăn, không thể xóa.";
    public const string CATEGORY_CREATED_SUCCESS = "Tạo danh mục thành công";
    public const string CATEGORY_UPDATED_SUCCESS = "Cập nhật danh mục thành công";
    public const string CATEGORY_DELETED_SUCCESS = "Xóa danh mục thành công";

    // ===== Payment history =====
    public const string PAYMENT_STATUS_INVALID = "Trạng thái thanh toán không hợp lệ.";

    // ===== Settings =====
    public const string SETTINGS_NAME_REQUIRED = "Tên nhà hàng không được để trống.";
    public const string SETTINGS_TIME_INVALID = "Giờ mở/đóng cửa không hợp lệ. Định dạng: HH:mm.";
    public const string SETTINGS_RATE_INVALID = "Tỷ lệ phải nằm trong khoảng 0 đến 100.";
    public const string SETTINGS_UPDATED_SUCCESS = "Cập nhật cấu hình nhà hàng thành công";

    // ===== Payment — success responses =====
    public const string PAYMENT_INTENT_CREATED = "Hóa đơn đã được tạo. Vui lòng quét mã QR để thanh toán.";

    // ===== AI =====
    public const string AI_PROMPT_EMPTY = "Nội dung câu hỏi không được để trống.";
}