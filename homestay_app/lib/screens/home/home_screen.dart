import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import '../../config/api_config.dart';
import '../../providers/favorites_provider.dart';
import '../../services/storage_service.dart';
import 'homestay_detail_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<dynamic> _homestays = [];
  List<dynamic> _filteredHomestays = [];
  bool _isLoading = true;
  int _currentImageIndex = 0;
  Timer? _slideTimer;
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

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
    _searchController.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _slideTimer?.cancel();
    _searchController.removeListener(_onSearchChanged);
    _searchController.dispose();
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
        final responseData = jsonDecode(response.body);
  final items = responseData['data']?['items'] ?? responseData['items'] ?? responseData;

        // Normalize images for each homestay
        final List<dynamic> normalized = (items as List).map((h) {
          if (h is Map) {
            final m = Map<String, dynamic>.from(h);
            final imagesRaw = m['images'];
            final List<String> images = [];
            if (imagesRaw is String) images.add(imagesRaw);
            else if (imagesRaw is List) {
              for (var it in imagesRaw) {
                  if (it is String) images.add(it);
                else if (it is Map) {
                  images.add(it['imageUrl']?.toString() ?? it['url']?.toString() ?? it['path']?.toString() ?? '');
                } else if (it != null) images.add(it.toString());
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
            _filteredHomestays = List<dynamic>.from(normalized);
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        title: const Text('Homestay', style: TextStyle(color: Colors.white)),
        backgroundColor: Colors.transparent,
        elevation: 0,
        automaticallyImplyLeading: false,
      ),
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFFFFE082), Color(0xFFF48FB1)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: RefreshIndicator(
          onRefresh: _loadHomestays,
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SafeArea(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 18.0),
                    child: Row(
                      children: [
                        ClipRRect(
                          borderRadius: BorderRadius.circular(12),
                          child: Image.network(
                            '${ApiConfig.baseUrl}/images/admin/logo.png',
                            height: 56,
                            width: 56,
                            fit: BoxFit.cover,
                            errorBuilder: (context, error, stackTrace) {
                              return Container(
                                height: 56,
                                width: 56,
                                color: Colors.white24,
                                child: const Icon(Icons.home, size: 32, color: Colors.white),
                              );
                            },
                          ),
                        ),
                        const SizedBox(width: 12),
                        const Expanded(
                          child: Text(
                            'Khám Phá Homestay',
                            style: TextStyle(
                              fontSize: 22,
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),

                // Slideshow
                Container(
                  height: 200,
                  width: double.infinity,
                  margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(16),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.12),
                        blurRadius: 14,
                        offset: const Offset(0, 8),
                      ),
                    ],
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(16),
                    child: Image.network(
                      '${ApiConfig.baseUrl}/images/${_slideImages[_currentImageIndex]}',
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) {
                        return Container(
                          color: Colors.white24,
                          child: const Center(
                            child: Icon(Icons.image_not_supported, size: 50, color: Colors.white),
                          ),
                        );
                      },
                    ),
                  ),
                ),

                const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: Text(
                    'Danh Sách Homestay',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                ),

                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: TextField(
                    controller: _searchController,
                    decoration: InputDecoration(
                      hintText: 'Tìm theo tên hoặc địa chỉ...',
                      prefixIcon: const Icon(Icons.search, color: Color(0xFF9C27B0)),
                      suffixIcon: _searchQuery.isNotEmpty
                          ? IconButton(
                              icon: const Icon(Icons.clear),
                              onPressed: () {
                                _searchController.clear();
                              },
                            )
                          : null,
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide.none),
                      filled: true,
                      fillColor: Colors.white.withOpacity(0.95),
                      contentPadding: const EdgeInsets.symmetric(vertical: 14, horizontal: 12),
                    ),
                  ),
                ),

                // List area
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
                            itemCount: _filteredHomestays.length,
                            itemBuilder: (context, index) {
                              final homestay = _filteredHomestays[index];
                              return _buildHomestayCard(homestay);
                            },
                          ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  void _onSearchChanged() {
    final q = _searchController.text.trim().toLowerCase();
    if (q == _searchQuery) return;
    setState(() {
      _searchQuery = q;
      if (_searchQuery.isEmpty) {
        _filteredHomestays = List<dynamic>.from(_homestays);
      } else {
        _filteredHomestays = _homestays.where((h) {
          try {
            final name = (h['name'] ?? '').toString().toLowerCase();
            final addr = (h['address'] ?? '').toString().toLowerCase();
            return name.contains(_searchQuery) || addr.contains(_searchQuery);
          } catch (_) {
            return false;
          }
        }).toList();
      }
    });
  }

  Widget _buildHomestayCard(dynamic homestay) {
  String imageUrl = '${ApiConfig.baseUrl}/images/Tudong/1.jpg';
    try {
      if (homestay['images'] != null && (homestay['images'] is List && (homestay['images'] as List).isNotEmpty)) {
        final firstImage = (homestay['images'] as List).first;
        if (firstImage is String) {
          // if it's already a path or full url
          if (firstImage.startsWith('http')) imageUrl = firstImage;
          else if (firstImage.startsWith('/')) imageUrl = '${ApiConfig.baseUrl}$firstImage';
          else imageUrl = '${ApiConfig.baseUrl}/$firstImage';
        } else if (firstImage is Map) {
          final imagePath = firstImage['imageUrl'] ?? firstImage['url'] ?? firstImage['path'] ?? '';
          if (imagePath.toString().startsWith('http')) imageUrl = imagePath.toString();
          else if (imagePath.toString().startsWith('/')) imageUrl = '${ApiConfig.baseUrl}${imagePath.toString()}';
          else imageUrl = '${ApiConfig.baseUrl}/${imagePath.toString()}';
        } else {
          final s = firstImage?.toString() ?? '';
          if (s.startsWith('http')) imageUrl = s;
          else if (s.startsWith('/')) imageUrl = '${ApiConfig.baseUrl}$s';
          else if (s.isNotEmpty) imageUrl = '${ApiConfig.baseUrl}/$s';
        }
      }
    } catch (_) {
      // fallback to default imageUrl
      imageUrl = '${ApiConfig.baseUrl}/images/Tudong/1.jpg';
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(15)),
      elevation: 6,
      shadowColor: const Color(0xFFF48FB1).withOpacity(0.25),
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
              child: Stack(
                children: [
                  Image.network(
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
                  Positioned(
                    top: 8,
                    right: 8,
                    child: Consumer<FavoritesProvider>(
                      builder: (context, favProv, child) {
                        final isFav = favProv.isFavorite(homestay['id']);
                        return Material(
                          color: Colors.white.withOpacity(0.9),
                          shape: const CircleBorder(),
                          elevation: 2,
                          child: IconButton(
                            icon: Icon(isFav ? Icons.favorite : Icons.favorite_border, color: Colors.red),
                            onPressed: () async {
                              await favProv.toggleFavorite(homestay['id']);
                              if (mounted) {
                                ScaffoldMessenger.of(context).showSnackBar(
                                  SnackBar(content: Text(isFav ? 'Đã xóa khỏi yêu thích' : 'Đã thêm vào yêu thích')),
                                );
                              }
                            },
                          ),
                        );
                      },
                    ),
                  ),
                ],
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
                            (homestay['averageRating'] is num)
                                ? (homestay['averageRating'] as num).toStringAsFixed(1)
                                : (homestay['averageRating']?.toString() ?? '0.0'),
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
                          color: Color(0xFFF48FB1),
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
