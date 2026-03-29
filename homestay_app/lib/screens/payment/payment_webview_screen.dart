import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:webview_flutter/webview_flutter.dart';

import '../../config/app_colors.dart';

class PaymentWebViewScreen extends StatefulWidget {
  final String paymentUrl;
  final String title;

  const PaymentWebViewScreen({
    Key? key,
    required this.paymentUrl,
    this.title = 'Thanh toán',
  }) : super(key: key);

  @override
  State<PaymentWebViewScreen> createState() => _PaymentWebViewScreenState();
}

class _PaymentWebViewScreenState extends State<PaymentWebViewScreen> {
  late final WebViewController _controller;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _initializeWebView();
  }

  void _initializeWebView() {
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setBackgroundColor(Colors.white)
          ..setNavigationDelegate(
        NavigationDelegate(
          onProgress: (int progress) {
            if (progress == 100) {
              setState(() => _isLoading = false);
            }
          },
          onPageStarted: (String url) {
            setState(() => _isLoading = true);
            _checkPaymentCallback(url);
          },
          onPageFinished: (String url) {
            setState(() => _isLoading = false);
            _checkPaymentCallback(url);
          },
          onWebResourceError: (WebResourceError error) {
            setState(() {
              _error = error.description;
              _isLoading = false;
            });
          },
          onNavigationRequest: (NavigationRequest request) {
            // Check for payment result URLs (after backend capture)
            final requestUrl = request.url.toLowerCase();
            if (requestUrl.contains('/payment-result')) {
              _checkPaymentCallback(request.url);
              return NavigationDecision.navigate;
            }
            
            // Allow navigation to PaymentReturn (backend capture endpoint)
            if (requestUrl.contains('/payment/paymentreturn')) {
              // Let it navigate, backend will capture and redirect to /payment-result
              return NavigationDecision.navigate;
            }

            // For other payment callbacks (VNPay etc.)
            if (requestUrl.contains('/payment/callback') ||
                requestUrl.contains('/payment/return') ||
                requestUrl.contains('/payment/success') ||
                requestUrl.contains('/payment/failed')) {
              _checkPaymentCallback(request.url);
              return NavigationDecision.navigate;
            }
            return NavigationDecision.navigate;
          },
        ),
      );

    // Clear cookies before loading to avoid merchant account auto-login in WebView
    _clearCookiesAndLoad();
  }

  Future<void> _clearCookiesAndLoad() async {
    // Cookie clearing skipped (platform cookie API not available here).
    // Just load the payment URL; user can open in external browser if session blocks approval.
    _controller.loadRequest(Uri.parse(widget.paymentUrl));
  }

  void _checkPaymentCallback(String url) {
    final lower = url.toLowerCase();

    // Check for backend success/failure redirect (after PayPal capture)
    if (lower.contains('/payment-result')) {
      final uri = Uri.tryParse(url);
      if (uri != null) {
        final success = uri.queryParameters['success'];
        final cancelled = uri.queryParameters['cancelled'];
        
        if (cancelled == 'true') {
          Navigator.pop(context, {'success': false, 'url': url, 'cancelled': true});
          return;
        }
        
        if (success == 'true') {
          Navigator.pop(context, {'success': true, 'url': url});
          return;
        }
        
        // Success is not 'true', treat as failure
        Navigator.pop(context, {'success': false, 'url': url});
        return;
      }
    }

    // PayPal: DO NOT consider token+PayerID as success yet
    // User just approved, but backend needs to capture first
    // Backend will redirect to /payment-result after capture completes
    final hasToken = url.contains('token=');
    final hasPayerId = url.contains('PayerID=') || url.contains('payerid=');
    if (hasToken && hasPayerId) {
      // Just log it, let WebView navigate to PaymentReturn endpoint
      debugPrint('PayPal approval detected, waiting for capture...');
      return;
    }

    // Generic success patterns (e.g., VNPay)
    if (lower.contains('/payment/success') || lower.contains('vnp_responsecode=00')) {
      Navigator.pop(context, {'success': true, 'url': url});
      return;
    }

    // Failure patterns
    if (lower.contains('/payment/failed') || (lower.contains('vnp_responsecode=') && !lower.contains('vnp_responsecode=00'))) {
      Navigator.pop(context, {'success': false, 'url': url});
      return;
    }

    // Cancel pattern
    if (lower.contains('/payment/cancel') || lower.contains('paymentcancel')) {
      Navigator.pop(context, {'success': false, 'cancelled': true});
      return;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.title),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () => _showCancelDialog(),
        ),
      ),
      body: Stack(
        children: [
          if (_error != null)
            Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 64, color: Colors.red),
                  const SizedBox(height: 16),
                  Text(
                    'Lỗi tải trang thanh toán',
                    style: TextStyle(fontSize: 18, color: Colors.grey[800]),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    _error!,
                    style: TextStyle(fontSize: 14, color: Colors.grey[600]),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 12),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 24.0),
                    child: Text(
                      'Nếu bạn thấy trang PayPal tự động đăng nhập bằng tài khoản người bán (merchant), hãy mở bằng trình duyệt và đăng nhập bằng tài khoản cá nhân (buyer).',
                      style: TextStyle(fontSize: 13, color: Colors.grey[600]),
                      textAlign: TextAlign.center,
                    ),
                  ),
                  const SizedBox(height: 24),
                  ElevatedButton(
                    onPressed: () => Navigator.pop(context, {'success': false}),
                    child: const Text('Đóng'),
                  ),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: () async {
                      // try opening in external browser
                      final uri = Uri.tryParse(widget.paymentUrl);
                      if (uri != null && await canLaunchUrl(uri)) {
                        await launchUrl(uri, mode: LaunchMode.externalApplication);
                      } else {
                        // If cannot open, try reload after clearing cookies
                        await _clearCookiesAndLoad();
                      }
                    },
                    style: ElevatedButton.styleFrom(backgroundColor: Colors.white, foregroundColor: AppColors.primary),
                    child: const Text('Mở bằng trình duyệt'),
                  ),
                ],
              ),
            )
          else
            WebViewWidget(controller: _controller),
          if (_isLoading)
            Container(
              color: Colors.white,
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    CircularProgressIndicator(color: AppColors.primary),
                    const SizedBox(height: 16),
                    const Text('Đang tải trang thanh toán...'),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _showCancelDialog() async {
    final result = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Hủy thanh toán'),
        content: const Text('Bạn có chắc chắn muốn hủy giao dịch này?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Tiếp tục'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Hủy giao dịch'),
          ),
        ],
      ),
    );

    if (result == true && mounted) {
      Navigator.pop(context, {'success': false, 'cancelled': true});
    }
  }
}
