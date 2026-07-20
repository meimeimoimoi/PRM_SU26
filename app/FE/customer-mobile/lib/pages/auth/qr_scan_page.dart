import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

/// Quét QR dán trên bàn — QR BE tạo dạng `https://.../?table={tableNumber}`
/// hoặc legacy `smartdine://table/{n}`. Trả về số bàn qua `context.pop(tableNumber)`.
class QrScanPage extends StatefulWidget {
  const QrScanPage({super.key});

  @override
  State<QrScanPage> createState() => _QrScanPageState();
}

class _QrScanPageState extends State<QrScanPage> {
  final MobileScannerController _controller = MobileScannerController();
  final TextEditingController _manualController = TextEditingController();
  bool _handled = false;

  @override
  void dispose() {
    _controller.dispose();
    _manualController.dispose();
    super.dispose();
  }

  int? _parseTableNumber(String raw) {
    final trimmed = raw.trim();
    final queryMatch = RegExp(r'[?&#]table=(\d+)', caseSensitive: false).firstMatch(trimmed);
    if (queryMatch != null) return int.tryParse(queryMatch.group(1)!);
    final legacyMatch = RegExp(r'smartdine://table/(\d+)', caseSensitive: false).firstMatch(trimmed);
    if (legacyMatch != null) return int.tryParse(legacyMatch.group(1)!);
    final pathMatch = RegExp(r'/tables?/(\d+)', caseSensitive: false).firstMatch(trimmed);
    if (pathMatch != null) return int.tryParse(pathMatch.group(1)!);
    return int.tryParse(trimmed);
  }

  void _returnTable(int tableNumber) {
    if (_handled || !mounted) return;
    if (tableNumber <= 0) return;
    _handled = true;
    context.pop(tableNumber);
  }

  void _onDetect(BarcodeCapture capture) {
    if (_handled) return;
    for (final barcode in capture.barcodes) {
      final raw = barcode.rawValue ?? barcode.displayValue;
      if (raw == null || raw.isEmpty) continue;
      final tableNumber = _parseTableNumber(raw);
      if (tableNumber != null) {
        _returnTable(tableNumber);
        return;
      }
    }
  }

  void _submitManual() {
    final tableNumber = int.tryParse(_manualController.text.trim());
    if (tableNumber == null || tableNumber <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập số bàn hợp lệ')),
      );
      return;
    }
    _returnTable(tableNumber);
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
          if (!kIsWeb)
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
          if (kIsWeb)
            _buildWebFallback(colors)
          else
            MobileScanner(
              controller: _controller,
              onDetect: _onDetect,
              errorBuilder: (context, error, child) => _buildErrorState(context, error),
            ),
          if (!kIsWeb)
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
            bottom: 32.h,
            left: 24.w,
            right: 24.w,
            child: _buildManualEntry(colors),
          ),
        ],
      ),
    );
  }

  Widget _buildWebFallback(ColorScheme colors) {
    return Container(
      color: Colors.black,
      padding: EdgeInsets.fromLTRB(24.w, 48.h, 24.w, 180.h),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.qr_code_2, color: colors.onPrimary, size: 64.sp),
          SizedBox(height: 16.h),
          Text(
            'Trên trình duyệt web, camera quét QR thường không ổn định.\nHãy nhập số bàn in trên mã QR / bàn.',
            textAlign: TextAlign.center,
            style: TextStyle(color: colors.onPrimary, fontSize: 14.sp, height: 1.4),
          ),
        ],
      ),
    );
  }

  Widget _buildManualEntry(ColorScheme colors) {
    return Material(
      color: colors.surface,
      borderRadius: BorderRadius.circular(16.r),
      child: Padding(
        padding: EdgeInsets.all(16.r),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              kIsWeb ? 'Nhập số bàn' : 'Hoặc nhập số bàn thủ công',
              style: TextStyle(
                color: colors.onSurface,
                fontSize: 14.sp,
                fontWeight: FontWeight.w600,
              ),
            ),
            SizedBox(height: 12.h),
            TextField(
              controller: _manualController,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              style: TextStyle(fontSize: 16.sp, color: colors.onSurface),
              decoration: InputDecoration(
                hintText: 'VD: 3',
                filled: true,
                fillColor: colors.surfaceContainerHighest,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12.r)),
                contentPadding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
              ),
              onSubmitted: (_) => _submitManual(),
            ),
            SizedBox(height: 12.h),
            SizedBox(
              width: double.infinity,
              height: 48.h,
              child: ElevatedButton(
                onPressed: _submitManual,
                style: ElevatedButton.styleFrom(
                  backgroundColor: colors.primary,
                  foregroundColor: colors.onPrimary,
                  minimumSize: Size(0, 48.h),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
                ),
                child: const Text('Xác nhận số bàn'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildErrorState(BuildContext context, MobileScannerException error) {
    final colors = Theme.of(context).colorScheme;

    return Container(
      color: Colors.black,
      padding: EdgeInsets.fromLTRB(24.r, 24.r, 24.r, 200.h),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.no_photography, color: colors.onPrimary, size: 48.sp),
            SizedBox(height: 16.h),
            Text(
              'Không thể mở camera. Hãy nhập số bàn bên dưới.',
              textAlign: TextAlign.center,
              style: TextStyle(color: colors.onPrimary, fontSize: 14.sp),
            ),
          ],
        ),
      ),
    );
  }
}
