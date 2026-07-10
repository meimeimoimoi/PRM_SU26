import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/menu_models.dart';
import '../services/menu_repository.dart';

final menuItemsProvider = FutureProvider.autoDispose<List<MenuItemSummary>>((ref) async {
  final repository = ref.watch(menuRepositoryProvider);
  return repository.getMenuItems(limit: 20);
});

final aiRecommendationsProvider = FutureProvider.autoDispose<List<AiRecommendationItem>>((ref) async {
  final repository = ref.watch(menuRepositoryProvider);
  return repository.getAiRecommendations(limit: 5);
});
