import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../services/socket/socket_service.dart';
import '../../widgets/customer_bottom_nav.dart';
import '../../theme/app_theme.dart';

class OrderListPage extends ConsumerStatefulWidget {
  const OrderListPage({super.key});

  @override
  ConsumerState<OrderListPage> createState() => _OrderListPageState();
}

class _OrderListPageState extends ConsumerState<OrderListPage> {
  Timer? _pollingTimer;
  final SocketService _socketService = SocketService();

  @override
  void initState() {
    super.initState();
    _pollingTimer = Timer.periodic(const Duration(seconds: 5), (timer) {
      if (mounted) {
        ref.invalidate(orderListProvider);
      }
    });

    final tableId = ref.read(authViewModelProvider).tableId;
    _socketService.subscribeToEvent('ReceiveOrderStatusUpdate', (data) {
      if (mounted) ref.invalidate(orderListProvider);
    });
    _socketService.subscribeToEvent('ReceiveNewOrder', (data) {
      if (mounted) ref.invalidate(orderListProvider);
    });
    if (tableId != null) {
      // ignore: unawaited_futures
      _socketService.connect(tableId);
    }
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    _socketService.unsubscribeFromEvent('ReceiveOrderStatusUpdate');
    _socketService.unsubscribeFromEvent('ReceiveNewOrder');
    _socketService.disconnect();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final ordersAsync = ref.watch(orderListProvider);

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: AppTheme.primary),
          onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
        ),
        title: Text(
          'Lịch sử đơn hàng',
          style: TextStyle(
            color: AppTheme.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
            letterSpacing: -0.5,
          ),
        ),
        centerTitle: true,
      ),
      body: ordersAsync.when(
        data: (orders) {
          if (orders.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.receipt_long, size: 80.sp, color: AppTheme.outlineVariant),
                  SizedBox(height: 16.h),
                  Text(
                    'Chưa có đơn hàng nào',
                    style: TextStyle(
                      color: AppTheme.onSurface,
                      fontSize: 20.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  SizedBox(height: 8.h),
                  Text(
                    'Đơn hàng sẽ xuất hiện ở đây sau khi bạn đặt món',
                    style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            );
          }

          final now = DateTime.now();
          final today = <dynamic>[];
          final older = <dynamic>[];
          for (final order in orders) {
            if (order.createdAt != null &&
                order.createdAt!.year == now.year &&
                order.createdAt!.month == now.month &&
                order.createdAt!.day == now.day) {
              today.add(order);
            } else {
              older.add(order);
            }
          }

          return SingleChildScrollView(
            padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 16.h),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (today.isNotEmpty) ...[
                  _buildDateHeader('Hôm nay'),
                  SizedBox(height: 12.h),
                  ...today.map((order) => _buildOrderItem(context, order)),
                ],
                if (older.isNotEmpty) ...[
                  if (today.isNotEmpty) SizedBox(height: 24.h),
                  _buildDateHeader('Trước đó'),
                  SizedBox(height: 12.h),
                  ...older.map((order) => _buildOrderItem(context, order)),
                ],
                SizedBox(height: 48.h),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, stack) => Center(child: Text('Lỗi tải danh sách: $err')),
      ),

      bottomNavigationBar: const CustomerBottomNav(activeTab: CustomerNavTab.orders),
    );
  }

  Widget _buildDateHeader(String title) {
    return Text(
      title.toUpperCase(),
      style: TextStyle(
        color: AppTheme.onSurfaceVariant,
        fontSize: 12.sp,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.2,
      ),
    );
  }

  Widget _buildOrderItem(BuildContext context, dynamic order) {
    final statusColor = order.status == 'COMPLETED' ? AppTheme.success : AppTheme.onSecondaryContainer;
    final statusBg = order.status == 'COMPLETED' ? AppTheme.successContainer : AppTheme.secondaryContainer;
    final statusLabel = _getStatusLabel(order.status);
    final timeStr = order.createdAt != null
        ? '${order.createdAt!.hour.toString().padLeft(2, '0')}:${order.createdAt!.minute.toString().padLeft(2, '0')}'
        : '';

    return Padding(
      padding: EdgeInsets.only(bottom: 16.h),
      child: _buildOrderCard(
        orderId: '#SD-${order.id}',
        time: timeStr,
        status: statusLabel,
        statusColor: statusColor,
        statusBg: statusBg,
        title: 'Đơn hàng Bàn ${order.tableNumber}',
        price: '${order.finalAmount}đ',
        onTap: () => context.push('/order_tracking/${order.id}'),
      ),
    );
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

  Widget _buildOrderCard({
    required String orderId,
    required String time,
    required String status,
    required Color statusColor,
    required Color statusBg,
    required String title,
    required String price,
    required VoidCallback onTap,
    double opacity = 1.0,
  }) {
    return Opacity(
      opacity: opacity,
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: AppTheme.surface,
          borderRadius: BorderRadius.circular(16.r),
          border: Border.all(color: AppTheme.outlineVariant.withOpacity(0.3)),
          boxShadow: AppTheme.shadowCard,
        ),
        child: Column(
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      orderId,
                      style: TextStyle(
                        color: AppTheme.onSurface,
                        fontSize: 16.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      time,
                      style: TextStyle(
                        color: AppTheme.onSurfaceVariant,
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w600,
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
                    status,
                    style: TextStyle(
                      color: statusColor,
                      fontSize: 10.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 12.h),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    color: AppTheme.onSurfaceVariant,
                    fontSize: 14.sp,
                  ),
                ),
                Text(
                  price,
                  style: TextStyle(
                    color: AppTheme.primary,
                    fontSize: 16.sp,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            SizedBox(height: 12.h),
            Container(
              padding: EdgeInsets.only(top: 12.h),
              decoration: BoxDecoration(
                border: Border(
                  top: BorderSide(color: AppTheme.outlineVariant.withOpacity(0.2)),
                ),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  _buildActionButton(
                    text: 'Chi tiết',
                    textColor: AppTheme.primary,
                    bgColor: AppTheme.primaryContainer,
                    onTap: onTap,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActionButton({
    required String text,
    required Color textColor,
    required Color bgColor,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12.r),
      child: Container(
        height: 40.h,
        padding: EdgeInsets.symmetric(horizontal: 24.w),
        decoration: BoxDecoration(
          color: bgColor,
          borderRadius: BorderRadius.circular(12.r),
        ),
        alignment: Alignment.center,
        child: Text(
          text,
          style: TextStyle(
            color: textColor,
            fontSize: 12.sp,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}