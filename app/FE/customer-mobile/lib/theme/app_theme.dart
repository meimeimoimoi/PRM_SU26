import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

/// SmartDine Customer App - Unified Design System
/// Clean white theme with blue accent (#2563EB) matching web dashboard

class AppTheme {
  // ===== Color Palette =====
  static const Color _blue50 = Color(0xFFEFF6FF);
  static const Color _blue100 = Color(0xFFDBEAFE);
  static const Color _blue500 = Color(0xFF3B82F6);
  static const Color _blue600 = Color(0xFF2563EB);
  static const Color _blue700 = Color(0xFF1D4ED8);

  static const Color _slate50 = Color(0xFFF8FAFC);
  static const Color _slate100 = Color(0xFFF1F5F9);
  static const Color _slate200 = Color(0xFFE2E8F0);
  static const Color _slate300 = Color(0xFFCBD5E1);
  static const Color _slate400 = Color(0xFF94A3B8);
  static const Color _slate500 = Color(0xFF64748B);
  static const Color _slate600 = Color(0xFF475569);
  static const Color _slate700 = Color(0xFF334155);
  static const Color _slate800 = Color(0xFF1E293B);
  static const Color _slate900 = Color(0xFF0F172A);

  static const Color _white = Colors.white;
  static const Color _red500 = Color(0xFFEF4444);
  static const Color _red100 = Color(0xFFFEE2E2);
  static const Color _emerald500 = Color(0xFF10B981);
  static const Color _emerald100 = Color(0xFFD1FAE5);
  static const Color _amber500 = Color(0xFFF59E0B);
  static const Color _amber100 = Color(0xFFFEF3C7);
  static const Color _purple500 = Color(0xFF8B5CF6);
  static const Color _purple100 = Color(0xFFEDE9FE);

  // ===== Semantic Colors =====
  static const Color primary = _blue600;
  static const Color primaryLight = _blue500;
  static const Color primaryContainer = _blue50;
  static const Color onPrimary = _white;
  static const Color onPrimaryContainer = _blue700;

  static const Color secondary = _slate600;
  static const Color secondaryContainer = _slate100;
  static const Color onSecondary = _white;
  static const Color onSecondaryContainer = _slate700;

  static const Color tertiary = _purple500;
  static const Color tertiaryContainer = _purple100;
  static const Color onTertiaryContainer = _purple500;

  static const Color surface = _white;
  static const Color surfaceContainer = _slate50;
  static const Color surfaceContainerHigh = _slate100;
  static const Color surfaceContainerLowest = _white;
  static const Color onSurface = _slate900;
  static const Color onSurfaceVariant = _slate500;

  static const Color background = _slate50;

  static const Color outline = _slate300;
  static const Color outlineVariant = _slate200;

  static const Color error = _red500;
  static const Color errorContainer = _red100;
  static const Color onError = _white;

  static const Color success = _emerald500;
  static const Color successContainer = _emerald100;
  static const Color onSuccess = _white;

  static const Color warning = _amber500;
  static const Color warningContainer = _amber100;
  static const Color onWarning = _slate900;

  // ===== Shadows =====
  static List<BoxShadow> get shadowCard => [
    BoxShadow(
      color: Colors.black.withOpacity(0.04),
      blurRadius: 12,
      offset: const Offset(0, 2),
    ),
    BoxShadow(
      color: Colors.black.withOpacity(0.02),
      blurRadius: 4,
      offset: const Offset(0, 1),
    ),
  ];

  static List<BoxShadow> get shadowCardHover => [
    BoxShadow(
      color: primary.withOpacity(0.08),
      blurRadius: 16,
      offset: const Offset(0, 4),
    ),
    BoxShadow(
      color: Colors.black.withOpacity(0.04),
      blurRadius: 8,
      offset: const Offset(0, 2),
    ),
  ];

  static List<BoxShadow> get shadowAppBar => [
    BoxShadow(
      color: Colors.black.withOpacity(0.04),
      blurRadius: 8,
      offset: const Offset(0, 2),
    ),
  ];

  static List<BoxShadow> get shadowBottomNav => [
    BoxShadow(
      color: Colors.black.withOpacity(0.06),
      blurRadius: 20,
      offset: const Offset(0, -4),
    ),
  ];

  static List<BoxShadow> get shadowFAB => [
    BoxShadow(
      color: primary.withOpacity(0.3),
      blurRadius: 12,
      offset: const Offset(0, 4),
    ),
  ];

  // ===== Radius =====
  static const double radiusXS = 8;
  static const double radiusSM = 12;
  static const double radiusMD = 16;
  static const double radiusLG = 20;
  static const double radiusXL = 24;
  static const double radiusFull = 100;

  // ===== Spacing (using ScreenUtil) =====
  static const double spaceXS = 4;
  static const double spaceSM = 8;
  static const double spaceMD = 16;
  static const double spaceLG = 24;
  static const double spaceXL = 32;

