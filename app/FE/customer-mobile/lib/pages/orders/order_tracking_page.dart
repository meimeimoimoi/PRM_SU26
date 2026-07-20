import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../services/socket/socket_service.dart';
import '../../models/order_models.dart';
import '../../widgets/customer_bottom_nav.dart';
import '../../theme/app_theme.dart';

bool _isStaffRole(String? role) {
  return role == 'MANAGER' || role == 'STAFF' || role == 'CHEF';
}

void _showComingSoon(BuildContext context, String feature) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('$feature đang được phát triển, sẽ sớm ra mắt!')),
  );
}

class OrderTrackingPage extends ConsumerStatefulWidget {
  final int? orderId;

  const OrderTrackingPage({super.key, this.orderId});

  @override
  ConsumerState<OrderTrackingPage> createState() => _OrderTrackingPageState();
}

class _OrderTrackingPageState extends ConsumerState<OrderTrackingPage> with SingleTickerProviderStateMixin {
  late AnimationController _pulseController;
  late Animation<double> _pulseAnimation;
  final SocketService _socketService = SocketService();
  int? _orderId;
  String _currentStatus = 'PENDING';
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _orderId = widget.orderId;
    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 1),
    )..repeat(reverse: true);

    _pulseAnimation = Tween<double>(begin: 1.0, end: 1.15).animate(
      CurvedAnimation(parent: _pulseController, curve: Curves.easeInOut),
    );

    final authState = ref.read(authViewModelProvider);
    final tableId = authState.tableId;
    _socketService.subscribeToEvent('ReceiveOrderStatusUpdate', (data) {
      if (!mounted || data is! Map) return;
      final eventOrderId = data['orderId'] ?? data['OrderId'];
      if (eventOrderId?.toString() != _orderId?.toString()) return;
      final status = (data['status'] ?? data['Status'])?.toString();
      if (status != null && status.isNotEmpty) {
        setState(() => _currentStatus = status);
      }
      ref.invalidate(orderDetailProvider(_orderId!));
      ref.invalidate(orderStatusProvider(_orderId!));
    });
    if (tableId != null) {
      // ignore: unawaited_futures
      _socketService.connect(tableId);
    }

    _startPolling();
  }

  void _startPolling() {
    _pollingTimer?.cancel();
    _pollingTimer = Timer.periodic(const Duration(seconds: 4), (timer) {
      if (mounted && _orderId != null) {
        ref.invalidate(orderDetailProvider(_orderId!));
        ref.invalidate(orderStatusProvider(_orderId!));
      }
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    _pulseController.dispose();
    _socketService.unsubscribeFromEvent('ReceiveOrderStatusUpdate');
    _socketService.disconnect();
    super.dispose();
  }

  int _getTimelineStep(String status) {
    // 4 bước UI: 0 Nhận đơn → 1 Đang nấu → 2 Sẵn sàng → 3 Đã phục vụ
    switch (status) {
      case 'PENDING':
      case 'CONFIRMED':
        return 0;
      case 'COOKING':
        return 1;
      case 'READY':
        return 2;
      case 'COMPLETED':
        return 3;
      default:
        return 0;
    }
  }

  String _getStatusTitle(String status) {
    switch (status) {
      case 'PENDING':
        return 'Đơn hàng đang chờ xác nhận';
      case 'CONFIRMED':
        return 'Đơn hàng đã xác nhận';
      case 'COOKING':
        return 'Món ăn đang chế biến';
      case 'READY':
        return 'Món ăn sẵn sàng';
      case 'COMPLETED':
        return 'Đơn hàng hoàn thành';
      default:
        return 'Đơn hàng đang xử lý';
    }
  }

  int _timelineNodeStatus(int step, int index) {
    if (step > index) return 2; // xong
    if (step == index) return 1; // đang ở bước này
    return 0; // chưa tới
  }

  /// Đồng bộ UI với kitchen: nếu mọi món đã DONE mà order.status còn kẹt COOKING
  /// (race cũ), vẫn hiển thị READY.
  String _resolveDisplayStatus(String orderStatus, List<String> itemStatuses) {
    final active = itemStatuses
        .where((s) => s != 'CANCELLED' && s != 'RETURNED' && s.isNotEmpty)
        .toList();
    if (active.isEmpty) return orderStatus;

    if (active.every((s) => s == 'SERVED')) return 'COMPLETED';
    if (active.every((s) => s == 'DONE' || s == 'SERVED')) return 'READY';
    if (active.any((s) => s == 'DOING' || s == 'DONE' || s == 'SERVED')) {
      if (orderStatus == 'READY' || orderStatus == 'COMPLETED') return orderStatus;
      return 'COOKING';
    }
    return orderStatus;
  }

  String _getItemStatusLabel(String status) {
    switch (status) {
      case 'WAITING': return 'Chờ chế biến';
      case 'DOING': return 'Đang nấu';
      case 'DONE': return 'Sẵn sàng';
      case 'SERVED': return 'Đã phục vụ';
      case 'CANCELLED': return 'Đã hủy';
      default: return 'Chờ chế biến';
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final tableNumber = authState.tableNumber;

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        automaticallyImplyLeading: false,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: AppTheme.primary),
          onPressed: () => context.pop(),
        ),
        title: Row(
          children: [
            Icon(Icons.restaurant, color: AppTheme.primary, size: 24.sp),
            SizedBox(width: 8.w),
            Text(
              'SmartDine',
              style: TextStyle(
                color: AppTheme.primary,
                fontSize: 20.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        actions: [
          Container(
            margin: EdgeInsets.only(right: 20.w),
            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 6.h),
            decoration: BoxDecoration(
              color: AppTheme.primaryContainer,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              tableNumber != null && tableNumber > 0 ? 'Bàn $tableNumber' : 'Chưa chọn bàn',
              style: TextStyle(
                color: AppTheme.onPrimaryContainer,
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
      body: _buildBody(),
      bottomNavigationBar: const CustomerBottomNav(activeTab: CustomerNavTab.orders),
    );
  }

  Widget _buildBody() {
    final authState = ref.watch(authViewModelProvider);
    if (_orderId == null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.receipt_long, size: 64.sp, color: AppTheme.outline),
            SizedBox(height: 16.h),
            Text(
              'Chưa có đơn hàng đang theo dõi',
              style: TextStyle(
                color: AppTheme.onSurfaceVariant,
                fontSize: 18.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              'Đặt món từ thực đơn để bắt đầu',
              style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp),
            ),
            SizedBox(height: 24.h),
            ElevatedButton(
              onPressed: () => context.go('/home'),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primary,
                foregroundColor: AppTheme.onPrimary,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
              ),
              child: Text('Đặt món ngay', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      );
    }

    final orderAsync = ref.watch(orderDetailProvider(_orderId!));
    final statusAsync = ref.watch(orderStatusProvider(_orderId!));

    return orderAsync.when(
      data: (order) {
        final statusData = statusAsync.value;
        final itemStatuses = (statusData?.items.isNotEmpty == true)
            ? statusData!.items.map((e) => e.status).toList()
            : order.items.map((e) => e.status).toList();
        final displayStatus = _resolveDisplayStatus(
          statusData?.status.isNotEmpty == true ? statusData!.status : order.status,
          itemStatuses,
        );
        final step = _getTimelineStep(displayStatus);
        final items = statusData?.items ??
            order.items
                .map((e) => OrderItemStatusResponse(
                      name: e.name,
                      quantity: e.quantity,
                      status: e.status,
                    ))
                .toList();

        return SingleChildScrollView(
          padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 24.h),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                _getStatusTitle(displayStatus),
                style: TextStyle(
                  color: AppTheme.onSurface,
                  fontSize: 26.sp,
                  fontWeight: FontWeight.bold,
                ),
              ),
              SizedBox(height: 16.h),

              // Status Card
              Container(
                padding: EdgeInsets.all(24.r),
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
                              'MÃ ĐƠN HÀNG',
                              style: TextStyle(
                                color: AppTheme.onSurfaceVariant,
                                fontSize: 12.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 1.2,
                              ),
                            ),
                            SizedBox(height: 4.h),
                            Text(
                              '#SD-${order.id}',
                              style: TextStyle(
                                color: AppTheme.onSurface,
                                fontSize: 20.sp,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                        if (displayStatus != 'COMPLETED' && displayStatus != 'CANCELLED')
                          Container(
                            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 8.h),
                            decoration: BoxDecoration(
                              color: AppTheme.tertiaryContainer.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(8.r),
                            ),
                            child: Row(
                              children: [
                                ScaleTransition(
                                  scale: _pulseAnimation,
                                  child: Icon(Icons.timer, color: AppTheme.tertiary, size: 20.sp),
                                ),
                                SizedBox(width: 6.w),
                                Text(
                                  'Đang theo dõi',
                                  style: TextStyle(
                                    color: AppTheme.tertiary,
                                    fontSize: 12.sp,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
                            ),
                          ),
                      ],
                    ),
                    SizedBox(height: 32.h),

                    // 4-step Visual Timeline
                    Stack(
                      children: [
                        Positioned(
                          top: 18.h,
                          left: 20.w,
                          right: 20.w,
                          child: Container(
                            height: 2.h,
                            color: AppTheme.outlineVariant.withOpacity(0.4),
                          ),
                        ),
                        Positioned(
                          top: 18.h,
                          left: 20.w,
                          right: 20.w,
                          child: FractionallySizedBox(
                            alignment: Alignment.centerLeft,
                            widthFactor: (step / 3).clamp(0.0, 1.0),
                            child: Container(
                              height: 2.h,
                              color: AppTheme.primary,
                            ),
                          ),
                        ),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            _buildTimelineStep(
                              icon: Icons.check,
                              label: 'Nhận đơn',
                              status: _timelineNodeStatus(step, 0),
                              pulse: step == 0,
                            ),
                            _buildTimelineStep(
                              icon: Icons.soup_kitchen,
                              label: 'Đang nấu',
                              status: _timelineNodeStatus(step, 1),
                              pulse: step == 1,
                            ),
                            _buildTimelineStep(
                              icon: Icons.notifications_active,
                              label: 'Sẵn sàng',
                              status: _timelineNodeStatus(step, 2),
                              pulse: step == 2,
                            ),
                            _buildTimelineStep(
                              icon: Icons.restaurant,
                              label: 'Đã phục vụ',
                              status: _timelineNodeStatus(step, 3),
                              pulse: step == 3,
                            ),
                          ],
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              SizedBox(height: 32.h),

              // Order Detailed List
              if (items.isNotEmpty) ...[
                Text(
                  'Chi tiết các món',
                  style: TextStyle(
                    color: AppTheme.onSurface,
                    fontSize: 20.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                SizedBox(height: 16.h),
                ...items.map((item) {
                  final isServed = item.status == 'SERVED';
                  final statusLabel = _getItemStatusLabel(item.status);
                  return Padding(
                    padding: EdgeInsets.only(bottom: 16.h),
                    child: _buildOrderItem(
                      title: item.name,
                      statusLabel: statusLabel,
                      statusBgColor: isServed
                          ? AppTheme.secondaryContainer
                          : AppTheme.primaryContainer.withOpacity(0.1),
                      statusTextColor: isServed
                          ? AppTheme.onSecondaryContainer
                          : AppTheme.primary,
                      quantity: item.quantity.toString().padLeft(2, '0'),
                      isGrayscale: isServed,
                    ),
                  );
                }),
              ] else if (order.items.isNotEmpty) ...[
                Text(
                  'Chi tiết các món',
                  style: TextStyle(
                    color: AppTheme.onSurface,
                    fontSize: 20.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                SizedBox(height: 16.h),
                ...order.items.map((item) {
                  return Padding(
                    padding: EdgeInsets.only(bottom: 16.h),
                    child: _buildOrderItem(
                      title: item.name,
                      statusLabel: _getItemStatusLabel(item.status),
                      statusBgColor: item.status == 'SERVED'
                          ? AppTheme.secondaryContainer
                          : AppTheme.primaryContainer.withOpacity(0.1),
                      statusTextColor: item.status == 'SERVED'
                          ? AppTheme.onSecondaryContainer
                          : AppTheme.primary,
                      quantity: item.quantity.toString().padLeft(2, '0'),
                      isGrayscale: item.status == 'SERVED',
                    ),
                  );
                }),
              ],

              SizedBox(height: 32.h),

              // Support CTA
              Container(
                padding: EdgeInsets.symmetric(vertical: 24.h),
                decoration: BoxDecoration(
                  border: Border(top: BorderSide(color: AppTheme.outlineVariant.withOpacity(0.2))),
                ),
                child: Column(
                  children: [
                    Text(
                      'Cần trợ giúp với đơn hàng của bạn?',
                      style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp),
                    ),
                    SizedBox(height: 16.h),
                    OutlinedButton(
                      onPressed: () => _showComingSoon(context, 'Gọi nhân viên hỗ trợ'),
                      style: OutlinedButton.styleFrom(
                        side: BorderSide(color: AppTheme.primary, width: 2),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                        padding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 24.w),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.support_agent, color: AppTheme.primary, size: 24.sp),
                          SizedBox(width: 12.w),
                          Text(
                            'Gọi nhân viên hỗ trợ',
                            style: TextStyle(
                              color: AppTheme.primary,
                              fontSize: 16.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              // Staff Actions
              if (_isStaffRole(authState.user?.role) && order.status != 'COMPLETED' && order.status != 'CANCELLED') ...[
                SizedBox(height: 16.h),
                _buildStaffActions(context, ref, order.id, order.status),
              ],

              SizedBox(height: 20.h),
            ],
          ),
        );
      },
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (err, stack) => Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline, size: 48.sp, color: AppTheme.error),
            SizedBox(height: 16.h),
            Text('Không thể tải thông tin đơn hàng', style: TextStyle(fontSize: 16.sp, color: AppTheme.onSurface)),
            SizedBox(height: 8.h),
            Text('$err', style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp)),
          ],
        ),
      ),
    );
  }

  Widget _buildTimelineStep({
    required IconData icon,
    required String label,
    required int status,
    bool pulse = false,
  }) {
    final bgColor = status > 0 ? AppTheme.primary : AppTheme.surfaceContainerHigh;
    final iconColor = status > 0 ? AppTheme.onPrimary : AppTheme.outline;
    final textColor = status > 0 ? AppTheme.primary : AppTheme.outline;
    final isBold = status == 1;

    Widget circle = Container(
      width: 36.r,
      height: 36.r,
      decoration: BoxDecoration(
        color: bgColor,
        shape: BoxShape.circle,
        boxShadow: status == 1 ? [
          BoxShadow(
            color: AppTheme.primary.withOpacity(0.3),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ] : null,
      ),
      child: Icon(icon, color: iconColor, size: 20.sp),
    );

    if (pulse) {
      circle = ScaleTransition(
        scale: _pulseAnimation,
        child: circle,
      );
    }

    return Column(
      children: [
        circle,
        SizedBox(height: 8.h),
        Text(
          label,
          style: TextStyle(
            color: textColor,
            fontSize: 12.sp,
            fontWeight: isBold ? FontWeight.bold : FontWeight.w600,
          ),
        ),
      ],
    );
  }

  Widget _buildOrderItem({
    required String title,
    required String statusLabel,
    required Color statusBgColor,
    required Color statusTextColor,
    required String quantity,
    required bool isGrayscale,
  }) {
    return Container(
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: AppTheme.outlineVariant.withOpacity(0.2)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.02),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 64.r,
            height: 64.r,
            decoration: BoxDecoration(
              color: AppTheme.surfaceContainerHigh,
              borderRadius: BorderRadius.circular(8.r),
            ),
            child: Icon(
              isGrayscale ? Icons.check_circle : Icons.restaurant,
              color: isGrayscale ? AppTheme.onSurfaceVariant : AppTheme.primary,
              size: 28.sp,
            ),
          ),
          SizedBox(width: 16.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: AppTheme.onSurface,
                          fontSize: 16.sp,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                      decoration: BoxDecoration(
                        color: statusBgColor,
                        borderRadius: BorderRadius.circular(6.r),
                      ),
                      child: Text(
                        statusLabel,
                        style: TextStyle(
                          color: statusTextColor,
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 8.h),
                Text(
                  'Số lượng: $quantity',
                  style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStaffActions(BuildContext context, WidgetRef ref, int orderId, String currentStatus) {
    final nextStatus = _getNextStatus(currentStatus);
    final statusLabels = {
      'CONFIRMED': 'Xác nhận',
      'COOKING': 'Bắt đầu nấu',
      'READY': 'Sẵn sàng',
      'COMPLETED': 'Hoàn thành',
    };

    return Container(
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: AppTheme.outlineVariant.withOpacity(0.3)),
        boxShadow: AppTheme.shadowCard,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.admin_panel_settings, color: AppTheme.primary, size: 20.sp),
              SizedBox(width: 8.w),
              Text(
                'Quản lý đơn hàng',
                style: TextStyle(
                  color: AppTheme.onSurface,
                  fontSize: 16.sp,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          SizedBox(height: 12.h),
          if (nextStatus != null)
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () async {
                  await ref.read(orderStatusUpdateProvider.notifier).updateStatus(orderId, nextStatus);
                  ref.invalidate(orderDetailProvider(orderId));
                  ref.invalidate(orderStatusProvider(orderId));
                  if (context.mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text('Đã cập nhật: ${statusLabels[nextStatus] ?? nextStatus}')),
                    );
                  }
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primary,
                  foregroundColor: AppTheme.onPrimary,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                  padding: EdgeInsets.symmetric(vertical: 14.h),
                ),
                child: Text(
                  'Chuyển: ${statusLabels[nextStatus] ?? nextStatus}',
                  style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold),
                ),
              ),
            )
          else
            Container(
              width: double.infinity,
              padding: EdgeInsets.symmetric(vertical: 14.h),
              decoration: BoxDecoration(
                color: AppTheme.secondaryContainer,
                borderRadius: BorderRadius.circular(12.r),
              ),
              child: Center(
                child: Text(
                  'Đơn hàng đã kết thúc',
                  style: TextStyle(
                    color: AppTheme.onSecondaryContainer,
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),
        ],
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
}