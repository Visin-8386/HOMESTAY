import 'package:flutter/material.dart';
import 'package:flutter_rating_bar/flutter_rating_bar.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../providers/booking_provider.dart';
import '../../services/payment_service.dart';

class PaymentCheckoutScreen extends StatefulWidget {
  final Homestay homestay;
  final DateTime checkIn;
  final DateTime checkOut;
  final int guests;

  const PaymentCheckoutScreen({
    super.key,
    required this.homestay,
    required this.checkIn,
    required this.checkOut,
    required this.guests,
  });

  @override
  State<PaymentCheckoutScreen> createState() => _PaymentCheckoutScreenState();
}

class _PaymentCheckoutScreenState extends State<PaymentCheckoutScreen> {
  final PaymentService _paymentService = PaymentService();
  bool _isProcessing = false;
  String? _specialRequests;

  int get _numberOfNights {
    return widget.checkOut.difference(widget.checkIn).inDays;
  }

  double get _subtotal {
    return widget.homestay.pricePerNight * _numberOfNights;
  }

  double get _serviceFee {
    return _subtotal * 0.05; // 5% service fee
  }

  double get _total {
    return _subtotal + _serviceFee;
  }

  Future<void> _processPayment() async {
    setState(() => _isProcessing = true);

    try {
      // Create booking first
      final bookingProvider = Provider.of<BookingProvider>(context, listen: false);
      final booking = await bookingProvider.createBooking(
        homestayId: widget.homestay.id,
        checkIn: widget.checkIn,
        checkOut: widget.checkOut,
        guests: widget.guests,
        specialRequests: _specialRequests,
      );

      if (booking != null) {
        // Process payment
        final paymentResult = await _paymentService.createPayment(
          bookingId: booking.id,
          method: 'PayPal',
        );

        if (paymentResult['success'] == true && mounted) {
          final paymentUrl = paymentResult['paymentUrl'];
          // Navigate to payment webview
          Navigator.pushNamed(
            context,
            '/payment-webview',
            arguments: {
              'url': paymentUrl,
              'bookingId': booking.id,
            },
          );
        } else {
          _showError(paymentResult['message'] ?? 'Không thể tạo thanh toán. Vui lòng thử lại.');
        }
      } else {
        _showError('Không thể tạo đơn đặt phòng. Vui lòng thử lại.');
      }
    } catch (e) {
      _showError('Lỗi: ${e.toString()}');
    } finally {
      if (mounted) {
        setState(() => _isProcessing = false);
      }
    }
  }

  void _showError(String message) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Thanh toán'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Homestay Info
            _buildHomestayInfo(),

            const SizedBox(height: 24),

            // Booking Details
            _buildBookingDetails(),

            const SizedBox(height: 24),

            // Price Breakdown
            _buildPriceBreakdown(),

            const SizedBox(height: 24),

            // Special Requests
            _buildSpecialRequests(),

            const SizedBox(height: 24),

            // Payment Method
            _buildPaymentMethod(),

            const SizedBox(height: 32),

            // Pay Button
            SizedBox(
              width: double.infinity,
              height: 55,
              child: ElevatedButton(
                onPressed: _isProcessing ? null : _processPayment,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF0070BA), // PayPal blue
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 5,
                ),
                child: _isProcessing
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text(
                        'Thanh toán với PayPal',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
              ),
            ),

            const SizedBox(height: 16),

            // Terms
            _buildTermsText(),
          ],
        ),
      ),
    );
  }

  Widget _buildHomestayInfo() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            // Image
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: widget.homestay.images.isNotEmpty
                  ? Image.network(
                      widget.homestay.images.first,
                      width: 80,
                      height: 80,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) =>
                          Container(
                            width: 80,
                            height: 80,
                            color: Colors.grey[300],
                            child: const Icon(Icons.home, size: 40),
                          ),
                    )
                  : Container(
                      width: 80,
                      height: 80,
                      color: Colors.grey[300],
                      child: const Icon(Icons.home, size: 40),
                    ),
            ),

            const SizedBox(width: 16),

            // Info
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.homestay.name,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      if (widget.homestay.averageRating != null) ...[
                        RatingBarIndicator(
                          rating: widget.homestay.averageRating!,
                          itemBuilder: (context, _) => const Icon(
                            Icons.star,
                            color: AppColors.rating,
                          ),
                          itemCount: 5,
                          itemSize: 16,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${widget.homestay.averageRating!.toStringAsFixed(1)} (${widget.homestay.reviewCount} đánh giá)',
                          style: TextStyle(color: Colors.grey[600]),
                        ),
                      ],
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    widget.homestay.fullAddress,
                    style: TextStyle(color: Colors.grey[600]),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBookingDetails() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Chi tiết đặt phòng',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            _buildDetailRow('Nhận phòng', _formatDate(widget.checkIn)),
            _buildDetailRow('Trả phòng', _formatDate(widget.checkOut)),
            _buildDetailRow('Số đêm', '$_numberOfNights đêm'),
            _buildDetailRow('Số khách', '${widget.guests} khách'),
          ],
        ),
      ),
    );
  }

  Widget _buildPriceBreakdown() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Chi tiết giá',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            _buildPriceRow('₫${widget.homestay.pricePerNight.toStringAsFixed(0)} x $_numberOfNights đêm', _subtotal),
            _buildPriceRow('Phí dịch vụ (5%)', _serviceFee),
            const Divider(),
            _buildPriceRow('Tổng cộng', _total, isTotal: true),
          ],
        ),
      ),
    );
  }

  Widget _buildSpecialRequests() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Yêu cầu đặc biệt (tùy chọn)',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            TextField(
              maxLines: 3,
              decoration: const InputDecoration(
                hintText: 'Ví dụ: Phòng không hút thuốc, cần giường phụ...',
                border: OutlineInputBorder(),
              ),
              onChanged: (value) => _specialRequests = value,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPaymentMethod() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Image.asset(
              'assets/images/paypal_logo.png',
              width: 60,
              height: 40,
              errorBuilder: (context, error, stackTrace) => Container(
                width: 60,
                height: 40,
                color: const Color(0xFF0070BA),
                child: const Center(
                  child: Text(
                    'PayPal',
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(width: 16),
            const Expanded(
              child: Text(
                'Thanh toán an toàn với PayPal',
                style: TextStyle(fontSize: 16),
              ),
            ),
            const Icon(Icons.check_circle, color: Colors.green),
          ],
        ),
      ),
    );
  }

  Widget _buildTermsText() {
    return Text(
      'Bằng việc nhấn "Thanh toán", bạn đồng ý với Điều khoản sử dụng và Chính sách hủy phòng của chúng tôi.',
      style: TextStyle(
        color: Colors.grey[600],
        fontSize: 12,
      ),
      textAlign: TextAlign.center,
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey[600])),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }

  Widget _buildPriceRow(String label, double amount, {bool isTotal = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              color: isTotal ? Colors.black : Colors.grey[600],
              fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
            ),
          ),
          Text(
            '₫${amount.toStringAsFixed(0)}',
            style: TextStyle(
              fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
              fontSize: isTotal ? 18 : 14,
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year}';
  }
}