import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import '../../theme/app_theme.dart';

class _AppColors {
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color secondary = Color(0xFF685b5a);
}

enum CustomerNavTab { home, orders, cart, account }

/// Bottom nav dùng chung cho toàn bộ luồng khách hàng (Home/Orders/OrderTracking/
/// Profile/Settings) — trước đây mỗi trang tự copy-paste 1 bản riêng, mỗi bản có tập
/// tab khác nhau (có trang có "Giỏ hàng", có trang thay bằng "Cài đặt") khiến thanh
/// nav đổi hẳn bố cục mỗi khi chuyển trang. Cố định đúng 1 bộ 4 tab ở đây.
///
/// "Cài đặt" không phải 1 tab riêng — luôn vào từ nút gear trong trang Tài khoản,
/// nên trang Settings không có tab nào active (đang ở ngoài phạm vi 4 tab chính).
class CustomerBottomNav extends StatelessWidget {
  final CustomerNavTab? activeTab;

  const CustomerBottomNav({super.key, this.activeTab});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppTheme.surface,
        boxShadow: AppTheme.shadowBottomNav,
        borderRadius: BorderRadius.vertical(top: Radius.circular(16.r)),
      ),
      child: SafeArea(
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 8.h),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _NavItem(
                icon: Icons.menu_book,
                label: 'Thực đơn',
                isActive: activeTab == CustomerNavTab.home,
                onTap: () => context.go('/home'),
              ),
              _NavItem(
                icon: Icons.receipt_long,
                label: 'Đơn hàng',
                isActive: activeTab == CustomerNavTab.orders,
                onTap: () => context.go('/orders'),
              ),
              _NavItem(
                icon: Icons.shopping_cart,
                label: 'Giỏ hàng',
                isActive: activeTab == CustomerNavTab.cart,
                onTap: () => context.go('/cart'),
              ),
              _NavItem(
                icon: Icons.person,
                label: 'Tài khoản',
                isActive: activeTab == CustomerNavTab.account,
                onTap: () => context.go('/profile'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool isActive;
  final VoidCallback onTap;

  const _NavItem({
    required this.icon,
    required this.label,
    required this.isActive,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: isActive ? null : onTap,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
        decoration: BoxDecoration(
          color: isActive ? AppTheme.primaryContainer : Colors.transparent,
          borderRadius: BorderRadius.circular(12.r),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? AppTheme.primary : AppTheme.onSurfaceVariant,
              size: 24.sp,
            ),
            SizedBox(height: 4.h),
            Text(
              label,
              style: TextStyle(
                color: isActive ? AppTheme.primary : AppTheme.onSurfaceVariant,
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