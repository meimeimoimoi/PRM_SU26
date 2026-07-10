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
  static const Color surfaceContainerLow = Color(0xFFf6f3f2);
  static const Color surfaceContainerHigh = Color(0xFFeae7e7);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color surfaceVariant = Color(0xFFe5e2e1);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondary = Color(0xFF685b5a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color tertiary = Color(0xFF005cac);
  static const Color onTertiaryContainer = Color(0xFFfefcff);
  static const Color outline = Color(0xFF8f7068);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color errorContainer = Color(0xFFffdad6);
  static const Color onErrorContainer = Color(0xFF93000a);
  static const Color error = Color(0xFFba1a1a);
  static const Color onPrimary = Color(0xFFffffff);
}

class InvoiceItem {
  final String title;
  final String imageUrl;
  final String note;
  final String price;

  InvoiceItem({
    required this.title,
    required this.imageUrl,
    required this.note,
    required this.price,
  });
}

class OrderHistoryPage extends StatefulWidget {
  const OrderHistoryPage({super.key});

  @override
  State<OrderHistoryPage> createState() => _OrderHistoryPageState();
}

class _OrderHistoryPageState extends State<OrderHistoryPage> {
  int _selectedPaymentMethod = 0; // 0: Wallet, 1: QR, 2: Cash