  // ===== Typography =====
  static TextTheme get textTheme => TextTheme(
    displayLarge: _textStyle(32, FontWeight.bold, letterSpacing: -0.5),
    displayMedium: _textStyle(28, FontWeight.bold, letterSpacing: -0.3),
    displaySmall: _textStyle(24, FontWeight.bold, letterSpacing: -0.2),
    headlineLarge: _textStyle(22, FontWeight.w600, letterSpacing: -0.1),
    headlineMedium: _textStyle(20, FontWeight.w600),
    headlineSmall: _textStyle(18, FontWeight.w600),
    titleLarge: _textStyle(16, FontWeight.w600),
    titleMedium: _textStyle(14, FontWeight.w600),
    titleSmall: _textStyle(12, FontWeight.w600, letterSpacing: 0.5),
    labelLarge: _textStyle(14, FontWeight.w600),
    labelMedium: _textStyle(12, FontWeight.w600, letterSpacing: 0.5),
    labelSmall: _textStyle(10, FontWeight.w600, letterSpacing: 0.8),
    bodyLarge: _textStyle(16, FontWeight.normal),
    bodyMedium: _textStyle(14, FontWeight.normal),
    bodySmall: _textStyle(12, FontWeight.normal),
  );

  static TextStyle _textStyle(double size, FontWeight weight, {double? letterSpacing}) {
    return TextStyle(
      fontSize: size.sp,
      fontWeight: weight,
      letterSpacing: letterSpacing ?? 0,
      fontFamily: 'Roboto',
      height: 1.4,
    );
  }

  // ===== Component Themes =====
  static AppBarTheme get appBarTheme => AppBarTheme(
    backgroundColor: surface,
    foregroundColor: onSurface,
    elevation: 0,
    scrolledUnderElevation: 2,
    surfaceTintColor: Colors.transparent,
    shadowColor: Colors.black.withOpacity(0.04),
    centerTitle: false,
    titleTextStyle: textTheme.titleLarge!.copyWith(
      color: onSurface,
      fontWeight: FontWeight.bold,
    ),
    iconTheme: IconThemeData(color: onSurface, size: 24.sp),
    actionsIconTheme: IconThemeData(color: onSurface, size: 24.sp),
  );

