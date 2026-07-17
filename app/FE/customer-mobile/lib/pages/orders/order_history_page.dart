import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../widgets/simple_qr_view.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../services/order_repository.dart';
import '../../services/settings_repository.dart';
import '../../services/socket/socket_service.dart';
import '../../models/order_models.dart';
import '../../utils/error_utils.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color surfaceContainerHigh = Color(0xFFeae7e7);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color surfaceVariant = Color(0xFFe5e2e1);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondary = Color(0xFF685b5a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color tertiary = Color(0xFF005cac);
  static const Color onTertiaryContainer = Color(0xFFfefcff);
  static const Color outline = Color(0xFF8f7068);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color errorContainer = Color(0xFFffdad6);
  static const Color onErrorContainer = Color(0xFF93000a);
  static const Color error = Color(0xFFba1a1a);
  static const Color onPrimary = Color(0xFFffffff);
}

class OrderHistoryPage extends ConsumerStatefulWidget {
  const OrderHistoryPage({super.key});

  @override
  ConsumerState<OrderHistoryPage> createState() => _OrderHistoryPageState();
}

class _OrderHistoryPageState extends ConsumerState<OrderHistoryPage> {
  int _selectedPaymentMethod = 0;
  bool _isProcessingPayment = false;
  final SocketService _socketService = SocketService();

  // Theo dõi hộp thoại QR/tiền mặt đang mở để tự đóng khi ReceivePaymentSuccess
  // báo về, thay vì bắt khách phải tự bấm "Đóng".
  bool _isPaymentDialogOpen = false;
  String? _pendingInvoiceId;

  @override
  void initState() {
    super.initState();
    final tableId = ref.read(authViewModelProvider).guestSession?.tableId;
    if (tableId != null) {
      _socketService.connect(tableId);
    }
    _socketService.subscribeToEvent('ReceivePaymentSuccess', _onPaymentSuccess);
  }

  @override
  void dispose() {
    _socketService.unsubscribeFromEvent('ReceivePaymentSuccess');
    _socketService.disconnect();
    super.dispose();
  }

  void _onPaymentSuccess(dynamic data) {
    if (!mounted || data is! Map) return;
    final invoiceId = (data['invoiceId'] ?? data['InvoiceId'])?.toString();
    if (invoiceId == null || invoiceId != _pendingInvoiceId) return;

    _pendingInvoiceId = null;
    if (_isPaymentDialogOpen) {
      Navigator.of(context, rootNavigator: true).pop();
    }
    ref.invalidate(orderListProvider);
    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Thanh toán thành công'),
        content: const Text('Cảm ơn quý khách! Phiên ăn đã được thanh toán.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  /// Khách bấm "Hủy thanh toán" trên dialog QR/tiền mặt — trước đây nút này chỉ đóng
  /// dialog, payment vẫn PENDING và session vẫn khóa CHECKOUT tới khi PaymentExpiryJob
  /// tự dọn sau tối đa 30 phút. Gọi API cancel-intent để mở khóa ngay lập tức.
  Future<void> _cancelAndClose(int sessionId) async {
    Navigator.of(context).pop();
    try {
      await ref.read(orderRepositoryProvider).cancelPaymentIntent(sessionId);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể hủy ngay: ${extractErrorMessage(e)}')),
        );
      }
    } finally {
      ref.invalidate(orderListProvider);
    }
  }