  final List<InvoiceItem> _items = [
    InvoiceItem(
      title: 'Phở Bò Đặc Biệt',
      imageUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCB0qjvb5fvSoVIpZoItxTYB_Fy0GtrL7tMPYPqat9pFhQ21tkCCn5TE2tmmuIa3WoXm7HV-ClUAhYoweiknIIpoLg15F0bk5wely6-JxzQtT67ullN5VM9WblG6ubJj8H3aWS2FUdByiInJWVnfBNAM-h7T9LQ6h9z3CdcwgAcDIwq-XS4NJZCPpfRv1D3GMc6r4mi393D5SdnUglYzPcG9J9V8sNsWYYqe6iTZCbzWgyn9M5R1onGJFX3_oBBRU_M7EWDc_xxonGH',
      note: 'x1 • Không hành tây',
      price: '125.000đ',
    ),
    InvoiceItem(
      title: 'Cà Phê Sữa Đá',
      imageUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAXaVJX4OAXvpUzYMr1MJPg2W9SNCamv7zICVAM7i3MwkXoAE6ErqZc2Ex_w5yjAQKzKUIrutxNRgrkiQjh8TdVnwTwS4uL-UK-3L_NTmTyAqfsgGxDP29T67DoN1CRqAhrYgkL2KtjwCWvINFN61KaCcodSj4eZATULBw4gA9xpsCEerJ0GuwRzvuu7ktJOfe-AcFusKOV2YMPwuyonS0qiymQPuAJdhsUs-Glg9wUIcgspGSvYzx4vDi4J4jFOBCBtFH_A8YD-eiD',
      note: 'x2 • Ít đường',
      price: '90.000đ',
    ),
    InvoiceItem(
      title: 'Gỏi Cuốn Tôm Thịt',
      imageUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDSFUlph_Rk9PYgBr12W3WZLOUqCYvOcmaUiGqz8vRFh0vj1ZDfzaTizlWeKWMcr1bBsFenje49WqCZJBAdXzZaC4BytfPwIkAfrMjW9upPqao_TidU3SflAe-Run8sXLKgMnAeKqaUPcHsMHjjqzK4-DPu7U_fwmFA6wkb9hOZ9-pmfy6dvGSXXvBmwi3p5ASCVM6GqaZotW707ejaXT5R4B9PNOyMxZP-HfKSABx_zaloRR_CXzPhyS-vgCeMt6cSNiu3pLDJ5tO0',
      note: 'x1 • Phần 3 cuốn',
      price: '65.000đ',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Hóa đơn tạm tính',
          style: TextStyle(
            color: _AppColors.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          Container(
            margin: EdgeInsets.only(right: 20.w),
            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
            decoration: BoxDecoration(
              color: _AppColors.surfaceVariant,
              borderRadius: BorderRadius.circular(100.r),
            ),
            child: Text(
              'Bàn 12',
              style: TextStyle(
                color: _AppColors.onSurfaceVariant,
                fontSize: 12.sp,
                fontWeight: FontWeight.bold,
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
            // Details Section
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: _AppColors.surfaceContainerLowest,
                borderRadius: BorderRadius.circular(16.r),
                border: Border.all(color: _AppColors.surfaceVariant),
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
                  Text(
                    'Chi tiết món ăn',
                    style: TextStyle(
                      color: _AppColors.onSurface,
                      fontSize: 20.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  SizedBox(height: 16.h),
                  ..._items.map((item) {
                    final isLast = _items.indexOf(item) == _items.length - 1;
                    return Container(
                      padding: EdgeInsets.only(bottom: 16.h),
                      margin: EdgeInsets.only(bottom: isLast ? 0 : 16.h),
                      decoration: BoxDecoration(
                        border: isLast
                            ? null
                            : const Border(bottom: BorderSide(color: _AppColors.surfaceVariant)),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              ClipRRect(
                                borderRadius: BorderRadius.circular(8.r),
                                child: Image.network(
                                  item.imageUrl,
                                  width: 48.r,
                                  height: 48.r,
                                  fit: BoxFit.cover,
                                ),
                              ),
                              SizedBox(width: 16.w),
                              Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    item.title,
                                    style: TextStyle(
                                      color: _AppColors.onSurface,
                                      fontSize: 16.sp,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                  SizedBox(height: 2.h),
                                  Text(
                                    item.note,
                                    style: TextStyle(
                                      color: _AppColors.secondary,
                                      fontSize: 14.sp,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                          Text(
                            item.price,
                            style: TextStyle(
                              color: _AppColors.primary,
                              fontSize: 18.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    );
                  }),
                ],
              ),
            ),
            SizedBox(height: 16.h),

            // Loyalty Rewards
            Container(
              padding: EdgeInsets.all(16.r),
              decoration: BoxDecoration(
                color: _AppColors.primaryContainer,
                borderRadius: BorderRadius.circular(16.r),
              ),
              child: Stack(
                children: [
                  Row(
                    children: [
                      Icon(Icons.stars, color: _AppColors.primaryFixed, size: 24.sp),
                      SizedBox(width: 12.w),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Loyalty Rewards',
                            style: TextStyle(
                              color: _AppColors.onPrimaryContainer.withOpacity(0.9),
                              fontSize: 12.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          Text(
                            'Điểm tích lũy nhận được: +83 points',
                            style: TextStyle(
                              color: _AppColors.onPrimaryContainer,
                              fontSize: 14.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
            SizedBox(height: 32.h),

            // Payment Methods
            Text(
              'Phương thức thanh toán',
              style: TextStyle(
                color: _AppColors.onSurface,
                fontSize: 20.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
            SizedBox(height: 16.h),
            
            _buildPaymentOption(
              index: 0,
              title: 'Ví Điện Tử (Momo/ZaloPay)',
              icon: Icons.account_balance_wallet,
              iconBgColor: _AppColors.onTertiaryContainer,
              iconColor: _AppColors.tertiary,
            ),
            SizedBox(height: 12.h),
            _buildPaymentOption(
              index: 1,
              title: 'Thẻ Ngân Hàng/VietQR',
              icon: Icons.qr_code_2,
              iconBgColor: _AppColors.secondaryContainer,
              iconColor: _AppColors.secondary,
            ),
            SizedBox(height: 12.h),
            _buildPaymentOption(
              index: 2,
              title: 'Tiền Mặt tại quầy',
              icon: Icons.payments,
              iconBgColor: _AppColors.surfaceContainerHighest,
              iconColor: _AppColors.onSurfaceVariant,
            ),
            
            SizedBox(height: 32.h),

            // VietQR Card Mock
            Container(
              padding: EdgeInsets.all(24.r),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(24.r),
                border: Border.all(color: _AppColors.surfaceVariant),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.1),
                    blurRadius: 20,
                    offset: const Offset(0, 10),
                  ),
                ],
              ),
              child: Column(
                children: [
                  Container(
                    height: 4.h,
                    width: double.infinity,
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [_AppColors.primary, _AppColors.primaryContainer],
                      ),
                      borderRadius: BorderRadius.circular(4.r),
                    ),
                  ),
                  SizedBox(height: 16.h),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'VIETQR QUICK SCAN',
                        style: TextStyle(
                          color: _AppColors.secondary,
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 1.5,
                        ),
                      ),
                      Image.network(
                        'https://lh3.googleusercontent.com/aida-public/AB6AXuB94Pnd5m8b9hPt0wgRoNx9jBXBUwF_VLhf8o90-HQz6kew8wnjl5nLWQjwKq3FGD__hlAKV0y_JNq4NdwYSomPQSNFN6yUL7LU0hdh506ngzqwvH2gAH-y92igscrkJyWOnBum_3UmlKT2BV_gYoRZNj31z45bT0zYWitaPqZ3mosBkCFu5JNs3HwEN92TC8MgzPStnCuzB6TeRs8OazKgrzEWWwFiY2KTEa2ace7zFtIHlUS-iK-OUOWj_btpMmf8NpQd4VO-sCDg',
                        height: 20.h,
                      ),
                    ],
                  ),
                  SizedBox(height: 24.h),
                  Container(
                    padding: EdgeInsets.all(16.r),
                    decoration: BoxDecoration(
                      color: _AppColors.surfaceContainerLow,
                      borderRadius: BorderRadius.circular(16.r),
                      border: Border.all(color: _AppColors.outlineVariant),
                    ),
                    child: Container(
                      width: 192.r,
                      height: 192.r,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(8.r),
                        image: const DecorationImage(
                          image: NetworkImage('https://lh3.googleusercontent.com/aida-public/AB6AXuDgyulBZV-utYUj7AcyfVxnkZym_s-BuY5h6YdumF9oOfRaTjsX8ynLcPb8X6XFHFG5uWa_CQ9gFa31pjqZ5P3T_6FAC1kvLBaLdoFVajxDdMQ1K01AR3fYG6Z7MeYSRS51nGBcHc9PEXn1AIOxzOGQwO1LM5n57SwCQUvdfG7Htfd2vNcLGqCMxvUhKbJoyyhoWGj-yKFMrPZLLNkYAvmPyQ0VnFiWiBy7uwAvx9jSpkJkXP6wZe-uCQbL4zn4edn2kMrMmYtBwVFR'),
                          fit: BoxFit.cover,
                        ),
                      ),
                      child: Center(
                        child: Container(
                          padding: EdgeInsets.all(4.r),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(8.r),
                            boxShadow: [
                              BoxShadow(
                                color: Colors.black.withOpacity(0.1),
                                blurRadius: 4,
                              ),
                            ],
                          ),
                          child: Icon(Icons.restaurant, color: _AppColors.primary, size: 24.sp),
                        ),
                      ),
                    ),
                  ),
                  SizedBox(height: 24.h),
                  Text(
                    'Số tiền thanh toán',
                    style: TextStyle(
                      color: _AppColors.secondary,
                      fontSize: 14.sp,
                    ),
                  ),
                  SizedBox(height: 4.h),
                  Text(
                    '280.000đ',
                    style: TextStyle(
                      color: _AppColors.onSurface,
                      fontSize: 26.sp,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  SizedBox(height: 16.h),
                  Container(
                    padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
                    decoration: BoxDecoration(
                      color: _AppColors.surfaceContainerHigh,
                      borderRadius: BorderRadius.circular(100.r),
                    ),
                    child: Text(
                      'Tài khoản: SmartDine Restaurant - Vietcombank',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: _AppColors.onSurfaceVariant,
                        fontSize: 12.sp,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(height: 32.h),

            // Footer Warning
            Container(
              padding: EdgeInsets.all(16.r),
              decoration: BoxDecoration(
                color: _AppColors.errorContainer,
                borderRadius: BorderRadius.circular(16.r),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info, color: _AppColors.error, size: 24.sp),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Text(
                      'Vui lòng dọn bàn sạch sẽ sau khi nhân viên xác nhận thanh toán thành công. Cảm ơn quý khách!',
                      style: TextStyle(
                        color: _AppColors.onErrorContainer,
                        fontSize: 14.sp,
                        height: 1.2,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            
            // Bottom Padding
            SizedBox(height: 40.h),
          ],
        ),
      ),
      bottomNavigationBar: Container(
        padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w, 32.h),
        decoration: BoxDecoration(
          color: _AppColors.surface,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.04),
              blurRadius: 20,
              offset: const Offset(0, -4),
            ),
          ],
        ),
        child: ElevatedButton(
          onPressed: () {},
          style: ElevatedButton.styleFrom(
            backgroundColor: _AppColors.primary,
            foregroundColor: _AppColors.onPrimary,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12.r),
            ),
            padding: EdgeInsets.symmetric(vertical: 16.h),
            elevation: 4,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.check_circle, size: 24.sp),
              SizedBox(width: 8.w),
              Text(
                'Xác nhận đã thanh toán',
                style: TextStyle(
                  fontSize: 18.sp,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPaymentOption({
    required int index,
    required String title,
    required IconData icon,
    required Color iconBgColor,
    required Color iconColor,
  }) {
    final isSelected = _selectedPaymentMethod == index;

    return InkWell(
      onTap: () {
        setState(() {
          _selectedPaymentMethod = index;
        });
      },
      borderRadius: BorderRadius.circular(16.r),
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: _AppColors.surfaceContainerLowest,
          borderRadius: BorderRadius.circular(16.r),
          border: Border.all(
            color: isSelected ? _AppColors.primary : Colors.transparent,
            width: 2,
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.02),
              blurRadius: 10,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Row(
              children: [
                Container(
                  width: 40.r,
                  height: 40.r,
                  decoration: BoxDecoration(
                    color: iconBgColor,
                    borderRadius: BorderRadius.circular(12.r),
                  ),
                  child: Icon(icon, color: iconColor, size: 24.sp),
                ),
                SizedBox(width: 16.w),
                Text(
                  title,
                  style: TextStyle(
                    color: _AppColors.onSurface,
                    fontSize: 16.sp,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
            Icon(
              isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
              color: isSelected ? _AppColors.primary : _AppColors.outline,
            ),
          ],
        ),
      ),
    );
  }
}
