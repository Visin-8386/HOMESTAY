import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../services/storage_service.dart';
import '../auth/login_screen.dart';
import 'homestay_detail_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<dynamic> _homestays = [];
  bool _isLoading = true;
  int _currentImageIndex = 0;
  Timer? _slideTimer;

  String _formatMoney(dynamic value) {
    if (value == null) return '0';
    try {
      if (value is String) return value;
      if (value is int) return value.toString();
      if (value is double) return value.toStringAsFixed(0);
      if (value is num) return value.toString();
      return value.toString();
    } catch (e) {
      return value.toString();
    }
  }

  final List<String> _slideImages = [
    'Tudong/1.jpg',
    'Tudong/2.jpg',
    'Tudong/3.jpg',
    'Tudong/4.jpg',
    'Tudong/5.jpg',
    'Tudong/6.jpg',
    'Tudong/7.jpg',
  ];

  @override
  void initState() {
    super.initState();
    _loadHomestays();
    _startSlideshow();
  }

  @override
  void dispose() {
    _slideTimer?.cancel();
    super.dispose();
  }

  void _startSlideshow() {
    _slideTimer = Timer.periodic(const Duration(seconds: 3), (timer) {
      if (mounted) {
        setState(() {
          _currentImageIndex = (_currentImageIndex + 1) % _slideImages.length;
        });
      }
    });
  }

  Future<void> _loadHomestays() async {
    if (mounted) setState(() => _isLoading = true);
    
    try {
      final token = await StorageService().getToken();
      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/homestays'),
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
  final items = data['data'] ?? data['items'] ?? data;

        // normalize images
        final normalized = (items as List).map((h) {
          if (h is Map) {
            final m = Map<String, dynamic>.from(h);
            final imagesRaw = m['images'];
            final List<String> images = [];
            if (imagesRaw is String) images.add(imagesRaw);
            else if (imagesRaw is List) {
              for (var it in imagesRaw) {
                if (it is String) images.add(it);
                else if (it is Map) images.add(it['imageUrl']?.toString() ?? it['url']?.toString() ?? it['path']?.toString() ?? '');
                else if (it != null) images.add(it.toString());
              }
            }
            m['images'] = images;
            return m;
          }
          return h;
        }).toList();

        if (mounted) {
          setState(() {
            _homestays = normalized;
            _isLoading = false;
          });
        }
      } else {
        if (mounted) {
          setState(() => _isLoading = false);
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Không thể tải danh sách homestay')),
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

  Future<void> _logout() async {
    await StorageService().clearAll();
    if (mounted) {
      Navigator.of(context).pushAndRemoveUntil(
        MaterialPageRoute(builder: (_) => const LoginScreen()),
        (route) => false,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[100],
      appBar: AppBar(
        title: const Text('Homestay', style: TextStyle(color: Colors.white)),
        backgroundColor: const Color(0xFF667eea),
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.logout, color: Colors.white),
            onPressed: _logout,
            tooltip: 'Đăng xuất',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadHomestays,
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header với logo
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(20),
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    colors: [Color(0xFF667eea), Color(0xFF764ba2)],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                ),
                child: Column(
                  children: [
                    Image.network(
                      '${ApiConfig.baseUrl}/images/admin/logo.png',
                      height: 80,
                      errorBuilder: (context, error, stackTrace) {
                        return const Icon(Icons.home, size: 80, color: Colors.white);
                      },
                    ),
                    const SizedBox(height: 10),
                    const Text(
                      'Khám Phá Homestay',
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),

              // Slideshow Tudong
              Container(
                height: 200,
                width: double.infinity,
                margin: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(15),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.1),
                      blurRadius: 10,
                      offset: const Offset(0, 5),
                    ),
                  ],
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(15),
                  child: Image.network(
                    '${ApiConfig.baseUrl}/images/${_slideImages[_currentImageIndex]}',
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) {
                      return Container(
                        color: Colors.grey[300],
                        child: const Center(
                          child: Icon(Icons.image_not_supported, size: 50),
                        ),
                      );
                    },
                  ),
                ),
              ),

              // Tiêu đề danh sách
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                child: Text(
                  'Danh Sách Homestay',
                  style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF667eea),
                  ),
                ),
              ),

              // Danh sách homestay
              _isLoading
                  ? const Center(
                      child: Padding(
                        padding: EdgeInsets.all(50),
                        child: CircularProgressIndicator(),
                      ),
                    )
                  : _homestays.isEmpty
                      ? const Center(
                          child: Padding(
                            padding: EdgeInsets.all(50),
                            child: Text(
                              'Không có homestay nào',
                              style: TextStyle(fontSize: 16, color: Colors.grey),
                            ),
                          ),
                        )
                      : ListView.builder(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          itemCount: _homestays.length,
                          itemBuilder: (context, index) {
                            final homestay = _homestays[index];
                            return _buildHomestayCard(homestay);
                          },
                        ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHomestayCard(dynamic homestay) {
    String imageUrl = '${ApiConfig.baseUrl}/images/Tudong/1.jpg';
    try {
      if (homestay['images'] != null && (homestay['images'] as List).isNotEmpty) {
  final first = (homestay['images'] as List).first;
        if (first is String) {
          if (first.startsWith('http')) imageUrl = first;
          else if (first.startsWith('/')) imageUrl = '${ApiConfig.baseUrl}$first';
          else imageUrl = '${ApiConfig.baseUrl}/$first';
        } else if (first is Map) {
          final path = first['imageUrl'] ?? first['url'] ?? first['path'] ?? '';
          if (path.toString().startsWith('http')) imageUrl = path.toString();
          else if (path.toString().startsWith('/')) imageUrl = '${ApiConfig.baseUrl}${path.toString()}';
          else imageUrl = '${ApiConfig.baseUrl}/${path.toString()}';
        }
      }
    } catch (_) {
      imageUrl = '${ApiConfig.baseUrl}/images/Tudong/1.jpg';
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(15)),
      elevation: 3,
      child: InkWell(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => HomestayDetailScreen(homestayId: homestay['id']),
            ),
          );
        },
        borderRadius: BorderRadius.circular(15),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Hình ảnh
            ClipRRect(
              borderRadius: const BorderRadius.vertical(top: Radius.circular(15)),
              child: Image.network(
                imageUrl,
                height: 180,
                width: double.infinity,
                fit: BoxFit.cover,
                errorBuilder: (context, error, stackTrace) {
                  return Container(
                    height: 180,
                    color: Colors.grey[300],
                    child: const Icon(Icons.image_not_supported, size: 50),
                  );
                },
              ),
            ),

            // Thông tin
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    homestay['name'] ?? 'Homestay',
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Icon(Icons.location_on, size: 16, color: Colors.grey),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          homestay['address'] ?? 'Địa chỉ chưa cập nhật',
                          style: TextStyle(color: Colors.grey[600], fontSize: 14),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.star, color: Colors.amber, size: 18),
                          const SizedBox(width: 4),
                          Text(
                            (homestay['rating'] is num)
                                ? (homestay['rating'] as num).toStringAsFixed(1)
                                : (homestay['rating']?.toString() ?? '0.0'),
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                      Text(
                        '${_formatMoney(homestay['pricePerNight'])} VNĐ/đêm',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF667eea),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
