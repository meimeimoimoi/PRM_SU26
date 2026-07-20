import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_web_plugins/url_strategy.dart';
import 'routes/app_router.dart';
import 'routes/app_routes.dart';
import 'services/socket/socket_service.dart';
import 'viewmodels/auth_viewmodel.dart';
import 'viewmodels/pending_table_provider.dart';
import 'viewmodels/payment_lock_provider.dart';
import 'theme/app_theme.dart';

/// Đọc ?table=N từ QR TRƯỚC khi go_router đổi URL.
int? _bootTableFromQrUrl() {
  if (!kIsWeb) return null;
  final n = int.tryParse(Uri.base.queryParameters['table'] ?? '');
  if (n == null || n <= 0) return null;
  return n;
}

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  if (kIsWeb) {
    usePathUrlStrategy();
  }

  runApp(
    ProviderScope(
      child: MyApp(bootTableNumber: _bootTableFromQrUrl()),
    ),
  );
}

class MyApp extends ConsumerStatefulWidget {
  const MyApp({super.key, this.bootTableNumber});

  final int? bootTableNumber;

  @override
  ConsumerState<MyApp> createState() => _MyAppState();
}

class _MyAppState extends ConsumerState<MyApp> {
  final SocketService _socketService = SocketService();
  bool _paymentListenerAttached = false;
  bool _paymentSuccessDialogOpen = false;
  bool _bootHandled = false;

  @override
  void dispose() {
    _socketService.unsubscribeFromEvent('ReceivePaymentSuccess');
    // ignore: unawaited_futures
    _socketService.disconnect();
    super.dispose();
  }

  void _ensurePaymentSuccessListener() {
    if (_paymentListenerAttached) return;
    _paymentListenerAttached = true;
    _socketService.subscribeToEvent('ReceivePaymentSuccess', _onPaymentSuccess);
  }

  void _onPaymentSuccess(dynamic data) {
    if (data is! Map) return;
    if (_paymentSuccessDialogOpen) return;

    final auth = ref.read(authViewModelProvider);
    final myTableId = auth.tableId;
    if (myTableId == null || myTableId <= 0) return;

    final eventTableId = data['tableId'] ?? data['TableId'];
    if (eventTableId == null || eventTableId.toString() != myTableId.toString()) {
      return;
    }

    ref.read(sessionCheckoutLockedProvider.notifier).state = false;

    final ctx = rootNavigatorKey.currentContext;
    if (ctx == null || !ctx.mounted) {
      // ignore: unawaited_futures
      ref.read(authViewModelProvider.notifier).clearDiningAfterPayment();
      return;
    }

    _paymentSuccessDialogOpen = true;
    showDialog<void>(
      context: ctx,
      barrierDismissible: false,
      builder: (dialogContext) => AlertDialog(
        title: Text('Thanh toán thành công', style: TextStyle(color: AppTheme.onSurface)),
        content: Text(
          'Cảm ơn quý khách! Phiên ăn đã kết thúc. Vui lòng đăng nhập lại khi dùng bàn tiếp theo.',
          style: TextStyle(color: AppTheme.onSurfaceVariant),
        ),
        actions: [
          TextButton(
            onPressed: () async {
              Navigator.of(dialogContext).pop();
              _paymentSuccessDialogOpen = false;
              await ref.read(authViewModelProvider.notifier).clearDiningAfterPayment();
              // AuthState.initial → listener trong build đã go(login).
            },
            child: Text('OK', style: TextStyle(color: AppTheme.primary)),
          ),
        ],
        backgroundColor: AppTheme.surface,
      ),
    ).whenComplete(() {
      _paymentSuccessDialogOpen = false;
    });
  }

  Future<void> _connectPaymentSocket(int tableId) async {
    _ensurePaymentSuccessListener();
    await _socketService.connect(tableId);
  }

  @override
  Widget build(BuildContext context) {
    // Prefill bàn từ QR link, rồi về /login sạch (không giữ ?table=1 trên URL).
    if (!_bootHandled) {
      _bootHandled = true;
      final boot = widget.bootTableNumber;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (boot != null) {
          ref.read(pendingTableNumberProvider.notifier).state = boot;
        }
        // Luôn về login sạch — KHÔNG auto vào home với session bàn 1 cũ.
        appRouter.go(AppRoutes.login);
      });
    }

    ref.listen<AuthState>(authViewModelProvider, (previous, next) {
      if (next.status == AuthStateStatus.initial) {
        appRouter.go(AppRoutes.login);
        return;
      }

      final tableId = next.tableId;
      if (tableId != null &&
          tableId > 0 &&
          (next.status == AuthStateStatus.guest || next.status == AuthStateStatus.authenticated)) {
        // ignore: unawaited_futures
        _connectPaymentSocket(tableId);
      }

      // Chỉ auto-home sau login GUEST vừa thực hiện (không restore session cũ).
      // Thành viên: SignupPage tự context.go(home) sau login-with-table.
      final guestJustLoggedIn = next.status == AuthStateStatus.guest &&
          next.hasDiningSession &&
          previous?.status != AuthStateStatus.guest;
      if (guestJustLoggedIn) {
        appRouter.go(AppRoutes.home);
      }
    });

    final auth = ref.watch(authViewModelProvider);
    if (auth.hasDiningSession && auth.tableId != null && !_paymentListenerAttached) {
      // ignore: unawaited_futures
      _connectPaymentSocket(auth.tableId!);
    }

    return ScreenUtilInit(
      designSize: const Size(375, 812),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) {
        return MaterialApp.router(
          title: 'SmartDine',
          theme: AppTheme.light,
          routerConfig: appRouter,
        );
      },
    );
  }
}
