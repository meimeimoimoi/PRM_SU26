import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/menu_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../viewmodels/cart_viewmodel.dart';
import '../../services/order_repository.dart';

bool _isStaffRole(String? role) {
  return role == 'MANAGER' || role == 'STAFF' || role == 'CHEF';
}

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainer = Color(0xFFf0eded);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color outline = Color(0xFF8f7068);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color secondary = Color(0xFF685b5a);
  static const Color tertiary = Color(0xFF005cac);
  static const Color error = Color(0xFFba1a1a);
  static const Color onPrimary = Color(0xFFffffff);
}

class MenuItemModel {
  final String title;
  final String imageUrl;
  final double rating;
  final String price;

  MenuItemModel({
    required this.title,
    required this.imageUrl,
    required this.rating,
    required this.price,
  });
}


class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  int _selectedCategoryIndex = 0;
  Timer? _pollingTimer;
  int _selectedPaymentMethod = 2;
  bool _isProcessingPayment = false;
  final Set<int> _closedSessionIds = {};

  @override
  void initState() {
    super.initState();
    _startPolling();
  }

  void _startPolling() {
    _pollingTimer?.cancel();
    _pollingTimer = Timer.periodic(const Duration(seconds: 5), (timer) {
      if (mounted) {
        ref.invalidate(todayOrdersProvider);
      }
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  void _showEndSessionSheet(int sessionId, double totalAmount) {
    _selectedPaymentMethod = 2;
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            return Container(
              decoration: BoxDecoration(
                color: _AppColors.background,
                borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    margin: EdgeInsets.only(top: 12.h),
                    width: 40.w,
                    height: 4.h,
                    decoration: BoxDecoration(
                      color: _AppColors.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(2.r),
                    ),
                  ),
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 16.h),
                    child: Text(
                      'Kết thúc phiên',
                      style: TextStyle(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.bold,
                        color: _AppColors.primary,
                      ),
                    ),
                  ),
                  Container(
                    margin: EdgeInsets.symmetric(horizontal: 24.w),
                    padding: EdgeInsets.all(16.r),
                    decoration: BoxDecoration(
                      color: _AppColors.surfaceContainerLowest,
                      borderRadius: BorderRadius.circular(12.r),
                      border: Border.all(color: _AppColors.outlineVariant.withOpacity(0.3)),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('Tổng cộng', style: TextStyle(fontSize: 16.sp, color: _AppColors.onSurfaceVariant)),
                        Text(
                          '${totalAmount.toStringAsFixed(0)}đ',
                          style: TextStyle(fontSize: 20.sp, fontWeight: FontWeight.bold, color: _AppColors.primary),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(height: 16.h),
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 24.w),
                    child: Column(
                      children: [
                        _buildPaymentOption(ctx, setSheetState, 0, 'Ví Điện Tử (Momo)', Icons.account_balance_wallet),
                        SizedBox(height: 10.h),
                        _buildPaymentOption(ctx, setSheetState, 1, 'Thẻ Ngân Hàng / VietQR', Icons.qr_code_2),
                        SizedBox(height: 10.h),
                        _buildPaymentOption(ctx, setSheetState, 2, 'Tiền Mặt tại quầy', Icons.payments),
                      ],
                    ),
                  ),
                  SizedBox(height: 20.h),
                  Padding(
                    padding: EdgeInsets.fromLTRB(24.w, 0, 24.w, 32.h),
                    child: SizedBox(
                      width: double.infinity,
                      height: 52.h,
                      child: ElevatedButton(
                        onPressed: _isProcessingPayment
                            ? null
                            : () async {
                                setSheetState(() => _isProcessingPayment = true);
                                String method;
                                switch (_selectedPaymentMethod) {
                                  case 0: method = 'MOMO'; break;
                                  case 2: method = 'CASH'; break;
                                  default: method = 'VNPAY'; break;
                                }
                                try {
                                  final repo = ref.read(orderRepositoryProvider);
                                  final response = await repo.createPaymentIntent(sessionId, method);
                                  if (!mounted) return;
                                  Navigator.of(ctx).pop();
                                  showDialog(
                                    context: context,
                                    barrierDismissible: false,
                                    builder: (_) => AlertDialog(
                                      title: Text(method == 'CASH' ? 'Thanh toán tiền mặt' : 'Tạo liên kết thanh toán'),
                                      content: Column(
                                        mainAxisSize: MainAxisSize.min,
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text('Mã hóa đơn: ${response.invoiceId}'),
                                          SizedBox(height: 8.h),
                                          Text('Số tiền: ${response.totalPayable.toStringAsFixed(0)}đ'),
                                          SizedBox(height: 12.h),
                                          if (method == 'CASH')
                                            const Text('Vui lòng di chuyển đến quầy thu ngân để thanh toán.')
                                          else
                                            const Text('Khách hàng quét mã QR để thanh toán.'),
                                        ],
                                      ),
                                      actions: [
                                        TextButton(
                                          onPressed: () {
                                            Navigator.of(context).pop();
                                            setState(() => _closedSessionIds.add(sessionId));
                                            ref.invalidate(todayOrdersProvider);
                                          },
                                          child: const Text('Đóng'),
                                        ),
                                      ],
                                    ),
                                  );
                                } catch (e) {
                                  if (mounted) {
                                    Navigator.of(ctx).pop();
                                    final msg = e.toString().contains('đang chờ') || e.toString().contains('ALREADY')
                                        ? 'Bàn này đã có thanh toán đang chờ xử lý.'
                                        : 'Lỗi: $e';
                                    ScaffoldMessenger.of(context).showSnackBar(
                                      SnackBar(content: Text(msg)),
                                    );
                                  }
                                } finally {
                                  if (mounted) setSheetState(() => _isProcessingPayment = false);
                                }
                              },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: _AppColors.primary,
                          foregroundColor: Colors.white,
                          disabledBackgroundColor: _AppColors.surfaceContainerHighest,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                        ),
                        child: _isProcessingPayment
                            ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                            : Text('Xác nhận kết thúc', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold)),
                      ),
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildPaymentOption(BuildContext ctx, StateSetter setSheetState, int index, String title, IconData icon) {
    final isSelected = _selectedPaymentMethod == index;
    return GestureDetector(
      onTap: () => setSheetState(() => _selectedPaymentMethod = index),
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
        decoration: BoxDecoration(
          color: isSelected ? _AppColors.primary.withOpacity(0.08) : _AppColors.surfaceContainerLowest,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(
            color: isSelected ? _AppColors.primary : _AppColors.outlineVariant.withOpacity(0.3),
            width: isSelected ? 1.5 : 1,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 36.w,
              height: 36.w,
              decoration: BoxDecoration(
                color: isSelected ? _AppColors.primary.withOpacity(0.12) : _AppColors.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(8.r),
              ),
              child: Icon(icon, color: isSelected ? _AppColors.primary : _AppColors.onSurfaceVariant, size: 20.sp),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Text(title, style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600, color: _AppColors.onSurface)),
            ),
            Icon(
              isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
              color: isSelected ? _AppColors.primary : _AppColors.surfaceContainerHighest,
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final userRole = authState.user?.role;
    final isStaff = _isStaffRole(userRole);

    if (isStaff) {
      return _buildStaffView(context, ref, authState);
    }
    return _buildCustomerView(context, ref, authState);
  }

  Widget _buildStaffView(BuildContext context, WidgetRef ref, AuthState authState) {
    final staffName = authState.user?.fullName ?? 'Staff';
    final userRole = authState.user?.role;
    final todayOrdersAsync = ref.watch(todayOrdersProvider);

    return Scaffold(
      backgroundColor: _AppColors.background,
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            backgroundColor: _AppColors.surface,
            pinned: true,
            elevation: 0,
            scrolledUnderElevation: 2,
            surfaceTintColor: Colors.transparent,
            toolbarHeight: 60.h,
            title: Row(
              children: [
                Icon(Icons.restaurant, color: _AppColors.primary, size: 24.sp),
                SizedBox(width: 8.w),
                Text(
                  'SmartDine',
                  style: TextStyle(
                    color: _AppColors.primary,
                    fontSize: 20.sp,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            actions: [
              Container(
                margin: EdgeInsets.only(right: 20.w),
                padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
                decoration: BoxDecoration(
                  color: _AppColors.primaryContainer.withOpacity(0.1),
                  border: Border.all(color: _AppColors.primaryContainer.withOpacity(0.2)),
                  borderRadius: BorderRadius.circular(100.r),
                ),
                child: Row(
                  children: [
                    Icon(Icons.badge, color: _AppColors.primary, size: 16.sp),
                    SizedBox(width: 4.w),
                    Text(
                      staffName,
                      style: TextStyle(
                        color: _AppColors.primary,
                        fontSize: 12.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w, 8.h),
              child: Text(
                'ĐƠN HÀNG HÔM NAY',
                style: TextStyle(
                  color: _AppColors.onSurfaceVariant,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.2,
                ),
              ),
            ),
          ),
          todayOrdersAsync.when(
            data: (orders) {
              if (orders.isEmpty) {
                return SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.symmetric(vertical: 60.h),
                    child: Center(
                      child: Column(
                        children: [
                          Icon(Icons.check_circle_outline, size: 64.sp, color: _AppColors.tertiary),
                          SizedBox(height: 16.h),
                          Text(
                            'Không có đơn nào đang xử lý',
                            style: TextStyle(
                              color: _AppColors.onSurfaceVariant,
                              fontSize: 16.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              }

              // Nhóm đơn theo từng bàn/phiên ăn — mỗi phiên có nút "Kết thúc phiên" riêng.
              final Map<int, List<dynamic>> grouped = {};
              for (final order in orders) {
                grouped.putIfAbsent(order.sessionId, () => []).add(order);
              }
              final sessionIds = grouped.keys.toList();

              return SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    final sessionId = sessionIds[index];
                    final groupOrders = grouped[sessionId]!;
                    final tableNumber = groupOrders.first.tableNumber;
                    final totalAmount = groupOrders.fold<double>(0, (sum, o) => sum + o.finalAmount);
                    final allCompleted = groupOrders.every((o) => o.status == 'COMPLETED');
                    final canEndSession = allCompleted &&
                        userRole != 'CHEF' &&
                        !_closedSessionIds.contains(sessionId);

                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Padding(
                          padding: EdgeInsets.fromLTRB(20.w, 12.h, 20.w, 4.h),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                'Bàn $tableNumber',
                                style: TextStyle(
                                  color: _AppColors.onSurface,
                                  fontSize: 14.sp,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              if (canEndSession)
                                ElevatedButton.icon(
                                  onPressed: () => _showEndSessionSheet(sessionId, totalAmount),
                                  icon: const Icon(Icons.stop_circle_outlined, size: 16),
                                  label: Text('Kết thúc phiên', style: TextStyle(fontSize: 11.sp, fontWeight: FontWeight.bold)),
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: _AppColors.error,
                                    foregroundColor: Colors.white,
                                    padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8.r)),
                                  ),
                                ),
                            ],
                          ),
                        ),
                        ...groupOrders.map((order) => _buildStaffOrderCard(context, ref, order)),
                      ],
                    );
                  },
                  childCount: sessionIds.length,
                ),
              );
            },
            loading: () => const SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.all(40.0),
                child: Center(child: CircularProgressIndicator()),
              ),
            ),
            error: (error, stack) => SliverToBoxAdapter(
              child: Padding(
                padding: EdgeInsets.all(40.0),
                child: Center(child: Text('Lỗi: $error')),
              ),
            ),
          ),
        ],
      ),
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          color: _AppColors.surface,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.04),
              blurRadius: 20,
              offset: const Offset(0, -4),
            ),
          ],
          borderRadius: BorderRadius.vertical(top: Radius.circular(16.r)),
        ),
        child: SafeArea(
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 8.h),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(Icons.dashboard, 'Đơn hàng', true, () {}),
                _buildNavItem(Icons.receipt_long, 'Lịch sử', false, () => context.push('/orders')),
                _buildNavItem(Icons.person, 'Tài khoản', false, () => context.push('/profile')),
                _buildNavItem(Icons.settings, 'Cài đặt', false, () => context.push('/settings')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildStaffOrderCard(BuildContext context, WidgetRef ref, dynamic order) {
    final statusColor = _getStatusColor(order.status);
    final statusBg = _getStatusBgColor(order.status);
    final statusLabel = _getStatusLabel(order.status);
    final nextStatus = _getNextStatus(order.status);

    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 6.h),
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: _AppColors.surfaceContainerLowest,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: _AppColors.outlineVariant.withOpacity(0.3)),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.04),
              blurRadius: 10,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '#SD-${order.id}',
                      style: TextStyle(
                        color: _AppColors.onSurface,
                        fontSize: 16.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      'Bàn ${order.tableNumber}',
                      style: TextStyle(
                        color: _AppColors.onSurfaceVariant,
                        fontSize: 12.sp,
                      ),
                    ),
                  ],
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                  decoration: BoxDecoration(
                    color: statusBg,
                    borderRadius: BorderRadius.circular(100.r),
                  ),
                  child: Text(
                    statusLabel,
                    style: TextStyle(
                      color: statusColor,
                      fontSize: 10.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            if (order.items.isNotEmpty) ...[
              SizedBox(height: 12.h),
              ...order.items.take(3).map((item) => Padding(
                padding: EdgeInsets.only(bottom: 4.h),
                child: Row(
                  children: [
                    Icon(Icons.circle, size: 6.sp, color: _AppColors.outlineVariant),
                    SizedBox(width: 8.w),
                    Expanded(
                      child: Text(
                        '${item.name} x${item.quantity}',
                        style: TextStyle(
                          color: _AppColors.onSurfaceVariant,
                          fontSize: 13.sp,
                        ),
                      ),
                    ),
                  ],
                ),
              )),
              if (order.items.length > 3)
                Text(
                  '+${order.items.length - 3} món khác',
                  style: TextStyle(
                    color: _AppColors.primary,
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
            ],
            SizedBox(height: 12.h),
            Row(
              children: [
                if (nextStatus != null)
                  Expanded(
                    child: ElevatedButton(
                      onPressed: () async {
                        await ref.read(orderStatusUpdateProvider.notifier).updateStatus(order.id, nextStatus);
                        ref.invalidate(todayOrdersProvider);
                        if (context.mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(content: Text('Đã cập nhật: ${_getStatusLabel(nextStatus)}')),
                          );
                        }
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: _AppColors.primary,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8.r)),
                        padding: EdgeInsets.symmetric(vertical: 10.h),
                      ),
                      child: Text(
                        nextStatus == 'COMPLETED' ? 'Hoàn thành' : 'Chuyển: ${_getStatusLabel(nextStatus)}',
                        style: TextStyle(fontSize: 12.sp, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),
                if (nextStatus != null) SizedBox(width: 8.w),
                Expanded(
                  child: OutlinedButton(
                    onPressed: () => context.push('/order_tracking/${order.id}'),
                    style: OutlinedButton.styleFrom(
                      side: BorderSide(color: _AppColors.primary, width: 1.5),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8.r)),
                      padding: EdgeInsets.symmetric(vertical: 10.h),
                    ),
                    child: Text(
                      'Chi tiết',
                      style: TextStyle(
                        color: _AppColors.primary,
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  String? _getNextStatus(String current) {
    switch (current) {
      case 'PENDING': return 'CONFIRMED';
      case 'CONFIRMED': return 'COOKING';
      case 'COOKING': return 'READY';
      case 'READY': return 'COMPLETED';
      default: return null;
    }
  }

  String _getStatusLabel(String status) {
    switch (status) {
      case 'PENDING': return 'Chờ xác nhận';
      case 'CONFIRMED': return 'Đã xác nhận';
      case 'COOKING': return 'Đang nấu';
      case 'READY': return 'Sẵn sàng';
      case 'COMPLETED': return 'Hoàn thành';
      case 'CANCELLED': return 'Đã hủy';
      default: return status;
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'PENDING': return const Color(0xFFE65100);
      case 'CONFIRMED': return const Color(0xFF1565C0);
      case 'COOKING': return const Color(0xFF6A1B9A);
      case 'READY': return const Color(0xFF2E7D32);
      case 'COMPLETED': return _AppColors.secondary;
      case 'CANCELLED': return _AppColors.error;
      default: return _AppColors.onSurfaceVariant;
    }
  }

  Color _getStatusBgColor(String status) {
    switch (status) {
      case 'PENDING': return const Color(0xFFE65100).withOpacity(0.1);
      case 'CONFIRMED': return const Color(0xFF1565C0).withOpacity(0.1);
      case 'COOKING': return const Color(0xFF6A1B9A).withOpacity(0.1);
      case 'READY': return const Color(0xFF2E7D32).withOpacity(0.1);
      case 'COMPLETED': return _AppColors.secondary.withOpacity(0.1);
      case 'CANCELLED': return _AppColors.error.withOpacity(0.1);
      default: return _AppColors.surfaceContainerHighest;
    }
  }

  Widget _buildCustomerView(BuildContext context, WidgetRef ref, AuthState authState) {
    final tableNumber = authState.guestSession?.tableNumber ?? 1;

    final cartState = ref.watch(cartViewModelProvider);
    final cartItemCount = cartState.items.fold<int>(0, (sum, item) => sum + item.quantity);
    final cartTotal = cartState.total;

    final categoriesAsync = ref.watch(menuCategoriesProvider);

    return Scaffold(
      backgroundColor: _AppColors.background,
      body: Stack(
        children: [
          CustomScrollView(
            slivers: [
              // App Bar & Search
              SliverAppBar(
                backgroundColor: _AppColors.surface,
                pinned: true,
                elevation: 0,
                scrolledUnderElevation: 2,
                surfaceTintColor: Colors.transparent,
                toolbarHeight: 60.h,
                title: Row(
                  children: [
                    Icon(Icons.restaurant, color: _AppColors.primary, size: 24.sp),
                    SizedBox(width: 8.w),
                    Text(
                      'SmartDine',
                      style: TextStyle(
                        color: _AppColors.primary,
                        fontSize: 20.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
                actions: [
                  Container(
                    margin: EdgeInsets.only(right: 20.w),
                    padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
                    decoration: BoxDecoration(
                      color: _AppColors.primaryContainer.withOpacity(0.1),
                      border: Border.all(color: _AppColors.primaryContainer.withOpacity(0.2)),
                      borderRadius: BorderRadius.circular(100.r),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.table_restaurant, color: _AppColors.primary, size: 16.sp),
                        SizedBox(width: 4.w),
                        Text(
                          'Bàn $tableNumber',
                          style: TextStyle(
                            color: _AppColors.primary,
                            fontSize: 12.sp,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                bottom: PreferredSize(
                  preferredSize: Size.fromHeight(64.h),
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(20.w, 0, 20.w, 16.h),
                    child: Container(
                      height: 48.h,
                      decoration: BoxDecoration(
                        color: _AppColors.surfaceContainer,
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      padding: EdgeInsets.symmetric(horizontal: 16.w),
                      child: Row(
                        children: [
                          Icon(Icons.search, color: _AppColors.onSurfaceVariant, size: 20.sp),
                          SizedBox(width: 12.w),
                          Expanded(
                            child: TextField(
                              style: TextStyle(
                                fontSize: 16.sp,
                                color: _AppColors.onSurface,
                              ),
                              decoration: InputDecoration(
                                hintText: 'Tìm món ăn...',
                                hintStyle: TextStyle(
                                  color: _AppColors.outline,
                                  fontSize: 16.sp,
                                ),
                                border: InputBorder.none,
                                isDense: true,
                              ),
                            ),
                          ),
                          Icon(Icons.tune, color: _AppColors.onSurfaceVariant, size: 20.sp),
                        ],
                      ),
                    ),
                  ),
                ),
              ),

              SliverToBoxAdapter(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Categories List
                    SizedBox(height: 16.h),
                    categoriesAsync.when(
                      data: (categories) {
                        final allCategories = ['Tất cả', ...categories.map((c) => c.name)];
                        return SizedBox(
                          height: 40.h,
                          child: ListView.separated(
                            padding: EdgeInsets.symmetric(horizontal: 20.w),
                            scrollDirection: Axis.horizontal,
                            itemCount: allCategories.length,
                            separatorBuilder: (context, index) => SizedBox(width: 12.w),
                            itemBuilder: (context, index) {
                              final isSelected = _selectedCategoryIndex == index;
                              return GestureDetector(
                                onTap: () {
                                  setState(() {
                                    _selectedCategoryIndex = index;
                                  });
                                },
                                child: Container(
                                  alignment: Alignment.center,
                                  padding: EdgeInsets.symmetric(horizontal: 24.w),
                                  decoration: BoxDecoration(
                                    color: isSelected ? _AppColors.primary : _AppColors.secondaryContainer,
                                    borderRadius: BorderRadius.circular(100.r),
                                  ),
                                  child: Text(
                                    allCategories[index],
                                    style: TextStyle(
                                      color: isSelected ? Colors.white : _AppColors.primary,
                                      fontSize: 12.sp,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ),
                              );
                            },
                          ),
                        );
                      },
                      loading: () => SizedBox(
                        height: 40.h,
                        child: ListView.separated(
                          padding: EdgeInsets.symmetric(horizontal: 20.w),
                          scrollDirection: Axis.horizontal,
                          itemCount: 5,
                          separatorBuilder: (context, index) => SizedBox(width: 12.w),
                          itemBuilder: (context, index) {
                            return Container(
                              width: 80.w,
                              height: 40.h,
                              decoration: BoxDecoration(
                                color: _AppColors.surfaceContainer,
                                borderRadius: BorderRadius.circular(100.r),
                              ),
                            );
                          },
                        ),
                      ),
                      error: (_, __) => SizedBox(
                        height: 40.h,
                        child: ListView(
                          padding: EdgeInsets.symmetric(horizontal: 20.w),
                          scrollDirection: Axis.horizontal,
                          children: [
                            Container(
                              alignment: Alignment.center,
                              padding: EdgeInsets.symmetric(horizontal: 24.w),
                              decoration: BoxDecoration(
                                color: _AppColors.primary,
                                borderRadius: BorderRadius.circular(100.r),
                              ),
                              child: Text(
                                'Tất cả',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 12.sp,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),

                    // Featured Banner
                    SizedBox(height: 24.h),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 20.w),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(24.r),
                        child: AspectRatio(
                          aspectRatio: 16 / 9,
                          child: Stack(
                            fit: StackFit.expand,
                            children: [
                              Image.network(
                                'https://lh3.googleusercontent.com/aida-public/AB6AXuBj0hUT5Eb5C_HSnRvwVydi1vZP2Kj813DK_jzABQ5oSHqAaOIp0buH31jGzUILWenfCb91eq_o0y09tSkBeSc2WKxyH1ylWxHrnQMi2dDuBUCQ4crfZPQSAaMPZzw7C37rXl3DPbfxnR5fcHa_57RQ1Ap2mWRL9_6AbbtkTCBrTmFCzYZx8Bfn3GuZh1kdNJKGLLI7b8NXEPfx2F4l1mmSdrJLVxzMvzDKorTwLs83SJxvnmKv76aU1IuDcty9d_T0ZcMnN95ZXXKt',
                                fit: BoxFit.cover,
                              ),
                              Container(
                                decoration: BoxDecoration(
                                  gradient: LinearGradient(
                                    colors: [
                                      Colors.black.withOpacity(0.8),
                                      Colors.black.withOpacity(0.2),
                                      Colors.transparent,
                                    ],
                                    begin: Alignment.bottomCenter,
                                    end: Alignment.topCenter,
                                  ),
                                ),
                              ),
                              Padding(
                                padding: EdgeInsets.all(24.r),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  mainAxisAlignment: MainAxisAlignment.end,
                                  children: [
                                    Text(
                                      'MÓN NGON MỖI NGÀY',
                                      style: TextStyle(
                                        color: Colors.white.withOpacity(0.8),
                                        fontSize: 12.sp,
                                        fontWeight: FontWeight.w600,
                                        letterSpacing: 1.2,
                                      ),
                                    ),
                                    SizedBox(height: 4.h),
                                    Text(
                                      'Gợi ý món ngon\nhôm nay',
                                      style: TextStyle(
                                        color: Colors.white,
                                        fontSize: 26.sp,
                                        fontWeight: FontWeight.bold,
                                        height: 1.2,
                                      ),
                                    ),
                                    SizedBox(height: 16.h),
                                    ElevatedButton(
                                      onPressed: () {},
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: _AppColors.primary,
                                        foregroundColor: Colors.white,
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(12.r),
                                        ),
                                        padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
                                        elevation: 0,
                                      ),
                                      child: Text(
                                        'Khám phá ngay',
                                        style: TextStyle(
                                          fontSize: 16.sp,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),

                    // Menu Grid Title
                    SizedBox(height: 32.h),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 20.w),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            'Phổ biến',
                            style: TextStyle(
                              color: _AppColors.onSurface,
                              fontSize: 20.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          Text(
                            'Xem tất cả',
                            style: TextStyle(
                              color: _AppColors.primary,
                              fontSize: 12.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(height: 16.h),
                  ],
                ),
              ),

              // Menu Grid Items
              SliverPadding(
                padding: EdgeInsets.only(left: 20.w, right: 20.w, bottom: 120.h),
                sliver: ref.watch(menuItemsProvider).when(
                  data: (items) => SliverGrid(
                    gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      mainAxisSpacing: 16.h,
                      crossAxisSpacing: 16.w,
                      childAspectRatio: 0.72,
                    ),
                    delegate: SliverChildBuilderDelegate(
                      (context, index) {
                        final item = items[index];
                        return Container(
                          decoration: BoxDecoration(
                            color: _AppColors.surfaceContainerLowest,
                            borderRadius: BorderRadius.circular(16.r),
                            boxShadow: [
                              BoxShadow(
                                color: Colors.black.withOpacity(0.03),
                                blurRadius: 10,
                                offset: const Offset(0, 2),
                              ),
                            ],
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Image
                              Expanded(
                                flex: 5,
                                child: Stack(
                                  fit: StackFit.expand,
                                  children: [
                                    ClipRRect(
                                      borderRadius: BorderRadius.vertical(top: Radius.circular(16.r)),
                                      child: item.imageUrl != null && item.imageUrl!.isNotEmpty
                                          ? Image.network(
                                              item.imageUrl!,
                                              fit: BoxFit.cover,
                                            )
                                          : Container(
                                              color: _AppColors.surfaceContainer,
                                              child: Icon(Icons.restaurant, color: _AppColors.outline, size: 40.sp),
                                            ),
                                    ),
                                    Positioned(
                                      top: 8.h,
                                      right: 8.w,
                                      child: Container(
                                        padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
                                        decoration: BoxDecoration(
                                          color: Colors.white.withOpacity(0.9),
                                          borderRadius: BorderRadius.circular(100.r),
                                          boxShadow: [
                                            BoxShadow(
                                              color: Colors.black.withOpacity(0.1),
                                              blurRadius: 4,
                                            ),
                                          ],
                                        ),
                                        child: Row(
                                          children: [
                                            Icon(Icons.star, color: Colors.amber, size: 14.sp),
                                            SizedBox(width: 2.w),
                                            Text(
                                              item.rating.toString(),
                                              style: TextStyle(
                                                color: _AppColors.onSurface,
                                                fontSize: 10.sp,
                                                fontWeight: FontWeight.bold,
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              // Details
                              Expanded(
                                flex: 4,
                                child: Padding(
                                  padding: EdgeInsets.all(12.r),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        item.name,
                                        maxLines: 2,
                                        overflow: TextOverflow.ellipsis,
                                        style: TextStyle(
                                          color: _AppColors.onSurface,
                                          fontSize: 14.sp,
                                          fontWeight: FontWeight.bold,
                                          height: 1.2,
                                        ),
                                      ),
                                      const Spacer(),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Text(
                                            '${item.price}k',
                                            style: TextStyle(
                                              color: _AppColors.primary,
                                              fontSize: 16.sp,
                                              fontWeight: FontWeight.bold,
                                            ),
                                          ),
                                          Container(
                                            width: 32.r,
                                            height: 32.r,
                                            decoration: BoxDecoration(
                                              color: _AppColors.primary,
                                              shape: BoxShape.circle,
                                              boxShadow: [
                                                BoxShadow(
                                                  color: _AppColors.primary.withOpacity(0.3),
                                                  blurRadius: 8,
                                                  offset: const Offset(0, 2),
                                                ),
                                              ],
                                            ),
                                            child: InkWell(
                                              onTap: () {
                                                ref.read(cartViewModelProvider.notifier).addItem(item);
                                                ScaffoldMessenger.of(context).showSnackBar(
                                                  SnackBar(content: Text('Đã thêm ${item.name} vào giỏ hàng')),
                                                );
                                              },
                                              child: Icon(
                                                Icons.add,
                                                color: Colors.white,
                                                size: 20.sp,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                      childCount: items.length,
                    ),
                  ),
                  loading: () => const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(40.0),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  ),
                  error: (error, stack) => SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(40.0),
                      child: Center(child: Text('Lỗi: $error')),
                    ),
                  ),
                ),
              ),
            ],
          ),

          // Floating Cart Summary
          Positioned(
            bottom: 24.h,
            left: 20.w,
            right: 20.w,
            child: Container(
              padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 12.h),
              decoration: BoxDecoration(
                color: _AppColors.onSurface.withOpacity(0.95),
                borderRadius: BorderRadius.circular(16.r),
                border: Border.all(color: Colors.white.withOpacity(0.1)),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.2),
                    blurRadius: 20,
                    offset: const Offset(0, 8),
                  ),
                ],
              ),
              child: Row(
                children: [
                  Stack(
                    clipBehavior: Clip.none,
                    children: [
                      Icon(Icons.shopping_bag_outlined, color: _AppColors.primaryFixed, size: 28.sp),
                      Positioned(
                        top: -4,
                        right: -4,
                        child: Container(
                          width: 16.r,
                          height: 16.r,
                          decoration: const BoxDecoration(
                            color: _AppColors.primary,
                            shape: BoxShape.circle,
                          ),
                          child: Center(
                            child: Text(
                              '$cartItemCount',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 10.sp,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(width: 16.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Giỏ hàng của bàn',
                          style: TextStyle(
                            color: Colors.white.withOpacity(0.7),
                            fontSize: 12.sp,
                          ),
                        ),
                        Text(
                          '$cartItemCount món • ${cartTotal}k',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 16.sp,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                  ElevatedButton(
                    onPressed: () => context.push('/cart'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: _AppColors.primary,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
                      elevation: 4,
                      shadowColor: _AppColors.primary.withOpacity(0.4),
                    ),
                    child: Text(
                      'Xem giỏ',
                      style: TextStyle(
                        fontSize: 14.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
      
      // Bottom Navigation Bar
      bottomNavigationBar: Container(
        decoration: BoxDecoration(
          color: _AppColors.surface,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.04),
              blurRadius: 20,
              offset: const Offset(0, -4),
            ),
          ],
          borderRadius: BorderRadius.vertical(top: Radius.circular(16.r)),
        ),
        child: SafeArea(
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 8.h),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(Icons.menu_book, 'Thực đơn', true, () {}),
                _buildNavItem(Icons.receipt_long, 'Đơn hàng', false, () => context.push('/orders')),
                _buildNavItem(Icons.shopping_cart, 'Giỏ hàng', false, () => context.push('/cart')),
                _buildNavItem(Icons.person, 'Tài khoản', false, () => context.push('/profile')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildNavItem(IconData icon, String label, bool isActive, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
        decoration: BoxDecoration(
          color: isActive ? _AppColors.primaryContainer : Colors.transparent,
          borderRadius: BorderRadius.circular(12.r),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? _AppColors.onPrimaryContainer : _AppColors.secondary,
              size: 24.sp,
            ),
            SizedBox(height: 4.h),
            Text(
              label,
              style: TextStyle(
                color: isActive ? _AppColors.onPrimaryContainer : _AppColors.secondary,
                fontSize: 12.sp,
                fontWeight: isActive ? FontWeight.bold : FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
