import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../services/storage_service.dart';

class HomestayDetailScreen extends StatefulWidget {
  final int homestayId;

  const HomestayDetailScreen({super.key, required this.homestayId});

  @override
  State<HomestayDetailScreen> createState() => _HomestayDetailScreenState();
}

class _HomestayDetailScreenState extends State<HomestayDetailScreen> {
  Map<String, dynamic>? _homestay;
  bool _isLoading = true;
  int _currentImageIndex = 0;
  bool _isFavorite = false;

  @override
  void initState() {
    super.initState();
    _loadHomestayDetail();
  }

  Future<void> _loadHomestayDetail() async {
    setState(() => _isLoading = true);

    try {
      final token = await StorageService().getToken();
      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/homestays/${widget.homestayId}'),
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (mounted) {
          setState(() {
            _homestay = data['data'];
            _isLoading = false;
          });
        }
      } else {
        if (mounted) {
          setState(() => _isLoading = false);
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Không thể tải thông tin homestay')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isLoading = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: $e')),
        );
      }
    }
  }

  Future<void> _toggleFavorite() async {
    setState(() => _isFavorite = !_isFavorite);
    
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(_isFavorite ? 'Đã thêm vào yêu thích' : 'Đã xóa khỏi yêu thích'),
        duration: const Duration(seconds: 1),
      ),
    );
  }

  void _bookNow() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Chức năng đặt phòng sẽ được bổ sung sau'),
        duration: Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Chi tiết Homestay'),
          backgroundColor: const Color(0xFF667eea),
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (_homestay == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Chi tiết Homestay')),
        body: const Center(child: Text('Không tìm thấy thông tin homestay')),
      );
    }

    final images = (_homestay!['images'] as List?)?.cast<Map<String, dynamic>>() ?? [];
    final amenities = (_homestay!['amenities'] as List?)?.cast<Map<String, dynamic>>() ?? [];

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          // App Bar với hình ảnh
          SliverAppBar(
            expandedHeight: 300,
            pinned: true,
            backgroundColor: const Color(0xFF667eea),
            flexibleSpace: FlexibleSpaceBar(
              background: images.isNotEmpty
                  ? PageView.builder(
                      itemCount: images.length,
                      onPageChanged: (index) {
                        setState(() => _currentImageIndex = index);
                      },
                      itemBuilder: (context, index) {
                        final imagePath = images[index]['imageUrl'] ?? '';
                        return Image.network(
                          '${ApiConfig.baseUrl}$imagePath',
                          fit: BoxFit.cover,
                          errorBuilder: (context, error, stackTrace) {
                            return Container(
                              color: Colors.grey[300],
                              child: const Icon(Icons.image_not_supported, size: 80),
                            );
                          },
                        );
                      },
                    )
                  : Container(
                      color: Colors.grey[300],
                      child: const Icon(Icons.home, size: 80),
                    ),
            ),
            actions: [
              IconButton(
                icon: Icon(_isFavorite ? Icons.favorite : Icons.favorite_border),
                onPressed: _toggleFavorite,
                color: Colors.white,
              ),
            ],
          ),

          // Nội dung chi tiết
          SliverToBoxAdapter(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Indicator cho ảnh
                if (images.length > 1)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 10),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: List.generate(
                        images.length,
                        (index) => Container(
                          margin: const EdgeInsets.symmetric(horizontal: 4),
                          width: 8,
                          height: 8,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: _currentImageIndex == index
                                ? const Color(0xFF667eea)
                                : Colors.grey[300],
                          ),
                        ),
                      ),
                    ),
                  ),

                // Thông tin cơ bản
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _homestay!['name'] ?? 'Homestay',
                        style: const TextStyle(
                          fontSize: 24,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          const Icon(Icons.location_on, size: 20, color: Colors.grey),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              _homestay!['address'] ?? 'Địa chỉ chưa cập nhật',
                              style: TextStyle(color: Colors.grey[600], fontSize: 16),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          const Icon(Icons.star, color: Colors.amber, size: 20),
                          const SizedBox(width: 4),
                          Text(
                            '${_homestay!['averageRating'] ?? 0.0}',
                            style: const TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            '(${_homestay!['reviewCount'] ?? 0} đánh giá)',
                            style: TextStyle(color: Colors.grey[600]),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      Text(
                        '${_homestay!['pricePerNight'] ?? 0} VNĐ/đêm',
                        style: const TextStyle(
                          fontSize: 28,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF667eea),
                        ),
                      ),
                    ],
                  ),
                ),

                const Divider(thickness: 8, color: Color(0xFFF5F5F5)),

                // Thông tin phòng
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Thông tin phòng',
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceAround,
                        children: [
                          _buildInfoCard(
                            Icons.people,
                            '${_homestay!['maxGuests'] ?? 0}',
                            'Khách',
                          ),
                          _buildInfoCard(
                            Icons.bed,
                            '${_homestay!['bedrooms'] ?? 0}',
                            'Phòng ngủ',
                          ),
                          _buildInfoCard(
                            Icons.bathroom,
                            '${_homestay!['bathrooms'] ?? 0}',
                            'Phòng tắm',
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                const Divider(thickness: 8, color: Color(0xFFF5F5F5)),

                // Mô tả
                if (_homestay!['description'] != null)
                  Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Mô tả',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          _homestay!['description'],
                          style: TextStyle(
                            fontSize: 16,
                            color: Colors.grey[700],
                            height: 1.5,
                          ),
                        ),
                      ],
                    ),
                  ),

                const Divider(thickness: 8, color: Color(0xFFF5F5F5)),

                // Tiện nghi
                if (amenities.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Tiện nghi',
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 12),
                        Wrap(
                          spacing: 12,
                          runSpacing: 12,
                          children: amenities.map((amenity) {
                            return Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 16,
                                vertical: 8,
                              ),
                              decoration: BoxDecoration(
                                color: const Color(0xFF667eea).withOpacity(0.1),
                                borderRadius: BorderRadius.circular(20),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(
                                    Icons.check_circle,
                                    size: 18,
                                    color: const Color(0xFF667eea),
                                  ),
                                  const SizedBox(width: 6),
                                  Text(
                                    amenity['name'] ?? '',
                                    style: const TextStyle(fontSize: 14),
                                  ),
                                ],
                              ),
                            );
                          }).toList(),
                        ),
                      ],
                    ),
                  ),

                const Divider(thickness: 8, color: Color(0xFFF5F5F5)),

                // Chủ nhà
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Chủ nhà',
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          CircleAvatar(
                            radius: 30,
                            backgroundColor: const Color(0xFF667eea),
                            child: Text(
                              (_homestay!['hostName'] ?? 'H')[0].toUpperCase(),
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 24,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _homestay!['hostName'] ?? 'Chủ nhà',
                                  style: const TextStyle(
                                    fontSize: 18,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                Text(
                                  'Chủ nhà',
                                  style: TextStyle(
                                    color: Colors.grey[600],
                                    fontSize: 14,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                const SizedBox(height: 80), // Space for button
              ],
            ),
          ),
        ],
      ),

      // Nút đặt phòng
      bottomNavigationBar: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.1),
              blurRadius: 10,
              offset: const Offset(0, -5),
            ),
          ],
        ),
        child: ElevatedButton(
          onPressed: _bookNow,
          style: ElevatedButton.styleFrom(
            backgroundColor: const Color(0xFF667eea),
            padding: const EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            elevation: 0,
          ),
          child: const Text(
            'Đặt phòng ngay',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildInfoCard(IconData icon, String value, String label) {
    return Column(
      children: [
        Icon(icon, size: 32, color: const Color(0xFF667eea)),
        const SizedBox(height: 8),
        Text(
          value,
          style: const TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.bold,
          ),
        ),
        Text(
          label,
          style: TextStyle(
            color: Colors.grey[600],
            fontSize: 14,
          ),
        ),
      ],
    );
  }
}
