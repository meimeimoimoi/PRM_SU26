import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/cart_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';


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
          onPressed: () => context.pop(),
        ),
        title: Row(
          children: [
            Icon(Icons.restaurant, color: _AppColors.primary, size: 24.sp),
            SizedBox(width: 8.w),
            Text(
              'Giỏ hàng bàn số $tableNumber',
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
      body: SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 24.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Participants Widget
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
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Thành viên cùng bàn',
                        style: TextStyle(
                          color: _AppColors.onSurface,
                          fontSize: 20.sp,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
                        decoration: BoxDecoration(
                          color: _AppColors.tertiaryContainer,
                          borderRadius: BorderRadius.circular(100.r),
                        ),
                        child: Text(
                          '2 Đang chờ',
                          style: TextStyle(
                            color: _AppColors.onTertiaryContainer,
                            fontSize: 12.sp,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 16.h),
                  Row(
                    children: [
                      SizedBox(
                        width: 70.w,
                        height: 40.r,
                        child: Stack(
                          children: [
                            Positioned(
                              left: 0,
                              child: Container(
                                width: 40.r,
                                height: 40.r,
                                decoration: BoxDecoration(
                                  shape: BoxShape.circle,
                                  border: Border.all(color: _AppColors.surface, width: 2),
                                  image: const DecorationImage(
                                    image: NetworkImage('https://lh3.googleusercontent.com/aida-public/AB6AXuD2cqWJtrn1AhSQzwqG-YMEBB91R4Y708Hen-QNuo-y1EW0FRdgOQOOY0k6L2xtLwv3TgbOTqSCfweHmlfXtF26bxfJPHDs9aGh2G0rubsII8riIyXXmJpbRjPVBiq0Gas0ZFiV-8UpV-AmpxikMl2kQtQkTuw2uwJozAiCDjc9iWSj3sua8UewEC_1Ew2Kabl_D9hI4k8roxG5lu1eL---v-Ou42vNdr5vynWNsOmvPrg9e7E0laBCEGoAle_gMU60cPWCTpdw23XQ'),
                                    fit: BoxFit.cover,
                                  ),
                                ),
                              ),
                            ),
                            Positioned(
                              left: 30.w,
                              child: Container(
                                width: 40.r,
                                height: 40.r,
                                decoration: BoxDecoration(
                                  color: _AppColors.secondaryFixed,
                                  shape: BoxShape.circle,
                                  border: Border.all(color: _AppColors.surface, width: 2),
                                ),
                                child: Icon(Icons.person, color: _AppColors.secondary, size: 20.sp),
                              ),
                            ),
                          ],
                        ),
                      ),
                      SizedBox(width: 8.w),
                      Text(
                        'Anh Hoàng, Khách 2',
                        style: TextStyle(
                          color: _AppColors.onSurfaceVariant,
                          fontSize: 16.sp,
                        ),
                      ),
                      const Spacer(),
                      Container(
                        width: 40.r,
                        height: 40.r,
                        decoration: const BoxDecoration(
                          color: _AppColors.surfaceContainerHigh,
                          shape: BoxShape.circle,
                        ),
                        child: Icon(Icons.person_add, color: _AppColors.primary, size: 20.sp),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

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
                                  '${item.menuItem.price}k',
                                  style: TextStyle(
                                    color: _AppColors.primary,
                                    fontSize: 18.sp,
                                    fontWeight: FontWeight.bold,
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
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Row(
                                  children: [
                                    Icon(Icons.person, color: _AppColors.tertiary, size: 16.sp),
                                    SizedBox(width: 4.w),
                                    Text(
                                      'Bạn',
                                      style: TextStyle(
                                        color: _AppColors.tertiary,
                                        fontSize: 12.sp,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ],
                                ),
                                Container(
                                  padding: EdgeInsets.all(4.r),
                                  decoration: BoxDecoration(
                                    color: _AppColors.surfaceContainerHigh,
                                    borderRadius: BorderRadius.circular(100.r),
                                  ),
                                  child: Row(
                                    children: [
                                      Container(
                                        width: 32.r,
                                        height: 32.r,
                                        decoration: const BoxDecoration(
                                          shape: BoxShape.circle,
                                        ),
                                        child: Icon(Icons.remove, size: 20.sp, color: _AppColors.onSurface),
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
                                      Container(
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
                        onPressed: () {},
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
                  SizedBox(height: 16.h),
                  Container(
                    padding: EdgeInsets.all(12.r),
                    decoration: BoxDecoration(
                      color: _AppColors.secondaryContainer,
                      borderRadius: BorderRadius.circular(8.r),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.check_circle, color: _AppColors.onSecondaryContainer, size: 20.sp),
                        SizedBox(width: 8.w),
                        Text(
                          'Mã "WELCOME" đã được áp dụng - Giảm 10%',
                          style: TextStyle(
                            color: _AppColors.onSecondaryContainer,
                            fontSize: 14.sp,
                          ),
                        ),
                      ],
                    ),
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
                final success = await ref.read(cartViewModelProvider.notifier).checkout(tableId, sessionId);
                if (success && mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Đặt món thành công!')),
                  );
                  context.push('/checkout');
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
