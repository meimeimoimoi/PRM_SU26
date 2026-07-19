import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/auth_models.dart';
import 'api_client.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(dioProvider));
});

class AuthRepository {
  final Dio _dio;

  AuthRepository(this._dio);

  Future<TokenResponse> login(String email, String password) async {
    final response = await _dio.post('auth/login', data: {
      'email': email,
      'password': password,
    });
    return TokenResponse.fromJson(response.data['data']);
  }

  Future<TokenResponse> register(String fullName, String email, String password, String? phoneNumber) async {
    final response = await _dio.post('auth/register', data: {
      'fullName': fullName,
      'email': email,
      'password': password,
      if (phoneNumber != null && phoneNumber.isNotEmpty) 'phoneNumber': phoneNumber,
    });
    return TokenResponse.fromJson(response.data['data']);
  }

  Future<GuestLoginResponse> loginGuest(int tableId, String? guestName, String? guestPhone) async {
    final response = await _dio.post('auth/login-guest', data: {
      'tableId': tableId,
      if (guestName != null && guestName.isNotEmpty) 'guestName': guestName,
      if (guestPhone != null && guestPhone.isNotEmpty) 'guestPhone': guestPhone,
    });
    return GuestLoginResponse.fromJson(response.data['data']);
  }

  Future<void> logout() async {
    await _dio.post('auth/logout');
  }

  /// Trả về resetToken nếu email tồn tại (BE hiện luôn trả trực tiếp trong response,
  /// chưa có hạ tầng gửi email — xem comment ForgotPasswordResponse ở BE).
  Future<String?> forgotPassword(String email) async {
    final response = await _dio.post('auth/forgot-password', data: {'email': email});
    return response.data['data']?['resetToken'];
  }

  Future<void> resetPassword(String token, String newPassword, String confirmPassword) async {
    await _dio.post('auth/reset-password', data: {
      'token': token,
      'newPassword': newPassword,
      'confirmPassword': confirmPassword,
    });
  }

  Future<UserInfo> getCurrentUser() async {
    final response = await _dio.get('auth/me');
    return UserInfo.fromJson(response.data['data']);
  }
}
