import 'package:go_router/go_router.dart';
import '../pages/auth/login_page.dart';
import '../pages/auth/signup_page.dart';
import '../pages/auth/qr_scan_page.dart';
import '../pages/home/home_page.dart';
import '../pages/menu/menu_page.dart';
import '../pages/cart/cart_page.dart';
import '../pages/checkout/checkout_page.dart';
import '../pages/orders/order_history_page.dart'; // Invoice
import '../pages/orders/order_list_page.dart';
import '../pages/orders/order_tracking_page.dart';
import '../pages/profile/profile_page.dart';
import '../pages/settings/settings_page.dart';
import 'app_routes.dart';

final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.login,
  routes: [
    GoRoute(
      path: AppRoutes.login,
      builder: (context, state) => const LoginPage(),
    ),
    GoRoute(
      path: AppRoutes.signup,
      builder: (context, state) => const SignupPage(),
    ),
    GoRoute(
      path: AppRoutes.qrScan,
      builder: (context, state) => const QrScanPage(),
    ),
    GoRoute(
      path: AppRoutes.home,
      builder: (context, state) => const HomePage(),
    ),
    GoRoute(
      path: AppRoutes.menu,
      builder: (context, state) => const MenuPage(),
    ),
    GoRoute(
      path: AppRoutes.cart,
      builder: (context, state) => const CartPage(),
    ),
    GoRoute(
      path: AppRoutes.checkout,
      builder: (context, state) {
        final orderId = state.extra as int?;
        return CheckoutPage(orderId: orderId);
      },
    ),
    GoRoute(
      path: AppRoutes.orders,
      builder: (context, state) => const OrderListPage(),
    ),
    GoRoute(
      path: AppRoutes.invoice,
      builder: (context, state) => const OrderHistoryPage(),
    ),
    GoRoute(
      path: '${AppRoutes.orderTracking}/:orderId',
      builder: (context, state) {
        final orderId = int.tryParse(state.pathParameters['orderId'] ?? '') ?? 0;
        return OrderTrackingPage(orderId: orderId);
      },
    ),
    GoRoute(
      path: AppRoutes.orderTracking,
      builder: (context, state) => const OrderTrackingPage(),
    ),
    GoRoute(
      path: AppRoutes.profile,
      builder: (context, state) => const ProfilePage(),
    ),
    GoRoute(
      path: AppRoutes.settings,
      builder: (context, state) => const SettingsPage(),
    ),
  ],
);
