import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/order_models.dart';
import '../services/order_repository.dart';

final orderListProvider = FutureProvider<List<OrderResponse>>((ref) async {
  final repo = ref.watch(orderRepositoryProvider);
  return repo.getMyOrders();
});
