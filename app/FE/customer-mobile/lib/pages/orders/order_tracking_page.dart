import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../services/socket/socket_service.dart';

bool _isStaffRole(String? role) {
  return role == 'MANAGER' || role == 'STAFF' || role == 'CHEF';
}

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color surfaceVariant = Color(0xFFe5e2e1);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondary = Color(0xFF685b5a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color tertiary = Color(0xFF005cac);
  static const Color tertiaryContainer = Color(0xFF0075d7);
  static const Color outline = Color(0xFF8f7068);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color onPrimary = Color(0xFFffffff);
  static const Color error = Color(0xFFba1a1a);
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
    final tableId = authState.guestSession?.tableId;
    if (tableId != null) {
      _socketService.connect(tableId);
    }
    _socketService.subscribeToEvent('ReceiveOrderStatusUpdate', (data) {
      if (!mounted || data is! Map) return;
      final eventOrderId = data['orderId'] ?? data['OrderId'];
      if (eventOrderId != _orderId) return;
      setState(() {
        _currentStatus = data['status'] ?? data['Status'] ?? _currentStatus;
      });
      ref.invalidate(orderDetailProvider(_orderId!));
      ref.invalidate(orderStatusProvider(_orderId!));
    });

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
    switch (status) {
      case 'PENDING':
        return 0;
      case 'CONFIRMED':
        return 1;
      case 'COOKING':
        return 2;
      case 'READY':
        return 3;
      case 'COMPLETED':
        return 4;
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

  String _getItemStatusLabel(String status) {
    switch (status) {
      case 'WAITING':
        return 'Chờ chế biến';
      case 'DOING':
        return 'Đang nấu';
      case 'DONE':
        return 'Sẵn sàng';
      case 'SERVED':
        return 'Đã phục vụ';
      case 'CANCELLED':
        return 'Đã hủy';
      default:
        return 'Chờ chế biến';
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final tableNumber = authState.guestSession?.tableNumber ?? 1;

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        automaticallyImplyLeading: false,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
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
            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 6.h),
            decoration: BoxDecoration(
              color: _AppColors.primaryContainer,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              'Bàn $tableNumber',
              style: TextStyle(
                color: _AppColors.onPrimaryContainer,
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
      body: _buildBody(),
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
                _buildNavItem(Icons.menu_book, 'Thực đơn', false, () => context.go('/home')),
                _buildNavItem(Icons.receipt_long, 'Đơn hàng', true, () {}),
                _buildNavItem(Icons.shopping_cart, 'Giỏ hàng', false, () => context.push('/cart')),
                _buildNavItem(Icons.person, 'Tài khoản', false, () => context.push('/profile')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildBody() {
    final authState = ref.watch(authViewModelProvider);
    if (_orderId == null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.receipt_long, size: 64.sp, color: _AppColors.outline),
            SizedBox(height: 16.h),
            Text(
              'Chưa có đơn hàng đang theo dõi',
              style: TextStyle(
                color: _AppColors.onSurfaceVariant,
                fontSize: 18.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              'Đặt món từ thực đơn để bắt đầu',
              style: TextStyle(color: _AppColors.secondary, fontSize: 14.sp),
            ),
            SizedBox(height: 24.h),
            ElevatedButton(
              onPressed: () => context.go('/home'),
              style: ElevatedButton.styleFrom(
                backgroundColor: _AppColors.primary,
                foregroundColor: _AppColors.onPrimary,
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
        final step = _getTimelineStep(order.status);
        final statusData = statusAsync.value;
        final items = statusData?.items ?? [];

        return SingleChildScrollView(
          padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 24.h),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                _getStatusTitle(order.status),
                style: TextStyle(
                  color: _AppColors.onSurface,
                  fontSize: 26.sp,
                  fontWeight: FontWeight.bold,
                ),
              ),
              SizedBox(height: 16.h),

              // Status Card
              Container(
                padding: EdgeInsets.all(24.r),
                decoration: BoxDecoration(
                  color: _AppColors.surfaceContainerLowest,
                  borderRadius: BorderRadius.circular(16.r),
                  border: Border.all(color: _AppColors.outlineVariant.withOpacity(0.3)),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.04),
                      blurRadius: 20,
                      offset: const Offset(0, 4),
                    ),
                  ],
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
                                color: _AppColors.secondary,
                                fontSize: 12.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 1.2,
                              ),
                            ),
                            SizedBox(height: 4.h),
                            Text(
                              '#SD-${order.id}',
                              style: TextStyle(
                                color: _AppColors.onSurface,
                                fontSize: 20.sp,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                        if (order.status != 'COMPLETED' && order.status != 'CANCELLED')
                          Container(
                            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 8.h),
                            decoration: BoxDecoration(
                              color: _AppColors.tertiaryContainer.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(8.r),
                            ),
                            child: Row(
                              children: [
                                ScaleTransition(
                                  scale: _pulseAnimation,
                                  child: Icon(Icons.timer, color: _AppColors.tertiary, size: 20.sp),
                                ),
                                SizedBox(width: 6.w),
                                Text(
                                  'Đang theo dõi',
                                  style: TextStyle(
                                    color: _AppColors.tertiary,
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
                            color: _AppColors.outlineVariant.withOpacity(0.4),
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
                              color: _AppColors.primary,
                            ),
                          ),
                        ),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            _buildTimelineStep(
                              icon: Icons.check,
                              label: 'Nhận đơn',
                              status: step >= 1 ? 2 : (step == 0 ? 1 : 0),
                              pulse: step == 0,
                            ),
                            _buildTimelineStep(
                              icon: Icons.soup_kitchen,
                              label: 'Đang nấu',
                              status: step >= 2 ? 2 : (step == 1 ? 1 : 0),
                              pulse: step == 1,
                            ),
                            _buildTimelineStep(
                              icon: Icons.notifications_active,
                              label: 'Sẵn sàng',
                              status: step >= 3 ? 2 : (step == 2 ? 1 : 0),
                              pulse: step == 2,
                            ),
                            _buildTimelineStep(
                              icon: Icons.restaurant,
                              label: 'Đã phục vụ',
                              status: step >= 4 ? 2 : (step == 3 ? 1 : 0),
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
                    color: _AppColors.onSurface,
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
                          ? _AppColors.secondaryContainer
                          : _AppColors.primaryContainer.withOpacity(0.1),
                      statusTextColor: isServed
                          ? _AppColors.onSecondaryContainer
                          : _AppColors.primary,
                      quantity: item.quantity.toString().padLeft(2, '0'),
                      isGrayscale: isServed,
                    ),
                  );
                }),
              ] else if (order.items.isNotEmpty) ...[
                Text(
                  'Chi tiết các món',
                  style: TextStyle(
                    color: _AppColors.onSurface,
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
                          ? _AppColors.secondaryContainer
                          : _AppColors.primaryContainer.withOpacity(0.1),
                      statusTextColor: item.status == 'SERVED'
                          ? _AppColors.onSecondaryContainer
                          : _AppColors.primary,
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
                  border: Border(top: BorderSide(color: _AppColors.outlineVariant.withOpacity(0.2))),
                ),
                child: Column(
                  children: [
                    Text(
                      'Cần trợ giúp với đơn hàng của bạn?',
                      style: TextStyle(color: _AppColors.secondary, fontSize: 14.sp),
                    ),
                    SizedBox(height: 16.h),
                    OutlinedButton(
                      onPressed: () {},
                      style: OutlinedButton.styleFrom(
                        side: BorderSide(color: _AppColors.primary, width: 2),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                        padding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 24.w),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.support_agent, color: _AppColors.primary, size: 24.sp),
                          SizedBox(width: 12.w),
                          Text(
                            'Gọi nhân viên hỗ trợ',
                            style: TextStyle(
                              color: _AppColors.primary,
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
            Icon(Icons.error_outline, size: 48.sp, color: _AppColors.error),
            SizedBox(height: 16.h),
            Text('Không thể tải thông tin đơn hàng', style: TextStyle(fontSize: 16.sp)),
            SizedBox(height: 8.h),
            Text('$err', style: TextStyle(color: _AppColors.secondary, fontSize: 14.sp)),
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
    final bgColor = status > 0 ? _AppColors.primary : _AppColors.surfaceContainerHighest;
    final iconColor = status > 0 ? _AppColors.onPrimary : _AppColors.outline;
    final textColor = status > 0 ? _AppColors.primary : _AppColors.outline;
    final isBold = status == 1;

    Widget circle = Container(
      width: 36.r,
      height: 36.r,
      decoration: BoxDecoration(
        color: bgColor,
        shape: BoxShape.circle,
        boxShadow: status == 1 ? [
          BoxShadow(
            color: _AppColors.primary.withOpacity(0.3),
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
        color: _AppColors.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: _AppColors.outlineVariant.withOpacity(0.2)),
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
              color: _AppColors.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(8.r),
            ),
            child: Icon(
              isGrayscale ? Icons.check_circle : Icons.restaurant,
              color: isGrayscale ? _AppColors.secondary : _AppColors.primary,
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
                          color: _AppColors.onSurface,
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
                  style: TextStyle(color: _AppColors.secondary, fontSize: 14.sp),
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
        color: _AppColors.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(16.r),
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
            children: [
              Icon(Icons.admin_panel_settings, color: _AppColors.primary, size: 20.sp),
              SizedBox(width: 8.w),
              Text(
                'Quản lý đơn hàng',
                style: TextStyle(
                  color: _AppColors.onSurface,
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
                  backgroundColor: _AppColors.primary,
                  foregroundColor: Colors.white,
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
                color: _AppColors.secondaryContainer,
                borderRadius: BorderRadius.circular(12.r),
              ),
              child: Center(
                child: Text(
                  'Đơn hàng đã kết thúc',
                  style: TextStyle(
                    color: _AppColors.onSecondaryContainer,
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
