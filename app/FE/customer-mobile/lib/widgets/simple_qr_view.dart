import 'package:flutter/material.dart';
import 'package:qr/qr.dart';

/// Vẽ QR trực tiếp bằng package:qr (chỉ lo encoding) thay vì dùng widget
/// QrImageView của qr_flutter — package đó không còn bản cập nhật nào mới
/// hơn 4.1.0 trong khi RenderObject của nó không tương thích với Flutter SDK
/// hiện tại, gây "Assertion failed" liên tục trong box.dart/shifted_box.dart
/// mỗi khi dialog thanh toán VietQR mở ra.
class SimpleQrView extends StatelessWidget {
  final String data;
  final double size;
  final Color foregroundColor;
  final Color backgroundColor;

  const SimpleQrView({
    super.key,
    required this.data,
    required this.size,
    this.foregroundColor = Colors.black,
    this.backgroundColor = Colors.white,
  });

  @override
  Widget build(BuildContext context) {
    QrImage? qrImage;
    if (data.isNotEmpty) {
      try {
        final qrCode = QrCode.fromData(
          data: data,
          errorCorrectLevel: QrErrorCorrectLevel.M,
        );
        qrImage = QrImage(qrCode);
      } catch (_) {
        qrImage = null;
      }
    }

    return SizedBox(
      width: size,
      height: size,
      child: qrImage == null
          ? ColoredBox(
              color: backgroundColor,
              child: Icon(Icons.qr_code, size: size * 0.5, color: Colors.grey),
            )
          : CustomPaint(
              size: Size(size, size),
              painter: _QrPainter(
                qrImage: qrImage,
                foregroundColor: foregroundColor,
                backgroundColor: backgroundColor,
              ),
            ),
    );
  }
}

class _QrPainter extends CustomPainter {
  final QrImage qrImage;
  final Color foregroundColor;
  final Color backgroundColor;

  _QrPainter({
    required this.qrImage,
    required this.foregroundColor,
    required this.backgroundColor,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final moduleCount = qrImage.moduleCount;
    final moduleSize = size.width / moduleCount;

    canvas.drawRect(Offset.zero & size, Paint()..color = backgroundColor);

    final darkPaint = Paint()..color = foregroundColor;
    for (var row = 0; row < moduleCount; row++) {
      for (var col = 0; col < moduleCount; col++) {
        if (qrImage.isDark(row, col)) {
          canvas.drawRect(
            Rect.fromLTWH(col * moduleSize, row * moduleSize, moduleSize, moduleSize),
            darkPaint,
          );
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant _QrPainter oldDelegate) =>
      oldDelegate.qrImage != qrImage ||
      oldDelegate.foregroundColor != foregroundColor ||
      oldDelegate.backgroundColor != backgroundColor;
}
