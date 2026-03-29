import 'package:flutter/material.dart';

import '../../services/admin_service.dart';

class AdminBookingsScreen extends StatefulWidget {
  const AdminBookingsScreen({super.key});

  @override
  State<AdminBookingsScreen> createState() => _AdminBookingsScreenState();
}

class _AdminBookingsScreenState extends State<AdminBookingsScreen> with TickerProviderStateMixin {
  final AdminService _admin = AdminService();
  List<dynamic> _items = [];
  bool _loading = true;
  int _page = 1;
  int _pageSize = 20;
  int _total = 0;
  String _status = 'all';
  late AnimationController _fadeController;
  late Animation<double> _fadeAnimation;

  @override
  void initState() {
    super.initState();
    _fadeController = AnimationController(vsync: this, duration: const Duration(milliseconds: 500));
    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(CurvedAnimation(parent: _fadeController, curve: Curves.easeIn));
    _load();
  }

  @override
  void dispose() {
    _fadeController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    if (!mounted) return;
    setState(() => _loading = true);
    try {
      final data = await _admin.getBookings(page: _page, pageSize: _pageSize);
      if (!mounted) return;
      setState(() {
        _items = data['items'] ?? [];
        _total = data['total'] ?? 0;
      });
      if (mounted) _fadeController.forward(from: 0.0);
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Lỗi khi tải đặt phòng: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _changeStatus(int id, String status) async {
    try {
      await _admin.updateBookingStatus(id, status);
      await _load();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Đã cập nhật trạng thái thành $status'),
          backgroundColor: Colors.greenAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Không thể cập nhật trạng thái: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'paid':
        return Colors.green;
      case 'completed':
        return Colors.blue;
      case 'cancelled':
        return Colors.red;
      case 'pending':
        return Colors.orange;
      default:
        return Colors.grey;
    }
  }

  String _getStatusText(String status) {
    switch (status.toLowerCase()) {
      case 'paid':
        return 'Đã thanh toán';
      case 'completed':
        return 'Hoàn thành';
      case 'cancelled':
        return 'Đã hủy';
      case 'pending':
        return 'Chờ xử lý';
      default:
        return status;
    }
  }

  // Normalize status returned from API. API may send enum as int or string.
  String _normalizeStatus(dynamic raw) {
    if (raw == null) return 'unknown';
    if (raw is int) {
      switch (raw) {
        case 0:
          return 'pending';
        case 1:
          return 'paid';
        case 2:
          return 'cancelled';
        case 3:
          return 'completed';
        default:
          return 'unknown';
      }
    }
    if (raw is String) return raw.toLowerCase();
    return raw.toString().toLowerCase();
  }

  @override
  Widget build(BuildContext context) {
    return Theme(
      data: Theme.of(context).copyWith(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: Colors.blue,
          brightness: Brightness.light,
        ),
      ),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Quản lý đặt phòng', style: TextStyle(fontWeight: FontWeight.bold)),
          flexibleSpace: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Colors.blue, Colors.indigo],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
            ),
          ),
          elevation: 4,
          shadowColor: Colors.blue.withOpacity(0.3),
        ),
        body: Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [Colors.grey.shade50, Colors.white],
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
            ),
          ),
          child: _loading
              ? const Center(
                  child: CircularProgressIndicator(
                    valueColor: AlwaysStoppedAnimation<Color>(Colors.blue),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  color: Colors.blue,
                  child: Column(
                    children: [
                      // Filters and pagination
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: const BorderRadius.only(
                            bottomLeft: Radius.circular(20),
                            bottomRight: Radius.circular(20),
                          ),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.grey.withOpacity(0.1),
                              spreadRadius: 2,
                              blurRadius: 8,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: Column(
                          children: [
                            // Status filter
                            Row(
                              children: [
                                const Text('Trạng thái:', style: TextStyle(fontWeight: FontWeight.w500)),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: DropdownButtonFormField<String>(
                                    value: _status,
                                    decoration: InputDecoration(
                                      filled: true,
                                      fillColor: Colors.grey.shade100,
                                      border: OutlineInputBorder(
                                        borderRadius: BorderRadius.circular(8),
                                        borderSide: BorderSide.none,
                                      ),
                                      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                                    ),
                                    items: const [
                                      DropdownMenuItem(value: 'all', child: Text('Tất cả')),
                                      DropdownMenuItem(value: 'Pending', child: Text('Chờ xử lý')),
                                      DropdownMenuItem(value: 'Paid', child: Text('Đã thanh toán')),
                                      DropdownMenuItem(value: 'Completed', child: Text('Hoàn thành')),
                                      DropdownMenuItem(value: 'Cancelled', child: Text('Đã hủy')),
                                    ],
                                    onChanged: (v) {
                                      setState(() {
                                        _status = v ?? 'all';
                                        _page = 1;
                                      });
                                      _load();
                                    },
                                  ),
                                ),
                                const SizedBox(width: 8),
                                const Text('Hiển thị:', style: TextStyle(fontWeight: FontWeight.w500)),
                                const SizedBox(width: 8),
                                DropdownButton<int>(
                                  value: _pageSize,
                                  underline: const SizedBox(),
                                  items: const [
                                    DropdownMenuItem(value: 10, child: Text('10')),
                                    DropdownMenuItem(value: 20, child: Text('20')),
                                    DropdownMenuItem(value: 50, child: Text('50')),
                                  ],
                                  onChanged: (v) {
                                    setState(() {
                                      _pageSize = v ?? 20;
                                      _page = 1;
                                    });
                                    _load();
                                  },
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                      // Pagination
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            IconButton(
                              onPressed: () {
                                if (_page > 1) {
                                  setState(() => _page = 1);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.first_page),
                              color: Colors.blue,
                            ),
                            IconButton(
                              onPressed: () {
                                if ((_page - 1) * _pageSize > 0) {
                                  setState(() => _page--);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.chevron_left),
                              color: Colors.blue,
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                              decoration: BoxDecoration(
                                color: Colors.blue.shade50,
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(
                                'Trang $_page / ${(_total / _pageSize).ceil().clamp(1, 9999)}',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                            ),
                            IconButton(
                              onPressed: () {
                                if (_page * _pageSize < _total) {
                                  setState(() => _page++);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.chevron_right),
                              color: Colors.blue,
                            ),
                            IconButton(
                              onPressed: () {
                                final last = (_total / _pageSize).ceil();
                                if (_page < last) {
                                  setState(() => _page = last);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.last_page),
                              color: Colors.blue,
                            ),
                          ],
                        ),
                      ),
                      // List
                      Expanded(
                        child: FadeTransition(
                          opacity: _fadeAnimation,
                          child: ListView.builder(
                            padding: const EdgeInsets.all(16),
                            itemCount: _items.length,
                            itemBuilder: (context, index) {
                              final b = _items[index];
                              final bid = b['id'] is int ? b['id'] as int : int.parse(b['id'].toString());
                              final statusRaw = b['status'] ?? b['Status'];
                              final status = _normalizeStatus(statusRaw);

                              // Date fields may be named checkIn/checkOut or checkInDate/checkOutDate depending on API
                              final checkIn = (b['checkIn'] ?? b['checkInDate'])?.toString() ?? '';
                              final checkOut = (b['checkOut'] ?? b['checkOutDate'])?.toString() ?? '';

                              // Price may be finalAmount or totalAmount or totalPrice
                              final totalPrice = (b['finalAmount'] ?? b['totalAmount'] ?? b['totalPrice'])?.toString() ?? '0';

                              return Card(
                                elevation: 4,
                                margin: const EdgeInsets.only(bottom: 12),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                child: Padding(
                                  padding: const EdgeInsets.all(16),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          Icon(Icons.receipt, color: Colors.blue, size: 24),
                                          const SizedBox(width: 8),
                                          Text(
                                            'Đặt phòng #$bid',
                                            style: const TextStyle(
                                              fontSize: 18,
                                              fontWeight: FontWeight.bold,
                                            ),
                                          ),
                                          const Spacer(),
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                                            decoration: BoxDecoration(
                                              color: _getStatusColor(status).withOpacity(0.1),
                                              borderRadius: BorderRadius.circular(16),
                                              border: Border.all(color: _getStatusColor(status)),
                                            ),
                                            child: Text(
                                              _getStatusText(status),
                                              style: TextStyle(
                                                color: _getStatusColor(status),
                                                fontWeight: FontWeight.w500,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 12),
                                      Row(
                                        children: [
                                          Icon(Icons.home, size: 16, color: Colors.grey),
                                          const SizedBox(width: 4),
                                          Expanded(
                                            child: Text(
                                              b['homestayName'] ?? 'Homestay không xác định',
                                              style: const TextStyle(fontSize: 14),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      Row(
                                        children: [
                                          Icon(Icons.person, size: 16, color: Colors.grey),
                                          const SizedBox(width: 4),
                                          Expanded(
                                            child: Text(
                                              'Khách: ${b['userName'] ?? b['userEmail'] ?? 'Không xác định'}',
                                              style: const TextStyle(fontSize: 14),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      // Use Wrap so date labels can flow to next line on small screens
                                      Wrap(
                                        spacing: 12,
                                        runSpacing: 6,
                                        crossAxisAlignment: WrapCrossAlignment.center,
                                        children: [
                                          Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                                              const SizedBox(width: 4),
                                              ConstrainedBox(
                                                constraints: const BoxConstraints(maxWidth: 220),
                                                child: Text(
                                                  'Nhận phòng: $checkIn',
                                                  style: const TextStyle(fontSize: 14),
                                                  overflow: TextOverflow.ellipsis,
                                                  maxLines: 1,
                                                ),
                                              ),
                                            ],
                                          ),
                                          Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                                              const SizedBox(width: 4),
                                              ConstrainedBox(
                                                constraints: const BoxConstraints(maxWidth: 220),
                                                child: Text(
                                                  'Trả phòng: $checkOut',
                                                  style: const TextStyle(fontSize: 14),
                                                  overflow: TextOverflow.ellipsis,
                                                  maxLines: 1,
                                                ),
                                              ),
                                            ],
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      // Price row - allow truncation on narrow screens
                                      Row(
                                        children: [
                                          Icon(Icons.attach_money, size: 16, color: Colors.green),
                                          const SizedBox(width: 4),
                                          Expanded(
                                            child: Text(
                                              'Tổng tiền: $totalPrice VND',
                                              style: const TextStyle(
                                                fontSize: 14,
                                                fontWeight: FontWeight.w500,
                                                color: Colors.green,
                                              ),
                                              overflow: TextOverflow.ellipsis,
                                              maxLines: 1,
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 12),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.end,
                                        children: [
                                          PopupMenuButton<String>(
                                            onSelected: (val) async {
                                              if (val == 'confirm') await _changeStatus(bid, 'Paid');
                                              if (val == 'complete') await _changeStatus(bid, 'Completed');
                                              if (val == 'cancel') await _changeStatus(bid, 'Cancelled');
                                            },
                                            itemBuilder: (_) => [
                                              const PopupMenuItem(value: 'confirm', child: Text('Xác nhận thanh toán')),
                                              const PopupMenuItem(value: 'complete', child: Text('Đánh dấu hoàn thành')),
                                              const PopupMenuItem(value: 'cancel', child: Text('Hủy đặt phòng')),
                                            ],
                                            child: Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                                              decoration: BoxDecoration(
                                                color: Colors.blue.shade50,
                                                borderRadius: BorderRadius.circular(8),
                                              ),
                                              child: const Row(
                                                mainAxisSize: MainAxisSize.min,
                                                children: [
                                                  Text('Thao tác', style: TextStyle(color: Colors.blue)),
                                                  Icon(Icons.arrow_drop_down, color: Colors.blue),
                                                ],
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              );
                            },
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
        ),
        floatingActionButton: FloatingActionButton(
          onPressed: _load,
          backgroundColor: Colors.blue,
          child: const Icon(Icons.refresh),
        ),
      ),
    );
  }
}
