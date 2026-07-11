import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/order_viewmodel.dart';


class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color surfaceContainerHighest = Color(0xFFe5e2e1);
  static const Color surfaceContainerHigh = Color(0xFFeae7e7);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color onSecondaryContainer = Color(0xFF6c605e);
  static const Color tertiary = Color(0xFF005cac);
  static const Color outlineVariant = Color(0xFFe3beb5);
  static const Color error = Color(0xFFba1a1a);
  static const Color onPrimary = Color(0xFFffffff);
}


class OrderListPage extends ConsumerWidget {
  const OrderListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final ordersAsync = ref.watch(orderListProvider);

    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: _AppColors.surface.withOpacity(0.8),
        elevation: 0,
        scrolledUnderElevation: 2,
        leading: IconButton(
          icon: Icon(Icons.arrow_back, color: _AppColors.primary),
          onPressed: () => context.pop(),
        ),
        title: Text(
          'Lịch sử đơn hàng',
          style: TextStyle(
            color: _AppColors.primary,
            fontSize: 20.sp,
            fontWeight: FontWeight.bold,
            letterSpacing: -0.5,
          ),
        ),
        centerTitle: true,
        actions: [
          SizedBox(width: 48.w), // Spacer for centering
        ],
      ),
      body: ordersAsync.when(
        data: (orders) {
          if (orders.isEmpty) {
            return const Center(child: Text('Chưa có đơn hàng nào.'));
          }
          return SingleChildScrollView(
            padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 16.h),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _buildDateHeader('Hôm nay'),
                SizedBox(height: 12.h),
                ...orders.map((order) => Padding(
                  padding: EdgeInsets.only(bottom: 16.h),
                  child: _buildOrderCard(
                    orderId: '#SD-${order.id}',
                    time: 'Hôm nay', // Use real date formatting in production
                    status: order.status,
                    statusColor: order.status == 'COMPLETED' ? _AppColors.tertiary : _AppColors.onSecondaryContainer,
                    statusBg: order.status == 'COMPLETED' ? _AppColors.tertiary.withOpacity(0.1) : _AppColors.secondaryContainer,
                    title: 'Đơn hàng Bàn ${order.tableNumber}',
                    price: '${order.finalAmount}đ',
                    imageUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCpE9ks-DpSB-h-XPKXxfApM8GyL70J_M50Akj2rBg69d7PWwYhxg1wc6PzeKQQyJBmcoa0Q-2U5TExraScWtNvpMxNmMkWYoIFV3bHuBjUrFLbbsmU0Yb4wNb6LHd_vCGInZ9M_bitKcLH285R91uJE9vbBITEp239VfLuq36fBPyBqr71tRxUCuFoKo7IcmdJnkBs8EQ6kU1vxLKlhg2MJUpDQFV3gHBwd5v00Ri6SOtr4uhF-rfC-NlruMvOqlh4580tsxkecMTj',
                    actions: [
                      _buildActionButton(
                        text: 'Chi tiết',
                        textColor: _AppColors.primary,
                        bgColor: _AppColors.primary.withOpacity(0.1),
                        onTap: () => context.push('/order-tracking'), // Navigate to tracking
                      ),
                    ],
                  ),
                )).toList(),
                SizedBox(height: 48.h),
              ],
            ),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, stack) => Center(child: Text('Lỗi tải danh sách: $err')),
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
        ),
        child: SafeArea(
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 8.h),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(Icons.menu_book, 'Thực đơn', false, () => context.go('/home')),
                _buildNavItem(Icons.receipt_long, 'Đơn hàng', true, () {}),
                _buildNavItem(Icons.person, 'Tài khoản', false, () => context.push('/profile')),
                _buildNavItem(Icons.settings, 'Cài đặt', false, () => context.push('/settings')),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildDateHeader(String title) {
    return Text(
      title.toUpperCase(),
      style: TextStyle(
        color: _AppColors.onSurfaceVariant,
        fontSize: 12.sp,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.2,
      ),
    );
  }

  Widget _buildOrderCard({
    required String orderId,
    required String time,
    required String status,
    required Color statusColor,
    required Color statusBg,
    required String title,
    required String price,
    required String imageUrl,
    List<Widget>? actions,
    Widget? customFooter,
    bool isGrayscale = false,
    double opacity = 1.0,
  }) {
    return Opacity(
      opacity: opacity,
      child: Container(
        padding: EdgeInsets.all(16.r),
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
        child: Column(
          children: [
            // Header: Order ID + Status
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      orderId,
                      style: TextStyle(
                        color: _AppColors.onSurfaceVariant,
                        fontSize: 14.sp,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      time,
                      style: TextStyle(
                        color: _AppColors.onSurfaceVariant,
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                  decoration: BoxDecoration(
                    color: statusBg,
                    borderRadius: BorderRadius.circular(100.r),
                  ),
                  child: Text(
                    status.toUpperCase(),
                    style: TextStyle(
                      color: statusColor,
                      fontSize: 10.sp,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 16.h),
            
            // Content: Image + Details
            Row(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(8.r),
                  child: isGrayscale
                      ? ColorFiltered(
                          colorFilter: const ColorFilter.matrix([
                            0.2126, 0.7152, 0.0722, 0, 0,
                            0.2126, 0.7152, 0.0722, 0, 0,
                            0.2126, 0.7152, 0.0722, 0, 0,
                            0,      0,      0,      1, 0,
                          ]),
                          child: Image.network(
                            imageUrl,
                            width: 64.r,
                            height: 64.r,
                            fit: BoxFit.cover,
                          ),
                        )
                      : Image.network(
                          imageUrl,
                          width: 64.r,
                          height: 64.r,
                          fit: BoxFit.cover,
                        ),
                ),
                SizedBox(width: 16.w),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: _AppColors.onSurface,
                          fontSize: 16.sp,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(height: 4.h),
                      Text(
                        price,
                        style: TextStyle(
                          color: _AppColors.primary,
                          fontSize: 18.sp,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 16.h),
            
            // Footer: Actions
            Container(
              padding: EdgeInsets.only(top: 12.h),
              decoration: BoxDecoration(
                border: Border(
                  top: BorderSide(color: _AppColors.outlineVariant.withOpacity(0.2)),
                ),
              ),
              child: customFooter ?? Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: actions ?? [],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActionButton({
    required String text,
    required Color textColor,
    required Color bgColor,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12.r),
      child: Container(
        height: 40.h,
        padding: EdgeInsets.symmetric(horizontal: 24.w),
        decoration: BoxDecoration(
          color: bgColor,
          borderRadius: BorderRadius.circular(12.r),
        ),
        alignment: Alignment.center,
        child: Text(
          text,
          style: TextStyle(
            color: textColor,
            fontSize: 12.sp,
            fontWeight: FontWeight.w600,
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
          color: isActive ? _AppColors.primaryContainer.withOpacity(0.1) : Colors.transparent,
          borderRadius: BorderRadius.circular(12.r),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? _AppColors.primary : _AppColors.onSurfaceVariant,
              size: 24.sp,
            ),
            SizedBox(height: 4.h),
            Text(
              label,
              style: TextStyle(
                color: isActive ? _AppColors.primary : _AppColors.onSurfaceVariant,
                fontSize: 12.sp,
                fontWeight: isActive ? FontWeight.bold : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
