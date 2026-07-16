import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../viewmodels/menu_viewmodel.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/cart_viewmodel.dart';
import '../../widgets/customer_bottom_nav.dart';
import 'package:intl/intl.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color primaryContainer = Color(0xFFd34011);
  static const Color background = Color(0xFFfcf9f8);
  static const Color surface = Color(0xFFfcf9f8);
  static const Color surfaceContainer = Color(0xFFf0eded);
  static const Color surfaceContainerLowest = Color(0xFFffffff);
  static const Color onSurface = Color(0xFF1b1c1c);
  static const Color onSurfaceVariant = Color(0xFF5a413a);
  static const Color secondaryContainer = Color(0xFFeddcda);
  static const Color outline = Color(0xFF8f7068);
}

class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  Timer? _searchDebounce;
  final TextEditingController _searchController = TextEditingController();
  final currencyFormat = NumberFormat('#,###', 'vi_VN');

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged(String value) {
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 400), () {
      ref.read(menuSearchQueryProvider.notifier).state = value.trim();
    });
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authViewModelProvider);
    return _buildCustomerView(context, ref, authState);
  }

  Widget _buildCustomerView(BuildContext context, WidgetRef ref, AuthState authState) {
    final tableNumber = authState.guestSession?.tableNumber ?? 1;

    final categoriesAsync = ref.watch(menuCategoriesProvider);

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
                    Flexible(
                      child:Text(
                      'SmartDine',
                      style: TextStyle(
                        color: _AppColors.primary,
                        fontSize: 20.sp,
                        fontWeight: FontWeight.bold,
                      ),
                      ),
                    ),
                  ],
                ),
                actions: [
                  // Nút thanh toán — vào thẳng hóa đơn tạm tính (/invoice, OrderHistoryPage)
                  // để khách xem chi tiết món + chọn phương thức thanh toán. Trước đây màn
                  // hình này tồn tại nhưng không có lối vào nào từ app, còn logic thanh toán
                  // lại bị nhúng trùng lặp (và chết, không ai gọi tới) ở trang "Lịch sử đơn hàng".
                  InkWell(
                    onTap: () => context.push('/invoice'),
                    borderRadius: BorderRadius.circular(100.r),
                    child: Container(
                      padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 4.h),
                      decoration: BoxDecoration(
                        color: _AppColors.primary,
                        borderRadius: BorderRadius.circular(100.r),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.payment, color: Colors.white, size: 16.sp),
                          SizedBox(width: 4.w),
                          Text(
                            'Thanh toán',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 12.sp,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  SizedBox(width: 8.w),
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
                              controller: _searchController,
                              onChanged: _onSearchChanged,
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
                    categoriesAsync.when(
                      data: (categories) {
                        final selectedCategoryId = ref.watch(selectedCategoryIdProvider);
                        // index 0 = "Tất cả" (categoryId = null); các index sau map 1-1 với categories.
                        final allCategories = ['Tất cả', ...categories.map((c) => c.name)];
                        return SizedBox(
                          height: 40.h,
                          child: ListView.separated(
                            padding: EdgeInsets.symmetric(horizontal: 20.w),
                            scrollDirection: Axis.horizontal,
                            itemCount: allCategories.length,
                            separatorBuilder: (context, index) => SizedBox(width: 12.w),
                            itemBuilder: (context, index) {
                              final categoryId = index == 0 ? null : categories[index - 1].id;
                              final isSelected = selectedCategoryId == categoryId;
                              return GestureDetector(
                                onTap: () {
                                  ref.read(selectedCategoryIdProvider.notifier).state = categoryId;
                                },
                                child: Container(
                                  alignment: Alignment.center,
                                  padding: EdgeInsets.symmetric(horizontal: 24.w),
                                  decoration: BoxDecoration(
                                    color: isSelected ? _AppColors.primary : _AppColors.secondaryContainer,
                                    borderRadius: BorderRadius.circular(100.r),
                                  ),
                                  child: Text(
                                    allCategories[index],
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
                        );
                      },
                      loading: () => SizedBox(
                        height: 40.h,
                        child: ListView.separated(
                          padding: EdgeInsets.symmetric(horizontal: 20.w),
                          scrollDirection: Axis.horizontal,
                          itemCount: 5,
                          separatorBuilder: (context, index) => SizedBox(width: 12.w),
                          itemBuilder: (context, index) {
                            return Container(
                              width: 80.w,
                              height: 40.h,
                              decoration: BoxDecoration(
                                color: _AppColors.surfaceContainer,
                                borderRadius: BorderRadius.circular(100.r),
                              ),
                            );
                          },
                        ),
                      ),
                      error: (_, __) => SizedBox(
                        height: 40.h,
                        child: ListView(
                          padding: EdgeInsets.symmetric(horizontal: 20.w),
                          scrollDirection: Axis.horizontal,
                          children: [
                            Container(
                              alignment: Alignment.center,
                              padding: EdgeInsets.symmetric(horizontal: 24.w),
                              decoration: BoxDecoration(
                                color: _AppColors.primary,
                                borderRadius: BorderRadius.circular(100.r),
                              ),
                              child: Text(
                                'Tất cả',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 12.sp,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ),
                          ],
                        ),
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
                                    // Chỉ hiện badge sao khi BE thật sự trả averageRating — không bịa số
                                    // đánh giá giả cho món chưa có review nào.
                                    if (item.rating != null)
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
                                                item.rating!.toStringAsFixed(1),
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
                                            '${item.price}',
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

        ],
      ),

      // Bottom nav dùng chung — trước đây có thêm 1 thanh "Giỏ hàng của bàn" luôn nổi
      // phía trên nav kể cả khi giỏ trống (0 món • 0k), đã bỏ vì khách đã có tab
      // "Giỏ hàng" ngay dưới để xem khi cần, không cần nhắc lại liên tục.
      bottomNavigationBar: const CustomerBottomNav(activeTab: CustomerNavTab.home),
    );
  }
}
