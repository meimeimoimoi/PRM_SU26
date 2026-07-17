import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/menu_models.dart';
import '../services/menu_repository.dart';

final menuCategoriesProvider = FutureProvider.autoDispose<List<MenuCategory>>((ref) async {
  final repository = ref.watch(menuRepositoryProvider);
  return repository.getMenuCategories();
});

/// null = "Tất cả" (không lọc theo danh mục).
final selectedCategoryIdProvider = StateProvider.autoDispose<int?>((ref) => null);

final menuSearchQueryProvider = StateProvider.autoDispose<String>((ref) => '');

final menuItemsProvider = FutureProvider.autoDispose<List<MenuItemSummary>>((ref) async {
  final repository = ref.watch(menuRepositoryProvider);
  final categoryId = ref.watch(selectedCategoryIdProvider);
  final search = ref.watch(menuSearchQueryProvider);
  return repository.getMenuItems(categoryId: categoryId, search: search);
});

final aiRecommendationsProvider = FutureProvider.autoDispose<List<AiRecommendationItem>>((ref) async {
  final repository = ref.watch(menuRepositoryProvider);
  return repository.getAiRecommendations(limit: 5);
});
