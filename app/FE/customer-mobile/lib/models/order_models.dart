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

class OrderDetailResponse {
  final int id;
  final int menuItemId;
  final String name;
  final double unitPrice;
  final int quantity;
  final double total;
  final String? notes;
  final String status;

  OrderDetailResponse({
    required this.id,
    required this.menuItemId,
    required this.name,
    required this.unitPrice,
    required this.quantity,
    required this.total,
    this.notes,
    required this.status,
  });

  factory OrderDetailResponse.fromJson(Map<String, dynamic> json) {
    return OrderDetailResponse(
      id: json['id'] ?? 0,
      menuItemId: json['menuItemId'] ?? 0,
      name: json['name'] ?? '',
      unitPrice: (json['unitPrice'] ?? 0).toDouble(),
      quantity: json['quantity'] ?? 1,
      total: (json['total'] ?? 0).toDouble(),
      notes: json['notes'],
      status: json['status'] ?? 'WAITING',
    );
  }
}

class OrderResponse {
  final int id;
  final int sessionId;
  final int? customerId;
  final String? customerName;
  final int tableNumber;
  final List<OrderDetailResponse> items;
  final double totalAmount;
  final double discountAmount;
  final double finalAmount;
  final String status;
  final String? specialInstructions;
  final DateTime? createdAt;

  OrderResponse({
    required this.id,
    required this.sessionId,
    this.customerId,
    this.customerName,
    required this.tableNumber,
    this.items = const [],
    required this.totalAmount,
    this.discountAmount = 0,
    required this.finalAmount,
    required this.status,
    this.specialInstructions,
    this.createdAt,
  });

  factory OrderResponse.fromJson(Map<String, dynamic> json) {
    final itemsList = json['items'] as List? ?? [];
    return OrderResponse(
      id: json['id'] ?? 0,
      sessionId: json['sessionId'] ?? 0,
      customerId: json['customerId'],
      customerName: json['customerName'],
      tableNumber: json['tableNumber'] ?? 0,
      items: itemsList.map((e) => OrderDetailResponse.fromJson(e)).toList(),
      totalAmount: (json['totalAmount'] ?? 0).toDouble(),
      discountAmount: (json['discountAmount'] ?? 0).toDouble(),
      finalAmount: (json['finalAmount'] ?? 0).toDouble(),
      status: json['status'] ?? 'PENDING',
      specialInstructions: json['specialInstructions'],
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
    );
  }
}

class OrderItemStatusResponse {
  final String name;
  final int quantity;
  final String status;

  OrderItemStatusResponse({
    required this.name,
    required this.quantity,
    required this.status,
  });

  factory OrderItemStatusResponse.fromJson(Map<String, dynamic> json) {
    return OrderItemStatusResponse(
      name: json['name'] ?? '',
      quantity: json['quantity'] ?? 1,
      status: json['status'] ?? 'WAITING',
    );
  }
}

class OrderStatusResponse {
  final int orderId;
  final String status;
  final List<OrderItemStatusResponse> items;

  OrderStatusResponse({
    required this.orderId,
    required this.status,
    this.items = const [],
  });

  factory OrderStatusResponse.fromJson(Map<String, dynamic> json) {
    final itemsList = json['items'] as List? ?? [];
    return OrderStatusResponse(
      orderId: json['orderId'] ?? 0,
      status: json['status'] ?? 'PENDING',
      items: itemsList.map((e) => OrderItemStatusResponse.fromJson(e)).toList(),
    );
  }
}

class PaymentIntentResponse {
  final String invoiceId;
  final double totalPayable;
  final String? qrUrl;
  final String? deeplink;

  PaymentIntentResponse({
    required this.invoiceId,
    required this.totalPayable,
    this.qrUrl,
    this.deeplink,
  });

  factory PaymentIntentResponse.fromJson(Map<String, dynamic> json) {
    return PaymentIntentResponse(
      invoiceId: json['invoiceId'] ?? '',
      totalPayable: (json['totalPayable'] ?? 0).toDouble(),
      qrUrl: json['qrUrl'],
      deeplink: json['deeplink'],
    );
  }
}
