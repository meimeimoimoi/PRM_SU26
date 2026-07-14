import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

class _AppColors {
  static const Color primary = Color(0xFFad2c00);
  static const Color background = Color(0xFF000000);
  static const Color onPrimary = Color(0xFFffffff);
}

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
    // Chấp nhận cả URI đầy đủ "smartdine://table/12" lẫn số bàn thuần "12"
    // (phòng trường hợp QR được in thủ công chỉ ghi số bàn).
    final uriMatch = RegExp(r'smartdine://table/(\d+)').firstMatch(raw);
    if (uriMatch != null) return int.tryParse(uriMatch.group(1)!);
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
    return Scaffold(
      backgroundColor: _AppColors.background,
      appBar: AppBar(
        backgroundColor: Colors.black.withOpacity(0.4),
        elevation: 0,
        iconTheme: const IconThemeData(color: _AppColors.onPrimary),
        title: const Text('Quét mã QR trên bàn', style: TextStyle(color: _AppColors.onPrimary)),
        actions: [
          IconButton(
            icon: ValueListenableBuilder(
              valueListenable: _controller,
              builder: (context, state, child) {
                return Icon(
                  state.torchState == TorchState.on ? Icons.flash_on : Icons.flash_off,
                  color: _AppColors.onPrimary,
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
                border: Border.all(color: _AppColors.primary, width: 3),
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
                  style: TextStyle(color: _AppColors.onPrimary, fontSize: 14.sp),
                ),
                SizedBox(height: 16.h),
                TextButton(
                  onPressed: () => context.pop(),
                  child: Text(
                    'Nhập tay số bàn thay vì quét',
                    style: TextStyle(color: _AppColors.onPrimary, fontSize: 13.sp, decoration: TextDecoration.underline),
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
    return Container(
      color: _AppColors.background,
      padding: EdgeInsets.all(24.r),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.no_photography, color: _AppColors.onPrimary, size: 48.sp),
            SizedBox(height: 16.h),
            Text(
              'Không thể mở camera (có thể do quyền truy cập bị từ chối).',
              textAlign: TextAlign.center,
              style: TextStyle(color: _AppColors.onPrimary, fontSize: 14.sp),
            ),
            SizedBox(height: 16.h),
            ElevatedButton(
              onPressed: () => context.pop(),
              style: ElevatedButton.styleFrom(backgroundColor: _AppColors.primary, foregroundColor: Colors.white),
              child: const Text('Nhập tay số bàn'),
            ),
          ],
        ),
      ),
    );
  }
}
