import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/auth_models.dart';
import '../services/auth_repository.dart';
import '../services/api_client.dart';

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

  AuthViewModel(this._repository, this._storage) : super(AuthState.initial()) {
    _checkInitialState();
  }

  Future<void> _checkInitialState() async {
    state = AuthState.loading();
    try {
      final token = await _storage.read(key: 'access_token');
      if (token != null) {
        // Try to get current user
        final user = await _repository.getCurrentUser();
        if (user.role == 'GUEST') {
          // It's a guest session. We might need a slightly different check, but backend /me supports GUEST
          state = AuthState.guest(GuestLoginResponse(
            token: token,
            sessionId: user.id,
            tableId: 0, // We don't have tableId from /me directly
            tableNumber: 0, 
            role: user.role
          ));
        } else {
          state = AuthState.authenticated(user);
        }
      } else {
        state = AuthState.initial();
      }
    } catch (e) {
      await _storage.deleteAll();
      state = AuthState.initial();
    }
  }

  Future<bool> login(String email, String password) async {
    state = AuthState.loading();
    try {
      final response = await _repository.login(email, password);
      await _storage.write(key: 'access_token', value: response.accessToken);
      await _storage.write(key: 'refresh_token', value: response.refreshToken);
      state = AuthState.authenticated(response.user);
      return true;
    } catch (e) {
      state = AuthState.error(e.toString());
      return false;
    }
  }

  Future<bool> register(String fullName, String email, String password, String? phoneNumber) async {
    state = AuthState.loading();
    try {
      final response = await _repository.register(fullName, email, password, phoneNumber);
      await _storage.write(key: 'access_token', value: response.accessToken);
      await _storage.write(key: 'refresh_token', value: response.refreshToken);
      state = AuthState.authenticated(response.user);
      return true;
    } catch (e) {
      state = AuthState.error(e.toString());
      return false;
    }
  }

  Future<bool> loginGuest(int tableId, String? guestName, String? guestPhone) async {
    state = AuthState.loading();
    try {
      final response = await _repository.loginGuest(tableId, guestName, guestPhone);
      await _storage.write(key: 'access_token', value: response.token);
      // Guests don't have refresh tokens
      state = AuthState.guest(response);
      return true;
    } catch (e) {
      state = AuthState.error(e.toString());
      return false;
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
    }
  }
}
