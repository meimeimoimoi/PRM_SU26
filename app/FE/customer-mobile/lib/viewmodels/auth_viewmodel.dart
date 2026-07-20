import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/auth_models.dart';
import '../services/auth_repository.dart';
import '../utils/error_utils.dart';
import '../services/api_client.dart' show secureStorageProvider, setTokenInvalidatedCallback;

enum AuthStateStatus { initial, loading, authenticated, guest, error }

class AuthState {
  final AuthStateStatus status;
  final UserInfo? user;
  final GuestLoginResponse? guestSession;
  final String? errorMessage;

  AuthState({
    required this.status,
    this.user,
    this.guestSession,
    this.errorMessage,
  });

  factory AuthState.initial() => AuthState(status: AuthStateStatus.initial);
  factory AuthState.loading() => AuthState(status: AuthStateStatus.loading);
  factory AuthState.authenticated(UserInfo user) => AuthState(status: AuthStateStatus.authenticated, user: user);
  factory AuthState.guest(GuestLoginResponse guestSession) => AuthState(status: AuthStateStatus.guest, guestSession: guestSession);
  factory AuthState.error(String message) => AuthState(status: AuthStateStatus.error, errorMessage: message);
}

final authViewModelProvider = StateNotifierProvider<AuthViewModel, AuthState>((ref) {
  return AuthViewModel(
    ref.watch(authRepositoryProvider),
    ref.watch(secureStorageProvider),
  );
});

class AuthViewModel extends StateNotifier<AuthState> {
  final AuthRepository _repository;
  final FlutterSecureStorage _storage;
  bool _isLoggingIn = false;

  AuthViewModel(this._repository, this._storage) : super(AuthState.initial()) {
    setTokenInvalidatedCallback(() {
      if (mounted) {
        state = AuthState.initial();
      }
    });
    _checkInitialState();
  }

  Future<void> _checkInitialState() async {
    state = AuthState.loading();
    try {
      final token = await _storage.read(key: 'access_token');
      if (token != null) {
        // Try to get current user
        final user = await _repository.getCurrentUser();
        // Don't overwrite state if login() already set it
        if (_isLoggingIn) return;
        if (user.role == 'GUEST') {
          final sessionId = await _storage.read(key: 'session_id');
          final tableIdStr = await _storage.read(key: 'table_id');
          final tableNumStr = await _storage.read(key: 'table_number');
          final guestName = await _storage.read(key: 'guest_name');
          state = AuthState.guest(GuestLoginResponse(
            token: token,
            sessionId: int.tryParse(sessionId ?? '') ?? 0,
            tableId: int.tryParse(tableIdStr ?? '') ?? 0,
            tableNumber: int.tryParse(tableNumStr ?? '') ?? 0,
            role: user.role,
            guestName: (guestName != null && guestName.isNotEmpty)
                ? guestName
                : (user.fullName.isNotEmpty ? user.fullName : 'Guest'),
          ));
        } else {
          state = AuthState.authenticated(user);
        }
      } else {
        if (_isLoggingIn) return;
        state = AuthState.initial();
      }
    } catch (e) {
      // Don't delete tokens or overwrite state if login is in progress
      if (_isLoggingIn) return;
      await _storage.deleteAll();
      state = AuthState.initial();
    }
  }

  Future<bool> login(String email, String password) async {
    _isLoggingIn = true;
    state = AuthState.loading();
    try {
      final response = await _repository.login(email, password);
      await _storage.write(key: 'access_token', value: response.accessToken);
      await _storage.write(key: 'refresh_token', value: response.refreshToken);
      state = AuthState.authenticated(response.user);
      return true;
    } catch (e) {
      state = AuthState.error(extractErrorMessage(e));
      return false;
    } finally {
      _isLoggingIn = false;
    }
  }

  Future<bool> register(String fullName, String email, String password, String? phoneNumber) async {
    _isLoggingIn = true;
    state = AuthState.loading();
    try {
      final response = await _repository.register(fullName, email, password, phoneNumber);
      await _storage.write(key: 'access_token', value: response.accessToken);
      await _storage.write(key: 'refresh_token', value: response.refreshToken);
      state = AuthState.authenticated(response.user);
      return true;
    } catch (e) {
      state = AuthState.error(extractErrorMessage(e));
      return false;
    } finally {
      _isLoggingIn = false;
    }
  }

  Future<bool> loginGuest(int tableId, String? guestName, String? guestPhone) async {
    _isLoggingIn = true;
    state = AuthState.loading();
    try {
      final response = await _repository.loginGuest(tableId, guestName, guestPhone);
      await _storage.write(key: 'access_token', value: response.token);
      await _storage.write(key: 'session_id', value: response.sessionId.toString());
      await _storage.write(key: 'table_id', value: response.tableId.toString());
      await _storage.write(key: 'table_number', value: response.tableNumber.toString());
      await _storage.write(key: 'guest_name', value: response.guestName);
      // Guests don't have refresh tokens
      state = AuthState.guest(response);
      return true;
    } catch (e) {
      state = AuthState.error(extractErrorMessage(e));
      return false;
    } finally {
      _isLoggingIn = false;
    }
  }

  Future<void> logout() async {
    try {
      if (state.status == AuthStateStatus.authenticated || state.status == AuthStateStatus.guest) {
        await _repository.logout();
      }
    } finally {
      await _storage.deleteAll();
      state = AuthState.initial();
      _isLoggingIn = false;
    }
  }
}
