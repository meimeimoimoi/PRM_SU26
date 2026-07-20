import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/cart_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/payment_lock_provider.dart';
import 'package:intl/intl.dart';
import '../../theme/app_theme.dart';

class CartPage extends ConsumerStatefulWidget {
  const CartPage({super.key});

  @override
  ConsumerState<CartPage> createState() => _CartPageState();
}

class _CartPageState extends ConsumerState<CartPage> {
  final TextEditingController _couponController = TextEditingController();
  final currencyFormat = NumberFormat('#,###', 'vi_VN');
  String? _appliedCoupon;

  @override
  void dispose() {
    _couponController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final tableNumber = authState.guestSession?.tableNumber ?? 1;
    final tableId = authState.guestSession?.tableId ?? 1;
    final sessionId = authState.guestSession?.sessionId ?? 1;

    final cartState = ref.watch(cartViewModelProvider);
    final items = cartState.items;
    final checkoutLocked = ref.watch(sessionCheckoutLockedProvider);

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: AppTheme.onSurface),
          onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
        ),
        title: Text(
          'Giỏ hàng',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: AppTheme.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          Padding(
            padding: EdgeInsets.only(right: 16.w),
            child: Center(
              child: Container(
                padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 6.h),
                decoration: BoxDecoration(
                  color: AppTheme.primaryContainer,
                  borderRadius: BorderRadius.circular(12.r),
                ),
                child: Text(
                  'Bàn $tableNumber',
                  style: TextStyle(
                    color: AppTheme.onPrimaryContainer,
                    fontSize: 12.sp,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
      body: items.isEmpty
          ? Center(
              child: Padding(
                padding: EdgeInsets.symmetric(horizontal: 24.w),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.shopping_cart_outlined, size: 80.sp, color: AppTheme.outlineVariant),
                    SizedBox(height: 24.h),
                    Text(
                      'Giỏ hàng trống',
                      style: TextStyle(
                        color: AppTheme.onSurface,
                        fontSize: 22.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(height: 8.h),
                    Text(
                      'Thêm món từ thực đơn để bắt đầu',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 16.sp),
                    ),
                    SizedBox(height: 32.h),
                    ElevatedButton(
                      onPressed: () => context.go('/home'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppTheme.primary,
                        foregroundColor: AppTheme.onPrimary,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                        padding: EdgeInsets.symmetric(horizontal: 32.w, vertical: 16.h),
                      ),
                      child: Text('Xem thực đơn', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold)),
                    ),
                  ],
                ),
              ),
            )
          : ListView(
              padding: EdgeInsets.fromLTRB(20.w, 24.h, 20.w, 24.h),
              children: [
                if (checkoutLocked) ...[
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 12.h),
                    decoration: BoxDecoration(
                      color: AppTheme.errorContainer,
                      borderRadius: BorderRadius.circular(12.r),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.lock_outline, color: AppTheme.error, size: 20.sp),
                        SizedBox(width: 10.w),
                        Expanded(
                          child: Text(
                            'Bàn đang khóa thanh toán. Không thể đặt món thêm.',
                            style: TextStyle(
                              color: AppTheme.error,
                              fontSize: 13.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(height: 16.h),
                ],
                for (var i = 0; i < items.length; i++) ...[
                  if (i > 0) SizedBox(height: 16.h),
                  _buildCartItem(items[i]),
                ],
                SizedBox(height: 24.h),
                _buildCouponSection(),
                SizedBox(height: 24.h),
                _buildSummarySection(cartState, items),
                SizedBox(height: 24.h),
              ],
            ),
      bottomNavigationBar: SafeArea(
        child: Container(
          padding: EdgeInsets.fromLTRB(20.w, 12.h, 20.w, 16.h),
          decoration: BoxDecoration(
            color: AppTheme.surface,
            boxShadow: AppTheme.shadowBottomNav,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: items.isEmpty || cartState.isSubmitting || checkoutLocked
                      ? null
                      : () async {
                          final orderId = await ref
                              .read(cartViewModelProvider.notifier)
                              .checkout(tableId, sessionId, couponCode: _appliedCoupon);
                          if (!mounted) return;
                          if (orderId != null) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Đặt món thành công!')),
                            );
                            context.push('/checkout', extra: orderId);
                          } else {
                            final err = ref.read(cartViewModelProvider).error;
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(content: Text(err ?? 'Lỗi khi đặt món')),
                            );
                          }
                        },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.primary,
                    foregroundColor: AppTheme.onPrimary,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12.r),
                    ),
                    padding: EdgeInsets.symmetric(vertical: 16.h),
                    elevation: 4,
                    shadowColor: AppTheme.primary.withOpacity(0.3),
                  ),
                  child: cartState.isSubmitting
                      ? SizedBox(
                          width: 22.r,
                          height: 22.r,
                          child: const CircularProgressIndicator(
                            strokeWidth: 2,
                            color: AppTheme.onPrimary,
                          ),
                        )
                      : Text(
                          checkoutLocked ? 'BÀN ĐANG KHÓA THANH TOÁN' : 'GỬI ĐƠN ĐẶT MÓN',
                          style: TextStyle(
                            fontSize: 16.sp,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                ),
              ),
              SizedBox(height: 8.h),
              Text(
                'Đơn hàng sẽ được gửi trực tiếp đến quầy chế biến',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: AppTheme.onSurfaceVariant,
                  fontSize: 12.sp,
                  fontStyle: FontStyle.italic,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildCartItem(CartItem item) {
    return Container(
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: AppTheme.shadowCard,
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(8.r),
            child: item.menuItem.imageUrl != null && item.menuItem.imageUrl!.isNotEmpty
                ? Image.network(
                    item.menuItem.imageUrl!,
                    width: 80.r,
                    height: 80.r,
                    fit: BoxFit.cover,
                    errorBuilder: (_, __, ___) => _imageFallback(),
                  )
                : _imageFallback(),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        item.menuItem.name,
                        style: TextStyle(
                          color: AppTheme.onSurface,
                          fontSize: 16.sp,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                    SizedBox(width: 8.w),
                    Text(
                      '${currencyFormat.format(item.menuItem.price)}đ',
                      style: TextStyle(
                        color: AppTheme.primary,
                        fontSize: 14.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    SizedBox(width: 4.w),
                    InkWell(
                      onTap: () => ref
                          .read(cartViewModelProvider.notifier)
                          .removeItem(item.menuItem.id),
                      child: Icon(Icons.delete_outline, size: 20.sp, color: AppTheme.outline),
                    ),
                  ],
                ),
                if (item.notes != null && item.notes!.isNotEmpty) ...[
                  SizedBox(height: 4.h),
                  Text(
                    item.notes!,
                    style: TextStyle(
                      color: AppTheme.onSurfaceVariant,
                      fontSize: 12.sp,
                      fontStyle: FontStyle.italic,
                    ),
                  ),
                ],
                SizedBox(height: 12.h),
                Align(
                  alignment: Alignment.centerRight,
                  child: Container(
                    padding: EdgeInsets.all(4.r),
                    decoration: BoxDecoration(
                      color: AppTheme.surfaceContainerHigh,
                      borderRadius: BorderRadius.circular(100.r),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        _qtyButton(
                          Icons.remove,
                          () => ref
                              .read(cartViewModelProvider.notifier)
                              .decrementQuantity(item.menuItem.id),
                        ),
                        SizedBox(
                          width: 28.w,
                          child: Text(
                            item.quantity.toString(),
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              color: AppTheme.onSurface,
                              fontSize: 16.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                        _qtyButton(
                          Icons.add,
                          () => ref
                              .read(cartViewModelProvider.notifier)
                              .incrementQuantity(item.menuItem.id),
                          filled: true,
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _imageFallback() {
    return Container(
      width: 80.r,
      height: 80.r,
      color: AppTheme.surfaceContainerHigh,
      child: Icon(Icons.restaurant, color: AppTheme.outline),
    );
  }

  Widget _qtyButton(IconData icon, VoidCallback onTap, {bool filled = false}) {
    return InkWell(
      onTap: onTap,
      customBorder: const CircleBorder(),
      child: Container(
        width: 28.r,
        height: 28.r,
        decoration: BoxDecoration(
          color: filled ? AppTheme.primary : null,
          shape: BoxShape.circle,
        ),
        child: Icon(
          icon,
          size: 18.sp,
          color: filled ? AppTheme.onPrimary : AppTheme.onSurface,
        ),
      ),
    );
  }

  Widget _buildCouponSection() {
    return InkWell(
      onTap: () {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Mã giảm giá đang được phát triển, sẽ sớm ra mắt!'),
          ),
        );
      },
      borderRadius: BorderRadius.circular(16.r),
      child: Container(
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
            'Mã giảm giá',
            style: TextStyle(
              color: AppTheme.onSurface,
              fontSize: 16.sp,
              fontWeight: FontWeight.w600,
            ),
          ),
          SizedBox(height: 12.h),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _couponController,
                  enabled: false,
                  textCapitalization: TextCapitalization.characters,
                  decoration: InputDecoration(
                    hintText: 'Sắp ra mắt',
                    isDense: true,
                    prefixIcon: Icon(Icons.confirmation_number_outlined, color: AppTheme.outline),
                  ),
                ),
              ),
              SizedBox(width: 12.w),
              OutlinedButton(
                onPressed: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Mã giảm giá đang được phát triển, sẽ sớm ra mắt!'),
                    ),
                  );
                },
                style: OutlinedButton.styleFrom(
                  // Theme mặc định minimumSize width=Infinity → crash trong Row
                  minimumSize: Size(0, 48.h),
                  tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12.r),
                  ),
                ),
                child: Text(
                  'Áp dụng',
                  style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w600),
                ),
              ),
            ],
          ),
        ],
      ),
    ),
    );
  }

  Widget _buildSummarySection(CartState cartState, List<CartItem> items) {
    final subtotal = cartState.total;
    final vat = subtotal * 0.08;
    final grand = subtotal + vat;

    return Container(
      padding: EdgeInsets.all(20.r),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: AppTheme.shadowCard,
      ),
      child: Column(
        children: [
          _summaryRow('Tạm tính (${items.length} món)', '${currencyFormat.format(subtotal)}đ'),
          SizedBox(height: 12.h),
          _summaryRow('Giảm giá', '0đ', valueColor: AppTheme.success),
          SizedBox(height: 12.h),
          _summaryRow('Thuế (VAT 8%)', '${currencyFormat.format(vat)}đ'),
          SizedBox(height: 12.h),
          Divider(color: AppTheme.outlineVariant),
          SizedBox(height: 12.h),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Tổng cộng',
                style: TextStyle(
                  color: AppTheme.onSurface,
                  fontSize: 16.sp,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Flexible(
                child: Text(
                  '${currencyFormat.format(grand)}đ',
                  textAlign: TextAlign.right,
                  style: TextStyle(
                    color: AppTheme.primary,
                    fontSize: 22.sp,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _summaryRow(String label, String value, {Color? valueColor}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Flexible(
          child: Text(
            label,
            style: TextStyle(color: AppTheme.onSurfaceVariant, fontSize: 14.sp),
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: valueColor ?? AppTheme.onSurfaceVariant,
            fontSize: 14.sp,
          ),
        ),
      ],
    );
  }
}
