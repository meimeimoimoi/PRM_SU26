import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'routes/app_router.dart';
import 'viewmodels/auth_viewmodel.dart';
import 'theme/app_theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  runApp(
    const ProviderScope(
      child: MyApp(),
    ),
  );
}

class MyApp extends ConsumerStatefulWidget {
  const MyApp({super.key});

  @override
  ConsumerState<MyApp> createState() => _MyAppState();
}

class _MyAppState extends ConsumerState<MyApp> {
  bool _autoLoginAttempted = false;

  @override
  void initState() {
    super.initState();
    if (kIsWeb && !_autoLoginAttempted) {
      _autoLoginAttempted = true;
      _tryAutoLoginFromUrl();
    }
  }

  void _tryAutoLoginFromUrl() {
    final uri = Uri.base;
    final tableStr = uri.queryParameters['table'];
    final tableId = int.tryParse(tableStr ?? '');
    if (tableId != null && tableId > 0) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        ref.read(authViewModelProvider.notifier).loginGuest(tableId, null, null);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    ref.listen<AuthState>(authViewModelProvider, (previous, next) {
      if (next.status == AuthStateStatus.initial) {
        appRouter.go('/login');
      }
      if (next.status == AuthStateStatus.guest) {
        final uri = Uri.base;
        if (uri.queryParameters.containsKey('table')) {
          appRouter.go('/home');
        }
      }
    });

    final router = appRouter;

    return ScreenUtilInit(
      designSize: const Size(375, 812),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) {
        return MaterialApp.router(
          title: 'SmartDine',
          theme: AppTheme.light,
          routerConfig: router,
        );
      },
    );
  }
}
