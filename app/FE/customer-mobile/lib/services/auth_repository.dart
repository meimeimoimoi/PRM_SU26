import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/auth_models.dart';
import 'api_client.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.watch(dioProvider));
});

class AuthRepository {
  final Dio _dio;

  AuthRepository(this._dio);

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

  /// CUSTOMER + bàn:
  /// - Đăng nhập → `POST auth/login` + `tableNumber`
  /// - Đăng ký → `POST auth/login-with-table`
  Future<CustomerDiningLoginResponse> loginWithTable({
    required String email,
    required String password,
    required int tableNumber,
    String? fullName,
    String? phoneNumber,
  }) async {
    if (tableNumber <= 0) {
      throw ArgumentError('tableNumber phải > 0');
    }

    final isRegister = fullName != null && fullName.isNotEmpty;
    final body = <String, dynamic>{
      'email': email,
      'password': password,
      'tableNumber': tableNumber,
    };
    if (isRegister) {
      body['fullName'] = fullName;
      if (phoneNumber != null && phoneNumber.isNotEmpty) {
        body['phoneNumber'] = phoneNumber;
      }
    }

    final path = isRegister ? 'auth/login-with-table' : 'auth/login';
    // Marker để nhận diện request từ app mới trong Chrome Network.
    debugPrint('[Auth] >>> POST $path bodyKeys=${body.keys.toList()} tableNumber=$tableNumber');
    final response = await _dio.post(
      path,
      data: body,
      options: Options(headers: {'X-SmartDine-Client': 'customer-mobile'}),
    );
    return CustomerDiningLoginResponse.fromJson(
      Map<String, dynamic>.from(response.data['data'] as Map),
    );
  }

  /// CUSTOMER join bàn theo số bàn — qua Identity (cùng service vừa login).
  Future<DiningContext> scanTableByNumber(int tableNumber) async {
    final response = await _dio.post('auth/join-table', data: {
      'tableNumber': tableNumber,
    });
    final data = Map<String, dynamic>.from(response.data['data'] as Map);
    final dining = DiningContext.fromJson(data);
    if (dining.tableNumber <= 0 && dining.sessionId > 0) {
      return DiningContext(
        sessionId: dining.sessionId,
        tableId: dining.tableId,
        tableNumber: tableNumber,
      );
    }
    return dining;
  }

  Future<void> logout() async {
    await _dio.post('auth/logout');
  }

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
