import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../models/booking.dart';
import '../../widgets/user_gradient_background.dart';
import '../profile/cccd_scan_screen.dart';

class HostDashboardScreen extends StatefulWidget {
  const HostDashboardScreen({Key? key}) : super(key: key);

  @override
  _HostDashboardScreenState createState() => _HostDashboardScreenState();
}

class _HostDashboardScreenState extends State<HostDashboardScreen> {
  final _storage = const FlutterSecureStorage();
  bool _isLoading = true;
  Map<String, dynamic> _stats = {};
  List<dynamic> _homestays = [];
  List<dynamic> _recentBookings = [];

  // Helper: format numeric values (int/double/string) into display string
  String _formatMoney(dynamic value) {
    if (value == null) return '0';
    try {
      if (value is String) return value;
      if (value is int) return value.toString();
      if (value is double) return value.toStringAsFixed(0);
      // sometimes backend returns decimals as num
      if (value is num) return value.toString();
      return value.toString();
    } catch (e) {
      return value.toString();
    }
  }

  @override
  void initState() {
    super.initState();
    _loadDashboardData();
  }

  Future<void> _loadDashboardData() async {
    if (mounted) setState(() => _isLoading = true);
    print('🔁 _loadDashboardData called');

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Vui lòng đăng nhập lại')),
        );
        return;
      }

      // Helper: coerce dynamic JSON into List or Map<String,dynamic>
      List<dynamic> _toList(dynamic parsed) {
        if (parsed is List) return List<dynamic>.from(parsed);
        if (parsed is Map) {
          if (parsed.containsKey('items') && parsed['items'] is List) return List<dynamic>.from(parsed['items']);
          if (parsed.containsKey('data') && parsed['data'] is List) return List<dynamic>.from(parsed['data']);
          if (parsed.containsKey('data') && parsed['data'] is Map && parsed['data'].containsKey('items') && parsed['data']['items'] is List) return List<dynamic>.from(parsed['data']['items']);
        }
        return <dynamic>[];
      }


      // Note: _toMap helper removed; casting done inline where needed.

      // Load stats
      final statsResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/host/stats'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (statsResponse.statusCode == 200) {
        final parsed = jsonDecode(statsResponse.body);
        // backend may return { success:true, data: { ... } } or raw object
        if (parsed is Map && parsed.containsKey('data')) {
          _stats = Map<String, dynamic>.from(parsed['data'] ?? {});
        } else if (parsed is Map) {
          _stats = Map<String, dynamic>.from(parsed);
        }
      }

      // Load homestays
      final homestaysResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/homestays/my-homestays'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (homestaysResponse.statusCode == 200) {
        final parsed = jsonDecode(homestaysResponse.body);
        // Convert to list in a safe way
        _homestays = _toList(parsed);

  // Normalize homestay items: ensure images is List<String>
        _homestays = _homestays.map((h) {
          if (h is Map) {
            final m = Map<String, dynamic>.from(h);
            final imagesRaw = m['images'];
            final List<String> images = [];

            if (imagesRaw is String) {
              images.add(imagesRaw);
            } else if (imagesRaw is List) {
              for (var item in imagesRaw) {
                if (item is String) {
                  images.add(item);
                } else if (item is Map) {
                  // common keys used by API
                  if (item.containsKey('imageUrl')) images.add(item['imageUrl']?.toString() ?? '');
                  else if (item.containsKey('url')) images.add(item['url']?.toString() ?? '');
                  else if (item.containsKey('image')) images.add(item['image']?.toString() ?? '');
                  else if (item.containsKey('fileName')) images.add(item['fileName']?.toString() ?? '');
                  else images.add(item.values.first.toString());
                } else if (item != null) {
                  images.add(item.toString());
                }
              }
            }

            m['images'] = images;
            return m;
          }
          return h;
        }).toList();
      }

      // Load recent bookings (use Booking model)
      final bookingsResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/bookings/my-bookings'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (bookingsResponse.statusCode == 200) {
        final parsed = jsonDecode(bookingsResponse.body);
        final list = _toList(parsed).take(5).toList();
        // convert to Booking model where possible
        _recentBookings = list.map((item) {
          if (item is Map<String, dynamic>) return item;
          if (item is Map) return Map<String, dynamic>.from(item);
          return {'id': item};
        }).toList();
      }

      // Load revenue data
      final revenueResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/host/revenue'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (revenueResponse.statusCode == 200) {
        final revenueData = jsonDecode(revenueResponse.body);
        if (revenueData is Map && revenueData.containsKey('data')) {
          _stats.addAll(Map<String, dynamic>.from(revenueData['data'] ?? {}));
        } else if (revenueData is Map) {
          _stats.addAll(Map<String, dynamic>.from(revenueData));
        }
      }

      // Load reviews stats
      final reviewsResponse = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/host/reviews'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (reviewsResponse.statusCode == 200) {
        final reviewsData = jsonDecode(reviewsResponse.body);
        if (reviewsData is Map) {
          if (reviewsData.containsKey('data') && reviewsData['data'] is Map && reviewsData['data'].containsKey('stats')) {
            _stats.addAll(Map<String, dynamic>.from(reviewsData['data']['stats'] ?? {}));
          } else if (reviewsData.containsKey('stats')) {
            _stats.addAll(Map<String, dynamic>.from(reviewsData['stats'] ?? {}));
          }
        }
      }

    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi tải dữ liệu: $e')),
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Bảng điều khiển Host'),
        backgroundColor: Colors.blue,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadDashboardData,
          ),
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: () {
              // Navigate to profile/settings
              Navigator.of(context).pushNamed('/edit-profile');
            },
          ),
        ],
      ),
      body: UserGradientBackground(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _loadDashboardData,
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _buildStatsCards(),
                      const SizedBox(height: 24),
                      _buildQuickActions(),
                      const SizedBox(height: 24),
                      _buildRevenueCard(),
                      const SizedBox(height: 24),
                      _buildReviewsCard(),
                      const SizedBox(height: 24),
                      _buildRecentBookings(),
                      const SizedBox(height: 24),
                      _buildMyHomestays(),
                    ],
                  ),
                ),
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await Navigator.of(context).pushNamed('/create-homestay');
          if (result != null) await _loadDashboardData();
        },
        backgroundColor: Colors.blue,
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildStatsCards() {
    return Row(
      children: [
        Expanded(
          child: _buildStatCard(
            'Homestay',
            _stats['totalHomestays']?.toString() ?? '0',
            Icons.home,
            Colors.blue,
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: _buildStatCard(
            'Đặt phòng',
            _stats['totalBookings']?.toString() ?? '0',
            Icons.book_online,
            Colors.green,
          ),
        ),
      ],
    );
  }

  Widget _buildStatCard(String title, String value, IconData icon, Color color) {
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
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            Text(title, style: const TextStyle(color: Colors.grey)),
          ],
        ),
      ),
    );
  }

  Widget _buildQuickActions() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Hành động nhanh',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: _buildActionButton(
                'Tạo homestay',
                Icons.add_home,
                () async {
                  final result = await Navigator.of(context).pushNamed('/create-homestay');
                  if (result != null) await _loadDashboardData();
                },
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: _buildActionButton(
                'Quản lý homestay',
                Icons.manage_accounts,
                () => Navigator.of(context).pushNamed('/manage-homestays'),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        // Quick access to scan ID for host verification
        SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            icon: const Icon(Icons.badge),
            label: const Text('Quét CCCD/CMND'),
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 14),
              backgroundColor: Colors.white,
              foregroundColor: Colors.blue,
              side: const BorderSide(color: Colors.blue),
            ),
            onPressed: () async {
              final res = await Navigator.of(context).push(MaterialPageRoute(builder: (_) => const CccdScanScreen()));
              if (res != null) {
                final name = res['fullName'] ?? '';
                final id = res['idNumber'] ?? '';
                ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Kết quả: $name - $id')));
                // TODO: submit to host verification endpoint if you have one
              }
            },
          ),
        ),
      ],
    );
  }

  Widget _buildActionButton(String title, IconData icon, VoidCallback onPressed) {
    return ElevatedButton(
      onPressed: onPressed,
      style: ElevatedButton.styleFrom(
        padding: const EdgeInsets.symmetric(vertical: 16),
        backgroundColor: Colors.white,
        foregroundColor: Colors.blue,
        side: const BorderSide(color: Colors.blue),
      ),
      child: Column(
        children: [
          Icon(icon, size: 24),
          const SizedBox(height: 8),
          Text(title, textAlign: TextAlign.center),
        ],
      ),
    );
  }

  Widget _buildRevenueCard() {
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Doanh thu',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Tổng doanh thu:'),
                Text(
                  '${_formatMoney(_stats['totalRevenue'])} VND',
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Colors.green,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Tháng này:'),
                Text(
                  '${_formatMoney(_stats['currentMonthRevenue'])} VND',
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Colors.blue,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () {
                  Navigator.pushNamed(context, '/host-revenue');
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.green,
                ),
                child: const Text('Xem chi tiết'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildReviewsCard() {
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Đánh giá',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Đánh giá trung bình:'),
                Row(
                  children: [
                    Text(
                      // averageRating might be int/double/string
                      (_stats['averageRating'] is num)
                          ? (_stats['averageRating'] as num).toStringAsFixed(1)
                          : (_stats['averageRating']?.toString() ?? '0.0'),
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        color: Colors.amber,
                      ),
                    ),
                    const Icon(Icons.star, color: Colors.amber, size: 16),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Tổng đánh giá:'),
                Text(
                  _stats['totalReviews']?.toString() ?? '0',
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    color: Colors.blue,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () {
                  Navigator.pushNamed(context, '/host-reviews');
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.amber,
                ),
                child: const Text('Xem đánh giá'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRecentBookings() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Đặt phòng gần đây',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            TextButton(
              onPressed: () => Navigator.of(context).pushNamed('/host-bookings'),
              child: const Text('Xem tất cả'),
            ),
          ],
        ),
        const SizedBox(height: 16),
        _recentBookings.isEmpty
            ? const Center(child: Text('Chưa có đặt phòng nào'))
            : ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _recentBookings.length,
                itemBuilder: (context, index) {
                  try {
                    final raw = _recentBookings[index];
                    Map<String, dynamic> map;
                    if (raw is Map<String, dynamic>) {
                      map = raw;
                    } else if (raw is Map) {
                      map = Map<String, dynamic>.from(raw);
                    } else {
                      map = {'id': raw};
                    }

                    // Try to convert to Booking model for consistent display
                    final bookingModel = Booking.fromJson(map);

                    // Determine amount: prefer server-side finalAmount (different casings), else totalPrice
                    dynamic amount;
                    if (map.containsKey('finalAmount')) amount = map['finalAmount'];
                    else if (map.containsKey('FinalAmount')) amount = map['FinalAmount'];
                    else amount = bookingModel.totalPrice;

                    return Card(
                      margin: const EdgeInsets.only(bottom: 8),
                      child: ListTile(
                        title: Text('Booking #${bookingModel.id}'),
                        subtitle: Text(
                          'Khách: ${bookingModel.guestName}\n'
                          'Ngày: ${bookingModel.checkInDate.toIso8601String()} - ${bookingModel.checkOutDate.toIso8601String()}',
                        ),
                        trailing: Text(
                          '${_formatMoney(amount)} VND',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ),
                    );
                  } catch (e, st) {
                    // Log raw JSON for debugging
                    debugPrint('Error rendering recent booking at index $index: $e');
                    debugPrint(st.toString());
                    final raw = _recentBookings.length > index ? _recentBookings[index] : null;
                    debugPrint('Raw booking data: $raw');
                    return Card(
                      color: Colors.red[700],
                      margin: const EdgeInsets.only(bottom: 8),
                      child: SizedBox(
                        height: 200,
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

  Widget _buildMyHomestays() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'Homestay của tôi',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            TextButton(
              onPressed: () => Navigator.of(context).pushNamed('/manage-homestays'),
              child: const Text('Quản lý'),
            ),
          ],
        ),
        const SizedBox(height: 16),
        _homestays.isEmpty
            ? const Center(child: Text('Chưa có homestay nào'))
            : ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _homestays.length,
                itemBuilder: (context, index) {
                  final homestay = _homestays[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: homestay['images'] != null && homestay['images'].isNotEmpty
                          ? Image.network(
                              homestay['images'][0],
                              width: 60,
                              height: 60,
                              fit: BoxFit.cover,
                              errorBuilder: (context, error, stackTrace) =>
                                  const Icon(Icons.home, size: 60),
                            )
                          : const Icon(Icons.home, size: 60),
                      title: Text(homestay['name'] ?? 'N/A'),
                      subtitle: Text(
                        '${homestay['address'] ?? 'N/A'}\n'
                        '${homestay['pricePerNight']?.toString() ?? '0'} VND/đêm',
                      ),
                      trailing: Icon(
                        homestay['isActive'] == true ? Icons.check_circle : Icons.pause_circle,
                        color: homestay['isActive'] == true ? Colors.green : Colors.orange,
                      ),
                    ),
                  );
                },
              ),
      ],
    );
  }
}
