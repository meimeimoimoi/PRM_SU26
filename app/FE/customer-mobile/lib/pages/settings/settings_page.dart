import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color surfaceContainer = Color(0xFFf0eded);
  static const Color surfaceContainerLow = Color(0xFFf6f3f2);
  static const Color surfaceContainerHigh = Color(0xFFeae7e7);
  static const Color surfaceVariant = Color(0xFFe5e2e1);
  static const Color secondary = Color(0xFF685b5a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color outline = Color(0xFF8f7068);
  static const Color outlineVariant = Color(0xFFe3beb5);
}

class SettingsPage extends ConsumerStatefulWidget {
  const SettingsPage({super.key});

  @override
  ConsumerState<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends ConsumerState<SettingsPage> {
  bool _notificationsEnabled = true;
  bool _darkModeEnabled = false;

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final isLoggedIn = authState.status == AuthStateStatus.authenticated;
    final isGuest = authState.status == AuthStateStatus.guest;
    final userName = authState.user?.fullName ?? (isGuest ? 'Khách' : 'Người dùng');
    final userRole = authState.user?.role ?? authState.guestSession?.role ?? 'GUEST';

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Cài đặt',
          style: TextStyle(
            color: _AppColors.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: Icon(Icons.settings, color: _AppColors.primary),
            onPressed: () {},
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 16.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Profile Mini Card
            Container(
              padding: EdgeInsets.all(16.r),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12.r),
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
                  Container(
                    width: 64.r,
                    height: 64.r,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: _AppColors.primary, width: 2),
                    ),
                    child: Icon(
                      isLoggedIn ? Icons.person : Icons.person_outline,
                      color: _AppColors.primary,
                      size: 32.sp,
                    ),
                  ),
                  SizedBox(width: 16.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '$userName',
                          style: TextStyle(
                            color: _AppColors.onSurface,
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w600,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        Text(
                          isGuest ? 'Khách' : 'Thành viên ${userRole == 'CUSTOMER' ? 'tiêu chuẩn' : userRole}',
                          style: TextStyle(
                            color: _AppColors.onSurfaceVariant,
                            fontSize: 14.sp,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(height: 32.h),

            // Section: Ứng dụng
            Padding(
              padding: EdgeInsets.only(left: 4.w, bottom: 12.h),
              child: Text(
                'ỨNG DỤNG',
                style: TextStyle(
                  color: _AppColors.primary,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.5,
                ),
              ),
            ),
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12.r),
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
                  _buildSettingRow(
                    icon: Icons.notifications,
                    title: 'Thông báo',
                    trailing: _buildCustomToggle(_notificationsEnabled, (val) {
                      setState(() => _notificationsEnabled = val);
                    }),
                    onTap: () {
                      setState(() => _notificationsEnabled = !_notificationsEnabled);
                    },
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.language,
                    title: 'Ngôn ngữ',
                    trailing: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          'Tiếng Việt',
                          style: TextStyle(
                            color: _AppColors.onSurfaceVariant,
                            fontSize: 14.sp,
                          ),
                        ),
                        Icon(Icons.chevron_right, color: _AppColors.outline, size: 20.sp),
                      ],
                    ),
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.dark_mode,
                    title: 'Chế độ tối',
                    trailing: _buildCustomToggle(_darkModeEnabled, (val) {
                      setState(() => _darkModeEnabled = val);
                    }),
                    onTap: () {
                      setState(() => _darkModeEnabled = !_darkModeEnabled);
                    },
                  ),
                ],
              ),
            ),
            SizedBox(height: 32.h),

            // Section: Hỗ trợ
            Padding(
              padding: EdgeInsets.only(left: 4.w, bottom: 12.h),
              child: Text(
                'HỖ TRỢ',
                style: TextStyle(
                  color: _AppColors.primary,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.5,
                ),
              ),
            ),
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12.r),
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
                  _buildSettingRow(
                    icon: Icons.help,
                    title: 'Trung tâm trợ giúp',
                    trailing: Icon(Icons.chevron_right, color: _AppColors.outline, size: 20.sp),
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.policy,
                    title: 'Điều khoản & Chính sách',
                    trailing: Icon(Icons.chevron_right, color: _AppColors.outline, size: 20.sp),
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.mail,
                    title: 'Liên hệ',
                    trailing: Icon(Icons.chevron_right, color: _AppColors.outline, size: 20.sp),
                  ),
                ],
              ),
            ),
            
            SizedBox(height: 48.h),
            
            // Logout Button
            ElevatedButton(
              onPressed: () async {
                await ref.read(authViewModelProvider.notifier).logout();
                if (mounted) {
                  context.go('/login');
                }
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: _AppColors.surfaceContainer,
                foregroundColor: _AppColors.secondary,
                elevation: 0,
                shadowColor: Colors.transparent,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
                padding: EdgeInsets.symmetric(vertical: 16.h),
                minimumSize: Size(double.infinity, 56.h),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.logout, size: 24.sp),
                  SizedBox(width: 8.w),
                  Text(
                    'Đăng xuất',
                    style: TextStyle(
                      fontSize: 18.sp,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ),
            
            SizedBox(height: 24.h),
            Center(
              child: Text(
                'SmartDine Phiên bản 2.4.0',
                style: TextStyle(
                  color: _AppColors.outlineVariant,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            SizedBox(height: 40.h),
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
                _buildNavItem(Icons.home, 'Home', false, () => context.go('/home')),
                _buildNavItem(Icons.receipt_long, 'Orders', false, () => context.push('/orders')),
                _buildNavItem(Icons.person, 'Account', false, () => context.push('/profile')),
                _buildNavItem(Icons.settings, 'Settings', true, () {}),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSettingRow({
    required IconData icon,
    required String title,
    required Widget trailing,
    VoidCallback? onTap,
  }) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: EdgeInsets.all(16.r),
          child: Row(
            children: [
              Container(
                width: 40.r,
                height: 40.r,
                decoration: BoxDecoration(
                  color: _AppColors.secondaryContainer,
                  borderRadius: BorderRadius.circular(8.r),
                ),
                child: Icon(icon, color: _AppColors.primary, size: 24.sp),
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
              trailing,
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDivider() {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 16.w),
      child: Divider(
        color: _AppColors.surfaceContainer,
        height: 1,
        thickness: 1,
      ),
    );
  }

  Widget _buildCustomToggle(bool value, ValueChanged<bool> onChanged) {
    return GestureDetector(
      onTap: () => onChanged(!value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 300),
        curve: Curves.easeOutCubic,
        width: 48.w,
        height: 24.h,
        padding: EdgeInsets.all(4.r),
        decoration: BoxDecoration(
          color: value ? _AppColors.primary : _AppColors.surfaceVariant,
          borderRadius: BorderRadius.circular(100.r),
        ),
        child: AnimatedAlign(
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOutCubic,
          alignment: value ? Alignment.centerRight : Alignment.centerLeft,
          child: Container(
            width: 16.r,
            height: 16.r,
            decoration: const BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
            ),
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
