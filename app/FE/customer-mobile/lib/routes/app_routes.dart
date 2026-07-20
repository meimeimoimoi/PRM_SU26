class AppRoutes {
  static const String login = '/login';
  static const String signup = '/signup';
  /// Đăng nhập thành viên gắn bàn — số bàn nằm trên path, không phụ thuộc query.
  static String signupWithTable(int tableNumber) => '/signup/$tableNumber';
  static const String forgotPassword = '/forgot_password';
  static const String qrScan = '/qr_scan';
  static const String home = '/home';
  static const String cart = '/cart';
  static const String checkout = '/checkout';
  static const String orders = '/orders';
  static const String invoice = '/invoice';
  static const String orderTracking = '/order_tracking';
  static const String profile = '/profile';
  static const String settings = '/settings';
}
