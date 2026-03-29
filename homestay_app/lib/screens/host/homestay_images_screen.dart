import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../services/homestay_service.dart';

class HomestayImagesScreen extends StatefulWidget {
  final int homestayId;
  final String homestayName;

  const HomestayImagesScreen({
    super.key,
    required this.homestayId,
    required this.homestayName,
  });

  @override
  State<HomestayImagesScreen> createState() => _HomestayImagesScreenState();
}

class _HomestayImagesScreenState extends State<HomestayImagesScreen> {
  final HomestayService _homestayService = HomestayService();
  final ImagePicker _picker = ImagePicker();

  Homestay? _homestay;
  bool _isLoading = true;
  bool _isUploading = false;

  @override
  void initState() {
    super.initState();
    _loadHomestay();
  }

  Future<void> _loadHomestay() async {
    setState(() => _isLoading = true);
    try {
      final homestay = await _homestayService.getHomestayById(widget.homestayId);
      setState(() {
        _homestay = homestay;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải homestay: $e')),
        );
      }
    }
  }

  Future<void> _pickAndUploadImages() async {
    try {
      final List<XFile> images = await _picker.pickMultiImage();
      if (images.isEmpty) return;

      setState(() => _isUploading = true);

      final imagePaths = images.map((xFile) => xFile.path).toList();
      await _homestayService.uploadHomestayImages(widget.homestayId, imagePaths);

      await _loadHomestay(); // Reload to get updated images

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã tải lên ảnh thành công')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải lên ảnh: $e')),
        );
      }
    } finally {
      setState(() => _isUploading = false);
    }
  }

  Future<void> _setPrimaryImage(String imageUrl) async {
    try {
      // Extract image ID from URL or find by URL
      // For now, assume we need to get image ID from backend
      // This might need adjustment based on how images are stored
      final imageId = await _getImageIdFromUrl(imageUrl);
      if (imageId == null) return;

      await _homestayService.setPrimaryImage(widget.homestayId, imageId);
      await _loadHomestay();

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã đặt làm ảnh chính')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi đặt ảnh chính: $e')),
        );
      }
    }
  }

  Future<void> _deleteImage(String imageUrl) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xóa ảnh'),
        content: const Text('Bạn có chắc muốn xóa ảnh này?'),
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
      final imageId = await _getImageIdFromUrl(imageUrl);
      if (imageId == null) return;

      await _homestayService.deleteHomestayImage(widget.homestayId, imageId);
      await _loadHomestay();

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã xóa ảnh')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi xóa ảnh: $e')),
        );
      }
    }
  }

  Future<int?> _getImageIdFromUrl(String imageUrl) async {
    // TODO: Backend needs to provide image IDs in homestay response
    // For now, this is not implemented
    // Possible solutions:
    // 1. Add image objects with id and url to Homestay model
    // 2. Create separate API to get images with IDs
    // 3. Extract ID from URL if it's included
    
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Chức năng này cần backend hỗ trợ image IDs')),
      );
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Ảnh - ${widget.homestayName}'),
        actions: [
          if (!_isUploading)
            IconButton(
              icon: const Icon(Icons.add_photo_alternate),
              onPressed: _pickAndUploadImages,
            ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadHomestay,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _homestay == null
              ? const Center(child: Text('Không tìm thấy homestay'))
              : _buildImagesGrid(),
      floatingActionButton: _isUploading
          ? const FloatingActionButton(
              onPressed: null,
              child: CircularProgressIndicator(
                valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
              ),
            )
          : FloatingActionButton.extended(
              onPressed: _pickAndUploadImages,
              icon: const Icon(Icons.add),
              label: const Text('Thêm ảnh'),
            ),
    );
  }

  Widget _buildImagesGrid() {
    final images = _homestay!.images;

    if (images.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.photo_library, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'Chưa có ảnh nào',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Nhấn nút + để thêm ảnh',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return GridView.builder(
      padding: const EdgeInsets.all(16),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        crossAxisSpacing: 16,
        mainAxisSpacing: 16,
        childAspectRatio: 1,
      ),
      itemCount: images.length,
      itemBuilder: (context, index) {
        final imageUrl = images[index];
        final isPrimary = index == 0; // Assume first image is primary

        return _buildImageCard(imageUrl, isPrimary);
      },
    );
  }

  Widget _buildImageCard(String imageUrl, bool isPrimary) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Stack(
        fit: StackFit.expand,
        children: [
          CachedNetworkImage(
            imageUrl: imageUrl,
            fit: BoxFit.cover,
            placeholder: (context, url) => Container(
              color: Colors.grey[200],
              child: const Center(child: CircularProgressIndicator()),
            ),
            errorWidget: (context, url, error) => Container(
              color: Colors.grey[300],
              child: const Icon(Icons.broken_image, size: 48),
            ),
          ),
          if (isPrimary)
            Positioned(
              top: 8,
              left: 8,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Text(
                  'Ảnh chính',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
          Positioned(
            top: 8,
            right: 8,
            child: PopupMenuButton<String>(
              onSelected: (value) {
                switch (value) {
                  case 'primary':
                    _setPrimaryImage(imageUrl);
                    break;
                  case 'delete':
                    _deleteImage(imageUrl);
                    break;
                }
              },
              itemBuilder: (context) => [
                if (!isPrimary)
                  const PopupMenuItem(
                    value: 'primary',
                    child: Text('Đặt làm ảnh chính'),
                  ),
                const PopupMenuItem(
                  value: 'delete',
                  child: Text('Xóa ảnh'),
                ),
              ],
              icon: Container(
                padding: const EdgeInsets.all(4),
                decoration: BoxDecoration(
                  color: Colors.black.withOpacity(0.5),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.more_vert,
                  color: Colors.white,
                  size: 20,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}