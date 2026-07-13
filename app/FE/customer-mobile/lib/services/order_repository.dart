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

  Future<List<OrderResponse>> getMyOrders({int page = 1, int pageSize = 20}) async {
    final response = await _dio.get('orders/my', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });

    final data = response.data['data'] as List;
    return data.map((e) => OrderResponse.fromJson(e)).toList();
  }

  /// Đơn đã gọi trong phiên ăn hiện tại — dùng cho GUEST (không có quyền gọi orders/my).
  Future<List<OrderResponse>> getSessionOrders(int sessionId, {int tableNumber = 0}) async {
    final response = await _dio.get('dining-sessions/$sessionId/orders');

    final data = response.data['data'] as Map<String, dynamic>;
    final orders = (data['orders'] as List?) ?? [];
    return orders.map((e) => OrderResponse(
      id: e['orderId'] ?? 0,
      tableNumber: tableNumber,
      finalAmount: (e['finalAmount'] ?? 0).toDouble(),
      status: e['orderStatus'] ?? 'PENDING',
    )).toList();
  }
}
