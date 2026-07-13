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

  Future<List<MenuItemSummary>> getMenuItems({int? categoryId, String? search, int page = 1, int limit = 10}) async {
    final response = await _dio.get('menu-items', queryParameters: {
      if (categoryId != null) 'category_id': categoryId,
      if (search != null && search.isNotEmpty) 'search': search,
      'page': page,
      'limit': limit,
    });
    
    final items = response.data['data'] as List;
    return items.map((e) => MenuItemSummary.fromJson(e)).toList();
  }

  Future<List<AiRecommendationItem>> getAiRecommendations({int limit = 5}) async {
    final response = await _dio.get('menu-items/ai-recommendations', queryParameters: {
      'limit': limit,
    });
    
    final data = response.data['data']['data'] as List;
    return data.map((e) => AiRecommendationItem.fromJson(e)).toList();
  }
}
