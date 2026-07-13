import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../services/order_repository.dart';
import '../../services/socket/socket_service.dart';
import '../../models/order_models.dart';


class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color surfaceContainerHigh = Color(0xFFeae7e7);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color tertiary = Color(0xFF005cac);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color error = Color(0xFFba1a1a);
  static const Color onPrimary = Color(0xFFffffff);
}

class OrderListPage extends ConsumerStatefulWidget {
  const OrderListPage({super.key});

  @override
  ConsumerState<OrderListPage> createState() => _OrderListPageState();
}

class _OrderListPageState extends ConsumerState<OrderListPage> {
  int _selectedPaymentMethod = 1; // Default VietQR
  bool _isProcessingPayment = false;
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

    final tableId = ref.read(authViewModelProvider).guestSession?.tableId;
    if (tableId != null) {
      _socketService.connect(tableId);
    }
    _socketService.subscribeToEvent('ReceiveOrderStatusUpdate', (data) {
      if (mounted) ref.invalidate(orderListProvider);
    });
    _socketService.subscribeToEvent('ReceiveNewOrder', (data) {
      if (mounted) ref.invalidate(orderListProvider);
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    _socketService.unsubscribeFromEvent('ReceiveOrderStatusUpdate');
    _socketService.unsubscribeFromEvent('ReceiveNewOrder');
    _socketService.disconnect();
    super.dispose();
  }

  Future<void> _handlePayment(int sessionId) async {
    if (_isProcessingPayment) return;
    setState(() => _isProcessingPayment = true);

    try {
      String method;
      switch (_selectedPaymentMethod) {
        case 0: method = 'MOMO'; break;
        case 2: method = 'CASH'; break;
        default: method = 'VNPAY'; break;
      }

      final repo = ref.read(orderRepositoryProvider);
      final response = await repo.createPaymentIntent(sessionId, method);
      if (!mounted) return;

      Navigator.of(context).pop(); // Close bottom sheet

      if (method == 'CASH') {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (_) => AlertDialog(
            title: const Text('Thanh toán tiền mặt'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Mã hóa đơn: ${response.invoiceId}'),
                SizedBox(height: 8.h),
                Text('Số tiền: ${response.totalPayable.toStringAsFixed(0)}đ'),
                SizedBox(height: 16.h),
                const Text('Vui lòng di chuyển đến quầy thu ngân để thanh toán.'),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  ref.invalidate(orderListProvider);
                },
                child: const Text('Đóng'),
              ),
            ],
          ),
        );
      } else {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (_) => AlertDialog(
            title: Text(method == 'MOMO' ? 'Thanh toán qua MoMo' : 'Thanh toán VietQR'),
            content: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Mã hóa đơn: ${response.invoiceId}'),
                  SizedBox(height: 8.h),
                  Text(
                    '${response.totalPayable.toStringAsFixed(0)}đ',
                    style: TextStyle(
                      fontSize: 22.sp,
                      fontWeight: FontWeight.bold,
                      color: _AppColors.primary,
                    ),
                  ),
                  SizedBox(height: 16.h),
                  if (response.qrUrl != null)
                    Container(
                      padding: EdgeInsets.all(8.r),
                      decoration: BoxDecoration(
                        border: Border.all(color: _AppColors.outlineVariant),
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      child: Image.network(
                        response.qrUrl!,
                        width: 200.r,
                        height: 200.r,
                        fit: BoxFit.contain,
                        errorBuilder: (_, __, ___) => Container(
                          width: 200.r,
                          height: 200.r,
                          color: _AppColors.surfaceContainerHigh,
                          child: Icon(Icons.qr_code, size: 64.sp, color: _AppColors.outlineVariant),
                        ),
                      ),
                    ),
                  SizedBox(height: 16.h),
                  const Text(
                    'Quét mã QR bằng ứng dụng ngân hàng hoặc ví điện tử.',
                    textAlign: TextAlign.center,
                  ),
                  if (response.deeplink != null) ...[
                    SizedBox(height: 16.h),
                    ElevatedButton.icon(
                      onPressed: () async {
                        final uri = Uri.parse(response.deeplink!);
                        if (await canLaunchUrl(uri)) {
                          await launchUrl(uri, mode: LaunchMode.externalApplication);
                        }
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: _AppColors.primary,
                        foregroundColor: Colors.white,
                      ),
                      icon: const Icon(Icons.open_in_new),
                      label: const Text('Mở liên kết thanh toán'),
                    ),
                  ],
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  ref.invalidate(orderListProvider);
                },
                child: const Text('Đóng'),
              ),
            ],
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        Navigator.of(context).pop(); // Close bottom sheet first
        final msg = e.toString().contains('đang chờ') || e.toString().contains('ALREADY')
            ? 'Bạn đã có thanh toán đang chờ xử lý.'
            : 'Lỗi thanh toán: $e';
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(msg)),
        );
      }
    } finally {
      if (mounted) setState(() => _isProcessingPayment = false);
    }
  }

  void _showPaymentSheet(int sessionId, double totalAmount) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setSheetState) => Container(
          constraints: BoxConstraints(maxHeight: MediaQuery.of(context).size.height * 0.75),
          decoration: BoxDecoration(
            color: _AppColors.background,
            borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Handle bar
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
                  'Thanh toán phiên ăn',
                  style: TextStyle(
                    fontSize: 20.sp,
                    fontWeight: FontWeight.bold,
                    color: _AppColors.primary,
                  ),
                ),
              ),

              // Total
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

              // Payment methods
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 24.w),
                child: Column(
                  children: [
                    _buildPaymentOption(
                      index: 0,
                      title: 'Ví Điện Tử (Momo)',
                      icon: Icons.account_balance_wallet,
                      iconBgColor: _AppColors.secondaryContainer,
                      iconColor: _AppColors.primary,
                      isSelected: _selectedPaymentMethod == 0,
                      onTap: () => setSheetState(() => _selectedPaymentMethod = 0),
                    ),
                    SizedBox(height: 10.h),
                    _buildPaymentOption(
                      index: 1,
                      title: 'Thẻ Ngân Hàng / VietQR',
                      icon: Icons.qr_code_2,
                      iconBgColor: _AppColors.secondaryContainer,
                      iconColor: _AppColors.primary,
                      isSelected: _selectedPaymentMethod == 1,
                      onTap: () => setSheetState(() => _selectedPaymentMethod = 1),
                    ),
                    SizedBox(height: 10.h),
                    _buildPaymentOption(
                      index: 2,
                      title: 'Tiền Mặt tại quầy',
                      icon: Icons.payments,
                      iconBgColor: _AppColors.surfaceContainerHighest,
                      iconColor: _AppColors.onSurfaceVariant,
                      isSelected: _selectedPaymentMethod == 2,
                      onTap: () => setSheetState(() => _selectedPaymentMethod = 2),
                    ),
                  ],
                ),
              ),
              SizedBox(height: 20.h),

              // Pay button
              Padding(
                padding: EdgeInsets.fromLTRB(24.w, 0, 24.w, 32.h),
                child: SizedBox(
                  width: double.infinity,
                  height: 52.h,
                  child: ElevatedButton(
                    onPressed: _isProcessingPayment
                        ? null
                        : () {
                            setSheetState(() => _isProcessingPayment = true);
                            _handlePayment(sessionId).then((_) {
                              if (mounted) setSheetState(() => _isProcessingPayment = false);
                            });
                          },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: _AppColors.primary,
                      foregroundColor: Colors.white,
                      disabledBackgroundColor: _AppColors.surfaceContainerHighest,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                    ),
                    child: _isProcessingPayment
                        ? const SizedBox(
                            width: 24, height: 24,
                            child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                          )
                        : Text(
                            'Xác nhận thanh toán',
                            style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold),
                          ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPaymentOption({
    required int index,
    required String title,
    required IconData icon,
    required Color iconBgColor,
    required Color iconColor,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12.r),
      child: Container(
        padding: EdgeInsets.all(14.r),
        decoration: BoxDecoration(
          color: _AppColors.surfaceContainerLowest,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(
            color: isSelected ? _AppColors.primary : Colors.transparent,
            width: 2,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 36.r,
              height: 36.r,
              decoration: BoxDecoration(
                color: iconBgColor,
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(icon, color: iconColor, size: 20.sp),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Text(
                title,
                style: TextStyle(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w600,
                  color: _AppColors.onSurface,
                ),
              ),
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
    final ordersAsync = ref.watch(orderListProvider);

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface.withOpacity(0.8),
        elevation: 0,
        scrolledUnderElevation: 2,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Lịch sử đơn hàng',
          style: TextStyle(
            color: _AppColors.primary,
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
            return const Center(child: Text('Chưa có đơn hàng nào.'));
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
        ),
        child: SafeArea(
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 8.h),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(Icons.menu_book, 'Thực đơn', false, () => context.go('/home')),
                _buildNavItem(Icons.receipt_long, 'Đơn hàng', true, () {}),
                _buildNavItem(Icons.person, 'Tài khoản', false, () => context.push('/profile')),
                _buildNavItem(Icons.settings, 'Cài đặt', false, () => context.push('/settings')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildDateHeader(String title) {
    return Text(
      title.toUpperCase(),
      style: TextStyle(
        color: _AppColors.onSurfaceVariant,
        fontSize: 12.sp,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.2,
      ),
    );
  }

  Widget _buildOrderItem(BuildContext context, dynamic order) {
    final statusColor = order.status == 'COMPLETED' ? _AppColors.tertiary : _AppColors.onSecondaryContainer;
    final statusBg = order.status == 'COMPLETED' ? _AppColors.tertiary.withOpacity(0.1) : _AppColors.secondaryContainer;
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
          color: _AppColors.surfaceContainerLowest,
          borderRadius: BorderRadius.circular(12.r),
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
                      orderId,
                      style: TextStyle(
                        color: _AppColors.onSurface,
                        fontSize: 16.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      time,
                      style: TextStyle(
                        color: _AppColors.onSurfaceVariant,
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
                    color: _AppColors.onSurfaceVariant,
                    fontSize: 14.sp,
                  ),
                ),
                Text(
                  price,
                  style: TextStyle(
                    color: _AppColors.primary,
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
                  top: BorderSide(color: _AppColors.outlineVariant.withOpacity(0.2)),
                ),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  _buildActionButton(
                    text: 'Chi tiết',
                    textColor: _AppColors.primary,
                    bgColor: _AppColors.primary.withOpacity(0.1),
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

  Widget _buildNavItem(IconData icon, String label, bool isActive, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
        decoration: BoxDecoration(
          color: isActive ? _AppColors.primaryContainer.withOpacity(0.1) : Colors.transparent,
          borderRadius: BorderRadius.circular(12.r),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? _AppColors.primary : _AppColors.onSurfaceVariant,
              size: 24.sp,
            ),
            SizedBox(height: 4.h),
            Text(
              label,
              style: TextStyle(
                color: isActive ? _AppColors.primary : _AppColors.onSurfaceVariant,
                fontSize: 12.sp,
                fontWeight: isActive ? FontWeight.bold : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
