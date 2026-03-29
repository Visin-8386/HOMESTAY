import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:http/http.dart' as http;
import 'package:url_launcher/url_launcher.dart';

import '../../config/api_config.dart';
import '../../services/storage_service.dart';
import '../../widgets/user_gradient_background.dart';
import '../homestay/homestay_detail_screen.dart' as provider_detail;

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
  GoogleMapController? _mapController;
  LatLng? _homestayLatLng;
  Set<Marker> _mapMarkers = {};

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
        final parsed = jsonDecode(response.body);
        Map<String, dynamic>? homestayData;
        if (parsed is Map) {
          if (parsed.containsKey('data') && parsed['data'] is Map) {
            homestayData = Map<String, dynamic>.from(parsed['data']);
          } else if (parsed.containsKey('homestay') && parsed['homestay'] is Map) {
            homestayData = Map<String, dynamic>.from(parsed['homestay']);
          } else if (parsed.containsKey('data') && parsed['data'] is List) {
            // sometimes backend returns data: [ ... ]
            homestayData = (parsed['data'] as List).isNotEmpty ? Map<String, dynamic>.from((parsed['data'] as List)[0]) : null;
          } else {
            homestayData = Map<String, dynamic>.from(parsed);
          }
        }

        if (mounted) {
          setState(() {
            _homestay = homestayData;
            // extract coordinates if available
            _homestayLatLng = _extractLatLng(homestayData);
            if (_homestayLatLng != null) {
              _mapMarkers = {
                Marker(
                  markerId: const MarkerId('homestay_location'),
                  position: _homestayLatLng!,
                  infoWindow: InfoWindow(title: homestayData?['name'] ?? 'Vị trí'),
                )
              };
            }
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
    // Open the full homestay detail page (provider-backed) which contains
    // the booking flow and reviews.
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => provider_detail.HomestayDetailScreen(homestayId: widget.homestayId),
      ),
    );
  }

  LatLng? _extractLatLng(Map<String, dynamic>? data) {
    if (data == null) return null;

    double? lat;
    double? lng;

    // common shapes
    if (data.containsKey('latitude') && data.containsKey('longitude')) {
      lat = _toDouble(data['latitude']);
      lng = _toDouble(data['longitude']);
    } else if (data.containsKey('lat') && data.containsKey('lng')) {
      lat = _toDouble(data['lat']);
      lng = _toDouble(data['lng']);
    } else if (data.containsKey('location') && data['location'] is Map) {
      final loc = data['location'] as Map;
      lat = _toDouble(loc['lat'] ?? loc['latitude']);
      lng = _toDouble(loc['lng'] ?? loc['longitude']);
    }

    if (lat != null && lng != null) return LatLng(lat, lng);
    return null;
  }

  double? _toDouble(dynamic v) {
    if (v == null) return null;
    if (v is double) return v;
    if (v is int) return v.toDouble();
    if (v is String) return double.tryParse(v);
    return null;
  }

  Future<void> _openInExternalMaps() async {
    if (_homestayLatLng == null) return;
    final lat = _homestayLatLng!.latitude;
    final lng = _homestayLatLng!.longitude;
    final uri = Uri.parse('https://www.google.com/maps/search/?api=1&query=$lat,$lng');
    try {
      final launched = await launchUrl(uri, mode: LaunchMode.externalApplication);
      if (!launched) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không thể mở bản đồ')));
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi khi mở bản đồ: $e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: const Text('Chi tiết Homestay'),
          backgroundColor: const Color(0xFF667eea),
        ),
        body: UserGradientBackground(child: const Center(child: CircularProgressIndicator())),
      );
    }

    if (_homestay == null) {
      return Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(title: const Text('Chi tiết Homestay')),
        body: UserGradientBackground(child: const Center(child: Text('Không tìm thấy thông tin homestay'))),
      );
    }

    // Normalize images: backend may return list of strings or list of objects with imageUrl
  final rawImages = _homestay!['images'];
    final List<String> images = [];
    if (rawImages is List) {
      for (var it in rawImages) {
        if (it is String) {
          images.add(it);
        } else if (it is Map && it.containsKey('imageUrl')) {
          images.add(it['imageUrl']?.toString() ?? '');
        } else if (it is Map && it.containsKey('url')) {
          images.add(it['url']?.toString() ?? '');
        }
      }
    }

  final amenities = (_homestay!['amenities'] as List?)?.cast<Map<String, dynamic>>() ?? [];

    return Scaffold(
      backgroundColor: Colors.transparent,
      body: UserGradientBackground(
        child: CustomScrollView(
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
                          final imagePath = images[index];
                          final uriStr = imagePath.startsWith('http') ? imagePath : '${ApiConfig.baseUrl}$imagePath';
                          return Image.network(
                            uriStr,
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

                        // Map preview
                        if (_homestayLatLng != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 8.0),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Container(
                                  height: 200,
                                  decoration: BoxDecoration(
                                    borderRadius: BorderRadius.circular(12),
                                    color: Colors.grey[200],
                                  ),
                                  clipBehavior: Clip.hardEdge,
                                  child: GoogleMap(
                                    initialCameraPosition: CameraPosition(
                                      target: _homestayLatLng!,
                                      zoom: 15,
                                    ),
                                    markers: _mapMarkers,
                                    onMapCreated: (controller) {
                                      _mapController = controller;
                                    },
                                    zoomControlsEnabled: false,
                                    myLocationButtonEnabled: false,
                                    liteModeEnabled: false,
                                  ),
                                ),
                                const SizedBox(height: 8),
                                Row(
                                  children: [
                                    ElevatedButton.icon(
                                      onPressed: _openInExternalMaps,
                                      icon: const Icon(Icons.map),
                                      label: const Text('Mở bản đồ'),
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: const Color(0xFF667eea),
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          )
                        else
                          Padding(
                            padding: const EdgeInsets.only(top: 12.0),
                            child: Text(
                              'Vị trí chưa được cung cấp',
                              style: TextStyle(color: Colors.grey[600]),
                            ),
                          ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            const Icon(Icons.star, color: Colors.amber, size: 20),
                            const SizedBox(width: 4),
                            Text(
                              (_homestay!['averageRating'] is num)
                                  ? (_homestay!['averageRating'] as num).toStringAsFixed(1)
                                  : (_homestay!['averageRating']?.toString() ?? '0.0'),
                              style: const TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Text(
                              '(${_homestay!['reviewCount']?.toString() ?? '0'} đánh giá)',
                              style: TextStyle(color: Colors.grey[600]),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        Text(
                          '${_formatMoney(_homestay!['pricePerNight'])} VNĐ/đêm',
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
                                  color: const Color(0xFF667eea).withAlpha((0.1 * 255).round()),
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
      ),

      // Nút đặt phòng
      bottomNavigationBar: Container(
        // Respect system padding (navigation bar) and keyboard insets
        padding: EdgeInsets.only(
          left: 16,
          right: 16,
          top: 16,
          bottom: 16 + MediaQuery.of(context).viewPadding.bottom + MediaQuery.of(context).viewInsets.bottom,
        ),
        decoration: BoxDecoration(
          color: Colors.white,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withAlpha((0.1 * 255).round()),
              blurRadius: 10,
              offset: const Offset(0, -5),
            ),
          ],
        ),
        child: SafeArea(
          top: false,
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

  @override
  void dispose() {
    _mapController?.dispose();
    super.dispose();
  }
}
