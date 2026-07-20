import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Số bàn đã quét trên trang login — dùng cho guest / member login.
final pendingTableNumberProvider = StateProvider<int?>((ref) => null);
