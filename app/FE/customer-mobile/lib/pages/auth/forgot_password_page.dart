import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../services/auth_repository.dart';
import '../../theme/app_theme.dart';

/// Luồng quên mật khẩu 2 bước, dùng chung 1 trang:
///   1. Nhập email → gọi POST /auth/forgot-password.
///   2. BE hiện chưa có hạ tầng gửi email nên trả thẳng resetToken trong response
///      (xem AuthService.ForgotPasswordAsync) — dùng token đó để đặt mật khẩu mới ngay
///      trong bước 2, gọi POST /auth/reset-password.
class ForgotPasswordPage extends ConsumerStatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  ConsumerState<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends ConsumerState<ForgotPasswordPage> {
  final _emailController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  bool _loading = false;
  String? _resetToken;

  @override
  void dispose() {
    _emailController.dispose();
    _newPasswordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  void _showError(Object e) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Có lỗi xảy ra: $e')),
    );
  }

  Future<void> _handleRequestToken() async {
    final email = _emailController.text.trim();
    if (email.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập email')),
      );
      return;
    }

    setState(() => _loading = true);
    try {
      final token = await ref.read(authRepositoryProvider).forgotPassword(email);
      if (!mounted) return;
      if (token == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.')),
        );
        return;
      }
      setState(() => _resetToken = token);
    } catch (e) {
      _showError(e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _handleResetPassword() async {
    final newPassword = _newPasswordController.text.trim();
    final confirmPassword = _confirmPasswordController.text.trim();
    if (newPassword.isEmpty || confirmPassword.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập đầy đủ mật khẩu mới')),
      );
      return;
    }
    if (newPassword != confirmPassword) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Mật khẩu xác nhận không khớp')),
      );
      return;
    }

    setState(() => _loading = true);
    try {
      await ref.read(authRepositoryProvider).resetPassword(_resetToken!, newPassword, confirmPassword);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.')),
      );
      context.pop();
    } catch (e) {
      _showError(e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final isStep2 = _resetToken != null;

    return Scaffold(
      backgroundColor: colors.surface,
      appBar: AppBar(
        backgroundColor: colors.surface,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: colors.onSurface),
          onPressed: () => context.pop(),
        ),
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 16.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              isStep2 ? 'Đặt mật khẩu mới' : 'Quên mật khẩu',
              style: theme.textTheme.headlineMedium?.copyWith(
                color: colors.onSurface,
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              isStep2
                  ? 'Nhập mật khẩu mới cho tài khoản của bạn.'
                  : 'Nhập email đã đăng ký để nhận hướng dẫn đặt lại mật khẩu.',
              style: theme.textTheme.bodyLarge?.copyWith(
                color: colors.onSurfaceVariant,
              ),
            ),
            SizedBox(height: 32.h),
            if (!isStep2) ...[
              _buildTextField(
                controller: _emailController,
                icon: Icons.email_outlined,
                hintText: 'Email',
                keyboardType: TextInputType.emailAddress,
                colors: colors,
              ),
              SizedBox(height: 24.h),
              _buildSubmitButton(colors, 'Gửi yêu cầu', _handleRequestToken),
            ] else ...[
              _buildTextField(
                controller: _newPasswordController,
                icon: Icons.lock_outline,
                hintText: 'Mật khẩu mới',
                obscureText: true,
                colors: colors,
              ),
              SizedBox(height: 16.h),
              _buildTextField(
                controller: _confirmPasswordController,
                icon: Icons.lock_outline,
                hintText: 'Xác nhận mật khẩu mới',
                obscureText: true,
                colors: colors,
              ),
              SizedBox(height: 24.h),
              _buildSubmitButton(colors, 'Đặt lại mật khẩu', _handleResetPassword),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildSubmitButton(ColorScheme colors, String label, VoidCallback onPressed) {
    return SizedBox(
      height: 56.h,
      child: ElevatedButton(
        onPressed: _loading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: colors.primary,
          foregroundColor: colors.onPrimary,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
          elevation: 2,
        ),
        child: _loading
            ? const SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
              )
            : Text(label, style: TextStyle(fontSize: 18.sp, fontWeight: FontWeight.bold)),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required IconData icon,
    required String hintText,
    TextInputType? keyboardType,
    bool obscureText = false,
    required ColorScheme colors,
  }) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      obscureText: obscureText,
      style: TextStyle(fontSize: 16.sp, color: colors.onSurface),
      decoration: InputDecoration(
        hintText: hintText,
        hintStyle: TextStyle(color: colors.outline, fontSize: 16.sp),
        prefixIcon: Icon(icon, color: colors.outline),
      ),
    );
  }
}