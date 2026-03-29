import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../providers/booking_provider.dart';
import '../../providers/homestay_provider.dart';
import '../../services/booking_service.dart';
import '../../services/payment_service.dart';
import '../../widgets/user_gradient_background.dart';
import '../payment/payment_webview_screen.dart';

class BookingScreen extends StatefulWidget {
  final int homestayId;
  final DateTime checkIn;
  final DateTime checkOut;
  final int guests;

  const BookingScreen({
    super.key,
    required this.homestayId,
    required this.checkIn,
    required this.checkOut,
    required this.guests,
  });

  @override
  State<BookingScreen> createState() => _BookingScreenState();
}

class _BookingScreenState extends State<BookingScreen> {
  final _specialRequestsController = TextEditingController();
  bool _isLoading = false;
  final _promoController = TextEditingController();
  double? _discountedPrice;
  List<Map<String, dynamic>> _availablePromotions = [];
  String? _selectedPromotionCode;
  bool _promosLoading = false;

  @override
  void initState() {
    super.initState();
    _loadAvailablePromotions();
  }

  Future<void> _loadAvailablePromotions() async {
    setState(() => _promosLoading = true);
    try {
      final bookingService = BookingService();
      final promos = await bookingService.getActivePromotions();
      if (mounted) {
        setState(() {
          _availablePromotions = promos;
        });
      }
      // debug log
      // ignore: avoid_print
      print('Loaded promotions: ${promos.length}');
    } catch (e) {
      // ignore errors silently for UX; user can still enter manual code
      // and surface a small log for debugging
      // ignore: avoid_print
      print('Failed to load promotions: $e');
    } finally {
      if (mounted) setState(() => _promosLoading = false);
    }
  }