  static ElevatedButtonThemeData get elevatedButtonTheme => ElevatedButtonThemeData(
    style: ElevatedButton.styleFrom(
      backgroundColor: primary,
      foregroundColor: onPrimary,
      elevation: 0,
      shadowColor: primary.withOpacity(0.3),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(radiusMD.r),
      ),
      padding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 24.w),
      textStyle: textTheme.labelLarge,
      minimumSize: Size(double.infinity, 56.h),
    ),
  );

  static OutlinedButtonThemeData get outlinedButtonTheme => OutlinedButtonThemeData(
    style: OutlinedButton.styleFrom(
      foregroundColor: primary,
      side: BorderSide(color: primary, width: 1.5),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(radiusMD.r),
      ),
      padding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 24.w),
      textStyle: textTheme.labelLarge,
      minimumSize: Size(double.infinity, 56.h),
    ),
  );

  static TextButtonThemeData get textButtonTheme => TextButtonThemeData(
    style: TextButton.styleFrom(
      foregroundColor: primary,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(radiusMD.r),
      ),
      padding: EdgeInsets.symmetric(vertical: 12.h, horizontal: 16.w),
      textStyle: textTheme.labelLarge,
    ),
  );

  static InputDecorationTheme get inputDecorationTheme => InputDecorationTheme(
    filled: true,
    fillColor: surfaceContainer,
    contentPadding: EdgeInsets.symmetric(vertical: 16.h, horizontal: 16.w),
    hintStyle: textTheme.bodyMedium!.copyWith(color: _slate400),
    labelStyle: textTheme.bodyMedium!.copyWith(color: onSurfaceVariant),
    floatingLabelStyle: textTheme.bodySmall!.copyWith(color: primary),
    border: OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusMD.r),
      borderSide: BorderSide.none,
    ),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusMD.r),
      borderSide: BorderSide.none,
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusMD.r),
      borderSide: BorderSide(color: primary, width: 2),
    ),
    errorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusMD.r),
      borderSide: BorderSide(color: error, width: 1.5),
    ),
    focusedErrorBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(radiusMD.r),
      borderSide: BorderSide(color: error, width: 2),
    ),
    prefixIconColor: _slate400,
    suffixIconColor: _slate400,
  );

  static CardThemeData get cardTheme => CardThemeData(
    color: surface,
    elevation: 0,
    shadowColor: Colors.black.withOpacity(0.04),
    surfaceTintColor: Colors.transparent,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(radiusMD.r),
      side: BorderSide(color: outlineVariant, width: 1),
    ),
    margin: EdgeInsets.zero,
  );

  static BottomNavigationBarThemeData get bottomNavTheme => BottomNavigationBarThemeData(
    backgroundColor: surface,
    elevation: 0,
    selectedItemColor: primary,
    unselectedItemColor: _slate400,
    selectedLabelStyle: textTheme.labelSmall!.copyWith(fontWeight: FontWeight.bold),
    unselectedLabelStyle: textTheme.labelSmall!.copyWith(fontWeight: FontWeight.w600),
    type: BottomNavigationBarType.fixed,
    showSelectedLabels: true,
    showUnselectedLabels: true,
  );

  static SnackBarThemeData get snackBarTheme => SnackBarThemeData(
    backgroundColor: _slate800,
    contentTextStyle: textTheme.bodyMedium!.copyWith(color: _white),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radiusSM.r)),
    behavior: SnackBarBehavior.floating,
    elevation: 4,
  );

  static DividerThemeData get dividerTheme => DividerThemeData(
    color: outlineVariant,
    thickness: 1,
    space: 1,
  );

  static ChipThemeData get chipTheme => ChipThemeData(
    backgroundColor: secondaryContainer,
    selectedColor: primaryContainer,
    labelStyle: textTheme.labelMedium!.copyWith(color: onSecondaryContainer),
    secondaryLabelStyle: textTheme.labelMedium!.copyWith(color: onPrimaryContainer),
    padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 8.h),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radiusFull.r)),
    side: BorderSide.none,
  );

  static DialogThemeData get dialogTheme => DialogThemeData(
    backgroundColor: surface,
    surfaceTintColor: Colors.transparent,
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radiusLG.r)),
    elevation: 8,
    titleTextStyle: textTheme.titleLarge!.copyWith(color: onSurface),
    contentTextStyle: textTheme.bodyMedium!.copyWith(color: onSurfaceVariant),
  );

  // ===== Complete ThemeData =====
  static ThemeData get light => ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    colorScheme: ColorScheme.light(
      primary: primary,
      onPrimary: onPrimary,
      primaryContainer: primaryContainer,
      onPrimaryContainer: onPrimaryContainer,
      secondary: secondary,
      onSecondary: onSecondary,
      secondaryContainer: secondaryContainer,
      onSecondaryContainer: onSecondaryContainer,
      tertiary: tertiary,
      onTertiary: onTertiaryContainer,
      tertiaryContainer: tertiaryContainer,
      onTertiaryContainer: onTertiaryContainer,
      surface: surface,
      onSurface: onSurface,
      onSurfaceVariant: onSurfaceVariant,
      background: background,
      onBackground: onSurface,
      error: error,
      onError: onError,
      errorContainer: errorContainer,
      outline: outline,
      outlineVariant: outlineVariant,
    ),
    scaffoldBackgroundColor: background,
    appBarTheme: appBarTheme,
    elevatedButtonTheme: elevatedButtonTheme,
    outlinedButtonTheme: outlinedButtonTheme,
    textButtonTheme: textButtonTheme,
    inputDecorationTheme: inputDecorationTheme,
    cardTheme: cardTheme,
    bottomNavigationBarTheme: bottomNavTheme,
    snackBarTheme: snackBarTheme,
    dividerTheme: dividerTheme,
    chipTheme: chipTheme,
    dialogTheme: dialogTheme,
    textTheme: textTheme.apply(
      bodyColor: onSurface,
      displayColor: onSurface,
    ),
    iconTheme: IconThemeData(
      color: onSurfaceVariant,
      size: 24.sp,
    ),
    primaryIconTheme: IconThemeData(
      color: primary,
      size: 24.sp,
    ),
    splashFactory: InkRipple.splashFactory,
    splashColor: primary.withOpacity(0.1),
    highlightColor: primary.withOpacity(0.05),
  );
}

/// Extension for easy access to theme colors
extension AppThemeExtension on BuildContext {
  AppThemeColors get themeColors => AppThemeColors.of(this);
}

class AppThemeColors {
  final Color primary = AppTheme.primary;
  final Color primaryLight = AppTheme.primaryLight;
  final Color primaryContainer = AppTheme.primaryContainer;
  final Color onPrimary = AppTheme.onPrimary;
  final Color onPrimaryContainer = AppTheme.onPrimaryContainer;
  final Color secondary = AppTheme.secondary;
  final Color secondaryContainer = AppTheme.secondaryContainer;
  final Color onSecondaryContainer = AppTheme.onSecondaryContainer;
  final Color surface = AppTheme.surface;
  final Color surfaceContainer = AppTheme.surfaceContainer;
  final Color surfaceContainerHigh = AppTheme.surfaceContainerHigh;
  final Color surfaceContainerLowest = AppTheme.surfaceContainerLowest;
  final Color onSurface = AppTheme.onSurface;
  final Color onSurfaceVariant = AppTheme.onSurfaceVariant;
  final Color background = AppTheme.background;
  final Color outline = AppTheme.outline;
  final Color outlineVariant = AppTheme.outlineVariant;
  final Color error = AppTheme.error;
  final Color errorContainer = AppTheme.errorContainer;
  final Color success = AppTheme.success;
  final Color successContainer = AppTheme.successContainer;
  final Color warning = AppTheme.warning;
  final Color warningContainer = AppTheme.warningContainer;

  AppThemeColors._();

  static AppThemeColors of(BuildContext context) => AppThemeColors._();
}