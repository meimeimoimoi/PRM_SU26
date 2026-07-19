import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../config/constants.dart';

class SocketService {
  HubConnection? _hubConnection;
  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();
  final Map<String, List<void Function(dynamic)>> _handlers = {};
  bool _connecting = false;

  static String get hubUrl {
    final base = AppConstants.baseUrl;
    final root = base.replaceFirst(RegExp(r'/api/v1/?$'), '');
    return '$root/hubs/orders';
  }

  Future<void> connect(int tableId) async {
    if (_connecting) return;
    if (_hubConnection?.state == HubConnectionState.Connected) {
      try {
        await _hubConnection?.invoke('JoinTableGroup', args: [tableId.toString()]);
      } catch (_) {}
      return;
    }

    final token = await _secureStorage.read(key: 'access_token');
    if (token == null || token.isEmpty) return;

    _connecting = true;
    try {
      await _hubConnection?.stop();
    } catch (_) {}

    // Không ép WebSockets-only: Flutter web / proxy (Render) thường fail WS,
    // thư viện sẽ negotiate LongPolling/SSE làm fallback.
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
          ),
        )
        .withAutomaticReconnect()
        .build();

    _attachQueuedHandlers();

    try {
      await _hubConnection!.start();
      await _hubConnection!.invoke('JoinTableGroup', args: [tableId.toString()]);
    } catch (e) {
      // Nuốt lỗi để không thành Uncaught (in promise) trên Flutter web.
      // App vẫn poll REST mỗi vài giây để đồng bộ trạng thái.
      // ignore: avoid_print
      print('SignalR connect error: $e');
    } finally {
      _connecting = false;
    }
  }

  void _attachQueuedHandlers() {
    final hub = _hubConnection;
    if (hub == null) return;
    for (final entry in _handlers.entries) {
      hub.off(entry.key);
      hub.on(entry.key, (arguments) {
        if (arguments == null || arguments.isEmpty) return;
        for (final cb in List.of(entry.value)) {
          try {
            cb(arguments[0]);
          } catch (_) {}
        }
      });
    }
  }

  void subscribeToEvent(String eventName, void Function(dynamic) callback) {
    _handlers.putIfAbsent(eventName, () => []).add(callback);
    final hub = _hubConnection;
    if (hub == null) return;
    hub.off(eventName);
    hub.on(eventName, (arguments) {
      if (arguments == null || arguments.isEmpty) return;
      for (final cb in List.of(_handlers[eventName] ?? const [])) {
        try {
          cb(arguments[0]);
        } catch (_) {}
      }
    });
  }

  void unsubscribeFromEvent(String eventName) {
    _handlers.remove(eventName);
    _hubConnection?.off(eventName);
  }

  Future<void> disconnect() async {
    try {
      await _hubConnection?.stop();
    } catch (_) {}
    _hubConnection = null;
  }
}
