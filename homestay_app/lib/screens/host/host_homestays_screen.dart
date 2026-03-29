import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../services/homestay_service.dart';
import 'create_homestay_screen.dart';
import 'homestay_images_screen.dart';

class HostHomestaysScreen extends StatefulWidget {
  const HostHomestaysScreen({Key? key}) : super(key: key);

  @override
  State<HostHomestaysScreen> createState() => _HostHomestaysScreenState();
}

class _HostHomestaysScreenState extends State<HostHomestaysScreen> {
  final HomestayService _homestayService = HomestayService();
  List<Homestay> _homestays = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadHostHomestays();
  }

  Future<void> _loadHostHomestays() async {
    setState(() => _isLoading = true);
    print('🔁 _loadHostHomestays called');
    
    try {
      final homestays = await _homestayService.getHostHomestays();
        print('🔁 _loadHostHomestays fetched ${homestays.length} items');
      setState(() {
        _homestays = homestays;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải danh sách: $e')),
        );
      }
    }
  }

  Future<void> _deleteHomestay(int id) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xóa homestay'),
        content: const Text('Bạn có chắc chắn muốn xóa homestay này?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    try {
      await _homestayService.deleteHomestay(id);
      setState(() {
        _homestays.removeWhere((h) => h.id == id);
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa homestay')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi xóa homestay: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Homestay của tôi'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadHostHomestays,
          ),
        ],
      ),
      body: _buildBody(),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          // Use the registered route name for creating a homestay and handle returned homestay
          final result = await Navigator.pushNamed(context, '/create-homestay');
          if (result is Homestay) {
            setState(() {
              _homestays.insert(0, result);
            });
          } else {
            await _loadHostHomestays();
          }
        },
        icon: const Icon(Icons.add),
        label: const Text('Thêm homestay'),
        backgroundColor: AppColors.primary,
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_homestays.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.home_outlined, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'Chưa có homestay nào',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Nhấn nút + để thêm homestay đầu tiên',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadHostHomestays,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _homestays.length,
        itemBuilder: (context, index) {
          final homestay = _homestays[index];
          return _buildHomestayCard(homestay);
        },
      ),
    );
  }

  Widget _buildHomestayCard(Homestay homestay) {
    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (homestay.images.isNotEmpty)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
              child: CachedNetworkImage(
                imageUrl: homestay.images.first,
                height: 200,
                width: double.infinity,
                fit: BoxFit.cover,
                placeholder: (context, url) => Container(
                  color: Colors.grey[200],
                  child: const Center(child: CircularProgressIndicator()),
                ),
                errorWidget: (context, url, error) => Container(
                  color: Colors.grey[300],
                  child: const Icon(Icons.image_not_supported, size: 64),
                ),
              ),
            ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        homestay.name,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    _buildStatusChip(true), // Assuming approved
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Icon(Icons.location_on, size: 16, color: Colors.grey[600]),
                    const SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        homestay.address,
                        style: TextStyle(color: Colors.grey[600]),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Text(
                      NumberFormat.currency(locale: 'vi_VN', symbol: '₫')
                          .format(homestay.pricePerNight),
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: AppColors.primary,
                      ),
                    ),
                    const Text(' / đêm'),
                    const Spacer(),
                    Icon(Icons.star, size: 16, color: Colors.amber[700]),
                    const SizedBox(width: 4),
                    Text('${homestay.averageRating?.toStringAsFixed(1) ?? '0.0'} (0 reviews)'),
                  ],
                ),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    SizedBox(
                      width: (MediaQuery.of(context).size.width - 48) / 2,
                      child: OutlinedButton.icon(
                        onPressed: () async {
                          // Navigate to edit using an explicit route so we can pass the id
                          final result = await Navigator.of(context).push(
                            MaterialPageRoute(
                              builder: (context) => CreateHomestayScreen(
                                homestayId: homestay.id.toString(),
                              ),
                            ),
                          );

                          // If the edit screen returned a Homestay, replace the item locally
                          if (result is Homestay) {
                            setState(() {
                              final idx = _homestays.indexWhere((h) => h.id == result.id);
                              if (idx >= 0) _homestays[idx] = result;
                            });
                          } else if (result == true) {
                            await _loadHostHomestays();
                          }
                        },
                        icon: const Icon(Icons.edit),
                        label: const Text('Sửa'),
                      ),
                    ),
                    SizedBox(
                      width: (MediaQuery.of(context).size.width - 48) / 2,
                      child: OutlinedButton.icon(
                        onPressed: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => HomestayImagesScreen(
                                homestayId: homestay.id,
                                homestayName: homestay.name,
                              ),
                            ),
                          );
                        },
                        icon: const Icon(Icons.photo),
                        label: const Text('Ảnh'),
                      ),
                    ),
                    SizedBox(
                      width: (MediaQuery.of(context).size.width - 48) / 2,
                      child: OutlinedButton.icon(
                        onPressed: () => _deleteHomestay(homestay.id),
                        icon: const Icon(Icons.delete),
                        label: const Text('Xóa'),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: Colors.red,
                          side: const BorderSide(color: Colors.red),
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatusChip(bool isApproved) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: isApproved ? Colors.green.withOpacity(0.1) : Colors.orange.withOpacity(0.1),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isApproved ? Colors.green : Colors.orange,
        ),
      ),
      child: Text(
        isApproved ? 'Đã duyệt' : 'Chờ duyệt',
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.bold,
          color: isApproved ? Colors.green : Colors.orange,
        ),
      ),
    );
  }
}
