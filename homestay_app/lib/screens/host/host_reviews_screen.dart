import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';

class HostReviewsScreen extends StatefulWidget {
  const HostReviewsScreen({Key? key}) : super(key: key);

  @override
  _HostReviewsScreenState createState() => _HostReviewsScreenState();
}

class _HostReviewsScreenState extends State<HostReviewsScreen> {
  final _storage = const FlutterSecureStorage();
  bool _isLoading = true;
  List<dynamic> _reviews = [];
  Map<String, dynamic> _stats = {};

  @override
  void initState() {
    super.initState();
    _loadReviews();
  }

  Future<void> _loadReviews() async {
    setState(() => _isLoading = true);

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Vui lòng đăng nhập lại')),
        );
        return;
      }

      // Load reviews for host's homestays
      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/host/reviews'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final parsed = jsonDecode(response.body);
        // Unwrap possible envelope: { data: {...} } or { success:..., data: {...} }
        dynamic data = parsed;
        if (parsed is Map && parsed.containsKey('data')) data = parsed['data'];

        // Extract reviews list from known shapes
        if (data is List) {
          _reviews = data.map((e) => e is Map ? Map<String, dynamic>.from(e) : {}).toList();
        } else if (data is Map) {
          if (data.containsKey('reviews') && data['reviews'] is List) {
            _reviews = (data['reviews'] as List).map((e) => e is Map ? Map<String, dynamic>.from(e) : {}).toList();
          } else if (data.containsKey('items') && data['items'] is List) {
            _reviews = (data['items'] as List).map((e) => e is Map ? Map<String, dynamic>.from(e) : {}).toList();
          } else if (parsed is Map && parsed.containsKey('reviews') && parsed['reviews'] is List) {
            _reviews = (parsed['reviews'] as List).map((e) => e is Map ? Map<String, dynamic>.from(e) : {}).toList();
          } else {
            _reviews = [];
          }

          // stats may be under data.stats or parsed.stats
          _stats = data['stats'] is Map ? Map<String, dynamic>.from(data['stats']) : (parsed['stats'] is Map ? Map<String, dynamic>.from(parsed['stats']) : {});
        } else {
          _reviews = [];
          _stats = {};
        }
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải đánh giá: ${response.statusCode}')),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi kết nối: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Đánh giá homestay'),
        backgroundColor: Colors.blue,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadReviews,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadReviews,
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _buildReviewStats(),
                    const SizedBox(height: 24),
                    _buildReviewsList(),
                  ],
                ),
              ),
            ),
    );
  }

  Widget _buildReviewStats() {
    return Card(
      elevation: 4,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Thống kê đánh giá',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _buildStatItem(
                    'Tổng đánh giá',
                    _stats['totalReviews']?.toString() ?? '0',
                    Icons.reviews,
                    Colors.blue,
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: _buildStatItem(
                    'Đánh giá trung bình',
                    '${_stats['averageRating']?.toStringAsFixed(1) ?? '0.0'} ⭐',
                    Icons.star,
                    Colors.amber,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _buildStatItem(
                    '5 sao',
                    '${_stats['fiveStarCount']?.toString() ?? '0'}',
                    Icons.star,
                    Colors.green,
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: _buildStatItem(
                    '1-4 sao',
                    '${_stats['otherStarsCount']?.toString() ?? '0'}',
                    Icons.star_half,
                    Colors.orange,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatItem(String title, String value, IconData icon, Color color) {
    return Column(
      children: [
        Icon(icon, size: 24, color: color),
        const SizedBox(height: 4),
        Text(
          value,
          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        Text(
          title,
          style: const TextStyle(color: Colors.grey, fontSize: 12),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }

  Widget _buildReviewsList() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Tất cả đánh giá',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        _reviews.isEmpty
            ? const Center(
                child: Text(
                  'Chưa có đánh giá nào',
                  style: TextStyle(color: Colors.grey),
                ),
              )
            : ListView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _reviews.length,
                itemBuilder: (context, index) {
                  final review = _reviews[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 16),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Header with homestay name and rating
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Expanded(
                                child: Text(
                                  review['homestayName'] ?? 'N/A',
                                  style: const TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                              Row(
                                children: List.generate(5, (starIndex) {
                                  return Icon(
                                    starIndex < (review['rating'] ?? 0)
                                        ? Icons.star
                                        : Icons.star_border,
                                    color: Colors.amber,
                                    size: 16,
                                  );
                                }),
                              ),
                            ],
                          ),
                          const SizedBox(height: 8),

                          // Guest name and date
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                'Khách: ${review['guestName'] ?? 'N/A'}',
                                style: const TextStyle(color: Colors.grey),
                              ),
                              Text(
                                review['createdAt'] != null
                                    ? _formatDate(review['createdAt'])
                                    : '',
                                style: const TextStyle(color: Colors.grey, fontSize: 12),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),

                          // Review comment
                          if (review['comment'] != null && review['comment'].toString().isNotEmpty)
                            Text(
                              review['comment'],
                              style: const TextStyle(fontSize: 14),
                            ),

                          const SizedBox(height: 12),

                          // Reply section
                          if (review['hostReply'] != null && review['hostReply'].toString().isNotEmpty)
                            Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: Colors.blue[50],
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Phản hồi của bạn:',
                                    style: TextStyle(
                                      fontWeight: FontWeight.bold,
                                      color: Colors.blue,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(review['hostReply']),
                                ],
                              ),
                            )
                          else
                            Align(
                              alignment: Alignment.centerRight,
                              child: TextButton.icon(
                                onPressed: () => _showReplyDialog(review),
                                icon: const Icon(Icons.reply),
                                label: const Text('Trả lời'),
                              ),
                            ),
                        ],
                      ),
                    ),
                  );
                },
              ),
      ],
    );
  }

  void _showReplyDialog(dynamic review) {
    final TextEditingController replyController = TextEditingController();

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Trả lời đánh giá'),
        content: TextField(
          controller: replyController,
          maxLines: 3,
          decoration: const InputDecoration(
            hintText: 'Nhập phản hồi của bạn...',
            border: OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Hủy'),
          ),
          TextButton(
            onPressed: () async {
              await _submitReply(review['id'], replyController.text);
              Navigator.of(context).pop();
            },
            child: const Text('Gửi'),
          ),
        ],
      ),
    );
  }

  Future<void> _submitReply(int reviewId, String reply) async {
    if (reply.trim().isEmpty) return;

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) return;

      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/reviews/$reviewId/response'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({'response': reply.trim()}),
      );

      if (response.statusCode == 200) {
        await _loadReviews(); // Reload reviews
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã gửi phản hồi thành công')),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Lỗi gửi phản hồi')),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi kết nối: $e')),
      );
    }
  }

  String _formatDate(dynamic dateString) {
    try {
      if (dateString == null) return '';
      // handle timestamp in milliseconds or seconds
      final s = dateString.toString();
      if (RegExp(r'^\d+\$').hasMatch(s)) {
        final n = int.tryParse(s) ?? 0;
        // Heuristic: if length > 12 treat as ms, else seconds
        final dt = s.length > 12 ? DateTime.fromMillisecondsSinceEpoch(n) : DateTime.fromMillisecondsSinceEpoch(n * 1000);
        return '${dt.day}/${dt.month}/${dt.year}';
      }
      final date = DateTime.parse(s);
      return '${date.day}/${date.month}/${date.year}';
    } catch (e) {
      return dateString.toString();
    }
  }
}