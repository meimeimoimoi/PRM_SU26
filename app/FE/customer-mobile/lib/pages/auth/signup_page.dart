import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/pending_table_provider.dart';
import '../../routes/app_routes.dart';

/// Đăng nhập / đăng ký thành viên — bắt buộc đã có số bàn từ QR (truyền qua [tableNumber]).
class SignupPage extends ConsumerStatefulWidget {
  const SignupPage({super.key, this.tableNumber});

  final int? tableNumber;

  @override
  ConsumerState<SignupPage> createState() => _SignupPageState();
}

class _SignupPageState extends ConsumerState<SignupPage> {
  bool _isLogin = true;

  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _nameController = TextEditingController();
  final _phoneController = TextEditingController();

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _nameController.dispose();
    _phoneController.dispose();
    super.dispose();
  }

  Future<void> _handleSubmit() async {
    // Số bàn BẮT BUỘC từ route (/signup/4) — không fallback storage cũ.
    final tableNumber = widget.tableNumber;
    if (tableNumber == null || tableNumber <= 0) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng quét mã QR chọn bàn trước khi đăng nhập thành viên')),
      );
      context.go(AppRoutes.login);
      return;
    }

    ref.read(pendingTableNumberProvider.notifier).state = tableNumber;

    final email = _emailController.text.trim();
    final password = _passwordController.text.trim();
    final name = _nameController.text.trim();
    final phone = _phoneController.text.trim();

    if (email.isEmpty || password.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập email và mật khẩu')),
      );
      return;
    }

    if (!_isLogin && name.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập họ tên')),
      );
      return;
    }

    debugPrint('[Signup] submit bàn=$tableNumber email=$email isLogin=$_isLogin');
    final viewModel = ref.read(authViewModelProvider.notifier);
    final success = await viewModel.loginOrRegisterWithTable(
      email: email,
      password: password,
      tableNumber: tableNumber,
      fullName: _isLogin ? null : name,
      phoneNumber: _isLogin ? null : phone,
    );

    if (!success || !mounted) return;

    final dining = ref.read(authViewModelProvider).dining;
    if (dining == null || dining.sessionId <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            ref.read(authViewModelProvider).errorMessage ??
                'Không tạo được phiên bàn $tableNumber',
          ),
        ),
      );
      return;
    }

    ref.read(pendingTableNumberProvider.notifier).state = null;
    if (!mounted) return;
    context.go(AppRoutes.home);
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final theme = Theme.of(context);
    final colors = theme.colorScheme;
    final tableNumber = widget.tableNumber ?? ref.watch(pendingTableNumberProvider);

    ref.listen<AuthState>(authViewModelProvider, (previous, next) {
      if (next.status == AuthStateStatus.error ||
          (next.errorMessage != null && next.errorMessage!.isNotEmpty)) {
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
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: colors.onSurface),
          onPressed: () => context.go(AppRoutes.login),
        ),
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 16.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (tableNumber != null && tableNumber > 0) ...[
              Container(
                padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
                decoration: BoxDecoration(
                  color: colors.primaryContainer.withOpacity(0.5),
                  borderRadius: BorderRadius.circular(12.r),
                  border: Border.all(color: colors.primary.withOpacity(0.3)),
                ),
                child: Row(
                  children: [
                    Icon(Icons.table_restaurant, color: colors.primary, size: 22.sp),
                    SizedBox(width: 12.w),
                    Text(
                      'Bạn đang ngồi bàn số $tableNumber',
                      style: TextStyle(
                        color: colors.onSurface,
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
              SizedBox(height: 20.h),
            ],
            Text(
              _isLogin ? 'Đăng nhập thành viên' : 'Tạo tài khoản mới',
              style: theme.textTheme.headlineMedium?.copyWith(
                color: colors.onSurface,
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              _isLogin
                  ? 'Đăng nhập để tích điểm và nhận ưu đãi tại bàn này.'
                  : 'Trở thành khách hàng thân thiết của SmartDine.',
              style: theme.textTheme.bodyLarge?.copyWith(
                color: colors.onSurfaceVariant,
              ),
            ),
            SizedBox(height: 32.h),

            if (!_isLogin) ...[
              _buildTextField(
                controller: _nameController,
                icon: Icons.person_outline,
                hintText: 'Họ và tên',
              ),
              SizedBox(height: 16.h),
            ],

            _buildTextField(
              controller: _emailController,
              icon: Icons.email_outlined,
              hintText: 'Email',
              keyboardType: TextInputType.emailAddress,
            ),
            SizedBox(height: 16.h),

            _buildTextField(
              controller: _passwordController,
              icon: Icons.lock_outline,
              hintText: 'Mật khẩu',
              obscureText: true,
            ),
            SizedBox(height: 16.h),

            if (!_isLogin) ...[
              _buildTextField(
                controller: _phoneController,
                icon: Icons.call_outlined,
                hintText: 'Số điện thoại (Tuỳ chọn)',
                keyboardType: TextInputType.phone,
              ),
              SizedBox(height: 16.h),
            ],

            if (_isLogin)
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () => context.push('/forgot_password'),
                  child: Text(
                    'Quên mật khẩu?',
                    style: TextStyle(
                      color: colors.primary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),

            SizedBox(height: 24.h),
            SizedBox(
              width: double.infinity,
              height: 56.h,
              child: ElevatedButton(
                onPressed: authState.status == AuthStateStatus.loading ? null : _handleSubmit,
                child: authState.status == AuthStateStatus.loading
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                      )
                    : Text(
                        _isLogin ? 'Đăng nhập & vào bàn' : 'Đăng ký & vào bàn',
                        style: TextStyle(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
              ),
            ),

            SizedBox(height: 24.h),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  _isLogin ? 'Chưa có tài khoản?' : 'Đã có tài khoản?',
                  style: TextStyle(
                    color: colors.onSurfaceVariant,
                    fontSize: 14.sp,
                  ),
                ),
                TextButton(
                  onPressed: () {
                    setState(() {
                      _isLogin = !_isLogin;
                    });
                  },
                  child: Text(
                    _isLogin ? 'Đăng ký ngay' : 'Đăng nhập',
                    style: TextStyle(
                      color: colors.primary,
                      fontWeight: FontWeight.bold,
                      fontSize: 14.sp,
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required IconData icon,
    required String hintText,
    TextInputType? keyboardType,
    bool obscureText = false,
  }) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      obscureText: obscureText,
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
      ),
    );
  }
}
