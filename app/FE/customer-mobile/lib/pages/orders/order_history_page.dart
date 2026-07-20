import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../widgets/simple_qr_view.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/order_viewmodel.dart';
import '../../services/order_repository.dart';
import '../../services/settings_repository.dart';
import '../../services/socket/socket_service.dart';
import '../../models/order_models.dart';
import '../../utils/error_utils.dart';
import '../../viewmodels/payment_lock_provider.dart';
import '../../theme/app_theme.dart';

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
    _socketService.subscribeToEvent('ReceivePaymentSuccess', _onPaymentSuccess);
    if (tableId != null) {
      // ignore: unawaited_futures
      _socketService.connect(tableId);
    }
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

    if (_isPaymentDialogOpen) {
      Navigator.of(context, rootNavigator: true).pop();
      _isPaymentDialogOpen = false;
    }
    _pendingInvoiceId = null;
    ref.read(sessionCheckoutLockedProvider.notifier).state = false;
    ref.invalidate(orderListProvider);
    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: Text('Thanh toán thành công', style: TextStyle(color: AppTheme.onSurface)),
        content: Text('Cảm ơn quý khách! Phiên ăn đã được thanh toán.', style: TextStyle(color: AppTheme.onSurfaceVariant)),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: Text('OK', style: TextStyle(color: AppTheme.primary)),
          ),
        ],
        backgroundColor: AppTheme.surface,
      ),
    );
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
      // Session chuyển CHECKOUT trên BE — khóa UI đặt món ngay. Không cho hủy.
      ref.read(sessionCheckoutLockedProvider.notifier).state = true;

      if (method == 'CASH') {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (context) => AlertDialog(
            title: Text('Yêu cầu thanh toán tiền mặt', style: TextStyle(color: AppTheme.onSurface)),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Mã hóa đơn: ${response.invoiceId}'),
                SizedBox(height: 8.h),
                Text('Số tiền: ${response.totalPayable.toStringAsFixed(0)}đ'),
                SizedBox(height: 16.h),
                Text(
                  'Phiên ăn đã được khóa và không thể hủy thanh toán. Vui lòng đến quầy thu ngân và cung cấp số bàn hoặc mã hóa đơn. Nhân viên sẽ xác nhận thanh toán.',
                  style: TextStyle(color: AppTheme.onSurfaceVariant),
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: Text('Đã hiểu', style: TextStyle(color: AppTheme.primary)),
              ),
            ],
            backgroundColor: AppTheme.surface,
          ),
        ).then((_) {
          _isPaymentDialogOpen = false;
        });
      } else {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (context) => AlertDialog(
            title: Text('Thanh toán VietQR', style: TextStyle(color: AppTheme.onSurface)),
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
                      color: AppTheme.primary,
                    ),
                  ),
                  SizedBox(height: 16.h),
                  if (response.qrUrl != null)
                    Container(
                      padding: EdgeInsets.all(8.r),
                      decoration: BoxDecoration(
                        border: Border.all(color: AppTheme.outlineVariant),
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      child: SimpleQrView(
                        data: response.qrUrl!,
                        size: 200.r,
                        backgroundColor: AppTheme.surface,
                        foregroundColor: AppTheme.onSurface,
                      ),
                    ),
                  SizedBox(height: 16.h),
                  Text(
                    'Quét mã QR bằng ứng dụng ngân hàng hoặc ví điện tử để hoàn tất thanh toán. Không thể hủy sau khi tạo yêu cầu.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: AppTheme.onSurfaceVariant),
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
                        backgroundColor: AppTheme.primary,
                        foregroundColor: AppTheme.onPrimary,
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
                onPressed: () => Navigator.of(context).pop(),
                child: Text('Đã hiểu', style: TextStyle(color: AppTheme.primary)),
              ),
            ],
            backgroundColor: AppTheme.surface,
          ),
        ).then((_) {
          _isPaymentDialogOpen = false;
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
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: AppTheme.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Hóa đơn tạm tính',
          style: TextStyle(
            color: AppTheme.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          Container(
            margin: EdgeInsets.only(right: 20.w),
            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
            decoration: BoxDecoration(
              color: AppTheme.surfaceContainerHigh,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              'Bàn $tableNumber',
              style: TextStyle(
                color: AppTheme.onSurfaceVariant,
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
              Icon(Icons.error_outline, size: 48.sp, color: AppTheme.error),
              SizedBox(height: 16.h),
              Text(
                'Lỗi khi tải hóa đơn',
                style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold, color: AppTheme.onSurface),
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
                  Icon(Icons.receipt_long, size: 64.sp, color: AppTheme.outline),
                  SizedBox(height: 16.h),
                  Text(
                    'Chưa có đơn hàng nào',
                    style: TextStyle(
                      color: AppTheme.onSurfaceVariant,
                      fontSize: 18.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  SizedBox(height: 8.h),
                  Text(
                    'Vui lòng gọi món từ thực đơn trước.',
                    style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp),
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
          // VAT + phí DV: snapshot phiên (fallback settings live nếu phiên cũ)
          OrderResponse? snap;
          for (final o in activeOrders) {
            if (o.taxRate != null || o.serviceChargeRate != null) {
              snap = o;
              break;
            }
          }
          final taxRatePercent = snap?.taxRate ?? billingSettings.taxRate;
          final serviceRatePercent = snap?.serviceChargeRate ?? billingSettings.serviceChargeRate;
          final netAmount = subtotal - discount;
          final serviceFee = netAmount * serviceRatePercent / 100;
          final vat = netAmount * taxRatePercent / 100;
          final payableTotal = netAmount + serviceFee + vat;

          final currencyFormat = NumberFormat('#,###', 'vi_VN');

          return SingleChildScrollView(
            padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 24.h),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Details Section
                Container(
                  padding: EdgeInsets.all(24.r),
                  decoration: BoxDecoration(
                    color: AppTheme.surface,
                    borderRadius: BorderRadius.circular(16.r),
                    border: Border.all(color: AppTheme.outlineVariant),
                    boxShadow: AppTheme.shadowCard,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Chi tiết món ăn',
                        style: TextStyle(
                          color: AppTheme.onSurface,
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
                                : Border(bottom: BorderSide(color: AppTheme.outlineVariant.withOpacity(0.3))),
                          ),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      item.name,
                                      style: TextStyle(
                                        color: AppTheme.onSurface,
                                        fontSize: 16.sp,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                    if (item.notes != null && item.notes!.isNotEmpty) ...[
                                      SizedBox(height: 2.h),
                                      Text(
                                        item.notes!,
                                        style: TextStyle(
                                          color: AppTheme.onSurfaceVariant,
                                          fontSize: 12.sp,
                                          fontStyle: FontStyle.italic,
                                        ),
                                      ),
                                    ],
                                  ],
                                ),
                              ),
                              Column(
                                crossAxisAlignment: CrossAxisAlignment.end,
                                children: [
                                  Text(
                                    '${currencyFormat.format(item.unitPrice)}đ',
                                    style: TextStyle(
                                      color: AppTheme.primary,
                                      fontSize: 16.sp,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                  SizedBox(height: 4.h),
                                  Text(
                                    'x${item.quantity}',
                                    style: TextStyle(
                                      color: AppTheme.onSurfaceVariant,
                                      fontSize: 14.sp,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        );
                      }),
                    ],
                  ),
                ),
                SizedBox(height: 24.h),

                // Payment Method Selection
                Container(
                  padding: EdgeInsets.all(16.r),
                  decoration: BoxDecoration(
                    color: AppTheme.surface,
                    borderRadius: BorderRadius.circular(16.r),
                    boxShadow: AppTheme.shadowCard,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Phương thức thanh toán',
                        style: TextStyle(
                          color: AppTheme.onSurface,
                          fontSize: 20.sp,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(height: 12.h),
                      ...[
                        {'method': 'VNPAY', 'icon': Icons.qr_code, 'desc': 'Quét mã QR ngân hàng/ví điện tử'},
                        {'method': 'CASH', 'icon': Icons.payments, 'desc': 'Thanh toán tiền mặt tại quầy'},
                      ].map((pm) {
                        final isSelected = (_selectedPaymentMethod == 0 && pm['method'] == 'VNPAY') ||
                            (_selectedPaymentMethod == 1 && pm['method'] == 'CASH');
                        return InkWell(
                          onTap: () {
                            setState(() {
                              _selectedPaymentMethod = pm['method'] == 'VNPAY' ? 0 : 1;
                            });
                          },
                          borderRadius: BorderRadius.circular(12.r),
                          child: Container(
                            padding: EdgeInsets.all(16.r),
                            margin: EdgeInsets.only(bottom: 8.h),
                            decoration: BoxDecoration(
                              color: isSelected ? AppTheme.primaryContainer : AppTheme.surfaceContainerHigh,
                              borderRadius: BorderRadius.circular(12.r),
                              border: Border.all(
                                color: isSelected ? AppTheme.primary : AppTheme.outlineVariant,
                                width: isSelected ? 2 : 1,
                              ),
                            ),
                            child: Row(
                              children: [
                                Container(
                                  width: 40.r,
                                  height: 40.r,
                                  decoration: BoxDecoration(
                                    color: isSelected ? AppTheme.primary : AppTheme.secondaryContainer,
                                    shape: BoxShape.circle,
                                  ),
                                  child: Icon(
                                    pm['icon'] as IconData,
                                    color: isSelected ? AppTheme.onPrimary : AppTheme.onSecondaryContainer,
                                    size: 20.sp,
                                  ),
                                ),
                                SizedBox(width: 16.w),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        pm['method'] as String,
                                        style: TextStyle(
                                          color: AppTheme.onSurface,
                                          fontSize: 16.sp,
                                          fontWeight: FontWeight.w600,
                                        ),
                                      ),
                                      SizedBox(height: 2.h),
                                      Text(
                                        pm['desc'] as String,
                                        style: TextStyle(
                                          color: AppTheme.onSurfaceVariant,
                                          fontSize: 12.sp,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                if (isSelected)
                                  Icon(Icons.check_circle, color: AppTheme.primary, size: 24.sp),
                              ],
                            ),
                          ),
                        );
                      }).toList(),
                    ],
                  ),
                ),
                SizedBox(height: 24.h),

                // Summary Section
                Container(
                  padding: EdgeInsets.all(24.r),
                  decoration: BoxDecoration(
                    color: AppTheme.surface,
                    borderRadius: BorderRadius.circular(16.r),
                    boxShadow: AppTheme.shadowCard,
                  ),
                  child: Column(
                    children: [
                      _buildSummaryRow('Tạm tính', '${currencyFormat.format(subtotal)}đ', AppTheme.onSurfaceVariant, AppTheme.onSurfaceVariant, 16.sp),
                      if (discount > 0)
                        _buildSummaryRow('Giảm giá', '-${currencyFormat.format(discount)}đ', AppTheme.success, AppTheme.success, 16.sp),
                      _buildSummaryRow('Phí dịch vụ (${serviceRatePercent.toStringAsFixed(0)}%)', '${currencyFormat.format(serviceFee)}đ', AppTheme.onSurfaceVariant, AppTheme.onSurfaceVariant, 16.sp),
                      _buildSummaryRow('VAT (${taxRatePercent.toStringAsFixed(0)}%)', '${currencyFormat.format(vat)}đ', AppTheme.onSurfaceVariant, AppTheme.onSurfaceVariant, 16.sp),
                      Divider(color: AppTheme.outlineVariant),
                      _buildSummaryRow('Tổng cộng', '${currencyFormat.format(payableTotal)}đ', AppTheme.primary, AppTheme.primary, 24.sp, bold: true),
                    ],
                  ),
                ),
                SizedBox(height: 24.h),

                // Action Button
                ElevatedButton(
                  onPressed: _isProcessingPayment ? null : () => _handlePayment(sessionId),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.primary,
                    foregroundColor: AppTheme.onPrimary,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                    padding: EdgeInsets.symmetric(vertical: 20.h),
                    elevation: 4,
                    shadowColor: AppTheme.primary.withOpacity(0.3),
                  ),
                  child: _isProcessingPayment
                      ? const CircularProgressIndicator(color: AppTheme.onPrimary)
                      : Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.payment, size: 24.sp),
                            SizedBox(width: 12.w),
                            Text(
                              'THANH TOÁN ${currencyFormat.format(payableTotal)}đ',
                              style: TextStyle(
                                fontSize: 18.sp,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildSummaryRow(String label, String value, Color labelColor, Color valueColor, double fontSize, {bool bold = false}) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 8.h),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              color: labelColor,
              fontSize: fontSize,
              fontWeight: bold ? FontWeight.bold : FontWeight.normal,
            ),
          ),
          Text(
            value,
            style: TextStyle(
              color: valueColor,
              fontSize: fontSize,
              fontWeight: bold ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        ],
      ),
    );
  }
}