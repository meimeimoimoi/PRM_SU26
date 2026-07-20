import 'package:dio/dio.dart';

/// Rút thông điệp lỗi thật từ backend (`{success:false, errors:[...]}`) thay vì
/// hiện nguyên `DioException.toString()` (dòng "This exception was thrown
/// because the response has a status code of ..." khó hiểu với người dùng).
String extractErrorMessage(Object error) {
  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map && data['errors'] is List && (data['errors'] as List).isNotEmpty) {
      final raw = (data['errors'] as List).map((e) => e.toString()).join(', ');
      return _localizeAuthError(raw);
    }
    if (data is Map && data['message'] is String) {
      return _localizeAuthError(data['message'] as String);
    }
    if (error.response?.statusCode == 422) {
      return 'Email hoặc mật khẩu không đúng';
    }
  }
  return error.toString();
}

String _localizeAuthError(String code) {
  switch (code) {
    case 'EMAIL_OR_PASSWORD_INVALID':
      return 'Email hoặc mật khẩu không đúng';
    case 'EMAIL_ALREADY_EXISTS':
      return 'Email này đã được đăng ký';
    case 'PHONE_ALREADY_EXISTS':
      return 'Số điện thoại này đã được đăng ký';
    case 'TABLE_NUMBER_REQUIRED':
      return 'Vui lòng chọn số bàn trước khi đăng nhập';
    case 'CUSTOMER_ACCOUNT_REQUIRED':
      return 'Tài khoản này không dùng được trên app khách';
    default:
      if (code.contains('Table') || code.contains('TABLE') || code.contains('not found') || code.contains('NotFound')) {
        return 'Không tìm thấy bàn. Hãy nhập đúng số bàn (VD: 1–5).';
      }
      return code;
  }
}
