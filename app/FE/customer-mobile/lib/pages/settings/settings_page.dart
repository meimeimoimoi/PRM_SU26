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
    final userName = authState.user?.fullName
        ?? (isGuest
            ? (authState.guestSession?.guestName.isNotEmpty == true
                ? authState.guestSession!.guestName
                : 'Khách')
            : 'Người dùng');
    final userRole = authState.user?.role ?? authState.guestSession?.role ?? 'GUEST';

    return Scaffold(
      backgroundColor: AppTheme.background,
      appBar: AppBar(
        backgroundColor: AppTheme.surface,
        elevation: 0,
        scrolledUnderElevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: AppTheme.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Cài đặt',
          style: TextStyle(
            color: AppTheme.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        centerTitle: true,
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
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(12.r),
                boxShadow: AppTheme.shadowCard,
              ),
              child: Row(
                children: [
                  Container(
                    width: 64.r,
                    height: 64.r,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: AppTheme.primary, width: 2),
                    ),
                    child: Icon(
                      isLoggedIn ? Icons.person : Icons.person_outline,
                      color: AppTheme.primary,
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
                            color: AppTheme.onSurface,
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w600,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        Text(
                          isGuest ? 'Khách' : 'Thành viên ${userRole == 'CUSTOMER' ? 'tiêu chuẩn' : userRole}',
                          style: TextStyle(
                            color: AppTheme.onSurfaceVariant,
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
                  color: AppTheme.primary,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.5,
                ),
              ),
            ),
            Container(
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(12.r),
                boxShadow: AppTheme.shadowCard,
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
                            color: AppTheme.onSurfaceVariant,
                            fontSize: 14.sp,
                          ),
                        ),
                        Icon(Icons.chevron_right, color: AppTheme.outline, size: 20.sp),
                      ],
                    ),
                    onTap: () => _showComingSoon(context, 'Đổi ngôn ngữ'),
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.dark_mode,
                    title: 'Chế độ tối',
                    trailing: _buildCustomToggle(_darkModeEnabled, (val) {
                      setState(() => _darkModeEnabled = val);
                      _showComingSoon(context, 'Chế độ tối');
                    }),
                    onTap: () {
                      setState(() => _darkModeEnabled = !_darkModeEnabled);
                      _showComingSoon(context, 'Chế độ tối');
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
                  color: AppTheme.primary,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 1.5,
                ),
              ),
            ),
            Container(
              decoration: BoxDecoration(
                color: AppTheme.surface,
                borderRadius: BorderRadius.circular(12.r),
                boxShadow: AppTheme.shadowCard,
              ),
              child: Column(
                children: [
                  _buildSettingRow(
                    icon: Icons.help,
                    title: 'Trung tâm trợ giúp',
                    trailing: Icon(Icons.chevron_right, color: AppTheme.outline, size: 20.sp),
                    onTap: () => _showComingSoon(context, 'Trung tâm trợ giúp'),
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.policy,
                    title: 'Điều khoản & Chính sách',
                    trailing: Icon(Icons.chevron_right, color: AppTheme.outline, size: 20.sp),
                    onTap: () => _showComingSoon(context, 'Điều khoản & Chính sách'),
                  ),
                  _buildDivider(),
                  _buildSettingRow(
                    icon: Icons.mail,
                    title: 'Liên hệ',
                    trailing: Icon(Icons.chevron_right, color: AppTheme.outline, size: 20.sp),
                    onTap: () => _showComingSoon(context, 'Trang liên hệ'),
                  ),
                ],
              ),
            ),

            SizedBox(height: 48.h),

            // Logout Button
            OutlinedButton(
              onPressed: () async {
                await ref.read(authViewModelProvider.notifier).logout();
                if (mounted) {
                  context.go('/login');
                }
              },
              style: OutlinedButton.styleFrom(
                foregroundColor: AppTheme.onSurfaceVariant,
                side: BorderSide(color: AppTheme.outlineVariant, width: 1.5),
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
                  color: AppTheme.outlineVariant,
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            SizedBox(height: 40.h),
          ],
        ),
      ),

      bottomNavigationBar: const CustomerBottomNav(),
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
                  color: AppTheme.secondaryContainer,
                  borderRadius: BorderRadius.circular(8.r),
                ),
                child: Icon(icon, color: AppTheme.primary, size: 24.sp),
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
        color: AppTheme.outlineVariant,
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
          color: value ? AppTheme.primary : AppTheme.surfaceContainerHigh,
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
}