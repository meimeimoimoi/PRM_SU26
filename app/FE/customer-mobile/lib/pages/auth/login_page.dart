import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../routes/app_routes.dart';
import '../../theme/app_theme.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _nameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _tableIdController = TextEditingController(text: '1');

  @override
  void dispose() {
    _nameController.dispose();
    _phoneController.dispose();
    _tableIdController.dispose();
    super.dispose();
  }

  void _handleGuestLogin() async {
    final name = _nameController.text.trim();
    final phone = _phoneController.text.trim();
    final tableId = int.tryParse(_tableIdController.text.trim()) ?? 1;

    final success = await ref.read(authViewModelProvider.notifier).loginGuest(tableId, name, phone);

    if (success && mounted) {
      context.go('/home');
    }
  }

  Future<void> _handleScanQr() async {
    final tableNumber = await context.push<int>(AppRoutes.qrScan);
    if (tableNumber != null) {
      _tableIdController.text = tableNumber.toString();
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    ref.listen<AuthState>(authViewModelProvider, (previous, next) {
      if (next.status == AuthStateStatus.error) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(next.errorMessage ?? 'Có lỗi xảy ra')),
        );
      }
    });

    return Scaffold(
      backgroundColor: colors.surface,
      appBar: AppBar(
        backgroundColor: colors.surface,
        elevation: 0,
        scrolledUnderElevation: 0,
        title: Row(
          children: [
            Icon(Icons.restaurant, color: colors.primary, size: 24.sp),
            SizedBox(width: 8.w),
            Text(
              'SmartDine',
              style: TextStyle(
                color: colors.primary,
                fontSize: 20.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        actions: [
          Container(
            margin: EdgeInsets.only(right: 20.w),
            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
            decoration: BoxDecoration(
              color: colors.primaryContainer,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              'GUEST',
              style: TextStyle(
                color: colors.onPrimaryContainer,
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
                letterSpacing: 0.6,
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
            // Hero Section
            Center(
              child: Container(
                width: 128.r,
                height: 128.r,
                margin: EdgeInsets.only(bottom: 16.h),
                decoration: BoxDecoration(
                  color: colors.primaryContainer.withOpacity(0.3),
                  shape: BoxShape.circle,
                ),
                child: Center(
                  child: Icon(
                    Icons.restaurant_menu,
                    color: colors.primary,
                    size: 48.sp,
                  ),
                ),
              ),
            ),
            Text(
              'Chào mừng bạn đến với SmartDine',
              textAlign: TextAlign.center,
              style: theme.textTheme.headlineSmall?.copyWith(
                color: colors.onSurface,
                fontWeight: FontWeight.bold,
                letterSpacing: -0.5,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              'Trải nghiệm ẩm thực hiện đại trong tầm tay bạn.',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyLarge?.copyWith(
                color: colors.onSurfaceVariant,
              ),
            ),
            SizedBox(height: 32.h),

            // Table Information Card
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: colors.surface,
                borderRadius: BorderRadius.circular(16.r),
                border: Border.all(color: colors.outlineVariant.withOpacity(0.3)),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.04),
                    blurRadius: 20,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: ValueListenableBuilder<TextEditingValue>(
                valueListenable: _tableIdController,
                builder: (context, value, _) {
                  final tableText = value.text.trim();
                  return Row(
                    children: [
                      Container(
                        width: 64.r,
                        height: 64.r,
                        decoration: BoxDecoration(
                          color: colors.primaryContainer,
                          borderRadius: BorderRadius.circular(12.r),
                        ),
                        child: Center(
                          child: Text(
                            tableText.isEmpty ? '?' : tableText,
                            style: TextStyle(
                              color: colors.onPrimaryContainer,
                              fontSize: 20.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ),
                      SizedBox(width: 24.w),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              tableText.isEmpty ? 'Bàn của bạn' : 'Bàn số $tableText',
                              style: TextStyle(
                                color: colors.onSurface,
                                fontSize: 20.sp,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            SizedBox(height: 4.h),
                            Row(
                              children: [
                                Icon(Icons.info_outline, color: colors.onSurfaceVariant, size: 16.sp),
                                SizedBox(width: 4.w),
                                Expanded(
                                  child: Text(
                                    'Kiểm tra đúng số bàn trước khi tiếp tục',
                                    style: TextStyle(
                                      color: colors.onSurfaceVariant,
                                      fontSize: 14.sp,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ],
                  );
                },
              ),
            ),
            SizedBox(height: 24.h),

            // Guest Form
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: colors.surface,
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
                  _buildLabel('Số Bàn', true, colors),
                  SizedBox(height: 8.h),
                  _buildTextField(
                    controller: _tableIdController,
                    icon: Icons.table_restaurant,
                    hintText: 'Nhập số bàn hoặc quét mã QR',
                    keyboardType: TextInputType.number,
                    suffixIcon: IconButton(
                      icon: Icon(Icons.qr_code_scanner, color: colors.primary),
                      tooltip: 'Quét mã QR trên bàn',
                      onPressed: _handleScanQr,
                    ),
                    colors: colors,
                  ),
                  SizedBox(height: 16.h),
                  _buildLabel('Tên của bạn', true, colors),
                  SizedBox(height: 8.h),
                  _buildTextField(
                    controller: _nameController,
                    icon: Icons.person_outline,
                    hintText: 'Nhập tên để chúng tôi xưng hô',
                    colors: colors,
                  ),
                  SizedBox(height: 16.h),
                  _buildLabel('Số điện thoại', false, colors, '(Không bắt buộc)'),
                  SizedBox(height: 8.h),
                  _buildTextField(
                    controller: _phoneController,
                    icon: Icons.call_outlined,
                    hintText: 'Để tích điểm tự động',
                    keyboardType: TextInputType.phone,
                    colors: colors,
                  ),
                  SizedBox(height: 24.h),
                  SizedBox(
                    width: double.infinity,
                    height: 56.h,
                    child: ElevatedButton(
                      onPressed: authState.status == AuthStateStatus.loading ? null : _handleGuestLogin,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: colors.primary,
                        foregroundColor: colors.onPrimary,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12.r),
                        ),
                        elevation: 4,
                        shadowColor: colors.primary.withOpacity(0.2),
                      ),
                      child: authState.status == AuthStateStatus.loading
                          ? SizedBox(
                              width: 24.sp,
                              height: 24.sp,
                              child: CircularProgressIndicator(color: colors.onPrimary, strokeWidth: 2),
                            )
                          : Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Flexible(
                                  child: Text(
                                    'Tiếp tục làm khách vãng lai',
                                    style: TextStyle(
                                      fontSize: 16.sp,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ),
                                SizedBox(width: 8.w),
                                Icon(Icons.arrow_forward, size: 20.sp),
                              ],
                            ),
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

            // Loyalty Card (Navigate to Login/Signup)
            InkWell(
              onTap: () => context.push('/signup'),
              borderRadius: BorderRadius.circular(12.r),
              child: Container(
                padding: EdgeInsets.all(24.r),
                decoration: BoxDecoration(
                  color: colors.secondaryContainer,
                  borderRadius: BorderRadius.circular(12.r),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 48.r,
                      height: 48.r,
                      decoration: BoxDecoration(
                        color: colors.onSecondaryContainer.withOpacity(0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        Icons.loyalty,
                        color: colors.onSecondaryContainer,
                      ),
                    ),
                    SizedBox(width: 16.w),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Đăng nhập thành viên',
                            style: TextStyle(
                              color: colors.onSecondaryContainer,
                              fontSize: 20.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Icon(
                      Icons.chevron_right,
                      color: colors.onSecondaryContainer,
                    ),
                  ],
                ),
              ),
            ),
            SizedBox(height: 32.h),

            // Support Message
            Text(
              '"Chúng tôi rất vui được phục vụ quý khách tại bàn hôm nay."',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: colors.onSurfaceVariant,
                fontSize: 14.sp,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLabel(String text, bool isRequired, ColorScheme colors, [String? suffix]) {
    return Row(
      children: [
        Text(
          text,
          style: TextStyle(
            color: colors.onSurfaceVariant,
            fontSize: 12.sp,
            fontWeight: FontWeight.w600,
            letterSpacing: 0.5,
          ),
        ),
        if (isRequired) ...[
          SizedBox(width: 4.w),
          Text(
            '*',
            style: TextStyle(
              color: colors.primary,
              fontSize: 12.sp,
            ),
          ),
        ],
        if (suffix != null) ...[
          SizedBox(width: 4.w),
          Text(
            suffix,
            style: TextStyle(
              color: colors.onSurfaceVariant.withOpacity(0.6),
              fontSize: 12.sp,
            ),
          ),
        ]
      ],
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required IconData icon,
    required String hintText,
    TextInputType? keyboardType,
    Widget? suffixIcon,
    required ColorScheme colors,
  }) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      style: TextStyle(
        fontSize: 16.sp,
        color: colors.onSurface,
      ),
      decoration: InputDecoration(
        hintText: hintText,
        hintStyle: TextStyle(
          color: colors.outline,
          fontSize: 16.sp,
        ),
        prefixIcon: Icon(
          icon,
          color: colors.outline,
        ),
        suffixIcon: suffixIcon,
        filled: true,
        fillColor: colors.surfaceContainerHighest,
        contentPadding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 16.w),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12.r),
          borderSide: BorderSide(color: colors.outlineVariant),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12.r),
          borderSide: BorderSide(color: colors.primary, width: 2),
        ),
      ),
    );
  }
}