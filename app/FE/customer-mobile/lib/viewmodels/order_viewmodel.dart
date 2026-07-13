import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/order_models.dart';
import '../services/order_repository.dart';
import 'auth_viewmodel.dart';

final orderListProvider = FutureProvider<List<OrderResponse>>((ref) async {
  final authState = ref.watch(authViewModelProvider);
  final repo = ref.watch(orderRepositoryProvider);
  final sessionId = authState.guestSession?.sessionId;
  if (sessionId != null) {
    return repo.getSessionOrders(sessionId);
  }
  return repo.getMyOrders();
});

final orderDetailProvider = FutureProvider.family<OrderResponse, int>((ref, orderId) async {
  final repo = ref.watch(orderRepositoryProvider);
  return repo.getOrderById(orderId);
});

final orderStatusProvider = FutureProvider.family<OrderStatusResponse, int>((ref, orderId) async {
  final repo = ref.watch(orderRepositoryProvider);
  return repo.getOrderStatus(orderId);
});

final activeOrdersProvider = FutureProvider<List<OrderResponse>>((ref) async {
  final repo = ref.watch(orderRepositoryProvider);
  return repo.getActiveOrders();
});

final todayOrdersProvider = FutureProvider<List<OrderResponse>>((ref) async {
  final repo = ref.watch(orderRepositoryProvider);
  return repo.getTodayOrders();
});

class OrderStatusUpdateNotifier extends StateNotifier<AsyncValue<OrderResponse?>> {
  final OrderRepository _repo;

  OrderStatusUpdateNotifier(this._repo) : super(const AsyncValue.data(null));

  Future<void> updateStatus(int orderId, String status) async {
    state = const AsyncValue.loading();
    try {
      final updated = await _repo.updateOrderStatus(orderId, status);
      state = AsyncValue.data(updated);
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }
}

final orderStatusUpdateProvider = StateNotifierProvider<OrderStatusUpdateNotifier, AsyncValue<OrderResponse?>>((ref) {
  return OrderStatusUpdateNotifier(ref.watch(orderRepositoryProvider));
});
