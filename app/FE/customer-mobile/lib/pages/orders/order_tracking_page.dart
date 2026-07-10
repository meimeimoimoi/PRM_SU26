import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';

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
}

class OrderTrackingPage extends StatefulWidget {
  const OrderTrackingPage({super.key});

  @override
  State<OrderTrackingPage> createState() => _OrderTrackingPageState();
}

class _OrderTrackingPageState extends State<OrderTrackingPage> with SingleTickerProviderStateMixin {
  late AnimationController _pulseController;
  late Animation<double> _pulseAnimation;

  @override
  void initState() {
    super.initState();
    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 1),
    )..repeat(reverse: true);
    
    _pulseAnimation = Tween<double>(begin: 1.0, end: 1.15).animate(
      CurvedAnimation(parent: _pulseController, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _pulseController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        automaticallyImplyLeading: false,
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
              'Bàn 12',
              style: TextStyle(
                color: _AppColors.onPrimaryContainer,
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
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
            // Live Status Hero
            Text(
              'Món ăn đang chế biến',
              style: TextStyle(
                color: _AppColors.onSurface,
                fontSize: 26.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 16.h),
            
            // Bento-style Status Card
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
                            '#SD-8829',
                            style: TextStyle(
                              color: _AppColors.onSurface,
                              fontSize: 20.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
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
                              'Dự kiến: 12 phút',
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
                      // Progress Line Background
                      Positioned(
                        top: 18.h,
                        left: 20.w,
                        right: 20.w,
                        child: Container(
                          height: 2.h,
                          color: _AppColors.outlineVariant.withOpacity(0.4),
                        ),
                      ),
                      // Active Progress Line (50%)
                      Positioned(
                        top: 18.h,
                        left: 20.w,
                        right: 20.w,
                        child: FractionallySizedBox(
                          alignment: Alignment.centerLeft,
                          widthFactor: 0.5,
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
                            status: 2, // completed
                          ),
                          _buildTimelineStep(
                            icon: Icons.soup_kitchen, // cooking
                            label: 'Đang nấu',
                            status: 1, // active
                            pulse: true,
                          ),
                          _buildTimelineStep(
                            icon: Icons.notifications_active,
                            label: 'Sẵn sàng',
                            status: 0, // inactive
                          ),
                          _buildTimelineStep(
                            icon: Icons.restaurant,
                            label: 'Đã phục vụ',
                            status: 0, // inactive
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
            Text(
              'Chi tiết các món',
              style: TextStyle(
                color: _AppColors.onSurface,
                fontSize: 20.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
            SizedBox(height: 16.h),
            
            // Item 1: Cooking
            _buildOrderItem(
              title: 'Lẩu Thái',
              statusLabel: 'Đang nấu 🍳',
              statusBgColor: _AppColors.primaryContainer.withOpacity(0.1),
              statusTextColor: _AppColors.primary,
              imageUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDn5dSRdkjG6q7gWC6uNiVg7bmMoCVad5QOP8CBRBWfZ5dY6Ol90Xnwb-SRG8-i6jz1JGw7rziCJbNPUCJwgSVlZumCAkBWwyLNj5T6lwuqBC7dvXMA0P2hwgQOi3ZCd_iXIFtiV5tlcfrR0RWFOpP16iHgbVkC6CFdOwg7qkR1vmtHIhbx2sKkJNdwCKgpTtmt1GfMS7lEyz7HulYvM8QasyU49ZfiF8k1pBzW0WZ2X1CcMLyj1jPGPy7drM4L92rHqpq5YbXZ8DEo',
              quantity: '01',
              isGrayscale: false,
            ),
            SizedBox(height: 16.h),
            
            // Item 2: Served
            _buildOrderItem(
              title: 'Trà gừng',
              statusLabel: 'Đã phục vụ ✅',
              statusBgColor: _AppColors.secondaryContainer,
              statusTextColor: _AppColors.onSecondaryContainer,
              imageUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBTYCOOVlp_OuAHuQ3amaZrRJHWXidpB07nP224O4UIQ3IFOh_6mFXQE-c8UGEl8iLhYgpbObFIqlVhEl9-h-_gXBcHKWmJZKq0T7yUZ8zoiY84oc_1k_XYPsiUhdcSojOlHLvyc5EYphwhDpV2pG3O7NGBwEfJIV3Ls9NV0wiGT7ih8_5xvDQnoX8GPhtkCXEL4-u9zUI64uddPecd38z_lJQVzuAdqdZw08i6EmoFvM_Sm6jpgYUvjW8Wuu6QZR7d5nPiKJA2nwoh',
              quantity: '01',
              isGrayscale: true,
            ),
            
            SizedBox(height: 32.h),

            // Support CTA Section
            Container(
              padding: EdgeInsets.symmetric(vertical: 24.h),
              decoration: BoxDecoration(
                border: Border(top: BorderSide(color: _AppColors.outlineVariant.withOpacity(0.2))),
              ),
              child: Column(
                children: [
                  Text(
                    'Cần trợ giúp với đơn hàng của bạn?',
                    style: TextStyle(
                      color: _AppColors.secondary,
                      fontSize: 14.sp,
                    ),
                  ),
                  SizedBox(height: 16.h),
                  OutlinedButton(
                    onPressed: () {},
                    style: OutlinedButton.styleFrom(
                      side: BorderSide(color: _AppColors.primary, width: 2),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12.r),
                      ),
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
            SizedBox(height: 20.h),
          ],
        ),
      ),
      
      // Bottom Navigation Bar
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

  Widget _buildTimelineStep({
    required IconData icon,
    required String label,
    required int status, // 0: inactive, 1: active, 2: completed
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
    required String imageUrl,
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
          ClipRRect(
            borderRadius: BorderRadius.circular(8.r),
            child: isGrayscale
                ? ColorFiltered(
                    colorFilter: const ColorFilter.matrix([
                      0.2126, 0.7152, 0.0722, 0, 0,
                      0.2126, 0.7152, 0.0722, 0, 0,
                      0.2126, 0.7152, 0.0722, 0, 0,
                      0,      0,      0,      1, 0,
                    ]),
                    child: Image.network(
                      imageUrl,
                      width: 64.r,
                      height: 64.r,
                      fit: BoxFit.cover,
                    ),
                  )
                : Image.network(
                    imageUrl,
                    width: 64.r,
                    height: 64.r,
                    fit: BoxFit.cover,
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
                    Text(
                      title,
                      style: TextStyle(
                        color: _AppColors.onSurface,
                        fontSize: 16.sp,
                        fontWeight: FontWeight.bold,
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
    );
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
