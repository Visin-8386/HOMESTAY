import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../models/booking.dart';

class HostRevenueScreen extends StatefulWidget {
  const HostRevenueScreen({Key? key}) : super(key: key);

  @override
  _HostRevenueScreenState createState() => _HostRevenueScreenState();
}

class _HostRevenueScreenState extends State<HostRevenueScreen> {
  final _storage = const FlutterSecureStorage();
  bool _isLoading = true;
  Map<String, dynamic> _revenueData = {};
  List<dynamic> _completedBookings = [];

  String _formatVND(dynamic value) {
    try {
      double v;
      if (value == null) return '0 VND';
      if (value is num) v = value.toDouble();
      else v = double.tryParse(value.toString()) ?? 0.0;

      // Round to integer dong and format with dots as thousand separators
      final intVal = v.round();
      final s = intVal.toString();
      if (s.isEmpty) return '0 VND';
      final reg = RegExp(r"\B(?=(\d{3})+(?!\d))");
      final withDots = s.replaceAllMapped(reg, (m) => '.');
      return '$withDots VND';
    } catch (e) {
      return '0 VND';
    }
  }

  @override
  void initState() {
    super.initState();
    _loadRevenueData();
  }

  Future<void> _loadRevenueData() async {
    setState(() => _isLoading = true);

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Vui lòng đăng nhập lại')),
        );
        return;
      }

      // Load revenue stats
      final statsResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/host/revenue'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (statsResponse.statusCode == 200) {
        final parsed = jsonDecode(statsResponse.body);
        if (parsed is Map && parsed.containsKey('data')) {
          _revenueData = Map<String, dynamic>.from(parsed['data'] ?? {});
        } else if (parsed is Map) {
          _revenueData = Map<String, dynamic>.from(parsed);
        } else {
          _revenueData = {};
        }
      }

      // Load completed bookings
      final bookingsResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/bookings/my-bookings?status=completed'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (bookingsResponse.statusCode == 200) {
        final parsed = jsonDecode(bookingsResponse.body);
        final data = parsed is Map ? (parsed['data'] ?? parsed) : parsed;
        // ensure list
        if (data is List) {
          _completedBookings = data.map((e) => e is Map ? Map<String, dynamic>.from(e) : {'id': e}).toList();
        } else if (data is Map && data.containsKey('items')) {
          final items = data['items'];
          if (items is List) _completedBookings = items.map((e) => e is Map ? Map<String, dynamic>.from(e) : {'id': e}).toList();
        } else {
          _completedBookings = [];
        }
      }

    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi tải dữ liệu doanh thu: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Doanh thu'),
        backgroundColor: Colors.blue,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadRevenueData,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadRevenueData,
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _buildRevenueOverview(),
                    const SizedBox(height: 24),
                    _buildMonthlyRevenue(),
                    const SizedBox(height: 24),
                    _buildCompletedBookings(),
                  ],
                ),
              ),
            ),
    );
  }

  Widget _buildRevenueOverview() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Tổng quan doanh thu',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _buildRevenueCard(
                  'Tổng doanh thu',
                  _formatVND(_revenueData['totalRevenue']),
                  Icons.attach_money,
                  Colors.green,
                ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: _buildRevenueCard(
                'Tháng này',
                _formatVND(_revenueData['currentMonthRevenue']),
                Icons.calendar_month,
                Colors.blue,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _buildRevenueCard(
                'Số booking hoàn thành',
                _revenueData['completedBookingsCount']?.toString() ?? '0',
                Icons.check_circle,
                Colors.orange,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: _buildRevenueCard(
                'Tỷ lệ hoàn thành',
                '${_revenueData['completionRate']?.toString() ?? '0'}%',
                Icons.trending_up,
                Colors.purple,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildRevenueCard(String title, String value, IconData icon, Color color) {
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            Icon(icon, size: 32, color: color),
            const SizedBox(height: 8),
            Text(
              value,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 4),
            Text(
              title,
              style: const TextStyle(color: Colors.grey, fontSize: 12),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMonthlyRevenue() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Doanh thu theo tháng',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
  _revenueData['monthlyRevenue'] != null && (_revenueData['monthlyRevenue'] is List || _revenueData['monthlyRevenue'] is Map)
    ? ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: (() {
                  final m = _revenueData['monthlyRevenue'];
                  if (m == null) return 0;
                  if (m is List) return m.length;
                  if (m is Map) return 1;
                  return 0;
                })(),
                itemBuilder: (context, index) {
                  final rawMonthly = _revenueData['monthlyRevenue'];
                  Map<String, dynamic> monthData;
                  if (rawMonthly is List) {
                    final item = rawMonthly[index];
                    monthData = item is Map<String, dynamic> ? Map<String, dynamic>.from(item) : {'month': item};
                  } else if (rawMonthly is Map) {
                    monthData = Map<String, dynamic>.from(rawMonthly);
                  } else {
                    monthData = {};
                  }
                  return Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      title: Text('Tháng ${monthData['month']}/${monthData['year']}'),
                      trailing: Text(
                        _formatVND(monthData['revenue']),
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Colors.green,
                        ),
                      ),
                    ),
                  );
                },
              )
            : const Center(
                child: Text(
                  'Chưa có dữ liệu doanh thu theo tháng',
                  style: TextStyle(color: Colors.grey),
                ),
              ),
      ],
    );
  }

  Widget _buildCompletedBookings() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Booking đã hoàn thành',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        _completedBookings.isEmpty
            ? const Center(
                child: Text(
                  'Chưa có booking nào hoàn thành',
                  style: TextStyle(color: Colors.grey),
                ),
              )
            : ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _completedBookings.length,
                itemBuilder: (context, index) {
                  try {
                    final raw = _completedBookings[index];
                    Map<String, dynamic> map;
                    if (raw is Map<String, dynamic>) map = raw;
                    else if (raw is Map) map = Map<String, dynamic>.from(raw);
                    else map = {'id': raw};

                    final booking = Booking.fromJson(map);
                    // prefer FinalAmount if present on server, else totalPrice
                    final rawAmount = map['finalAmount'] ?? map['FinalAmount'] ?? booking.totalPrice;
                    final amount = _formatVND(rawAmount);

                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        title: Text('Booking #${booking.id}'),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('Khách: ${booking.guestName}'),
                            Text('Homestay: ${booking.homestayName}'),
                            Text('Ngày: ${booking.checkInDate.toIso8601String()} - ${booking.checkOutDate.toIso8601String()}'),
                          ],
                        ),
                        trailing: Text(
                          amount,
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            color: Colors.green,
                          ),
                        ),
                      ),
                    );
                  } catch (e, st) {
                    debugPrint('Error rendering completed booking at index $index: $e');
                    debugPrint(st.toString());
                    final raw = _completedBookings.length > index ? _completedBookings[index] : null;
                    debugPrint('Raw completed booking data: $raw');
                    return Card(
                      color: Colors.red[700],
                      margin: const EdgeInsets.only(bottom: 8),
                      child: SizedBox(
                        height: 120,
                        child: Center(
                          child: Text(
                            'Lỗi hiển thị booking: $e',
                            style: const TextStyle(color: Colors.white),
                          ),
                        ),
                      ),
                    );
                  }
                },
              ),
      ],
    );
  }
}