import 'package:dio/dio.dart';

/// Rút thông điệp lỗi thật từ backend (`{success:false, errors:[...]}`) thay vì
/// hiện nguyên `DioException.toString()` (dòng "This exception was thrown
/// because the response has a status code of ..." khó hiểu với người dùng).
String extractErrorMessage(Object error) {
  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map && data['errors'] is List && (data['errors'] as List).isNotEmpty) {
      return (data['errors'] as List).join(', ');
    }
    if (data is Map && data['message'] is String) {
      return data['message'] as String;
    }
  }
  return error.toString();
}
