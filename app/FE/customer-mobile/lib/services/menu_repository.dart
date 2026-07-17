import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/menu_models.dart';
import 'api_client.dart';

final menuRepositoryProvider = Provider<MenuRepository>((ref) {
  return MenuRepository(ref.watch(dioProvider));
});

class MenuRepository {
  final Dio _dio;

  MenuRepository(this._dio);

  Future<List<MenuCategory>> getMenuCategories() async {
    final response = await _dio.get('menu-categories');
    final data = response.data['data'] as List;
    return data.map((e) => MenuCategory.fromJson(e)).toList();
  }

  /// Lấy toàn bộ menu (mọi trang) — BE mặc định phân trang, gọi 1 lần với limit cố định sẽ
  /// âm thầm cắt mất món ở trang sau nếu nhà hàng có nhiều hơn limit món. Vòng lặp cho tới
  /// khi hết totalPages để khách hàng luôn thấy đủ menu.
  Future<List<MenuItemSummary>> getMenuItems({int? categoryId, String? search, int limit = 50}) async {
    final items = <MenuItemSummary>[];
    var page = 1;
    var totalPages = 1;

    do {
      final response = await _dio.get('menu-items', queryParameters: {
        if (categoryId != null) 'category_id': categoryId,
        if (search != null && search.isNotEmpty) 'search': search,
        'page': page,
        'limit': limit,
      });

      final data = response.data['data'] as List;
      items.addAll(data.map((e) => MenuItemSummary.fromJson(e)));
      totalPages = response.data['pagination']?['totalPages'] ?? 1;
      page += 1;
    } while (page <= totalPages);

    return items;
  }

  Future<List<AiRecommendationItem>> getAiRecommendations({int limit = 5}) async {
    final response = await _dio.get('menu-items/ai-recommendations', queryParameters: {
      'limit': limit,
    });
    
    final data = response.data['data']['data'] as List;
    return data.map((e) => AiRecommendationItem.fromJson(e)).toList();
  }
}
