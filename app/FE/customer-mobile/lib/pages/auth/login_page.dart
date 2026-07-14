import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../routes/app_routes.dart';

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
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color primaryFixed = Color(0xFFffdbd1);
}

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
        scrolledUnderElevation: 0,
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
            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
            decoration: BoxDecoration(
              color: _AppColors.primaryContainer,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              'GUEST',
              style: TextStyle(
                color: _AppColors.onPrimaryContainer,
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
                  color: _AppColors.primaryFixed.withOpacity(0.3),
                  shape: BoxShape.circle,
                ),
                child: Center(
                  child: Icon(
                    Icons.restaurant_menu,
                    color: _AppColors.primary,
                    size: 48.sp,
                  ),
                ),
              ),
            ),
            Text(
              'Chào mừng bạn đến với SmartDine',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: _AppColors.onSurface,
                fontSize: 26.sp,
                fontWeight: FontWeight.bold,
                letterSpacing: -0.5,
              ),
            ),
            SizedBox(height: 8.h),
            Text(
              'Trải nghiệm ẩm thực hiện đại trong tầm tay bạn.',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: _AppColors.onSurfaceVariant,
                fontSize: 16.sp,
              ),
            ),
            SizedBox(height: 32.h),

            // Table Information Card
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: _AppColors.surfaceContainerLowest,
                borderRadius: BorderRadius.circular(12.r),
                border: Border.all(color: _AppColors.outlineVariant.withOpacity(0.3)),
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
                      color: _AppColors.primaryContainer,
                      borderRadius: BorderRadius.circular(12.r),
                    ),
                    child: Center(
                      child: Text(
                        '12',
                        style: TextStyle(
                          color: _AppColors.onPrimaryContainer,
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
                          'Bàn của bạn',
                          style: TextStyle(
                            color: _AppColors.onSurface,
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        SizedBox(height: 4.h),
                        Row(
                          children: [
                            Icon(Icons.groups, color: _AppColors.onSurfaceVariant, size: 16.sp),
                            SizedBox(width: 4.w),
                            Text(
                              'Khu vực cửa sổ • 4 Chỗ ngồi',
                              style: TextStyle(
                                color: _AppColors.onSurfaceVariant,
                                fontSize: 14.sp,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(height: 24.h),

            // Guest Form
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: _AppColors.surfaceContainerLowest,
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
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildLabel('Số Bàn', true),
                  SizedBox(height: 8.h),
                  _buildTextField(
                    controller: _tableIdController,
                    icon: Icons.table_restaurant,
                    hintText: 'Nhập số bàn hoặc quét mã QR',
                    keyboardType: TextInputType.number,
                    suffixIcon: IconButton(
                      icon: Icon(Icons.qr_code_scanner, color: _AppColors.primary),
                      tooltip: 'Quét mã QR trên bàn',
                      onPressed: _handleScanQr,
                    ),
                  ),
                  SizedBox(height: 16.h),
                  _buildLabel('Tên của bạn', true),
                  SizedBox(height: 8.h),
                  _buildTextField(
                    controller: _nameController,
                    icon: Icons.person_outline,
                    hintText: 'Nhập tên để chúng tôi xưng hô',
                  ),
                  SizedBox(height: 16.h),
                  _buildLabel('Số điện thoại', false, '(Không bắt buộc)'),
                  SizedBox(height: 8.h),
                  _buildTextField(
                    controller: _phoneController,
                    icon: Icons.call_outlined,
                    hintText: 'Để tích điểm tự động',
                    keyboardType: TextInputType.phone,
                  ),
                  SizedBox(height: 24.h),
                  SizedBox(
                    width: double.infinity,
                    height: 56.h,
                    child: ElevatedButton(
                      onPressed: authState.status == AuthStateStatus.loading ? null : _handleGuestLogin,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: _AppColors.primary,
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12.r),
                        ),
                        elevation: 4,
                        shadowColor: _AppColors.primary.withOpacity(0.2),
                      ),
                      child: authState.status == AuthStateStatus.loading
                        ? SizedBox(
                            width: 24.sp,
                            height: 24.sp,
                            child: const CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                          )
                        : Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Text(
                                'Tiếp tục làm khách vãng lai',
                                style: TextStyle(
                                  fontSize: 16.sp,
                                  fontWeight: FontWeight.w600,
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
                  color: _AppColors.secondaryContainer,
                  borderRadius: BorderRadius.circular(12.r),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 48.r,
                      height: 48.r,
                      decoration: BoxDecoration(
                        color: _AppColors.onSecondaryContainer.withOpacity(0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        Icons.loyalty,
                        color: _AppColors.onSecondaryContainer,
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
                              color: _AppColors.onSecondaryContainer,
                              fontSize: 20.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          SizedBox(height: 4.h),
                          Text(
                            'Ưu đãi giảm 5% & tích điểm ngay',
                            style: TextStyle(
                              color: _AppColors.onSecondaryContainer.withOpacity(0.8),
                              fontSize: 14.sp,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Icon(
                      Icons.chevron_right,
                      color: _AppColors.onSecondaryContainer,
                    ),
                  ],
                ),
              ),
            ),
            SizedBox(height: 32.h),

            // Decorative Images Grid
            Row(
              children: [
                Expanded(
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(12.r),
                    child: ColorFiltered(
                      colorFilter: const ColorFilter.matrix([
                        0.2126, 0.7152, 0.0722, 0, 0,
                        0.2126, 0.7152, 0.0722, 0, 0,
                        0.2126, 0.7152, 0.0722, 0, 0,
                        0,      0,      0,      0.4, 0, // opacity 40%
                      ]),
                      child: Image.network(
                        'https://lh3.googleusercontent.com/aida-public/AB6AXuBX5tgS1X3Ax4IsSUEpahT_lvDfcgeW5OFqv_zUDUBBux-GBkGqONpoPTfBLc_Z7NeGUKaD2Qn3NFbXtV9d5xz6TM0azDkDN7F3v2bqmH-jrr4pGjL3EFIpTDCqU74fiI4wF59LroYjqk0sQ0ok_7k2be38u_PUToPjxoc7l3IccoUv1Gg6gGhLvNq1jA1PN1zuMKEsutU1oWMliGEZkpOk69-qwemyz1Vm1j4OJfagGxFvd3mlw6Ijibyfz7s-E0x4xKd4Aklep4At',
                        height: 128.h,
                        fit: BoxFit.cover,
                      ),
                    ),
                  ),
                ),
                SizedBox(width: 16.w),
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.only(top: 32.h), // Offset for asymmetric look
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(12.r),
                      child: ColorFiltered(
                        colorFilter: const ColorFilter.matrix([
                          0.2126, 0.7152, 0.0722, 0, 0,
                          0.2126, 0.7152, 0.0722, 0, 0,
                          0.2126, 0.7152, 0.0722, 0, 0,
                          0,      0,      0,      0.4, 0, // opacity 40%
                        ]),
                        child: Image.network(
                          'https://lh3.googleusercontent.com/aida-public/AB6AXuCiOxGv9fFWOrXdvFDbkKL--w7mbiAhQke_ZSqsHvcB-f1fNEGz4RGJA7iigO-Od5-X7xAE6sH3Ox9XUyY6hFbt89z1H8SXIeqf-fpp0pLEw51oiTU44NS3wMG9MVqs2Q1vMgdr-RbH4hEja2aujVxdRChCLw4fITVJPtc_16pzhSYbsggsn7HLrlJJ2NmflnjPRyufzLoAHHZRzQs2VcLI32JlvBFfUIh41Fa4R-aUCwtjXcgJWNGh2vw-2QSkMtDVJtd6-rw-Uj2W',
                          height: 128.h,
                          fit: BoxFit.cover,
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 32.h),

            // Support Message
            Text(
              '"Chúng tôi rất vui được phục vụ quý khách tại bàn hôm nay."',
              textAlign: TextAlign.center,
              style: TextStyle(
                color: _AppColors.onSurfaceVariant,
                fontSize: 14.sp,
                fontStyle: FontStyle.italic,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLabel(String text, bool isRequired, [String? suffix]) {
    return Row(
      children: [
        Text(
          text,
          style: TextStyle(
            color: _AppColors.onSurfaceVariant,
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
              color: _AppColors.primary,
              fontSize: 12.sp,
            ),
          ),
        ],
        if (suffix != null) ...[
          SizedBox(width: 4.w),
          Text(
            suffix,
            style: TextStyle(
              color: _AppColors.onSurfaceVariant.withOpacity(0.6),
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
  }) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
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
        suffixIcon: suffixIcon,
        filled: true,
        fillColor: _AppColors.background,
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
