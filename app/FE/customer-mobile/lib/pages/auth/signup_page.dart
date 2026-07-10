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
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color outline = Color(0xFF8f7068);
  static const Color secondary = Color(0xFF685b5a);
}

class SignupPage extends ConsumerStatefulWidget {
  const SignupPage({super.key});

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

  void _handleSubmit() async {
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

    bool success = false;
    final viewModel = ref.read(authViewModelProvider.notifier);

    if (_isLogin) {
      success = await viewModel.login(email, password);
    } else {
      if (name.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Vui lòng nhập họ tên')),
        );
        return;
      }
      success = await viewModel.register(name, email, password, phone);
    }

    if (success && mounted) {
      context.go('/home');
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);

    ref.listen<AuthState>(authViewModelProvider, (previous, next) {
      if (next.status == AuthStateStatus.error) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(next.errorMessage ?? 'Có lỗi xảy ra')),
        );
      }
    });

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.background,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.onSurface),
          onPressed: () => context.pop(),
        ),
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 16.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              _isLogin ? 'Đăng nhập thành viên' : 'Tạo tài khoản mới',
              style: TextStyle(
                color: _AppColors.onSurface,
                fontSize: 28.sp,
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              _isLogin 
                ? 'Đăng nhập để nhận ưu đãi giảm giá và tích điểm.' 
                : 'Trở thành khách hàng thân thiết của SmartDine.',
              style: TextStyle(
                color: _AppColors.onSurfaceVariant,
                fontSize: 16.sp,
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
                  onPressed: () {
                    // TODO: Navigate to forgot password
                  },
                  child: Text(
                    'Quên mật khẩu?',
                    style: TextStyle(
                      color: _AppColors.primary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),

            SizedBox(height: 24.h),
            SizedBox(
              height: 56.h,
              child: ElevatedButton(
                onPressed: authState.status == AuthStateStatus.loading ? null : _handleSubmit,
                style: ElevatedButton.styleFrom(
                  backgroundColor: _AppColors.primary,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12.r),
                  ),
                  elevation: 2,
                ),
                child: authState.status == AuthStateStatus.loading
                  ? const SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                    )
                  : Text(
                      _isLogin ? 'Đăng nhập' : 'Đăng ký',
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
                    color: _AppColors.onSurfaceVariant,
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
                      color: _AppColors.primary,
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
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      obscureText: obscureText,
      style: TextStyle(
        fontSize: 16.sp,
        color: _AppColors.onSurface,
      ),
      decoration: InputDecoration(
        hintText: hintText,
        hintStyle: TextStyle(
          color: _AppColors.outline,
          fontSize: 16.sp,
        ),
        prefixIcon: Icon(
          icon,
          color: _AppColors.outline,
        ),
        filled: true,
        fillColor: _AppColors.surfaceContainerLowest,
        contentPadding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 16.w),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12.r),
          borderSide: const BorderSide(color: _AppColors.outlineVariant),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12.r),
          borderSide: const BorderSide(color: _AppColors.primary),
        ),
      ),
    );
  }
}
