import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainer = Color(0xFFf0eded);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color onPrimary = Color(0xFFffffff);
}

class CheckoutPage extends ConsumerWidget {
  const CheckoutPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authViewModelProvider);
    final tableNumber = authState.guestSession?.tableNumber ?? 1;

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 0,
        automaticallyImplyLeading: false,
        title: Row(
          children: [
            Icon(Icons.restaurant, color: _AppColors.primary, size: 24.sp),
            SizedBox(width: 8.w),
            Text(
              'Bàn $tableNumber',
              style: TextStyle(
                color: _AppColors.primary,
                fontSize: 20.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            onPressed: () => context.go('/home'),
            icon: Container(
              padding: EdgeInsets.all(4.r),
              decoration: const BoxDecoration(
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.close, color: _AppColors.onSurfaceVariant, size: 24.sp),
            ),
          ),
          SizedBox(width: 8.w),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: 20.w),
          child: Column(
            children: [
              Expanded(
                child: SingleChildScrollView(
                  child: Column(
                    children: [
                      SizedBox(height: 32.h),
                      // Celebration Section with Animation
                      TweenAnimationBuilder(
                        duration: const Duration(milliseconds: 800),
                        tween: Tween<double>(begin: 0, end: 1),
                        curve: Curves.elasticOut,
                        builder: (context, double value, child) {
                          return Transform.scale(
                            scale: value,
                            child: Opacity(
                              opacity: value.clamp(0.0, 1.0),
                              child: child,
                            ),
                          );
                        },
                        child: Container(
                          width: 96.r,
                          height: 96.r,
                          decoration: BoxDecoration(
                            color: _AppColors.primaryFixed,
                            shape: BoxShape.circle,
                            boxShadow: [
                              BoxShadow(
                                color: _AppColors.primary.withOpacity(0.1),
                                blurRadius: 20,
                                offset: const Offset(0, 8),
                              ),
                            ],
                          ),
                          child: Center(
                            child: Icon(
                              Icons.check_circle,
                              color: _AppColors.primary,
                              size: 48.sp,
                            ),
                          ),
                        ),
                      ),
                      SizedBox(height: 24.h),
                      Text(
                        'Đặt món thành công!',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: _AppColors.onSurface,
                          fontSize: 26.sp,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      SizedBox(height: 12.h),
                      Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16.w),
                        child: Text(
                          'Đơn hàng của bạn đã được gửi đến quầy chế biến. Chúc bạn một bữa ăn ngon miệng!',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: _AppColors.onSurfaceVariant,
                            fontSize: 16.sp,
                          ),
                        ),
                      ),
                      SizedBox(height: 40.h),
                      
                      SizedBox(height: 40.h),
                      
                      // Action Section
                      ElevatedButton(
                        onPressed: () => context.go('/home'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: _AppColors.primary,
                          foregroundColor: _AppColors.onPrimary,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12.r),
                          ),
                          padding: EdgeInsets.symmetric(vertical: 16.h),
                          elevation: 4,
                          shadowColor: _AppColors.primary.withOpacity(0.3),
                          minimumSize: Size(double.infinity, 56.h),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.add_shopping_cart, size: 24.sp),
                            SizedBox(width: 8.w),
                            Text(
                              'Tiếp tục gọi thêm món',
                              style: TextStyle(
                                fontSize: 18.sp,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                      ),
                      SizedBox(height: 16.h),
                      ElevatedButton(
                        onPressed: () => context.push('/orders'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: _AppColors.surfaceContainer,
                          foregroundColor: _AppColors.onSurface,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12.r),
                          ),
                          padding: EdgeInsets.symmetric(vertical: 16.h),
                          elevation: 0,
                          minimumSize: Size(double.infinity, 56.h),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.analytics_outlined, size: 24.sp),
                            SizedBox(width: 8.w),
                            Text(
                              'Theo dõi tiến độ',
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
                ),
              ),
              
              // Ambient Decoration Footer
              Padding(
                padding: EdgeInsets.symmetric(vertical: 24.h),
                child: Text(
                  'Thực hiện bởi SmartDine AI',
                  style: TextStyle(
                    color: _AppColors.onSurfaceVariant.withOpacity(0.6),
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
