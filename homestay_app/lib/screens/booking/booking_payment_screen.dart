import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../config/app_colors.dart';
import '../../models/booking.dart';
import '../../services/payment_service.dart';
import '../payment/payment_webview_screen.dart';

class BookingPaymentScreen extends StatefulWidget {
  final Booking booking;

  const BookingPaymentScreen({Key? key, required this.booking}) : super(key: key);

  @override
  State<BookingPaymentScreen> createState() => _BookingPaymentScreenState();
}

class _BookingPaymentScreenState extends State<BookingPaymentScreen> {
  final PaymentService _paymentService = PaymentService();
  bool _isLoading = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Thanh toán'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildBookingInfo(),
            const SizedBox(height: 24),
            _buildPaymentInfo(),
            const SizedBox(height: 24),
            _buildPaymentMethods(),
          ],
        ),
      ),
    );
  }

  Widget _buildBookingInfo() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Thông tin đặt phòng',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            _buildInfoRow('Mã đặt phòng', '#${widget.booking.id}'),
            _buildInfoRow('Homestay', widget.booking.homestayName),
            _buildInfoRow(
              'Nhận phòng',
              DateFormat('dd/MM/yyyy').format(widget.booking.checkInDate),
            ),
            _buildInfoRow(
              'Trả phòng',
              DateFormat('dd/MM/yyyy').format(widget.booking.checkOutDate),
            ),
            _buildInfoRow('Số khách', '${widget.booking.numberOfGuests}'),
          ],
        ),
      ),
    );
  }

  Widget _buildPaymentInfo() {
    final nights = widget.booking.numberOfNights;
    
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Chi tiết thanh toán',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            _buildPriceRow(
              '$nights đêm',
              NumberFormat.currency(locale: 'vi_VN', symbol: '₫')
                  .format(widget.booking.totalPrice),
            ),
            const Divider(height: 24),
            _buildPriceRow(
              'Tổng cộng',
              NumberFormat.currency(locale: 'vi_VN', symbol: '₫')
                  .format(widget.booking.totalPrice),
              isTotal: true,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPaymentMethods() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Chọn phương thức thanh toán',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 16),
            _buildPaymentMethodTile(
              'VNPay',
              'Thanh toán qua cổng VNPay',
              Icons.payment,
              Colors.blue,
              'VNPay',
            ),
            const SizedBox(height: 12),
            _buildPaymentMethodTile(
              'MoMo',
              'Ví điện tử MoMo',
              Icons.account_balance_wallet,
              Colors.pink,
              'MoMo',
            ),
            const SizedBox(height: 12),
            _buildPaymentMethodTile(
              'PayPal',
              'Thanh toán quốc tế PayPal',
              Icons.credit_card,
              Colors.indigo,
              'PayPal',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPaymentMethodTile(
    String title,
    String subtitle,
    IconData icon,
    Color color,
    String method,
  ) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey.shade300),
        borderRadius: BorderRadius.circular(12),
      ),
      child: ListTile(
        leading: Container(
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: color.withOpacity(0.1),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(icon, color: color, size: 28),
        ),
        title: Text(
          title,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Text(subtitle),
        trailing: _isLoading
            ? const SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Icon(Icons.chevron_right, color: AppColors.primary),
        onTap: _isLoading ? null : () => _processPayment(method),
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(color: Colors.grey[600]),
          ),
          Text(
            value,
            style: const TextStyle(fontWeight: FontWeight.w500),
          ),
        ],
      ),
    );
  }

  Widget _buildPriceRow(String label, String value, {bool isTotal = false, Color? color}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
              fontSize: isTotal ? 16 : 14,
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontWeight: isTotal ? FontWeight.bold : FontWeight.w500,
              fontSize: isTotal ? 18 : 14,
              color: color ?? (isTotal ? AppColors.primary : null),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _processPayment(String paymentMethod) async {
    setState(() => _isLoading = true);

    try {
      final result = await _paymentService.processPayment(
        bookingId: widget.booking.id,
        paymentMethod: paymentMethod,
      );

      setState(() => _isLoading = false);

      if (!mounted) return;

      if (result['success'] == true) {
        final paymentUrl = result['paymentUrl'];

        // Open payment WebView
        final paymentResult = await Navigator.push(
          context,
          MaterialPageRoute(
            builder: (context) => PaymentWebViewScreen(
              paymentUrl: paymentUrl,
              title: 'Thanh toán - $paymentMethod',
            ),
          ),
        );

        if (!mounted) return;

        if (paymentResult != null && paymentResult['success'] == true) {
          // Payment successful
          Navigator.of(context).pop(true); // Return to previous screen
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Thanh toán thành công!'),
              backgroundColor: Colors.green,
            ),
          );
        } else if (paymentResult?['cancelled'] == true) {
          // Payment cancelled
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Đã hủy thanh toán'),
              backgroundColor: Colors.orange,
            ),
          );
        } else {
          // Payment failed
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Thanh toán thất bại. Vui lòng thử lại.'),
              backgroundColor: Colors.red,
            ),
          );
        }
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(result['message'] ?? 'Lỗi xử lý thanh toán'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      setState(() => _isLoading = false);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }
}
