class MenuItemSummary {
  final int id;
  final String name;
  final double price;
  final String? imageUrl;
  final bool isAvailable;
  // Note: API doesn't return rating in summary, but the UI expects a rating. We might mock it or default it.
  final double rating; 

  MenuItemSummary({
    required this.id,
    required this.name,
    required this.price,
    this.imageUrl,
    required this.isAvailable,
    this.rating = 4.5,
  });

  factory MenuItemSummary.fromJson(Map<String, dynamic> json) {
    return MenuItemSummary(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      price: (json['price'] ?? 0).toDouble(),
      imageUrl: json['imageUrl'],
      isAvailable: json['isAvailable'] ?? true,
      rating: (json['averageRating'] ?? 4.5).toDouble(), // some detail API might have it
    );
  }
}

class AiRecommendationItem {
  final int id;
  final String name;
  final double price;
  final String? imageUrl;
  final String? reason;

  AiRecommendationItem({
    required this.id,
    required this.name,
    required this.price,
    this.imageUrl,
    this.reason,
  });

  factory AiRecommendationItem.fromJson(Map<String, dynamic> json) {
    return AiRecommendationItem(
      id: json['id'] ?? 0,
      name: json['name'] ?? '',
      price: (json['price'] ?? 0).toDouble(),
      imageUrl: json['imageUrl'],
      reason: json['reason'],
    );
  }
}
