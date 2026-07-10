import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../config/constants.dart';

final secureStorageProvider = Provider((ref) => const FlutterSecureStorage());

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
      // Add token if exists
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
      // Handle global errors, e.g., token expiration
      if (e.response?.statusCode == 401) {
        final refreshToken = await storage.read(key: 'refresh_token');
        if (refreshToken != null) {
          try {
            // Fetch a new access token
            final tokenResponse = await Dio().post(
              '${dio.options.baseUrl}/auth/refresh-token',
              data: {'refreshToken': refreshToken},
            );
            final newAccessToken = tokenResponse.data['data']['accessToken'];
            final newRefreshToken = tokenResponse.data['data']['refreshToken'];
            
            await storage.write(key: 'access_token', value: newAccessToken);
            await storage.write(key: 'refresh_token', value: newRefreshToken);

            // Clone and resubmit the failed request
            e.requestOptions.headers['Authorization'] = 'Bearer $newAccessToken';
            final response = await dio.fetch(e.requestOptions);
            return handler.resolve(response);
          } catch (_) {
            await storage.delete(key: 'access_token');
            await storage.delete(key: 'refresh_token');
            // TODO: Send logout event or redirect to login
          }
        }
      }
      return handler.next(e);
    },
  ));

  return dio;
});
