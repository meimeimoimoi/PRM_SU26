import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/order_models.dart';
import 'api_client.dart';

final orderRepositoryProvider = Provider<OrderRepository>((ref) {
  return OrderRepository(ref.watch(dioProvider));
});

class OrderRepository {
  final Dio _dio;

  OrderRepository(this._dio);

  Future<OrderResponse> placeOrder(PlaceOrderRequest request) async {
    final response = await _dio.post('orders', data: request.toJson());
    return OrderResponse.fromJson(response.data['data']);
  }

  Future<OrderResponse> getOrderById(int orderId) async {
    final response = await _dio.get('orders/$orderId');
    return OrderResponse.fromJson(response.data['data']);
  }

  Future<OrderStatusResponse> getOrderStatus(int orderId) async {
    final response = await _dio.get('orders/$orderId/status');
    return OrderStatusResponse.fromJson(response.data['data']);
  }

  Future<List<OrderResponse>> getMyOrders({int page = 1, int pageSize = 20}) async {
    final response = await _dio.get('orders/session', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    final data = response.data['data'] as List;
    return data.map((e) => OrderResponse.fromJson(e)).toList();
  }

  Future<List<OrderResponse>> getSessionOrders(int sessionId) async {
    final response = await _dio.get('orders/session/$sessionId');
    final data = response.data['data'] as List;
    return data.map((e) => OrderResponse.fromJson(e)).toList();
  }

  Future<List<OrderResponse>> getActiveOrders() async {
    final response = await _dio.get('orders/active');
    final data = response.data['data'] as List;
    return data.map((e) => OrderResponse.fromJson(e)).toList();
  }

  Future<List<OrderResponse>> getTodayOrders() async {
    final response = await _dio.get('orders/today');
    final data = response.data['data'] as List;
    return data.map((e) => OrderResponse.fromJson(e)).toList();
  }

  Future<OrderResponse> updateOrderStatus(int orderId, String status) async {
    final response = await _dio.patch('orders/$orderId/status', data: {
      'status': status,
    });
    return OrderResponse.fromJson(response.data['data']);
  }

  Future<PaymentIntentResponse> createPaymentIntent(int sessionId, String paymentMethod) async {
    final response = await _dio.post('payments/create-intent', data: {
      'sessionId': sessionId,
      'paymentMethod': paymentMethod,
      'splitCount': 1,
    });
    return PaymentIntentResponse.fromJson(response.data['data']);
  }
}