  @override
  void dispose() {
    _specialRequestsController.dispose();
    _promoController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final homestayProvider = Provider.of<HomestayProvider>(context);
    final homestay = homestayProvider.selectedHomestay;

    if (homestay == null) {
      return const Scaffold(
        body: Center(child: Text('Không tìm thấy thông tin')),
      );
    }

    final nights = widget.checkOut.difference(widget.checkIn).inDays;
    final totalPrice = homestay.pricePerNight * nights;

    String _formatVnd(num amount) {
      try {
        final f = NumberFormat.currency(locale: 'vi_VN', symbol: 'đ', decimalDigits: 0);
        // NumberFormat puts symbol after number for vi locale; ensure spacing
        final s = f.format(amount);
        return s.replaceAll('\u00A0', ' ');
      } catch (_) {
        return '${amount.toStringAsFixed(0)}đ';
      }
    }

    final mq = MediaQuery.of(context);
    final bottomInset = mq.viewInsets.bottom + mq.viewPadding.bottom;

    return Scaffold(
      resizeToAvoidBottomInset: true,
      backgroundColor: Colors.transparent,
      appBar: AppBar(title: const Text('Xác nhận đặt phòng'), backgroundColor: Colors.transparent, elevation: 0),
      body: UserGradientBackground(
        child: SafeArea(
          bottom: false,
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Thông tin homestay', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 12),
                    Text(homestay.name, style: Theme.of(context).textTheme.titleMedium),
                    Text(homestay.fullAddress, style: Theme.of(context).textTheme.bodySmall),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Chi tiết đặt phòng', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 12),
                    _buildInfoRow('Nhận phòng', '${widget.checkIn.day}/${widget.checkIn.month}/${widget.checkIn.year}'),
                    _buildInfoRow('Trả phòng', '${widget.checkOut.day}/${widget.checkOut.month}/${widget.checkOut.year}'),
                    _buildInfoRow('Số đêm', '$nights đêm'),
                    _buildInfoRow('Số khách', '${widget.guests} khách'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Yêu cầu đặc biệt', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _specialRequestsController,
                      maxLines: 3,
                      decoration: const InputDecoration(
                        hintText: 'Nhập yêu cầu đặc biệt (nếu có)',
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Chi tiết giá', style: Theme.of(context).textTheme.titleLarge),
                    const SizedBox(height: 12),
                    _buildPriceRow('${_formatVnd(homestay.pricePerNight)} x $nights đêm', _formatVnd(totalPrice)),
                    const SizedBox(height: 8),
                    TextField(
                      controller: _promoController,
                      decoration: InputDecoration(
                        hintText: 'Mã khuyến mãi (nếu có)',
                        border: const OutlineInputBorder(),
                        suffixIcon: TextButton(
                          onPressed: () async {
                            final bookingService = BookingService();
                            try {
                              final promoToUse = _selectedPromotionCode ?? (_promoController.text.trim().isEmpty ? null : _promoController.text.trim());
                              final result = await bookingService.calculateAmount(
                                homestayId: homestay.id,
                                checkIn: widget.checkIn,
                                checkOut: widget.checkOut,
                                promotionCode: promoToUse,
                              );
                              // Try common keys for total
                              double? newTotal;
                              if (result['total'] != null) newTotal = (result['total'] as num).toDouble();
                              if (newTotal == null && result['discountedTotal'] != null) newTotal = (result['discountedTotal'] as num).toDouble();
                              if (newTotal == null && result['subtotal'] != null) newTotal = (result['subtotal'] as num).toDouble();
                              if (newTotal != null) {
                                setState(() => _discountedPrice = newTotal);
                              }
                            } catch (e) {
                              if (mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                  SnackBar(content: Text('Không thể áp dụng mã: $e')),
                                );
                              }
                            }
                          },
                          child: const Text('Áp dụng'),
                        ),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const SizedBox(height: 8),
                          if (_promosLoading)
                            const Padding(
                              padding: EdgeInsets.symmetric(vertical: 8.0),
                              child: SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)),
                            ),
                          if (!_promosLoading && _availablePromotions.isEmpty)
                            Padding(
                              padding: const EdgeInsets.only(bottom: 8.0),
                              child: Text('Không có khuyến mãi khả dụng', style: Theme.of(context).textTheme.bodySmall),
                            ),
                          Text('Chọn khuyến mãi có sẵn', style: Theme.of(context).textTheme.bodyMedium),
                          const SizedBox(height: 6),
                          DropdownButtonFormField<String?>(
                            value: _selectedPromotionCode,
                            items: [
                              const DropdownMenuItem<String?>(value: null, child: Text('Không dùng mã khuyến mãi')),
                              ..._availablePromotions.map((p) {
                                final code = p['code']?.toString() ?? p['promotionCode']?.toString();
                                final title = p['title'] ?? p['name'] ?? code;
                                return DropdownMenuItem<String?>(
                                  value: code,
                                  child: Text('$title (${code ?? ''})'),
                                );
                              }).toList(),
                            ],
                            onChanged: (val) async {
                              setState(() {
                                _selectedPromotionCode = val;
                                _promoController.text = val ?? '';
                                _discountedPrice = null; // reset until recalculated
                              });
                              // Recalculate price when selecting a promo
                              try {
                                final bookingService = BookingService();
                                final result = await bookingService.calculateAmount(
                                  homestayId: homestay.id,
                                  checkIn: widget.checkIn,
                                  checkOut: widget.checkOut,
                                  promotionCode: val,
                                );
                                double? newTotal;
                                if (result['total'] != null) newTotal = (result['total'] as num).toDouble();
                                if (newTotal == null && result['discountedTotal'] != null) newTotal = (result['discountedTotal'] as num).toDouble();
                                if (newTotal != null) {
                                  setState(() => _discountedPrice = newTotal);
                                }
                              } catch (_) {
                                // ignore calc errors silently
                              }
                            },
                            decoration: const InputDecoration(border: OutlineInputBorder()),
                            isExpanded: true,
                            hint: const Text('Chọn mã khuyến mãi'),
                          ),
                          const SizedBox(height: 6),
                          // Show selected promotion details if available
                          if (_selectedPromotionCode != null)
                            Builder(builder: (context) {
                              final promo = _availablePromotions.firstWhere(
                                (p) {
                                  final code = p['code']?.toString() ?? p['promotionCode']?.toString();
                                  return code == _selectedPromotionCode;
                                },
                                orElse: () => {},
                              );
                              if (promo.isEmpty) return const SizedBox.shrink();
                              final desc = promo['description'] ?? promo['summary'] ?? '';
                              String discountText = '';
                              if (promo['discount'] != null) {
                                discountText = promo['discount'].toString();
                              } else if (promo['percent'] != null) {
                                discountText = '${promo['percent']}%';
                              }
                              return Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  if (desc.toString().isNotEmpty) Text(desc.toString(), style: Theme.of(context).textTheme.bodySmall),
                                  if (discountText.isNotEmpty)
                                    Padding(
                                      padding: const EdgeInsets.only(top: 4.0),
                                      child: Text('Chiết khấu: $discountText', style: const TextStyle(fontWeight: FontWeight.w500)),
                                    ),
                                  Align(
                                    alignment: Alignment.centerRight,
                                    child: TextButton.icon(
                                      onPressed: () {
                                        setState(() {
                                          _selectedPromotionCode = null;
                                          _promoController.clear();
                                          _discountedPrice = null;
                                        });
                                      },
                                      icon: const Icon(Icons.clear, size: 18),
                                      label: const Text('Xóa chọn'),
                                    ),
                                  ),
                                ],
                              );
                            }),
                        ],
                      ),
                    const SizedBox(height: 8),
                    if (_discountedPrice != null)
                      _buildPriceRow('Giá sau khi áp dụng khuyến mãi', _formatVnd(_discountedPrice!), isTotal: true),
                    const Divider(),
                    _buildPriceRow('Tổng cộng', _formatVnd(_discountedPrice ?? totalPrice), isTotal: true),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
      ),
      ),
      bottomNavigationBar: Container(
        // include system bottom padding (navigation bars) and keyboard insets
        padding: EdgeInsets.fromLTRB(16, 8, 16, 8 + bottomInset),
        decoration: BoxDecoration(
          color: Colors.white,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.06),
              blurRadius: 8,
              offset: const Offset(0, -2),
            ),
          ],
        ),
        child: SizedBox(
          // let the button size itself but ensure a minimum height for tap target
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 48),
            child: ElevatedButton(
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              onPressed: _isLoading ? null : () => _confirmBooking(totalPrice),
              child: _isLoading
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                      ),
                    )
                  : Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Xác nhận đặt phòng', style: TextStyle(fontSize: 14)),
                              const SizedBox(height: 2),
                              Text(
                                'Nhấn để hoàn tất',
                                style: TextStyle(fontSize: 11, color: Colors.white.withOpacity(0.9)),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 12),
                        Flexible(
                          child: Text(
                            '${(_discountedPrice ?? totalPrice).toStringAsFixed(0)}đ',
                            textAlign: TextAlign.right,
                            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                          ),
                        ),
                      ],
                    ),
            ),
            ),
          ),
        ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }

  Widget _buildPriceRow(String label, String value, {bool isTotal = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: isTotal
                  ? const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)
                  : null,
              softWrap: true,
            ),
          ),
          const SizedBox(width: 8),
          Flexible(
            flex: 0,
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: isTotal
                  ? const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 18,
                      color: AppColors.primary,
                    )
                  : null,
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _confirmBooking(double totalPrice) async {
    setState(() {
      _isLoading = true;
    });

    final bookingProvider = Provider.of<BookingProvider>(context, listen: false);

    final booking = await bookingProvider.createBooking(
      homestayId: widget.homestayId,
      checkIn: widget.checkIn,
      checkOut: widget.checkOut,
      guests: widget.guests,
      specialRequests: _specialRequestsController.text.trim().isEmpty
          ? null
          : _specialRequestsController.text.trim(),
      promotionCode: _selectedPromotionCode ?? (_promoController.text.trim().isEmpty ? null : _promoController.text.trim()),
    );

    setState(() {
      _isLoading = false;
    });

    if (!mounted) return;

    if (booking != null) {
      // Show payment options dialog
      _showPaymentOptionsDialog(booking.id);
    } else {
      final mq = MediaQuery.of(context);
      final bottomMargin = 80 + mq.viewInsets.bottom + mq.padding.bottom;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          behavior: SnackBarBehavior.floating,
          margin: EdgeInsets.fromLTRB(16, 0, 16, bottomMargin),
          content: Text(bookingProvider.error ?? 'Đặt phòng thất bại'),
          backgroundColor: AppColors.error,
        ),
      );
    }
  }

  Future<void> _showPaymentOptionsDialog(int bookingId) async {
    final result = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Chọn phương thức thanh toán'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildPaymentOption(
              'PayPal',
              'Thanh toán bằng PayPal',
              Icons.credit_card,
              Colors.indigo,
            ),
            const SizedBox(height: 8),
            _buildPaymentOption(
              'FREE',
              'Miễn phí (không cần thanh toán)',
              Icons.check_circle,
              Colors.green,
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Hủy'),
          ),
        ],
      ),
    );

    if (!mounted) return;

    if (result == 'FREE') {
      // Process free payment - create payment record and update status
      await _processFreePayment(bookingId);
      return;
    }

    if (result != null) {
      await _processPayment(bookingId, result);
    }
  }

  Widget _buildPaymentOption(String method, String label, IconData icon, Color color) {
    return ListTile(
      leading: Icon(icon, color: color, size: 32),
      title: Text(label),
      trailing: const Icon(Icons.chevron_right),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
        side: BorderSide(color: Colors.grey.shade300),
      ),
      onTap: () => Navigator.pop(context, method),
    );
  }

  Future<void> _processFreePayment(int bookingId) async {
    setState(() => _isLoading = true);

    try {
      // Create payment record with FREE method
      final paymentService = PaymentService();
      final result = await paymentService.processPayment(
        bookingId: bookingId,
        paymentMethod: 'FREE',
      );

      setState(() => _isLoading = false);

      if (!mounted) return;

      // For free payment, mark as completed immediately
      if (result['success'] == true) {
        Navigator.of(context).pop(); // close booking screen
        Navigator.of(context).pop(); // close previous screen
        Navigator.pushNamed(context, '/my-bookings');
        
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đặt phòng thành công (miễn phí)!'),
            backgroundColor: Colors.green,
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(result['message'] ?? 'Lỗi xử lý đặt phòng'),
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

  Future<void> _processPayment(int bookingId, String paymentMethod) async {
    setState(() => _isLoading = true);

    try {
      final paymentService = PaymentService();
      final result = await paymentService.processPayment(
        bookingId: bookingId,
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
          Navigator.of(context).pop();
          Navigator.of(context).pop();
          Navigator.pushNamed(context, '/my-bookings');
          
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Thanh toán thành công!'),
              backgroundColor: Colors.green,
            ),
          );
        } else {
          // Payment failed or cancelled
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                paymentResult?['cancelled'] == true
                    ? 'Đã hủy thanh toán'
                    : 'Thanh toán thất bại',
              ),
              backgroundColor: AppColors.error,
            ),
          );
        }
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(result['message'] ?? 'Lỗi xử lý thanh toán'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } catch (e) {
      setState(() => _isLoading = false);
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi: $e'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }
}
