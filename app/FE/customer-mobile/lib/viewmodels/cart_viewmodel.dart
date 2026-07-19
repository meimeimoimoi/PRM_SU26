import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/order_models.dart';
import '../models/menu_models.dart';
import '../services/order_repository.dart';
import 'order_viewmodel.dart';

class CartItem {
  final MenuItemSummary menuItem;
  int quantity;
  String? notes;

  CartItem({
    required this.menuItem,
    this.quantity = 1,
    this.notes,
  });
}

class CartState {
  final List<CartItem> items;
  final bool isSubmitting;
  final String? error;

  CartState({this.items = const [], this.isSubmitting = false, this.error});

  CartState copyWith({List<CartItem>? items, bool? isSubmitting, String? error}) {
    return CartState(
      items: items ?? this.items,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      error: error,
    );
  }

  double get total => items.fold(0, (sum, item) => sum + (item.menuItem.price * item.quantity));
}

final cartViewModelProvider = StateNotifierProvider<CartViewModel, CartState>((ref) {
  return CartViewModel(ref.watch(orderRepositoryProvider), ref);
});

class CartViewModel extends StateNotifier<CartState> {
  final OrderRepository _orderRepository;
  final Ref _ref;

  CartViewModel(this._orderRepository, this._ref) : super(CartState());

  void addItem(MenuItemSummary item, {String? notes}) {
    final existingIndex = state.items.indexWhere((element) => element.menuItem.id == item.id);
    if (existingIndex >= 0) {
      final updatedItems = List<CartItem>.from(state.items);
      updatedItems[existingIndex].quantity += 1;
      state = state.copyWith(items: updatedItems);
    } else {
      state = state.copyWith(items: [...state.items, CartItem(menuItem: item, notes: notes)]);
    }
  }

  void removeItem(int itemId) {
    state = state.copyWith(items: state.items.where((element) => element.menuItem.id != itemId).toList());
  }

  void incrementQuantity(int itemId) {
    final index = state.items.indexWhere((element) => element.menuItem.id == itemId);
    if (index < 0) return;
    final updatedItems = List<CartItem>.from(state.items);
    updatedItems[index].quantity += 1;
    state = state.copyWith(items: updatedItems);
  }

  /// Giảm số lượng 1 món — nếu về 0 thì xóa hẳn khỏi giỏ (giống hành vi UX phổ biến).
  void decrementQuantity(int itemId) {
    final index = state.items.indexWhere((element) => element.menuItem.id == itemId);
    if (index < 0) return;
    if (state.items[index].quantity <= 1) {
      removeItem(itemId);
      return;
    }
    final updatedItems = List<CartItem>.from(state.items);
    updatedItems[index].quantity -= 1;
    state = state.copyWith(items: updatedItems);
  }

  Future<int?> checkout(int tableId, int sessionId, {String? couponCode}) async {
    if (state.items.isEmpty) return null;

    state = state.copyWith(isSubmitting: true, error: null);
    try {
      final request = PlaceOrderRequest(
        tableId: tableId,
        diningSessionId: sessionId,
        couponCode: couponCode,
        items: state.items.map((e) => OrderDetailRequest(
          menuItemId: e.menuItem.id,
          quantity: e.quantity,
          notes: e.notes,
        )).toList(),
      );
      
      final order = await _orderRepository.placeOrder(request);
      state = CartState(); // Clear cart on success
      _ref.invalidate(orderListProvider);
      return order.id;
    } catch (e) {
      state = state.copyWith(isSubmitting: false, error: e.toString());
      return null;
    }
  }
}
