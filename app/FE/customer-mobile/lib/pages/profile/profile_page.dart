import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color surfaceContainer = Color(0xFFf0eded);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color error = Color(0xFFba1a1a);
  static const Color errorContainer = Color(0xFFffdad6);
  static const Color onPrimary = Color(0xFFffffff);
}


class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authViewModelProvider);
    final user = authState.user;
    final guest = authState.guestSession;
    
    final name = user?.fullName ?? guest?.role ?? 'Khách';
    final phone = user?.phoneNumber != null && user!.phoneNumber!.isNotEmpty ? user.phoneNumber! : 'Không có SĐT';
    final isGuest = guest != null;
    
    final loyaltyPoints = user?.loyaltyPoints ?? 0;
    String membership = isGuest ? 'KHÁCH VÃNG LAI' : 'THÀNH VIÊN MỚI';
    if (user?.membershipLevel != null) {
      final level = user!.membershipLevel!.toUpperCase();
      if (level == 'BRONZE') membership = 'THÀNH VIÊN ĐỒNG';
      else if (level == 'SILVER') membership = 'THÀNH VIÊN BẠC';
      else if (level == 'GOLD') membership = 'THÀNH VIÊN VÀNG';
      else if (level == 'VIP') membership = 'THÀNH VIÊN VIP';
      else membership = 'THÀNH VIÊN $level';
    }

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Tài khoản',
          style: TextStyle(
            color: _AppColors.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.settings, color: _AppColors.primary),
            onPressed: () => context.push('/settings'),
          ),
          SizedBox(width: 8.w),
        ],
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 16.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Profile Header Section
            Row(
              children: [
                Container(
                  width: 80.r,
                  height: 80.r,
                  padding: EdgeInsets.all(4.r),
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(color: _AppColors.primaryContainer, width: 2),
                  ),
                  child: ClipOval(
                    child: Image.network(
                      'https://lh3.googleusercontent.com/aida-public/AB6AXuCu5YfCa6oZa9ZAq8fcb1EFOfjc3-Vf6uR3HzW_5oVoL2_f4BkzyeBOl3Z8uXGKRvZxZP0fvd_DVHJqKAFvv4DZp3_A6Wz58pBAWAJ2ljXhP9mVXcSATn4rR1tdSD_LCXmJ5NGQBSRkEImbNFwegAR9H3z5wfXpx8av2c5LXU7q82we_FSlLS1XfDrOk7znj4t7k3HBry7-rJo5fD8RdykQtUvrOu_eTXT-T3DSLKRwQVI9auXMHOpXIcYM3jmbdN1wyCyVD_3pkUEg',
                      fit: BoxFit.cover,
                    ),
                  ),
                ),
                SizedBox(width: 16.w),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      name,
                      style: TextStyle(
                        color: _AppColors.onSurface,
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      phone,
                      style: TextStyle(
                        color: _AppColors.onSurfaceVariant,
                        fontSize: 14.sp,
                      ),
                    ),
                    SizedBox(height: 8.h),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                      decoration: BoxDecoration(
                        color: _AppColors.secondaryContainer,
                        borderRadius: BorderRadius.circular(100.r),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.verified, color: _AppColors.onSecondaryContainer, size: 14.sp),
                          SizedBox(width: 4.w),
                          Text(
                            membership,
                            style: TextStyle(
                              color: _AppColors.onSecondaryContainer,
                              fontSize: 10.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
            ),
            SizedBox(height: 24.h),

            // Loyalty Points Card (Prominent Bento Style)
            Container(
              decoration: BoxDecoration(
                color: _AppColors.primary,
                borderRadius: BorderRadius.circular(16.r),
                boxShadow: [
                  BoxShadow(
                    color: _AppColors.primary.withOpacity(0.3),
                    blurRadius: 10,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: Stack(
                children: [
                  // Abstract Texture Overlay
                  Positioned(
                    right: -40.w,
                    top: -40.h,
                    child: Container(
                      width: 160.r,
                      height: 160.r,
                      decoration: BoxDecoration(
                        color: Colors.white.withOpacity(0.1),
                        shape: BoxShape.circle,
                      ),
                    ),
                  ),
                  Padding(
                    padding: EdgeInsets.all(24.r),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'SMARTDINE POINTS',
                                  style: TextStyle(
                                    color: Colors.white.withOpacity(0.8),
                                    fontSize: 12.sp,
                                    fontWeight: FontWeight.w600,
                                    letterSpacing: 1.2,
                                  ),
                                ),
                                SizedBox(height: 4.h),
                                Text(
                                  '$loyaltyPoints điểm',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 26.sp,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                            Container(
                              padding: EdgeInsets.all(8.r),
                              decoration: BoxDecoration(
                                color: Colors.white.withOpacity(0.2),
                                borderRadius: BorderRadius.circular(12.r),
                              ),
                              child: Icon(Icons.stars, color: Colors.white, size: 24.sp),
                            ),
                          ],
                        ),
                        SizedBox(height: 24.h),
                        Container(
                          width: double.infinity,
                          height: 6.h,
                          decoration: BoxDecoration(
                            color: Colors.white.withOpacity(0.2),
                            borderRadius: BorderRadius.circular(100.r),
                          ),
                          child: FractionallySizedBox(
                            alignment: Alignment.centerLeft,
                            widthFactor: 0.65, // 65% width
                            child: Container(
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(100.r),
                              ),
                            ),
                          ),
                        ),
                        SizedBox(height: 8.h),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              isGuest ? 'Đăng ký tài khoản để tích điểm' : 'Tiếp tục tích điểm để thăng hạng',
                              style: TextStyle(
                                color: Colors.white.withOpacity(0.9),
                                fontSize: 14.sp,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 16.h),
                        InkWell(
                          onTap: () {},
                          borderRadius: BorderRadius.circular(8.r),
                          child: Container(
                            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
                            decoration: BoxDecoration(
                              color: Colors.white.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(8.r),
                              border: Border.all(color: Colors.white.withOpacity(0.2)),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  'Xem quyền lợi',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: 12.sp,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                SizedBox(width: 8.w),
                                Icon(Icons.chevron_right, color: Colors.white, size: 16.sp),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

            // Menu Items List
            Container(
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
                  _buildMenuItem(
                    icon: Icons.receipt_long,
                    title: 'Lịch sử đơn hàng',
                    onTap: () => context.push('/orders'),
                  ),
                  _buildDivider(),
                  _buildMenuItem(
                    icon: Icons.payments,
                    title: 'Phương thức thanh toán',
                    onTap: () {},
                  ),
                  _buildDivider(),
                  _buildMenuItem(
                    icon: Icons.confirmation_number,
                    title: 'Mã giảm giá của tôi',
                    badge: Container(
                      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 2.h),
                      decoration: BoxDecoration(
                        color: _AppColors.primary,
                        borderRadius: BorderRadius.circular(4.r),
                      ),
                      child: Text(
                        '3 MỚI',
                        style: TextStyle(
                          color: _AppColors.onPrimary,
                          fontSize: 10.sp,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    onTap: () {},
                  ),
                ],
              ),
            ),
            SizedBox(height: 16.h),

            // Secondary Settings Group
            Container(
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
                  _buildMenuItem(
                    icon: Icons.location_on,
                    iconBgColor: _AppColors.surfaceContainer,
                    iconColor: _AppColors.onSurfaceVariant,
                    title: 'Địa chỉ đã lưu',
                    onTap: () {},
                  ),
                  _buildDivider(),
                  _buildMenuItem(
                    icon: Icons.help,
                    iconBgColor: _AppColors.surfaceContainer,
                    iconColor: _AppColors.onSurfaceVariant,
                    title: 'Trợ giúp & Hỗ trợ',
                    onTap: () {},
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

            // Logout Button
            ElevatedButton(
              onPressed: () async {
                await ref.read(authViewModelProvider.notifier).logout();
                if (context.mounted) {
                  context.go('/login');
                }
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.white,
                foregroundColor: _AppColors.error,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                  side: BorderSide(color: _AppColors.errorContainer),
                ),
                padding: EdgeInsets.symmetric(vertical: 16.h),
                elevation: 0,
              ),
              child: Text(
                'Đăng xuất',
                style: TextStyle(
                  fontSize: 18.sp,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            
            SizedBox(height: 48.h),
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
            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(Icons.home, 'Trang chủ', false, () => context.go('/home')),
                _buildNavItem(Icons.receipt_long, 'Đơn hàng', false, () => context.push('/orders')),
                _buildNavItem(Icons.person, 'Tài khoản', true, () {}),
                _buildNavItem(Icons.settings, 'Cài đặt', false, () => context.push('/settings')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildMenuItem({
    required IconData icon,
    required String title,
    Color? iconBgColor,
    Color? iconColor,
    Widget? badge,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: EdgeInsets.all(16.r),
        child: Row(
          children: [
            Container(
              width: 40.r,
              height: 40.r,
              decoration: BoxDecoration(
                color: iconBgColor ?? _AppColors.secondaryContainer,
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon,
                color: iconColor ?? _AppColors.primary,
                size: 24.sp,
              ),
            ),
            SizedBox(width: 16.w),
            Expanded(
              child: Text(
                title,
                style: TextStyle(
                  color: _AppColors.onSurface,
                  fontSize: 16.sp,
                ),
              ),
            ),
            if (badge != null) badge,
            if (badge != null) SizedBox(width: 4.w),
            Icon(Icons.chevron_right, color: _AppColors.outlineVariant.withOpacity(0.8), size: 24.sp),
          ],
        ),
      ),
    );
  }

  Widget _buildDivider() {
    return Padding(
      padding: EdgeInsets.only(left: 72.w, right: 16.w),
      child: Divider(
        color: _AppColors.outlineVariant.withOpacity(0.3),
        height: 1,
        thickness: 1,
      ),
    );
  }

  Widget _buildNavItem(IconData icon, String label, bool isActive, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
        decoration: BoxDecoration(
          color: isActive ? _AppColors.secondaryContainer : Colors.transparent,
          borderRadius: BorderRadius.circular(100.r), // capsule shape for nav
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? _AppColors.onSecondaryContainer : _AppColors.onSurfaceVariant,
              size: 24.sp,
            ),
            SizedBox(height: 4.h),
            Text(
              label,
              style: TextStyle(
                color: isActive ? _AppColors.onSecondaryContainer : _AppColors.onSurfaceVariant,
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
