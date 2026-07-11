import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;

class AppConstants {
  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5000/api/v1/';
    }
    if (Platform.isAndroid) {
      return 'http://10.0.2.2:5000/api/v1/'; // Android Emulator
    }
    return 'http://localhost:5000/api/v1/'; // iOS Simulator / Windows
  }
  static const int connectionTimeout = 30000;
  static const int receiveTimeout = 30000;
}
