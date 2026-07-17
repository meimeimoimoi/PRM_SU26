import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../config/constants.dart';

class SocketService {
  HubConnection? _hubConnection;
  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();

  Future<void> connect(int tableId) async {
    final token = await _secureStorage.read(key: 'access_token');
    if (token == null) return;

    final hubUrl = '${AppConstants.baseUrl.replaceAll('/api/v1/', '')}/hubs/orders';

    _hubConnection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
            transport: HttpTransportType.WebSockets,
          ),
        )
        .withAutomaticReconnect()
        .build();

    try {
      await _hubConnection?.start();
      await _hubConnection?.invoke('JoinTableGroup', args: [tableId.toString()]);
    } catch (e) {
      print('SignalR connect error: $e');
    }
  }

  void subscribeToEvent(String eventName, Function(dynamic) callback) {
    _hubConnection?.on(eventName, (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        callback(arguments[0]);
      }
    });
  }

  void unsubscribeFromEvent(String eventName) {
    _hubConnection?.off(eventName);
  }

  Future<void> disconnect() async {
    await _hubConnection?.stop();
  }
}
