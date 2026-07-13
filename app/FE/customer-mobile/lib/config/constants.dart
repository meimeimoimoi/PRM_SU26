import 'package:flutter/foundation.dart' show kIsWeb;

class AppConstants {
  static String get baseUrl {
    if (kIsWeb) return 'http://localhost:5000/api/v1/';
    return 'http://localhost:5000/api/v1/';
  }
  static const int connectionTimeout = 30000;
  static const int receiveTimeout = 30000;
}
