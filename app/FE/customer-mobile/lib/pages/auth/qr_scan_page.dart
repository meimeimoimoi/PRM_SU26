import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../theme/app_theme.dart';

/// Quét QR dán trên bàn — QR được BE tạo theo quy ước `smartdine://table/{tableNumber}`
/// (xem TableService.CreateAsync). Trả về số bàn qua context.pop(tableNumber) để
/// LoginPage điền vào ô "Số Bàn", không đổi luồng gọi auth/login-guest hiện có.
class QrScanPage extends StatefulWidget {
  const QrScanPage({super.key});

  @override
  State<QrScanPage> createState() => _QrScanPageState();
}

class _QrScanPageState extends State<QrScanPage> {
  final MobileScannerController _controller = MobileScannerController();
  bool _handled = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  int? _parseTableNumber(String raw) {
    // QR thật do BE sinh (TableService.CreateAsync) là URL web dạng
    // "https://.../?table=12" — khách quét bằng camera điện thoại thường sẽ mở thẳng
    // trang web, không cần qua màn hình này. Màn này chỉ dùng khi khách đã có app và
    // quét bằng camera trong app, nên vẫn cần đọc được ?table= từ URL đó.
    final queryMatch = RegExp(r'[?&]table=(\d+)').firstMatch(raw);
    if (queryMatch != null) return int.tryParse(queryMatch.group(1)!);
    // Tương thích ngược với QR cũ dạng "smartdine://table/12" nếu còn sót trên bàn nào.
    final legacyMatch = RegExp(r'smartdine://table/(\d+)').firstMatch(raw);
    if (legacyMatch != null) return int.tryParse(legacyMatch.group(1)!);
    // QR in thủ công chỉ ghi số bàn thuần.
    return int.tryParse(raw.trim());
  }

  void _onDetect(BarcodeCapture capture) {
    if (_handled) return;
    for (final barcode in capture.barcodes) {
      final raw = barcode.rawValue;
      if (raw == null) continue;
      final tableNumber = _parseTableNumber(raw);
      if (tableNumber != null) {
        _handled = true;
        context.pop(tableNumber);
        return;
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black.withOpacity(0.4),
        elevation: 0,
        iconTheme: IconThemeData(color: colors.onPrimary),
        title: Text('Quét mã QR trên bàn', style: TextStyle(color: colors.onPrimary)),
        actions: [
          IconButton(
            icon: ValueListenableBuilder(
              valueListenable: _controller,
              builder: (context, state, child) {
                return Icon(
                  state.torchState == TorchState.on ? Icons.flash_on : Icons.flash_off,
                  color: colors.onPrimary,
                );
              },
            ),
            onPressed: () => _controller.toggleTorch(),
          ),
        ],
      ),
      body: Stack(
        fit: StackFit.expand,
        children: [
          MobileScanner(
            controller: _controller,
            onDetect: _onDetect,
            errorBuilder: (context, error, child) => _buildErrorState(context, error),
          ),
          Center(
            child: Container(
              width: 240.r,
              height: 240.r,
              decoration: BoxDecoration(
                border: Border.all(color: colors.primary, width: 3),
                borderRadius: BorderRadius.circular(16.r),
              ),
            ),
          ),
          Positioned(
            bottom: 48.h,
            left: 24.w,
            right: 24.w,
            child: Column(
              children: [
                Text(
                  'Hướng camera vào mã QR dán trên bàn',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: colors.onPrimary, fontSize: 14.sp),
                ),
                SizedBox(height: 16.h),
                TextButton(
                  onPressed: () => context.pop(),
                  child: Text(
                    'Nhập tay số bàn thay vì quét',
                    style: TextStyle(color: colors.onPrimary, fontSize: 13.sp, decoration: TextDecoration.underline),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorState(BuildContext context, MobileScannerException error) {
    final theme = Theme.of(context);
    final colors = theme.colorScheme;

    return Container(
      color: Colors.black,
      padding: EdgeInsets.all(24.r),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.no_photography, color: colors.onPrimary, size: 48.sp),
            SizedBox(height: 16.h),
            Text(
              'Không thể mở camera (có thể do quyền truy cập bị từ chối).',
              textAlign: TextAlign.center,
              style: TextStyle(color: colors.onPrimary, fontSize: 14.sp),
            ),
            SizedBox(height: 16.h),
            ElevatedButton(
              onPressed: () => context.pop(),
              style: ElevatedButton.styleFrom(backgroundColor: colors.primary, foregroundColor: colors.onPrimary),
              child: const Text('Nhập tay số bàn'),
            ),
          ],
        ),
      ),
    );
  }
}