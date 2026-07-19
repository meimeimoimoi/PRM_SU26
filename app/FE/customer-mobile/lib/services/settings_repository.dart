import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

class RestaurantBillingSettings {
  final double taxRate;
  final double serviceChargeRate;

  const RestaurantBillingSettings({
    this.taxRate = 8,
    this.serviceChargeRate = 0,
  });

  factory RestaurantBillingSettings.fromJson(Map<String, dynamic> json) {
    return RestaurantBillingSettings(
      taxRate: (json['taxRate'] ?? 8).toDouble(),
      serviceChargeRate: (json['serviceChargeRate'] ?? 0).toDouble(),
    );
  }
}

final settingsRepositoryProvider = Provider<SettingsRepository>((ref) {
  return SettingsRepository(ref.watch(dioProvider));
});

/// Cache rates cho hóa đơn — Manager đổi settings thì invalidate provider này.
final billingSettingsProvider = FutureProvider<RestaurantBillingSettings>((ref) async {
  return ref.watch(settingsRepositoryProvider).getBillingSettings();
});

class SettingsRepository {
  final Dio _dio;

  SettingsRepository(this._dio);

  Future<RestaurantBillingSettings> getBillingSettings() async {
    final response = await _dio.get('settings');
    final data = response.data['data'] ?? response.data;
    return RestaurantBillingSettings.fromJson(Map<String, dynamic>.from(data as Map));
  }
}
