import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../models/homestay.dart';
import 'create_homestay_screen.dart';

class ManageHomestaysScreen extends StatefulWidget {
  const ManageHomestaysScreen({Key? key}) : super(key: key);

  @override
  _ManageHomestaysScreenState createState() => _ManageHomestaysScreenState();
}

class _ManageHomestaysScreenState extends State<ManageHomestaysScreen> {
  final _storage = const FlutterSecureStorage();
  bool _isLoading = true;
  List<dynamic> _homestays = [];

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
    _loadHomestays();
  }

  Future<void> _loadHomestays() async {
    if (mounted) setState(() => _isLoading = true);

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Vui lòng đăng nhập lại')),
        );
        return;
      }

      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/homestays/my-homestays'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);

        // Normalize list and images
        final List<dynamic> items = (data is Map && data.containsKey('data')) ? (data['data'] as List? ?? []) : (data as List? ?? []);
        final normalized = items.map((h) {
          if (h is Map) {
            final m = Map<String, dynamic>.from(h);
            final imagesRaw = m['images'];
            final List<String> images = [];
            if (imagesRaw is String) images.add(imagesRaw);
            else if (imagesRaw is List) {
              for (var it in imagesRaw) {
                if (it is String) images.add(it);
                else if (it is Map) images.add(it['imageUrl']?.toString() ?? it['url']?.toString() ?? '');
                else if (it != null) images.add(it.toString());
              }
            }
            m['images'] = images;
            return m;
          }
          return h;
        }).toList();

        if (mounted) setState(() => _homestays = normalized);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải danh sách homestay: ${response.statusCode}')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi kết nối: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _toggleHomestayStatus(int homestayId, bool currentStatus) async {
    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) return;

      final response = await http.patch(
        Uri.parse('${ApiConfig.baseUrl}/api/homestays/$homestayId/status'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({'isActive': !currentStatus}),
      );

      if (response.statusCode == 200) {
        await _loadHomestays(); // Reload list
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Cập nhật trạng thái thành công')),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Lỗi cập nhật trạng thái')),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi kết nối: $e')),
      );
    }
  }

  Future<void> _deleteHomestay(int homestayId) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xác nhận xóa'),
        content: const Text('Bạn có chắc muốn xóa homestay này?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Hủy'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Xóa', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) return;

      final response = await http.delete(
        Uri.parse('${ApiConfig.baseUrl}/api/homestays/$homestayId'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      // Accept 200 or 204 or structured success response
      if (response.statusCode == 204 || response.statusCode == 200) {
        // Try to parse response body for debug
        try {
          final body = response.body;
          debugPrint('Delete homestay response body: $body');
        } catch (_) {}

        // Backend performs soft-delete (sets isActive=false). To give the user
        // immediate feedback we remove the item locally instead of reloading
        // which may still return the soft-deleted item.
        if (mounted) {
          setState(() {
            _homestays.removeWhere((h) => h['id'] == homestayId);
          });
        }

        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Xóa homestay thành công')),
        );
      } else {
        debugPrint('Delete homestay failed: ${response.statusCode} ${response.body}');
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Lỗi xóa homestay')),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi kết nối: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Quản lý homestay'),
        backgroundColor: Colors.blue,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadHomestays,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadHomestays,
              child: _homestays.isEmpty
                  ? const Center(
                      child: Text(
                        'Bạn chưa có homestay nào\nNhấn nút + để tạo homestay mới',
                        textAlign: TextAlign.center,
                        style: TextStyle(fontSize: 16, color: Colors.grey),
                      ),
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(16),
                      itemCount: _homestays.length,
                      itemBuilder: (context, index) {
                        final homestay = _homestays[index];
                        return Card(
                          margin: const EdgeInsets.only(bottom: 16),
                          child: Column(
                            children: [
                              // Homestay Image
                homestay['images'] != null && (homestay['images'] is List && (homestay['images'] as List).isNotEmpty)
                  ? Image.network(
                    (homestay['images'] as List).first.toString(),
                                      height: 150,
                                      width: double.infinity,
                                      fit: BoxFit.cover,
                                      errorBuilder: (context, error, stackTrace) =>
                                          Container(
                                            height: 150,
                                            color: Colors.grey[300],
                                            child: const Icon(Icons.home, size: 50),
                                          ),
                                    )
                                  : Container(
                                      height: 150,
                                      color: Colors.grey[300],
                                      child: const Icon(Icons.home, size: 50),
                                    ),

                              // Homestay Info
                              Padding(
                                padding: const EdgeInsets.all(16),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                      children: [
                                        Expanded(
                                          child: Text(
                                            homestay['name'] ?? 'N/A',
                                            style: const TextStyle(
                                              fontSize: 18,
                                              fontWeight: FontWeight.bold,
                                            ),
                                          ),
                                        ),
                                        Container(
                                          padding: const EdgeInsets.symmetric(
                                            horizontal: 8,
                                            vertical: 4,
                                          ),
                                          decoration: BoxDecoration(
                                            color: homestay['isActive'] == true
                                                ? Colors.green
                                                : Colors.orange,
                                            borderRadius: BorderRadius.circular(12),
                                          ),
                                          child: Text(
                                            homestay['isActive'] == true ? 'Hoạt động' : 'Tạm dừng',
                                            style: const TextStyle(
                                              color: Colors.white,
                                              fontSize: 12,
                                            ),
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 8),
                                    Text(
                                      '${homestay['address'] ?? 'N/A'}, ${homestay['city'] ?? ''}',
                                      style: const TextStyle(color: Colors.grey),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      '${_formatMoney(homestay['pricePerNight'])} VND/đêm • ${homestay['maxGuests']?.toString() ?? '0'} khách',
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w500,
                                        color: Colors.blue,
                                      ),
                                    ),
                                    const SizedBox(height: 12),

                                    // Action Buttons
                                    Row(
                                      children: [
                                        Expanded(
                                          child: OutlinedButton.icon(
                                            onPressed: () async {
                                              // Navigate to edit screen and pass homestayId
                                              final result = await Navigator.of(context).push(
                                                MaterialPageRoute(
                                                  builder: (context) => CreateHomestayScreen(
                                                    homestayId: homestay['id']?.toString(),
                                                  ),
                                                ),
                                              );

                                              // If the edit screen returned a Homestay object, update the local list
                                              if (result is Homestay) {
                                                setState(() {
                                                  final idx = _homestays.indexWhere((h) => h['id'] == result.id);
                                                  if (idx >= 0) {
                                                    _homestays[idx] = result.toJson();
                                                  } else {
                                                    _homestays.insert(0, result.toJson());
                                                  }
                                                });
                                              } else if (result != null) {
                                                // Fallback: reload entire list
                                                await _loadHomestays();
                                              }
                                            },
                                            icon: const Icon(Icons.edit),
                                            label: const Text('Chỉnh sửa'),
                                          ),
                                        ),
                                        const SizedBox(width: 8),
                                        Expanded(
                                          child: OutlinedButton.icon(
                                            onPressed: () => _toggleHomestayStatus(
                                              homestay['id'],
                                              homestay['isActive'] ?? false,
                                            ),
                                            icon: Icon(
                                              homestay['isActive'] == true
                                                  ? Icons.pause
                                                  : Icons.play_arrow,
                                            ),
                                            label: Text(
                                              homestay['isActive'] == true ? 'Tạm dừng' : 'Kích hoạt',
                                            ),
                                          ),
                                        ),
                                        const SizedBox(width: 8),
                                        IconButton(
                                          onPressed: () => _deleteHomestay(homestay['id']),
                                          icon: const Icon(Icons.delete, color: Colors.red),
                                          tooltip: 'Xóa',
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
            ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final result = await Navigator.pushNamed(context, '/create-homestay');
          if (result != null) await _loadHomestays();
        },
        backgroundColor: Colors.blue,
        child: const Icon(Icons.add),
      ),
    );
  }
}