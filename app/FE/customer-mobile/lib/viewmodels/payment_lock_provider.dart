import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Phiên đang khóa CHECKOUT sau khi khách tạo payment intent (tiền mặt/QR).
/// Chặn đặt món thêm trên UI cho đến khi hủy thanh toán hoặc staff xác nhận thành công.
final sessionCheckoutLockedProvider = StateProvider<bool>((ref) => false);
