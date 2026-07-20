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
  /// Phiên bàn — GUEST luôn có; CUSTOMER có sau khi quét QR / join bàn.
  final DiningContext? dining;
  final String? errorMessage;

  AuthState({
    required this.status,
    this.user,
    this.guestSession,
    this.dining,
    this.errorMessage,
  });

  factory AuthState.initial() => AuthState(status: AuthStateStatus.initial);
  factory AuthState.loading() => AuthState(status: AuthStateStatus.loading);
  factory AuthState.authenticated(UserInfo user, {DiningContext? dining}) =>
      AuthState(status: AuthStateStatus.authenticated, user: user, dining: dining);
  factory AuthState.guest(GuestLoginResponse guestSession) => AuthState(
        status: AuthStateStatus.guest,
        guestSession: guestSession,
        dining: DiningContext(
          sessionId: guestSession.sessionId,
          tableId: guestSession.tableId,
          tableNumber: guestSession.tableNumber,
        ),
      );
  factory AuthState.error(String message) => AuthState(status: AuthStateStatus.error, errorMessage: message);

  int? get tableNumber => dining?.tableNumber ?? guestSession?.tableNumber;
  int? get tableId => dining?.tableId ?? guestSession?.tableId;
  int? get sessionId => dining?.sessionId ?? guestSession?.sessionId;
  bool get hasDiningSession => (dining?.isValid ?? false) ||
      (guestSession != null && guestSession!.sessionId > 0 && guestSession!.tableId > 0);
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
  int _authEpoch = 0;

  AuthViewModel(this._repository, this._storage) : super(AuthState.initial()) {
    setTokenInvalidatedCallback(() {
      if (mounted) {
        state = AuthState.initial();
      }
    });
    _checkInitialState();
  }

  Future<void> _persistDining(DiningContext dining) async {
    await _storage.write(key: 'session_id', value: dining.sessionId.toString());
    await _storage.write(key: 'table_id', value: dining.tableId.toString());
    await _storage.write(key: 'table_number', value: dining.tableNumber.toString());
  }

  Future<void> _clearDiningStorage() async {
    await _storage.delete(key: 'session_id');
    await _storage.delete(key: 'table_id');
    await _storage.delete(key: 'table_number');
    await _storage.delete(key: 'guest_name');
  }

  Future<DiningContext?> _readDiningFromStorage() async {
    final sessionId = int.tryParse(await _storage.read(key: 'session_id') ?? '') ?? 0;
    final tableId = int.tryParse(await _storage.read(key: 'table_id') ?? '') ?? 0;
    final tableNumber = int.tryParse(await _storage.read(key: 'table_number') ?? '') ?? 0;
    if (sessionId <= 0 || tableId <= 0 || tableNumber <= 0) return null;
    return DiningContext(sessionId: sessionId, tableId: tableId, tableNumber: tableNumber);
  }

  Future<void> _checkInitialState() async {
    final epoch = _authEpoch;
    state = AuthState.loading();
    try {
      final token = await _storage.read(key: 'access_token');
      if (epoch != _authEpoch || _isLoggingIn) return;

      if (token != null && token.isNotEmpty) {
        final user = await _repository.getCurrentUser();
        if (epoch != _authEpoch || _isLoggingIn) return;

        if (user.role == 'GUEST') {
          final dining = await _readDiningFromStorage();
          final guestName = await _storage.read(key: 'guest_name');
          if (epoch != _authEpoch || _isLoggingIn) return;

          state = AuthState.guest(GuestLoginResponse(
            token: token,
            sessionId: dining?.sessionId ?? 0,
            tableId: dining?.tableId ?? 0,
            tableNumber: dining?.tableNumber ?? 0,
            role: user.role,
            guestName: (guestName != null && guestName.isNotEmpty)
                ? guestName
                : (user.fullName.isNotEmpty ? user.fullName : 'Guest'),
          ));
        } else {
          final dining = await _readDiningFromStorage();
          if (epoch != _authEpoch || _isLoggingIn) return;
          state = AuthState.authenticated(user, dining: dining);
        }
      } else {
        state = AuthState.initial();
      }
    } catch (e) {
      if (epoch != _authEpoch || _isLoggingIn) return;
      await _storage.deleteAll();
      state = AuthState.initial();
    }
  }

  /// App khách KHÔNG dùng /auth/login thuần — bắt buộc gắn bàn.
  @Deprecated('Dùng loginOrRegisterWithTable')
  Future<bool> login(String email, String password) async {
    state = AuthState.error(
      'App khách phải chọn bàn trước. Dùng nút "Đăng nhập thành viên" → nhập số bàn.',
    );
    return false;
  }

  /// Login → POST /auth/login + tableNumber; đăng ký → /auth/login-with-table.
  Future<bool> loginOrRegisterWithTable({
    required String email,
    required String password,
    required int tableNumber,
    String? fullName,
    String? phoneNumber,
  }) async {
    if (tableNumber <= 0) {
      state = AuthState.error('Vui lòng chọn số bàn trước khi đăng nhập');
      return false;
    }

    _authEpoch++;
    _isLoggingIn = true;
    state = AuthState.loading();
    try {
      await _storage.delete(key: 'access_token');
      await _storage.delete(key: 'refresh_token');
      await _clearDiningStorage();

      final response = await _repository.loginWithTable(
        email: email.trim(),
        password: password,
        tableNumber: tableNumber,
        fullName: fullName,
        phoneNumber: phoneNumber,
      );

      if (response.accessToken.isEmpty || response.sessionId <= 0) {
        state = AuthState.error(
          response.sessionId <= 0
              ? 'Đăng nhập OK nhưng chưa tạo phiên bàn $tableNumber. Hãy restart Identity.API.'
              : 'Đăng nhập thất bại: không nhận được token',
        );
        return false;
      }
      final role = response.user.role.toUpperCase();
      if (role != 'CUSTOMER') {
        state = AuthState.error('Tài khoản này không dùng được trên app khách.');
        return false;
      }
      final dining = DiningContext(
        sessionId: response.sessionId,
        tableId: response.tableId,
        tableNumber: response.tableNumber > 0 ? response.tableNumber : tableNumber,
      );
      await _storage.write(key: 'access_token', value: response.accessToken);
      await _storage.write(key: 'refresh_token', value: response.refreshToken);
      await _persistDining(dining);
      state = AuthState.authenticated(response.user, dining: dining);
      return true;
    } catch (e) {
      state = AuthState.error(extractErrorMessage(e));
      return false;
    } finally {
      _isLoggingIn = false;
    }
  }

  Future<bool> register(String fullName, String email, String password, String? phoneNumber) async {
    _authEpoch++;
    _isLoggingIn = true;
    state = AuthState.loading();
    try {
      final response = await _repository.register(fullName, email, password, phoneNumber);
      if (response.accessToken.isEmpty) {
        state = AuthState.error('Đăng ký thất bại: không nhận được token');
        return false;
      }
      await _storage.write(key: 'access_token', value: response.accessToken);
      await _storage.write(key: 'refresh_token', value: response.refreshToken);
      await _clearDiningStorage();
      state = AuthState.authenticated(response.user);
      return true;
    } catch (e) {
      state = AuthState.error(extractErrorMessage(e));
      return false;
    } finally {
      _isLoggingIn = false;
    }
  }

  Future<bool> loginGuest(int tableNumber, String? guestName, String? guestPhone) async {
    _authEpoch++;
    _isLoggingIn = true;
    state = AuthState.loading();
    try {
      await _storage.delete(key: 'access_token');
      await _storage.delete(key: 'refresh_token');
      await _clearDiningStorage();

      final response = await _repository.loginGuest(tableNumber, guestName, guestPhone);
      if (response.token.isEmpty || response.sessionId <= 0) {
        state = AuthState.error('Đăng nhập bàn thất bại: phản hồi không hợp lệ');
        return false;
      }
      // BE có thể trả tableNumber=0 / lệch — luôn ưu tiên số bàn khách vừa chọn.
      final resolved = GuestLoginResponse(
        token: response.token,
        sessionId: response.sessionId,
        tableId: response.tableId,
        tableNumber: response.tableNumber > 0 ? response.tableNumber : tableNumber,
        role: response.role,
        guestName: response.guestName,
      );
      await _storage.write(key: 'access_token', value: resolved.token);
      await _persistDining(DiningContext(
        sessionId: resolved.sessionId,
        tableId: resolved.tableId,
        tableNumber: resolved.tableNumber,
      ));
      await _storage.write(key: 'guest_name', value: resolved.guestName);
      await _storage.delete(key: 'refresh_token');
      state = AuthState.guest(resolved);
      return true;
    } catch (e) {
      state = AuthState.error(extractErrorMessage(e));
      return false;
    } finally {
      _isLoggingIn = false;
    }
  }

  /// CUSTOMER quét/nhập số bàn → join session, giữ JWT thành viên (tích điểm / coupon).
  Future<bool> joinTable(int tableNumber) async {
    if (state.status != AuthStateStatus.authenticated || state.user == null) {
      state = AuthState.error('Vui lòng đăng nhập thành viên trước khi chọn bàn');
      return false;
    }
    final user = state.user!;
    state = AuthState.loading();
    try {
      var dining = await _repository.scanTableByNumber(tableNumber);
      if (dining.sessionId <= 0 || dining.tableId <= 0) {
        state = AuthState(
          status: AuthStateStatus.authenticated,
          user: user,
          errorMessage: 'Không gắn được bàn — thử lại',
        );
        return false;
      }
      // Đảm bảo số bàn hiển thị đúng bàn đã quét
      if (dining.tableNumber != tableNumber) {
        dining = DiningContext(
          sessionId: dining.sessionId,
          tableId: dining.tableId,
          tableNumber: tableNumber,
        );
      }
      await _persistDining(dining);
      state = AuthState.authenticated(user, dining: dining);
      return true;
    } catch (e) {
      state = AuthState(
        status: AuthStateStatus.authenticated,
        user: user,
        errorMessage: extractErrorMessage(e),
      );
      return false;
    }
  }

  /// Sau thanh toán / kết thúc phiên: CUSTOMER và GUEST đều logout → về màn đăng nhập.
  Future<void> clearDiningAfterPayment() async {
    await logout();
  }

  Future<void> logout() async {
    _authEpoch++;
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

  /// Vào cổng login: xóa token + dining cũ (thường kẹt bàn 1) để bắt buộc chọn bàn mới.
  Future<void> resetToLoginGate() async {
    _authEpoch++;
    _isLoggingIn = false;
    try {
      await _storage.deleteAll();
    } finally {
      if (mounted) {
        state = AuthState.initial();
      }
    }
  }
}
