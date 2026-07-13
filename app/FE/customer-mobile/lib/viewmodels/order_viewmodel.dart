import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/order_models.dart';
import '../services/order_repository.dart';
import 'auth_viewmodel.dart';

/// autoDispose: refetch mỗi lần mở lại trang, tránh cache đơn cũ sau khi đặt món mới.
/// GUEST không có quyền gọi orders/my → lấy đơn theo phiên ăn hiện tại.
final orderListProvider = FutureProvider.autoDispose<List<OrderResponse>>((ref) async {
  final repo = ref.watch(orderRepositoryProvider);
  final authState = ref.watch(authViewModelProvider);

  final guestSession = authState.guestSession;
  if (authState.status == AuthStateStatus.guest && guestSession != null && guestSession.sessionId > 0) {
    return repo.getSessionOrders(guestSession.sessionId, tableNumber: guestSession.tableNumber);
  }
  return repo.getMyOrders();
});
