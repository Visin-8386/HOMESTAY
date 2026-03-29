import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../services/storage_service.dart';
import '../../widgets/user_gradient_background.dart';
import '../review/create_review_screen.dart';

class MyBookingsScreen extends StatefulWidget {
  const MyBookingsScreen({super.key});

  @override
  State<MyBookingsScreen> createState() => _MyBookingsScreenState();
}

class _MyBookingsScreenState extends State<MyBookingsScreen> {
  List<dynamic> _bookings = [];
  bool _isLoading = true;
  String _selectedTab = 'all'; // all, pending, confirmed, completed, cancelled

  @override
  void initState() {
    super.initState();
    _loadBookings();
  }

  Future<void> _loadBookings() async {
    setState(() => _isLoading = true);

    try {
      final token = await StorageService().getToken();

      if (token == null) return;

      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/bookings/my-bookings'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );


      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (mounted) {
          setState(() {
            _bookings = data['data'] ?? [];
            _isLoading = false;
          });
        }
      } else {
        if (mounted) {
          setState(() => _isLoading = false);
        }
      }
    } catch (e) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<dynamic> get _filteredBookings {
    if (_selectedTab == 'all') return _bookings;
    if (_selectedTab == 'pending') return _bookings.where((b) => b['status'] == 0).toList();
    if (_selectedTab == 'confirmed') return _bookings.where((b) => b['status'] == 1).toList();
    if (_selectedTab == 'completed') return _bookings.where((b) => b['status'] == 3).toList();
    // Server uses 2 for Cancelled (see BookingStatus enum). Use 2 instead of 4.
    if (_selectedTab == 'cancelled') return _bookings.where((b) => b['status'] == 2).toList();
    return _bookings;
  }
  Color _getStatusColor(int? status) {
    switch (status) {
      case 0:
        return Colors.orange;
      case 1:
        return Colors.blue;
      case 3:
        return Colors.green;
      case 2:
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  String _getStatusText(int? status) {
    switch (status) {
      case 0:
        return 'Chờ';
      case 1:
        return 'Xác nhận';
      case 3:
        return 'Hoàn thành';
      case 2:
        return 'Đã hủy';
      default:
        return 'Không rõ';
    }
  }

      @override
      Widget build(BuildContext context) {
        return Scaffold(
            backgroundColor: Colors.transparent,
            appBar: AppBar(title: const Text('Đơn đặt của tôi')),
            body: UserGradientBackground(
              child: Column(
            children: [
              // Tab filter
              Container(
                color: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Row(
                    children: [
                      _buildTabChip('Tất cả', 'all'),
                      _buildTabChip('Chờ xác nhận', 'pending'),
                      _buildTabChip('Đã xác nhận', 'confirmed'),
                      _buildTabChip('Hoàn thành', 'completed'),
                      _buildTabChip('Đã hủy', 'cancelled'),
                    ],
                  ),
                ),
              ),

              // Danh sách bookings
              Expanded(
                child: _isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : _filteredBookings.isEmpty
                        ? Center(
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(
                                  Icons.event_busy,
                                  size: 80,
                                  color: Colors.grey[400],
                                ),
                                const SizedBox(height: 16),
                                Text(
                                  'Chưa có đơn đặt phòng nào',
                                  style: TextStyle(
                                    fontSize: 16,
                                    color: Colors.grey[600],
                                  ),
                                ),
                              ],
                            ),
                          )
                        : RefreshIndicator(
                            onRefresh: _loadBookings,
                            child: ListView.builder(
                              padding: const EdgeInsets.all(16),
                              itemCount: _filteredBookings.length,
                              itemBuilder: (context, index) {
                                final booking = _filteredBookings[index];
                                return _buildBookingCard(booking, index);
                              },
                            ),
                          ),
              ),
            ],
          ),
        ),
      );
      }

      Widget _buildTabChip(String label, String value) {
        final isSelected = _selectedTab == value;
        return Padding(
          padding: const EdgeInsets.only(right: 8),
          child: FilterChip(
            label: Text(label),
            selected: isSelected,
            onSelected: (selected) {
              setState(() => _selectedTab = value);
            },
            backgroundColor: Colors.grey[200],
            selectedColor: const Color(0xFF667eea),
            labelStyle: TextStyle(
              color: isSelected ? Colors.white : Colors.black87,
              fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
            ),
          ),
        );
      }

      Widget _buildBookingCard(dynamic booking, int index) {
        final homestayName = booking['homestayName'] ?? 'Homestay';
        final checkIn = booking['checkInDate'] ?? '';
        final checkOut = booking['checkOutDate'] ?? '';
        // API returns 'finalAmount' not 'totalPrice'
        final totalPrice = booking['finalAmount'] ?? booking['totalAmount'] ?? 0;
        final status = booking['status'];
        final guests = booking['numberOfGuests'] ?? 1;
        // server returns review fields as top-level fields (ReviewRating, ReviewComment, ReviewCreatedAt)
        // map them into a `review` object to keep client-side UI consistent
        final review = booking['review'] ?? (booking['reviewRating'] != null
            ? {
                'rating': booking['reviewRating'],
                'comment': booking['reviewComment'],
                'createdAt': booking['reviewCreatedAt']
              }
            : null);

        return Card(
          margin: const EdgeInsets.only(bottom: 16),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          elevation: 2,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        homestayName,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                        color: _getStatusColor(status).withOpacity(0.1),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Text(
                        _getStatusText(status),
                        style: TextStyle(
                          color: _getStatusColor(status),
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 12),

                // Check-in / Check-out
                Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Nhận phòng',
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey[600],
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            checkIn.split('T')[0],
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Icon(Icons.arrow_forward, color: Colors.grey[400], size: 20),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Trả phòng',
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey[600],
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            checkOut.split('T')[0],
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 12),

                // Guests & Total
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.people, size: 18, color: Colors.grey[600]),
                        const SizedBox(width: 4),
                        Text(
                          '$guests khách',
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.grey[700],
                          ),
                        ),
                      ],
                    ),
                    Text(
                      '$totalPrice VNĐ',
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF667eea),
                      ),
                    ),
                  ],
                ),

                // Buttons for pending status
                if (status == 0) ...[
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Chức năng hủy đơn sẽ được bổ sung')),
                            );
                          },
                          style: OutlinedButton.styleFrom(
                            side: const BorderSide(color: Colors.red),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                            ),
                          ),
                          child: const Text(
                            'Hủy đơn',
                            style: TextStyle(color: Colors.red),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: ElevatedButton(
                          onPressed: () {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Chức năng liên hệ sẽ được bổ sung')),
                            );
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFF667eea),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                            ),
                          ),
                          child: const Text(
                            'Liên hệ',
                            style: TextStyle(color: Colors.white),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],

                // If booking is completed (status == 3)
                if (status == 3) ...[
                  const SizedBox(height: 12),
                  // If a review exists, show it inline and hide the 'Đánh giá' button
                  if (review != null) ...[
                    Builder(builder: (context) {
                      // use local `review` variable (may come from server top-level fields)
                      // optimistic local marker while waiting server refresh
                      if (review is Map && review['local'] == true) {
                        return Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(color: Colors.grey[100], borderRadius: BorderRadius.circular(8)),
                          child: Row(
                            children: [
                              const Icon(Icons.rate_review, color: Colors.grey),
                              const SizedBox(width: 8),
                              Expanded(child: Text('Bạn đã gửi đánh giá (đang cập nhật...)', style: TextStyle(color: Colors.grey[700]))),
                            ],
                          ),
                        );
                      }

                      // Normal case: show rating and comment
                      final rating = (review is Map && review['rating'] != null) ? (review['rating'] as num).toDouble() : null;
                      final comment = (review is Map && review['comment'] != null) ? review['comment'].toString() : null;
                      final createdAt = (review is Map && review['createdAt'] != null) ? review['createdAt'].toString().split('T')[0] : null;

                      return Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(color: Colors.grey[50], borderRadius: BorderRadius.circular(8), border: Border.all(color: Colors.grey.shade200)),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                const Icon(Icons.star, color: Colors.amber, size: 18),
                                const SizedBox(width: 6),
                                Text(rating != null ? rating.toString() : '—', style: const TextStyle(fontWeight: FontWeight.bold)),
                                const SizedBox(width: 8),
                                if (createdAt != null) Text(createdAt, style: TextStyle(color: Colors.grey[600], fontSize: 12)),
                              ],
                            ),
                            if (comment != null && comment.isNotEmpty) ...[
                              const SizedBox(height: 8),
                              Text(comment, style: const TextStyle(fontSize: 14)),
                            ]
                          ],
                        ),
                      );
                    }),
                  ] else ...[
                    // No review yet: show Review button
                    Row(
                      children: [
                        Expanded(
                          child: ElevatedButton(
                            onPressed: () async {
                              // Open CreateReviewScreen and optimistically update the UI
                              final result = await Navigator.of(context).push<bool>(
                                MaterialPageRoute(
                                  builder: (_) => CreateReviewScreen(bookingId: booking['id'] ?? 0),
                                ),
                              );

                              if (result == true) {
                                // Optimistic/local update to hide the 'Đánh giá' button immediately
                                setState(() {
                                  // mark review as locally present to update UI quickly
                                  _bookings = List<dynamic>.from(_bookings);
                                  final globalIndex = _bookings.indexWhere((b) => b['id'] == booking['id']);
                                    if (globalIndex != -1) {
                                      // set optimistic review marker (UI reads `review` first)
                                      _bookings[globalIndex]['review'] = {'local': true};
                                    }
                                });

                                // Refresh from server in background to get real review data
                                await _loadBookings();
                              }
                            },
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFF00B894),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                            ),
                            child: const Text('Đánh giá', style: TextStyle(color: Colors.white)),
                          ),
                        ),
                      ],
                    ),
                  ]
                ],
              ],
            ),
          ),
        );
      }
    }
