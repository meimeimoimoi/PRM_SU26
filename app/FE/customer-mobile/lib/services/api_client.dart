import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../config/constants.dart';

final secureStorageProvider = Provider((ref) => const FlutterSecureStorage());

/// Callback when tokens are invalidated (e.g., refresh fails).
/// AuthViewModel sets this so Dio interceptor can trigger logout.
Function? _onTokenInvalidated;

void setTokenInvalidatedCallback(Function callback) {
  _onTokenInvalidated = callback;
}

final dioProvider = Provider<Dio>((ref) {
  final storage = ref.watch(secureStorageProvider);
  
  final dio = Dio(BaseOptions(
    baseUrl: AppConstants.baseUrl,
    connectTimeout: const Duration(milliseconds: AppConstants.connectionTimeout),
    receiveTimeout: const Duration(milliseconds: AppConstants.receiveTimeout),
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
  ));

  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) async {
      final token = await storage.read(key: 'access_token');
      if (token != null) {
        options.headers['Authorization'] = 'Bearer $token';
      }
      return handler.next(options);
    },
    onResponse: (response, handler) {
      return handler.next(response);
    },
    onError: (DioException e, handler) async {
      if (e.response?.statusCode == 401) {
        final refreshToken = await storage.read(key: 'refresh_token');
        if (refreshToken != null) {
          try {
            final tokenResponse = await Dio().post(
              '${dio.options.baseUrl}auth/refresh-token',
              data: {'refreshToken': refreshToken},
            );
            final newAccessToken = tokenResponse.data['data']['accessToken'];
            final newRefreshToken = tokenResponse.data['data']['refreshToken'];
            
            await storage.write(key: 'access_token', value: newAccessToken);
            await storage.write(key: 'refresh_token', value: newRefreshToken);

            e.requestOptions.headers['Authorization'] = 'Bearer $newAccessToken';
            final response = await dio.fetch(e.requestOptions);
            return handler.resolve(response);
          } catch (_) {
            await storage.delete(key: 'access_token');
            await storage.delete(key: 'refresh_token');
            // Notify auth state to logout
            _onTokenInvalidated?.call();
          }
        } else {
          // No refresh token — force logout
          await storage.delete(key: 'access_token');
          _onTokenInvalidated?.call();
        }
      }
      return handler.next(e);
    },
  ));

  return dio;
});
