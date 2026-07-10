class PlaceOrderRequest {
  final int tableId;
  final int diningSessionId;
  final String? specialInstructions;
  final String? couponCode;
  final List<OrderDetailRequest> items;

  PlaceOrderRequest({
    required this.tableId,
    required this.diningSessionId,
    this.specialInstructions,
    this.couponCode,
    required this.items,
  });

  Map<String, dynamic> toJson() => {
    'tableId': tableId,
    'diningSessionId': diningSessionId,
    if (specialInstructions != null) 'specialInstructions': specialInstructions,
    if (couponCode != null) 'couponCode': couponCode,
    'items': items.map((e) => e.toJson()).toList(),
  };
}

class OrderDetailRequest {
  final int menuItemId;
  final int quantity;
  final String? notes;

  OrderDetailRequest({
    required this.menuItemId,
    this.quantity = 1,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
    'menuItemId': menuItemId,
    'quantity': quantity,
    if (notes != null) 'notes': notes,
  };
}

class OrderResponse {
  final int id;
  final int? customerId;
  final String? customerName;
  final int tableNumber;
  final double finalAmount;
  final String status;
  final String? specialInstructions;

  OrderResponse({
    required this.id,
    this.customerId,
    this.customerName,
    required this.tableNumber,
    required this.finalAmount,
    required this.status,
    this.specialInstructions,
  });

  factory OrderResponse.fromJson(Map<String, dynamic> json) {
    return OrderResponse(
      id: json['id'] ?? 0,
      customerId: json['customerId'],
      customerName: json['customerName'],
      tableNumber: json['tableNumber'] ?? 0,
      finalAmount: (json['finalAmount'] ?? 0).toDouble(),
      status: json['status'] ?? 'PENDING',
      specialInstructions: json['specialInstructions'],
    );
  }
}