  Future<void> _handlePayment(int sessionId) async {
    if (_isProcessingPayment) return;

    setState(() {
      _isProcessingPayment = true;
    });

    try {
      // Map selection to payment method:
      // 0: VNPAY (Ngân hàng/QR)
      // 1: CASH
      final method = _selectedPaymentMethod == 1 ? 'CASH' : 'VNPAY';

      final repo = ref.read(orderRepositoryProvider);
      final response = await repo.createPaymentIntent(sessionId, method);

      if (!mounted) return;

      _pendingInvoiceId = response.invoiceId;
      _isPaymentDialogOpen = true;

      if (method == 'CASH') {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (context) => AlertDialog(
            title: const Text('Yêu cầu thanh toán tiền mặt'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Mã hóa đơn: ${response.invoiceId}'),
                SizedBox(height: 8.h),
                Text('Số tiền: ${response.totalPayable.toStringAsFixed(0)}đ'),
                SizedBox(height: 16.h),
                const Text(
                  'Phiên ăn đã được khóa. Vui lòng đến quầy thu ngân và cung cấp số bàn hoặc mã hóa đơn. Nhân viên sẽ xác nhận thanh toán.',
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => _cancelAndClose(sessionId),
                child: const Text('Hủy thanh toán'),
              ),
            ],
          ),
        ).then((_) {
          _isPaymentDialogOpen = false;
          _pendingInvoiceId = null;
        });
      } else {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (context) => AlertDialog(
            title: const Text('Thanh toán VietQR'),
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
                        border: Border.all(color: _AppColors.surfaceVariant),
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      // PayOS trả về qrUrl là chuỗi EMV thô (dữ liệu VietQR gốc), không
                      // phải link ảnh — phải tự sinh ảnh QR ở client, không dùng
                      // Image.network (luôn lỗi vì đó không phải 1 URL hợp lệ).
                      child: SimpleQrView(
                        data: response.qrUrl!,
                        size: 200.r,
                        backgroundColor: _AppColors.surface,
                        foregroundColor: _AppColors.onSurface,
                      ),
                    ),
                  SizedBox(height: 16.h),
                  const Text(
                    'Quét mã QR bằng ứng dụng ngân hàng hoặc ví điện tử để hoàn tất thanh toán.',
                    textAlign: TextAlign.center,
                  ),
                  if (response.deeplink != null) ...[
                    SizedBox(height: 16.h),
                    ElevatedButton.icon(
                      onPressed: () async {
                        final uri = Uri.parse(response.deeplink!);
                        if (await canLaunchUrl(uri)) {
                          await launchUrl(uri, mode: LaunchMode.externalApplication);
                        } else {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('Không thể mở liên kết thanh toán')),
                          );
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
                onPressed: () => _cancelAndClose(sessionId),
                child: const Text('Hủy thanh toán'),
              ),
            ],
          ),
        ).then((_) {
          _isPaymentDialogOpen = false;
          _pendingInvoiceId = null;
        });
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi thanh toán: ${extractErrorMessage(e)}')),
      );
    } finally {
      if (mounted) {
        setState(() {
          _isProcessingPayment = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final tableNumber = authState.guestSession?.tableNumber ?? 1;
    final sessionId = authState.guestSession?.sessionId ?? 1;

    final ordersAsync = ref.watch(orderListProvider);
    final billingSettings = ref.watch(billingSettingsProvider).valueOrNull
        ?? const RestaurantBillingSettings();

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Hóa đơn tạm tính',
          style: TextStyle(
            color: _AppColors.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          Container(
            margin: EdgeInsets.only(right: 20.w),
            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
            decoration: BoxDecoration(
              color: _AppColors.surfaceVariant,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              'Bàn $tableNumber',
              style: TextStyle(
                color: _AppColors.onSurfaceVariant,
                fontSize: 12.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
      body: ordersAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, stack) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.error_outline, size: 48.sp, color: _AppColors.error),
              SizedBox(height: 16.h),
              Text(
                'Lỗi khi tải hóa đơn',
                style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold),
              ),
              SizedBox(height: 8.h),
              ElevatedButton(
                onPressed: () => ref.refresh(orderListProvider),
                child: const Text('Thử lại'),
              ),
            ],
          ),
        ),
        data: (orders) {
          final activeOrders = orders.where((o) => o.status != 'CANCELLED').toList();

          if (activeOrders.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.receipt_long, size: 64.sp, color: _AppColors.outline),
                  SizedBox(height: 16.h),
                  Text(
                    'Chưa có đơn hàng nào',
                    style: TextStyle(
                      color: _AppColors.onSurfaceVariant,
                      fontSize: 18.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  SizedBox(height: 8.h),
                  Text(
                    'Vui lòng gọi món từ thực đơn trước.',
                    style: TextStyle(color: _AppColors.secondary, fontSize: 14.sp),
                  ),
                ],
              ),
            );
          }

          // Gather all items
          final List<OrderDetailResponse> allItems = [];
          double subtotal = 0;
          double discount = 0;

          for (final order in activeOrders) {
            subtotal += order.totalAmount;
            discount += order.discountAmount;
            allItems.addAll(order.items);
          }

          // Group items by name + unitPrice to show consolidated quantity
          final Map<String, OrderDetailResponse> groupedItems = {};
          for (final item in allItems) {
            final key = '${item.name}_${item.unitPrice}';
            if (groupedItems.containsKey(key)) {
              final existing = groupedItems[key]!;
              groupedItems[key] = OrderDetailResponse(
                id: existing.id,
                menuItemId: existing.menuItemId,
                name: existing.name,
                unitPrice: existing.unitPrice,
                quantity: existing.quantity + item.quantity,
                total: existing.total + item.total,
                notes: (existing.notes != null && item.notes != null)
                    ? '${existing.notes}, ${item.notes}'
                    : (existing.notes ?? item.notes),
                status: existing.status,
              );
            } else {
              groupedItems[key] = item;
            }
          }
          final itemsList = groupedItems.values.toList();
          // VAT + phí DV lấy từ RestaurantSettings (Manager chỉnh trên dashboard)
          final taxRatePercent = billingSettings.taxRate;
          final serviceRatePercent = billingSettings.serviceChargeRate;
          final netAmount = subtotal - discount;
          final serviceFee = netAmount * serviceRatePercent / 100;
          final vat = netAmount * taxRatePercent / 100;
          final payableTotal = netAmount + serviceFee + vat;

          return SingleChildScrollView(
            padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 24.h),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Details Section
                Container(
                  padding: EdgeInsets.all(24.r),
                  decoration: BoxDecoration(
                    color: _AppColors.surfaceContainerLowest,
                    borderRadius: BorderRadius.circular(16.r),
                    border: Border.all(color: _AppColors.surfaceVariant),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.04),
                        blurRadius: 20,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Chi tiết món ăn',
                        style: TextStyle(
                          color: _AppColors.onSurface,
                          fontSize: 20.sp,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(height: 16.h),
                      ...itemsList.map((item) {
                        final isLast = itemsList.indexOf(item) == itemsList.length - 1;
                        return Container(
                          padding: EdgeInsets.only(bottom: 16.h),
                          margin: EdgeInsets.only(bottom: isLast ? 0 : 16.h),
                          decoration: BoxDecoration(
                            border: isLast
                                ? null
                                : const Border(bottom: BorderSide(color: _AppColors.surfaceVariant)),
                          ),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Row(
                                  children: [
                                    Container(
                                      width: 48.r,
                                      height: 48.r,
                                      decoration: BoxDecoration(
                                        color: _AppColors.surfaceContainerHigh,
                                        borderRadius: BorderRadius.circular(8.r),
                                      ),
                                      child: Icon(Icons.restaurant, color: _AppColors.outline, size: 24.sp),
                                    ),
                                    SizedBox(width: 16.w),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            item.name,
                                            maxLines: 1,
                                            overflow: TextOverflow.ellipsis,
                                            style: TextStyle(
                                              color: _AppColors.onSurface,
                                              fontSize: 16.sp,
                                              fontWeight: FontWeight.w600,
                                            ),
                                          ),
                                          SizedBox(height: 2.h),
                                          Text(
                                            'x${item.quantity}${item.notes != null && item.notes!.isNotEmpty ? ' • ${item.notes}' : ''}',
                                            style: TextStyle(
                                              color: _AppColors.secondary,
                                              fontSize: 14.sp,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              SizedBox(width: 8.w),
                              Text(
                                '${item.total.toStringAsFixed(0)}đ',
                                style: TextStyle(
                                  color: _AppColors.primary,
                                  fontSize: 18.sp,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                        );
                      }),
                    ],
                  ),
                ),
                SizedBox(height: 16.h),

                // Loyalty Rewards
                Container(
                  padding: EdgeInsets.all(16.r),
                  decoration: BoxDecoration(
                    color: _AppColors.primaryContainer,
                    borderRadius: BorderRadius.circular(16.r),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.stars, color: _AppColors.primaryFixed, size: 24.sp),
                      SizedBox(width: 12.w),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Loyalty Rewards',
                            style: TextStyle(
                              color: _AppColors.onPrimaryContainer.withOpacity(0.9),
                              fontSize: 12.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          Text(
                            'Điểm tích lũy nhận được: +${(payableTotal / 1000).round()} points',
                            style: TextStyle(
                              color: _AppColors.onPrimaryContainer,
                              fontSize: 14.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                SizedBox(height: 32.h),

                // Payment Methods
                Text(
                  'Phương thức thanh toán',
                  style: TextStyle(
                    color: _AppColors.onSurface,
                    fontSize: 20.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                SizedBox(height: 16.h),

                _buildPaymentOption(
                  index: 0,
                  title: 'Ngân Hàng/QR',
                  icon: Icons.qr_code_2,
                  iconBgColor: _AppColors.secondaryContainer,
                  iconColor: _AppColors.secondary,
                ),
                SizedBox(height: 12.h),
                _buildPaymentOption(
                  index: 1,
                  title: 'Tiền Mặt tại quầy',
                  icon: Icons.payments,
                  iconBgColor: _AppColors.surfaceContainerHighest,
                  iconColor: _AppColors.onSurfaceVariant,
                ),

                SizedBox(height: 32.h),

                // Summary
                Container(
                  padding: EdgeInsets.all(24.r),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(24.r),
                    border: Border.all(color: _AppColors.surfaceVariant),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.1),
                        blurRadius: 20,
                        offset: const Offset(0, 10),
                      ),
                    ],
                  ),
                  child: Column(
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('Tạm tính (${itemsList.length} món)',
                              style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                          Text('${subtotal.toStringAsFixed(0)}đ',
                              style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                        ],
                      ),
                      if (discount > 0) ...[
                        SizedBox(height: 12.h),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text('Giảm giá',
                                style: TextStyle(color: _AppColors.error, fontSize: 16.sp)),
                            Text('-${discount.toStringAsFixed(0)}đ',
                                style: TextStyle(color: _AppColors.error, fontSize: 16.sp)),
                          ],
                        ),
                      ],
                      SizedBox(height: 12.h),
                      if (serviceFee > 0) ...[
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              'Phí dịch vụ (${serviceRatePercent.toStringAsFixed(serviceRatePercent.truncateToDouble() == serviceRatePercent ? 0 : 1)}%)',
                              style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp),
                            ),
                            Text('${serviceFee.round()}đ',
                                style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                          ],
                        ),
                        SizedBox(height: 12.h),
                      ],
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            'Thuế (VAT ${taxRatePercent.toStringAsFixed(taxRatePercent.truncateToDouble() == taxRatePercent ? 0 : 1)}%)',
                            style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp),
                          ),
                          Text('${vat.round()}đ',
                              style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                        ],
                      ),
                      SizedBox(height: 12.h),
                      const Divider(color: _AppColors.surfaceVariant),
                      SizedBox(height: 12.h),
                      Text(
                        'Số tiền thanh toán',
                        style: TextStyle(color: _AppColors.secondary, fontSize: 14.sp),
                      ),
                      SizedBox(height: 4.h),
                      Text(
                        '${payableTotal.round()}đ',
                        key: const ValueKey('payable_total_with_vat'),
                        style: TextStyle(
                          color: _AppColors.onSurface,
                          fontSize: 26.sp,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      if (vat > 0 || serviceFee > 0) ...[
                        SizedBox(height: 4.h),
                        Text(
                          serviceFee > 0
                              ? '(đã gồm phí DV ${serviceFee.round()}đ + VAT ${vat.round()}đ)'
                              : '(đã gồm VAT ${vat.round()}đ)',
                          style: TextStyle(
                            color: _AppColors.onSurfaceVariant,
                            fontSize: 12.sp,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                SizedBox(height: 32.h),

                // Footer Warning
                Container(
                  padding: EdgeInsets.all(16.r),
                  decoration: BoxDecoration(
                    color: _AppColors.errorContainer,
                    borderRadius: BorderRadius.circular(16.r),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.info, color: _AppColors.error, size: 24.sp),
                      SizedBox(width: 12.w),
                      Expanded(
                        child: Text(
                          'Vui lòng đến quầy thu ngân. Sau khi nhân viên xác nhận thanh toán, bàn sẽ được dọn sạch trước khi nhận khách mới. Cảm ơn quý khách!',
                          style: TextStyle(
                            color: _AppColors.onErrorContainer,
                            fontSize: 14.sp,
                            height: 1.2,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

                // Bottom Padding
                SizedBox(height: 40.h),
              ],
            ),
          );
        },
      ),
      bottomNavigationBar: Container(
        padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w, 32.h),
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
        child: ordersAsync.maybeWhen(
          data: (orders) {
            final activeOrders = orders.where((o) => o.status != 'CANCELLED').toList();
            if (activeOrders.isEmpty) return const SizedBox.shrink();

            return ElevatedButton(
              onPressed: _isProcessingPayment ? null : () => _handlePayment(sessionId),
              style: ElevatedButton.styleFrom(
                backgroundColor: _AppColors.primary,
                foregroundColor: _AppColors.onPrimary,
                disabledBackgroundColor: _AppColors.outline,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
                padding: EdgeInsets.symmetric(vertical: 16.h),
                elevation: 4,
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  if (_isProcessingPayment)
                    const CircularProgressIndicator(color: Colors.white)
                  else ...[
                    const Icon(Icons.payment, size: 24),
                    SizedBox(width: 8.w),
                    Text(
                      _selectedPaymentMethod == 1
                          ? 'XÁC NHẬN & KHÓA PHIÊN'
                          : 'TIẾN HÀNH THANH TOÁN',
                      style: TextStyle(
                        fontSize: 18.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ],
              ),
            );
          },
          orElse: () => const SizedBox.shrink(),
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
  }) {
    final isSelected = _selectedPaymentMethod == index;

    return InkWell(
      onTap: () {
        setState(() {
          _selectedPaymentMethod = index;
        });
      },
      borderRadius: BorderRadius.circular(16.r),
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: _AppColors.surfaceContainerLowest,
          borderRadius: BorderRadius.circular(16.r),
          border: Border.all(
            color: isSelected ? _AppColors.primary : Colors.transparent,
            width: 2,
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.02),
              blurRadius: 10,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Row(
              children: [
                Container(
                  width: 40.r,
                  height: 40.r,
                  decoration: BoxDecoration(
                    color: iconBgColor,
                    borderRadius: BorderRadius.circular(12.r),
                  ),
                  child: Icon(icon, color: iconColor, size: 24.sp),
                ),
                SizedBox(width: 16.w),
                Text(
                  title,
                  style: TextStyle(
                    color: _AppColors.onSurface,
                    fontSize: 16.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
            Icon(
              isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
              color: isSelected ? _AppColors.primary : _AppColors.outline,
            ),
          ],
        ),
      ),
    );
  }
}
