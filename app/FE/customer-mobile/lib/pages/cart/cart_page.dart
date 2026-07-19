import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/cart_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import 'package:intl/intl.dart';


class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color surfaceContainerHigh = Color(0xFFeae7e7);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color outline = Color(0xFF8f7068);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color secondary = Color(0xFF685b5a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color tertiary = Color(0xFF005cac);
  static const Color tertiaryContainer = Color(0xFF0075d7);
  static const Color onTertiaryContainer = Color(0xFFfefcff);
  static const Color secondaryFixed = Color(0xFFf0dfdd);
  static const Color surfaceVariant = Color(0xFFe5e2e1);
  static const Color onPrimary = Color(0xFFffffff);
  static const Color onSecondary = Color(0xFFffffff);
}

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

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.onSurface),
          // Giỏ hàng luôn được vào qua tab bottom-nav (context.go, không push), nên
          // không có gì để pop — quay lại nghĩa là về Thực đơn.
          onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
        ),
        title: Row(
          children: [
            Icon(Icons.restaurant, color: _AppColors.primary, size: 24.sp),
            SizedBox(width: 8.w),
            Expanded(
              child: Text(
                'Giỏ hàng',
                maxLines: 1, // Giới hạn chỉ hiển thị trên 1 dòng
                overflow: TextOverflow.ellipsis, // Tự động cắt bằng dấu ... nếu quá dài
                style: TextStyle(
                  color: _AppColors.primary,
                  fontSize: 20.sp,
                  fontWeight: FontWeight.bold,
                ),
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
              borderRadius: BorderRadius.circular(12.r),
            ),
            child: Text(
              'Bàn $tableNumber',
              style: TextStyle(
                color: _AppColors.onPrimaryContainer,
                fontSize: 12.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
      body: items.isEmpty
          ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.shopping_cart_outlined, size: 80.sp, color: _AppColors.outlineVariant),
                  SizedBox(height: 24.h),
                  Text(
                    'Giỏ hàng trống',
                    style: TextStyle(
                      color: _AppColors.onSurface,
                      fontSize: 22.sp,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  SizedBox(height: 8.h),
                  Text(
                    'Thêm món từ thực đơn để bắt đầu',
                    style: TextStyle(color: _AppColors.secondary, fontSize: 16.sp),
                  ),
                  SizedBox(height: 32.h),
                  ElevatedButton(
                    onPressed: () => context.go('/home'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: _AppColors.primary,
                      foregroundColor: _AppColors.onPrimary,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                      padding: EdgeInsets.symmetric(horizontal: 32.w, vertical: 16.h),
                    ),
                    child: Text('Xem thực đơn', style: TextStyle(fontSize: 16.sp, fontWeight: FontWeight.bold)),
                  ),
                ],
              ),
            )
          : SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 24.h),
          child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Item List
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Món đã chọn',
                  style: TextStyle(
                    color: _AppColors.onSurface,
                    fontSize: 20.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                Text(
                  '${items.length} món trong giỏ',
                  style: TextStyle(
                    color: _AppColors.secondary,
                    fontSize: 14.sp,
                  ),
                ),
              ],
            ),
            SizedBox(height: 16.h),
            
            ListView.separated(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: items.length,
              separatorBuilder: (context, index) => SizedBox(height: 16.h),
              itemBuilder: (context, index) {
                final item = items[index];
                return Container(
                  padding: EdgeInsets.all(16.r),
                  decoration: BoxDecoration(
                    color: _AppColors.surfaceContainerLowest,
                    borderRadius: BorderRadius.circular(16.r),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.04),
                        blurRadius: 20,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: Row(
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8.r),
                        child: item.menuItem.imageUrl != null
                            ? Image.network(
                                item.menuItem.imageUrl!,
                                width: 96.r,
                                height: 96.r,
                                fit: BoxFit.cover,
                              )
                            : Container(
                                width: 96.r,
                                height: 96.r,
                                color: _AppColors.surfaceContainerHigh,
                                child: Icon(Icons.restaurant, color: _AppColors.outline),
                              ),
                      ),
                      SizedBox(width: 16.w),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Expanded(
                                  child: Text(
                                    item.menuItem.name,
                                    style: TextStyle(
                                      color: _AppColors.onSurface,
                                      fontSize: 20.sp,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ),
                                Text(
                                  '${currencyFormat.format(item.menuItem.price)}VND',
                                  style: TextStyle(
                                    color: _AppColors.primary,
                                    fontSize: 18.sp,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                SizedBox(width: 8.w),
                                GestureDetector(
                                  onTap: () => ref
                                      .read(cartViewModelProvider.notifier)
                                      .removeItem(item.menuItem.id),
                                  child: Icon(
                                    Icons.delete_outline,
                                    size: 20.sp,
                                    color: _AppColors.outline,
                                  ),
                                ),
                              ],
                            ),
                            SizedBox(height: 4.h),
                            Text(
                              item.notes ?? '',
                              style: TextStyle(
                                color: _AppColors.secondary,
                                fontSize: 14.sp,
                                fontStyle: FontStyle.italic,
                              ),
                            ),
                            SizedBox(height: 12.h),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.end,
                              children: [
                                Container(
                                  padding: EdgeInsets.all(4.r),
                                  decoration: BoxDecoration(
                                    color: _AppColors.surfaceContainerHigh,
                                    borderRadius: BorderRadius.circular(100.r),
                                  ),
                                  child: Row(
                                    children: [
                                      GestureDetector(
                                        onTap: () => ref
                                            .read(cartViewModelProvider.notifier)
                                            .decrementQuantity(item.menuItem.id),
                                        child: Container(
                                          width: 32.r,
                                          height: 32.r,
                                          decoration: const BoxDecoration(
                                            shape: BoxShape.circle,
                                          ),
                                          child: Icon(Icons.remove, size: 20.sp, color: _AppColors.onSurface),
                                        ),
                                      ),
                                      SizedBox(
                                        width: 32.w,
                                        child: Text(
                                          item.quantity.toString(),
                                          textAlign: TextAlign.center,
                                          style: TextStyle(
                                            color: _AppColors.onSurface,
                                            fontSize: 18.sp,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                      GestureDetector(
                                        onTap: () => ref
                                            .read(cartViewModelProvider.notifier)
                                            .incrementQuantity(item.menuItem.id),
                                        child: Container(
                                          width: 32.r,
                                          height: 32.r,
                                          decoration: BoxDecoration(
                                            color: _AppColors.primary,
                                            shape: BoxShape.circle,
                                            boxShadow: [
                                              BoxShadow(
                                                color: Colors.black.withOpacity(0.1),
                                                blurRadius: 4,
                                              ),
                                            ],
                                          ),
                                          child: Icon(Icons.add, size: 20.sp, color: _AppColors.onPrimary),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
            SizedBox(height: 24.h),

            // Coupon Section
            Container(
              padding: EdgeInsets.all(16.r),
              decoration: BoxDecoration(
                color: _AppColors.surfaceContainerLowest,
                borderRadius: BorderRadius.circular(16.r),
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
                    'Mã giảm giá',
                    style: TextStyle(
                      color: _AppColors.onSurface,
                      fontSize: 20.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  SizedBox(height: 12.h),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _couponController,
                          textCapitalization: TextCapitalization.characters,
                          decoration: InputDecoration(
                            hintText: 'Nhập mã ưu đãi',
                            hintStyle: TextStyle(
                              color: _AppColors.outline,
                              fontSize: 16.sp,
                            ),
                            prefixIcon: Icon(Icons.confirmation_number_outlined, color: _AppColors.outline),
                            filled: true,
                            fillColor: _AppColors.surface,
                            contentPadding: EdgeInsets.symmetric(vertical: 12.h, horizontal: 16.w),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12.r),
                              borderSide: const BorderSide(color: _AppColors.outlineVariant),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12.r),
                              borderSide: const BorderSide(color: _AppColors.primary, width: 2),
                            ),
                          ),
                        ),
                      ),
                      SizedBox(width: 12.w),
                      ElevatedButton(
                        onPressed: () {
                          final code = _couponController.text.trim();
                          setState(() => _appliedCoupon = code.isEmpty ? null : code);
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(
                                code.isEmpty
                                    ? 'Đã bỏ áp dụng mã giảm giá'
                                    : 'Mã "$code" sẽ được áp dụng khi gửi đơn đặt món',
                              ),
                            ),
                          );
                        },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: _AppColors.secondary,
                          foregroundColor: _AppColors.onSecondary,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12.r),
                          ),
                          padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 14.h),
                          elevation: 0,
                        ),
                        child: Text(
                          'Áp dụng',
                          style: TextStyle(
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

            // Summary Section
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: _AppColors.surfaceContainerLowest,
                borderRadius: BorderRadius.circular(16.r),
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
                    children: [
                      Text('Tạm tính (${items.length} món)', style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                      Text('${cartState.total}k', style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                    ],
                  ),
                  SizedBox(height: 16.h),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Giảm giá', style: TextStyle(color: _AppColors.onSecondaryContainer, fontSize: 16.sp)),
                      Text('0k', style: TextStyle(color: _AppColors.onSecondaryContainer, fontSize: 16.sp)),
                    ],
                  ),
                  SizedBox(height: 16.h),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Thuế (VAT 8%)', style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                      Text('${(cartState.total * 0.08).toStringAsFixed(1)}k', style: TextStyle(color: _AppColors.onSurfaceVariant, fontSize: 16.sp)),
                    ],
                  ),
                  SizedBox(height: 16.h),
                  const Divider(color: _AppColors.surfaceVariant),
                  SizedBox(height: 16.h),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Tổng cộng', style: TextStyle(color: _AppColors.onSurface, fontSize: 20.sp, fontWeight: FontWeight.w600)),
                      Text('${(cartState.total * 1.08).toStringAsFixed(1)}k', style: TextStyle(color: _AppColors.primary, fontSize: 32.sp, fontWeight: FontWeight.bold)),
                    ],
                  ),
                ],
              ),
            ),
            // Padding for Bottom Action Bar
            SizedBox(height: 40.h),
          ],
        ),
      ),
      bottomNavigationBar: Container(
        padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w, 32.h), // Extra padding for safe area / modern devices
        decoration: BoxDecoration(
          color: _AppColors.surface,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.08),
              blurRadius: 20,
              offset: const Offset(0, -4),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ElevatedButton(
              onPressed: () async {
                // Submit order to API
                final orderId = await ref
                    .read(cartViewModelProvider.notifier)
                    .checkout(tableId, sessionId, couponCode: _appliedCoupon);
                if (orderId != null && mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Đặt món thành công!')),
                  );
                  context.push('/checkout', extra: orderId);
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(cartState.error ?? 'Lỗi khi đặt món')),
                  );
                }
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: _AppColors.primary,
                foregroundColor: _AppColors.onPrimary,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
                padding: EdgeInsets.symmetric(vertical: 24.h),
                elevation: 4,
                shadowColor: _AppColors.primaryContainer.withOpacity(0.3),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  if (cartState.isSubmitting)
                    const CircularProgressIndicator(color: Colors.white)
                  else ...[
                    Icon(Icons.send, size: 24.sp),
                    SizedBox(width: 16.w),
                    Text(
                      'GỬI ĐƠN ĐẶT MÓN',
                      style: TextStyle(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ]
                ],
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              'Đơn hàng sẽ được gửi trực tiếp đến quầy chế biến',
              style: TextStyle(
                color: _AppColors.secondary,
                fontSize: 14.sp,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
