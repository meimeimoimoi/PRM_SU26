class AppConstants {
  // Production build: flutter build apk --dart-define=API_BASE_URL=https://your-gateway-domain/api/v1/
  // Dev (không truyền define): rơi về localhost như cũ.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5000/api/v1/',
  );
  static const int connectionTimeout = 30000;
  static const int receiveTimeout = 30000;
}
