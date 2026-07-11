import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/menu_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/cart_viewmodel.dart';
import '../../models/menu_models.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color onPrimaryContainer = Color(0xFFffffff);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainer = Color(0xFFf0eded);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color outline = Color(0xFF8f7068);
  static const Color primaryFixed = Color(0xFFffdbd1);
  static const Color secondary = Color(0xFF685b5a);
  static const Color surfaceVariant = Color(0xFFe5e2e1);
}

class MenuItemModel {
  final String title;
  final String imageUrl;
  final double rating;
  final String price;

  MenuItemModel({
    required this.title,
    required this.imageUrl,
    required this.rating,
    required this.price,
  });
}


class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  int _selectedCategoryIndex = 0;
  final List<String> _categories = [
    'Tất cả',
    'Lẩu & Nướng',
    'Món Khai Vị',
    'Tráng Miệng',
    'Đồ Uống'
  ];


  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    final tableNumber = authState.guestSession?.tableNumber ?? 1;

    final cartState = ref.watch(cartViewModelProvider);
    final cartItemCount = cartState.items.fold<int>(0, (sum, item) => sum + item.quantity);
    final cartTotal = cartState.total;

    return Scaffold(
      backgroundColor: _AppColors.background,
      body: Stack(
        children: [
          CustomScrollView(
            slivers: [
              // App Bar & Search
              SliverAppBar(
                backgroundColor: _AppColors.surface,
                pinned: true,
                elevation: 0,
                scrolledUnderElevation: 2,
                surfaceTintColor: Colors.transparent,
                toolbarHeight: 60.h,
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
                    padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
                    decoration: BoxDecoration(
                      color: _AppColors.primaryContainer.withOpacity(0.1),
                      border: Border.all(color: _AppColors.primaryContainer.withOpacity(0.2)),
                      borderRadius: BorderRadius.circular(100.r),
                    ),
                    child: Row(
                      children: [
                        Icon(Icons.table_restaurant, color: _AppColors.primary, size: 16.sp),
                        SizedBox(width: 4.w),
                        Text(
                          'Bàn $tableNumber',
                          style: TextStyle(
                            color: _AppColors.primary,
                            fontSize: 12.sp,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                bottom: PreferredSize(
                  preferredSize: Size.fromHeight(64.h),
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(20.w, 0, 20.w, 16.h),
                    child: Container(
                      height: 48.h,
                      decoration: BoxDecoration(
                        color: _AppColors.surfaceContainer,
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      padding: EdgeInsets.symmetric(horizontal: 16.w),
                      child: Row(
                        children: [
                          Icon(Icons.search, color: _AppColors.onSurfaceVariant, size: 20.sp),
                          SizedBox(width: 12.w),
                          Expanded(
                            child: TextField(
                              style: TextStyle(
                                fontSize: 16.sp,
                                color: _AppColors.onSurface,
                              ),
                              decoration: InputDecoration(
                                hintText: 'Tìm món ăn...',
                                hintStyle: TextStyle(
                                  color: _AppColors.outline,
                                  fontSize: 16.sp,
                                ),
                                border: InputBorder.none,
                                isDense: true,
                              ),
                            ),
                          ),
                          Icon(Icons.tune, color: _AppColors.onSurfaceVariant, size: 20.sp),
                        ],
                      ),
                    ),
                  ),
                ),
              ),

              SliverToBoxAdapter(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Categories List
                    SizedBox(height: 16.h),
                    SizedBox(
                      height: 40.h,
                      child: ListView.separated(
                        padding: EdgeInsets.symmetric(horizontal: 20.w),
                        scrollDirection: Axis.horizontal,
                        itemCount: _categories.length,
                        separatorBuilder: (context, index) => SizedBox(width: 12.w),
                        itemBuilder: (context, index) {
                          final isSelected = _selectedCategoryIndex == index;
                          return GestureDetector(
                            onTap: () {
                              setState(() {
                                _selectedCategoryIndex = index;
                              });
                            },
                            child: Container(
                              alignment: Alignment.center,
                              padding: EdgeInsets.symmetric(horizontal: 24.w),
                              decoration: BoxDecoration(
                                color: isSelected ? _AppColors.primary : _AppColors.secondaryContainer,
                                borderRadius: BorderRadius.circular(100.r),
                              ),
                              child: Text(
                                _categories[index],
                                style: TextStyle(
                                  color: isSelected ? Colors.white : _AppColors.primary,
                                  fontSize: 12.sp,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          );
                        },
                      ),
                    ),

                    // Featured Banner
                    SizedBox(height: 24.h),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 20.w),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(24.r),
                        child: AspectRatio(
                          aspectRatio: 16 / 9,
                          child: Stack(
                            fit: StackFit.expand,
                            children: [
                              Image.network(
                                'https://lh3.googleusercontent.com/aida-public/AB6AXuBj0hUT5Eb5C_HSnRvwVydi1vZP2Kj813DK_jzABQ5oSHqAaOIp0buH31jGzUILWenfCb91eq_o0y09tSkBeSc2WKxyH1ylWxHrnQMi2dDuBUCQ4crfZPQSAaMPZzw7C37rXl3DPbfxnR5fcHa_57RQ1Ap2mWRL9_6AbbtkTCBrTmFCzYZx8Bfn3GuZh1kdNJKGLLI7b8NXEPfx2F4l1mmSdrJLVxzMvzDKorTwLs83SJxvnmKv76aU1IuDcty9d_T0ZcMnN95ZXXKt',
                                fit: BoxFit.cover,
                              ),
                              Container(
                                decoration: BoxDecoration(
                                  gradient: LinearGradient(
                                    colors: [
                                      Colors.black.withOpacity(0.8),
                                      Colors.black.withOpacity(0.2),
                                      Colors.transparent,
                                    ],
                                    begin: Alignment.bottomCenter,
                                    end: Alignment.topCenter,
                                  ),
                                ),
                              ),
                              Padding(
                                padding: EdgeInsets.all(24.r),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  mainAxisAlignment: MainAxisAlignment.end,
                                  children: [
                                    Text(
                                      'MÓN NGON MỖI NGÀY',
                                      style: TextStyle(
                                        color: Colors.white.withOpacity(0.8),
                                        fontSize: 12.sp,
                                        fontWeight: FontWeight.w600,
                                        letterSpacing: 1.2,
                                      ),
                                    ),
                                    SizedBox(height: 4.h),
                                    Text(
                                      'Gợi ý món ngon\nhôm nay',
                                      style: TextStyle(
                                        color: Colors.white,
                                        fontSize: 26.sp,
                                        fontWeight: FontWeight.bold,
                                        height: 1.2,
                                      ),
                                    ),
                                    SizedBox(height: 16.h),
                                    ElevatedButton(
                                      onPressed: () {},
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: _AppColors.primary,
                                        foregroundColor: Colors.white,
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(12.r),
                                        ),
                                        padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
                                        elevation: 0,
                                      ),
                                      child: Text(
                                        'Khám phá ngay',
                                        style: TextStyle(
                                          fontSize: 16.sp,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),

                    // Menu Grid Title
                    SizedBox(height: 32.h),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: 20.w),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            'Phổ biến',
                            style: TextStyle(
                              color: _AppColors.onSurface,
                              fontSize: 20.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          Text(
                            'Xem tất cả',
                            style: TextStyle(
                              color: _AppColors.primary,
                              fontSize: 12.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(height: 16.h),
                  ],
                ),
              ),

              // Menu Grid Items
              SliverPadding(
                padding: EdgeInsets.only(left: 20.w, right: 20.w, bottom: 120.h),
                sliver: ref.watch(menuItemsProvider).when(
                  data: (items) => SliverGrid(
                    gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      mainAxisSpacing: 16.h,
                      crossAxisSpacing: 16.w,
                      childAspectRatio: 0.72,
                    ),
                    delegate: SliverChildBuilderDelegate(
                      (context, index) {
                        final item = items[index];
                        return Container(
                          decoration: BoxDecoration(
                            color: _AppColors.surfaceContainerLowest,
                            borderRadius: BorderRadius.circular(16.r),
                            boxShadow: [
                              BoxShadow(
                                color: Colors.black.withOpacity(0.03),
                                blurRadius: 10,
                                offset: const Offset(0, 2),
                              ),
                            ],
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Image
                              Expanded(
                                flex: 5,
                                child: Stack(
                                  fit: StackFit.expand,
                                  children: [
                                    ClipRRect(
                                      borderRadius: BorderRadius.vertical(top: Radius.circular(16.r)),
                                      child: item.imageUrl != null && item.imageUrl!.isNotEmpty
                                          ? Image.network(
                                              item.imageUrl!,
                                              fit: BoxFit.cover,
                                            )
                                          : Container(
                                              color: _AppColors.surfaceContainer,
                                              child: Icon(Icons.restaurant, color: _AppColors.outline, size: 40.sp),
                                            ),
                                    ),
                                    Positioned(
                                      top: 8.h,
                                      right: 8.w,
                                      child: Container(
                                        padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
                                        decoration: BoxDecoration(
                                          color: Colors.white.withOpacity(0.9),
                                          borderRadius: BorderRadius.circular(100.r),
                                          boxShadow: [
                                            BoxShadow(
                                              color: Colors.black.withOpacity(0.1),
                                              blurRadius: 4,
                                            ),
                                          ],
                                        ),
                                        child: Row(
                                          children: [
                                            Icon(Icons.star, color: Colors.amber, size: 14.sp),
                                            SizedBox(width: 2.w),
                                            Text(
                                              item.rating.toString(),
                                              style: TextStyle(
                                                color: _AppColors.onSurface,
                                                fontSize: 10.sp,
                                                fontWeight: FontWeight.bold,
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              // Details
                              Expanded(
                                flex: 4,
                                child: Padding(
                                  padding: EdgeInsets.all(12.r),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        item.name,
                                        maxLines: 2,
                                        overflow: TextOverflow.ellipsis,
                                        style: TextStyle(
                                          color: _AppColors.onSurface,
                                          fontSize: 14.sp,
                                          fontWeight: FontWeight.bold,
                                          height: 1.2,
                                        ),
                                      ),
                                      const Spacer(),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Text(
                                            '${item.price}k',
                                            style: TextStyle(
                                              color: _AppColors.primary,
                                              fontSize: 16.sp,
                                              fontWeight: FontWeight.bold,
                                            ),
                                          ),
                                          Container(
                                            width: 32.r,
                                            height: 32.r,
                                            decoration: BoxDecoration(
                                              color: _AppColors.primary,
                                              shape: BoxShape.circle,
                                              boxShadow: [
                                                BoxShadow(
                                                  color: _AppColors.primary.withOpacity(0.3),
                                                  blurRadius: 8,
                                                  offset: const Offset(0, 2),
                                                ),
                                              ],
                                            ),
                                            child: InkWell(
                                              onTap: () {
                                                ref.read(cartViewModelProvider.notifier).addItem(item);
                                                ScaffoldMessenger.of(context).showSnackBar(
                                                  SnackBar(content: Text('Đã thêm ${item.name} vào giỏ hàng')),
                                                );
                                              },
                                              child: Icon(
                                                Icons.add,
                                                color: Colors.white,
                                                size: 20.sp,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                      childCount: items.length,
                    ),
                  ),
                  loading: () => const SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(40.0),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  ),
                  error: (error, stack) => SliverToBoxAdapter(
                    child: Padding(
                      padding: EdgeInsets.all(40.0),
                      child: Center(child: Text('Lỗi: $error')),
                    ),
                  ),
                ),
              ),
            ],
          ),

          // Floating Cart Summary
          Positioned(
            bottom: 24.h,
            left: 20.w,
            right: 20.w,
            child: Container(
              padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 12.h),
              decoration: BoxDecoration(
                color: _AppColors.onSurface.withOpacity(0.95),
                borderRadius: BorderRadius.circular(16.r),
                border: Border.all(color: Colors.white.withOpacity(0.1)),
                boxShadow: [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.2),
                    blurRadius: 20,
                    offset: const Offset(0, 8),
                  ),
                ],
              ),
              child: Row(
                children: [
                  Stack(
                    clipBehavior: Clip.none,
                    children: [
                      Icon(Icons.shopping_bag_outlined, color: _AppColors.primaryFixed, size: 28.sp),
                      Positioned(
                        top: -4,
                        right: -4,
                        child: Container(
                          width: 16.r,
                          height: 16.r,
                          decoration: const BoxDecoration(
                            color: _AppColors.primary,
                            shape: BoxShape.circle,
                          ),
                          child: Center(
                            child: Text(
                              '$cartItemCount',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 10.sp,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(width: 16.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Giỏ hàng của bàn',
                          style: TextStyle(
                            color: Colors.white.withOpacity(0.7),
                            fontSize: 12.sp,
                          ),
                        ),
                        Text(
                          '$cartItemCount món • ${cartTotal}k',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 16.sp,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                  ElevatedButton(
                    onPressed: () => context.push('/cart'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: _AppColors.primary,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12.r),
                      ),
                      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
                      elevation: 4,
                      shadowColor: _AppColors.primary.withOpacity(0.4),
                    ),
                    child: Text(
                      'Xem giỏ',
                      style: TextStyle(
                        fontSize: 14.sp,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
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
            padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 8.h),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildNavItem(Icons.menu_book, 'Thực đơn', true, () {}),
                _buildNavItem(Icons.receipt_long, 'Đơn hàng', false, () => context.push('/orders')),
                _buildNavItem(Icons.shopping_cart, 'Giỏ hàng', false, () => context.push('/cart')),
                _buildNavItem(Icons.person, 'Tài khoản', false, () => context.push('/profile')),
              ],
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
          color: isActive ? _AppColors.primaryContainer : Colors.transparent,
          borderRadius: BorderRadius.circular(12.r),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? _AppColors.onPrimaryContainer : _AppColors.secondary,
              size: 24.sp,
            ),
            SizedBox(height: 4.h),
            Text(
              label,
              style: TextStyle(
                color: isActive ? _AppColors.onPrimaryContainer : _AppColors.secondary,
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
