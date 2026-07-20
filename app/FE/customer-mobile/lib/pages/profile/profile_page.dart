import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../widgets/customer_bottom_nav.dart';
import '../../theme/app_theme.dart';

void _showComingSoon(BuildContext context, String feature) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(content: Text('$feature đang được phát triển, sẽ sớm ra mắt!')),
  );
}

class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authViewModelProvider);
    final user = authState.user;
    final guest = authState.guestSession;

    final name = user?.fullName
        ?? (guest?.guestName.isNotEmpty == true ? guest!.guestName : null)
        ?? 'Khách';
    final phone = user?.phoneNumber != null && user!.phoneNumber!.isNotEmpty
        ? user.phoneNumber!
        : 'Không có SĐT';
    final isGuest = authState.status == AuthStateStatus.guest;

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
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: AppTheme.primary),
          onPressed: () => context.canPop() ? context.pop() : context.go('/home'),
        ),
        title: Text(
          'Tài khoản',
          style: TextStyle(
            color: AppTheme.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.settings, color: AppTheme.primary),
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
                    border: Border.all(color: AppTheme.primary, width: 2),
                  ),
                  child: ClipOval(
                    child: user?.avatarUrl != null && user!.avatarUrl!.isNotEmpty
                        ? Image.network(
                            user.avatarUrl!,
                            fit: BoxFit.cover,
                            width: 80.r,
                            height: 80.r,
                            errorBuilder: (_, __, ___) => Container(
                              color: AppTheme.primaryContainer,
                              child: Icon(Icons.person, color: Colors.white, size: 40.sp),
                            ),
                          )
                        : Container(
                            color: AppTheme.primaryContainer,
                            child: Icon(Icons.person, color: Colors.white, size: 40.sp),
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
                        color: AppTheme.onSurface,
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      phone,
                      style: TextStyle(
                        color: AppTheme.onSurfaceVariant,
                        fontSize: 14.sp,
                      ),
                    ),
                    SizedBox(height: 8.h),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                      decoration: BoxDecoration(
                        color: AppTheme.secondaryContainer,
                        borderRadius: BorderRadius.circular(100.r),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.verified, color: AppTheme.onSecondaryContainer, size: 14.sp),
                          SizedBox(width: 4.w),
                          Text(
                            membership,
                            style: TextStyle(
                              color: AppTheme.onSecondaryContainer,
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

            // Loyalty Points — chỉ thành viên đăng ký (không hiện với khách vãng lai)
            if (!isGuest)
              Container(
              decoration: BoxDecoration(
                color: AppTheme.primary,
                borderRadius: BorderRadius.circular(16.r),
                boxShadow: [
                  BoxShadow(
                    color: AppTheme.primary.withOpacity(0.3),
                    blurRadius: 10,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: Stack(
                children: [
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
                            widthFactor: 0.65,
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
                          onTap: () => _showComingSoon(context, 'Trang quyền lợi thành viên'),
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
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16.r),
                boxShadow: AppTheme.shadowCard,
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
                    onTap: () => _showComingSoon(context, 'Quản lý phương thức thanh toán'),
                  ),
                  _buildDivider(),
                  _buildMenuItem(
                    icon: Icons.confirmation_number,
                    title: 'Mã giảm giá của tôi',
                    onTap: () => _showComingSoon(context, 'Danh sách mã giảm giá'),
                  ),
                ],
              ),
            ),
            SizedBox(height: 16.h),

            // Secondary Settings Group
            Container(
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(16.r),
                boxShadow: AppTheme.shadowCard,
              ),
              child: Column(
                children: [
                  _buildMenuItem(
                    icon: Icons.location_on,
                    iconBgColor: AppTheme.surfaceContainerHigh,
                    iconColor: AppTheme.onSurfaceVariant,
                    title: 'Địa chỉ đã lưu',
                    onTap: () => _showComingSoon(context, 'Địa chỉ đã lưu'),
                  ),
                  _buildDivider(),
                  _buildMenuItem(
                    icon: Icons.help,
                    iconBgColor: AppTheme.surfaceContainerHigh,
                    iconColor: AppTheme.onSurfaceVariant,
                    title: 'Trợ giúp & Hỗ trợ',
                    onTap: () => _showComingSoon(context, 'Trung tâm trợ giúp'),
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

            // Logout Button
            OutlinedButton(
              onPressed: () async {
                await ref.read(authViewModelProvider.notifier).logout();
                if (context.mounted) {
                  context.go('/login');
                }
              },
              style: OutlinedButton.styleFrom(
                foregroundColor: AppTheme.error,
                side: BorderSide(color: AppTheme.errorContainer, width: 1.5),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
                padding: EdgeInsets.symmetric(vertical: 16.h),
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

      bottomNavigationBar: const CustomerBottomNav(activeTab: CustomerNavTab.account),
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
                color: iconBgColor ?? AppTheme.secondaryContainer,
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon,
                color: iconColor ?? AppTheme.primary,
                size: 24.sp,
              ),
            ),
            SizedBox(width: 16.w),
            Expanded(
              child: Text(
                title,
                style: TextStyle(
                  color: AppTheme.onSurface,
                  fontSize: 16.sp,
                ),
              ),
            ),
            if (badge != null) badge,
            if (badge != null) SizedBox(width: 4.w),
            Icon(Icons.chevron_right, color: AppTheme.outlineVariant.withOpacity(0.8), size: 24.sp),
          ],
        ),
      ),
    );
  }

  Widget _buildDivider() {
    return Padding(
      padding: EdgeInsets.only(left: 72.w, right: 16.w),
      child: Divider(
        color: AppTheme.outlineVariant.withOpacity(0.3),
        height: 1,
        thickness: 1,
      ),
    );
  }
}