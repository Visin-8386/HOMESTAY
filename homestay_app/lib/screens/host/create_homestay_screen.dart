import 'dart:io';

import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:image_picker/image_picker.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../services/homestay_service.dart';

class CreateHomestayScreen extends StatefulWidget {
  final String? homestayId; // For edit mode

  const CreateHomestayScreen({super.key, this.homestayId});

  @override
  State<CreateHomestayScreen> createState() => _CreateHomestayScreenState();
}

class _CreateHomestayScreenState extends State<CreateHomestayScreen> {
  final _formKey = GlobalKey<FormState>();
  final HomestayService _homestayService = HomestayService();
  final ImagePicker _picker = ImagePicker();
  
  // Form fields
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _descriptionController = TextEditingController();
  final TextEditingController _youtubeController = TextEditingController();
  final TextEditingController _addressController = TextEditingController();
  final TextEditingController _cityController = TextEditingController();
  final TextEditingController _stateController = TextEditingController();
  final TextEditingController _zipCodeController = TextEditingController();
  final TextEditingController _priceController = TextEditingController();
  final TextEditingController _maxGuestsController = TextEditingController();
  final TextEditingController _bedroomsController = TextEditingController();
  final TextEditingController _bathroomsController = TextEditingController();

  double _latitude = 21.0285; // Default to Hanoi
  double _longitude = 105.8542;
  final List<File> _imageFiles = [];
  List<String> _existingImages = [];
  List<Map<String, dynamic>> _existingImageObjects = []; // { 'id': int?|null, 'url': string }
  final List<int> _imagesToDelete = [];
  List<Amenity> _allAmenities = [];
  List<int> _selectedAmenityIds = [];
  bool _amenitiesLoading = true;
  bool _isLoading = false;
  bool _isEditMode = false;

  @override
  void initState() {
    super.initState();
    _loadAmenities();
    if (widget.homestayId != null) {
      _isEditMode = true;
      _loadHomestayData();
    }
  }

  Future<void> _loadAmenities() async {
    setState(() => _amenitiesLoading = true);
    try {
      final amenities = await _homestayService.getAmenities();
      setState(() => _allAmenities = amenities);
    } catch (e) {
      // Error loading amenities - will show empty list
      debugPrint('Error loading amenities: $e');
      setState(() => _allAmenities = []);
    } finally {
      if (mounted) setState(() => _amenitiesLoading = false);
    }
  }

  Future<void> _loadHomestayData() async {
    if (widget.homestayId == null) return;
    
    setState(() => _isLoading = true);
    try {
      final homestay = await _homestayService.getHomestayById(int.parse(widget.homestayId!));
      
      _nameController.text = homestay.name;
      _descriptionController.text = homestay.description;
      _addressController.text = homestay.address;
      _cityController.text = homestay.city;
      _priceController.text = homestay.pricePerNight.toString();
      _maxGuestsController.text = homestay.maxGuests.toString();
      _bedroomsController.text = homestay.bedrooms.toString();
      _bathroomsController.text = homestay.bathrooms.toString();
      _latitude = homestay.latitude;
      _longitude = homestay.longitude;
      _stateController.text = homestay.state;
      _zipCodeController.text = homestay.zipCode;
  _youtubeController.text = homestay.youtubeVideoId ?? '';
      // Normalize existing images into simple url list and object list
      _existingImages = homestay.images;
      _existingImageObjects = homestay.images.map((img) => {'id': null, 'url': img}).toList();
      _selectedAmenityIds = homestay.amenities.map((a) => a.id).toList();
      setState(() => _isLoading = false);
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải dữ liệu: $e')),
        );
      }
    }
  }

  Future<void> _pickImages() async {
    try {
      final List<XFile> images = await _picker.pickMultiImage();
      setState(() {
        _imageFiles.addAll(images.map((xFile) => File(xFile.path)));
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi chọn ảnh: $e')),
        );
      }
    }
  }

  Future<void> _saveHomestay() async {
    if (!_formKey.currentState!.validate()) return;

    if (_existingImages.isEmpty && _imageFiles.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng thêm ít nhất 1 ảnh')),
      );
      return;
    }

    if (_selectedAmenityIds.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng chọn ít nhất 1 tiện nghi')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
  // Build payload (do not embed binary images here)
      final data = {
        'name': _nameController.text,
        'description': _descriptionController.text,
        // youtubeVideoId is optional; backend may accept null/absent
        if (_youtubeController.text.trim().isNotEmpty) 'youtubeVideoId': _youtubeController.text.trim(),
        'address': _addressController.text,
        'city': _cityController.text,
        'state': _stateController.text,
        'zipCode': _zipCodeController.text,
        'pricePerNight': double.parse(_priceController.text),
        'maxGuests': int.parse(_maxGuestsController.text),
        'bedrooms': int.parse(_bedroomsController.text),
        'bathrooms': int.parse(_bathroomsController.text),
        'latitude': _latitude,
        'longitude': _longitude,
        'amenityIds': _selectedAmenityIds,
        if (_isEditMode) 'existingImages': _existingImages,
        if (_isEditMode && _imagesToDelete.isNotEmpty) 'imagesToDelete': _imagesToDelete,
      };
      // Debug: print coordinates being sent
      try {
        print('📍 Saving homestay with coords: latitude=$_latitude longitude=$_longitude');
      } catch (_) {}
      // If we have new local image files, include their paths for multipart upload
      if (_imageFiles.isNotEmpty) {
        data['imagePaths'] = _imageFiles.map((f) => f.path).toList();
      }

      // Create or update homestay (HomestayService will use multipart when imagePaths is present)
      Homestay savedHomestay;
      if (_isEditMode) {
        savedHomestay = await _homestayService.updateHomestay(int.parse(widget.homestayId!), data);
      } else {
        savedHomestay = await _homestayService.createHomestay(data);
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(_isEditMode ? 'Đã cập nhật homestay' : 'Đã tạo homestay')),
        );
        // Print debug info so logs show the returned homestay payload
        try {
          print('🏁 Homestay saved: ${savedHomestay.toJson()}');
        } catch (_) {}
        // Return the saved homestay to the caller so it can update UI immediately
        Navigator.pop(context, savedHomestay);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isEditMode ? 'Sửa Homestay' : 'Tạo Homestay Mới'),
        actions: [
          if (!_isLoading)
            TextButton(
              onPressed: _saveHomestay,
              child: const Text('Lưu'),
            ),
        ],
      ),
      body: _isLoading && _isEditMode
          ? const Center(child: CircularProgressIndicator())
          : Form(
              key: _formKey,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  _buildImagesSection(),
                  const SizedBox(height: 24),
                  _buildBasicInfoSection(),
                  const SizedBox(height: 24),
                  _buildLocationSection(),
                  const SizedBox(height: 24),
                  _buildAmenitiesSection(),
                  const SizedBox(height: 32),
                  if (_isLoading)
                    const Center(child: CircularProgressIndicator())
                  else
                    ElevatedButton(
                      onPressed: _saveHomestay,
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        backgroundColor: AppColors.primary,
                      ),
                      child: Text(
                        _isEditMode ? 'Cập nhật Homestay' : 'Tạo Homestay',
                        style: const TextStyle(fontSize: 16),
                      ),
                    ),
                  const SizedBox(height: 32),
                ],
              ),
            ),
    );
  }

  Widget _buildImagesSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Hình ảnh',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        SizedBox(
          height: 120,
          child: ListView(
            scrollDirection: Axis.horizontal,
            children: [
              ..._existingImages.map((url) => _buildImageCard(url: url)),
              ..._imageFiles.map((file) => _buildImageCard(file: file)),
              _buildAddImageButton(),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildImageCard({String? url, File? file}) {
    return Container(
      width: 120,
      margin: const EdgeInsets.only(right: 12),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(12),
        image: DecorationImage(
          image: url != null ? NetworkImage(url) as ImageProvider : FileImage(file!),
          fit: BoxFit.cover,
        ),
      ),
      child: Stack(
        children: [
          Positioned(
            top: 4,
            right: 4,
            child: IconButton(
              icon: const Icon(Icons.close, color: Colors.white),
              style: IconButton.styleFrom(
                backgroundColor: Colors.black54,
                padding: EdgeInsets.zero,
                minimumSize: const Size(28, 28),
              ),
              onPressed: () {
                setState(() {
                  if (url != null) {
                    // mark existing image for deletion if it has id
                    final idx = _existingImageObjects.indexWhere((e) => e['url'] == url);
                    if (idx >= 0) {
                      final id = _existingImageObjects[idx]['id'];
                      if (id != null) _imagesToDelete.add(id as int);
                      _existingImageObjects.removeAt(idx);
                    }
                    _existingImages.remove(url);
                  } else {
                    _imageFiles.remove(file);
                  }
                });
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAddImageButton() {
    return GestureDetector(
      onTap: _pickImages,
      child: Container(
        width: 120,
        decoration: BoxDecoration(
          color: Colors.grey[200],
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.grey[400]!),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.add_photo_alternate, size: 32, color: Colors.grey[600]),
            const SizedBox(height: 4),
            Text('Thêm ảnh', style: TextStyle(color: Colors.grey[600])),
          ],
        ),
      ),
    );
  }

  Widget _buildBasicInfoSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Thông tin cơ bản',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _nameController,
          decoration: const InputDecoration(
            labelText: 'Tên homestay *',
            border: OutlineInputBorder(),
          ),
          validator: (value) => value?.isEmpty ?? true ? 'Vui lòng nhập tên' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _descriptionController,
          decoration: const InputDecoration(
            labelText: 'Mô tả *',
            border: OutlineInputBorder(),
          ),
          maxLines: 4,
          validator: (value) => value?.isEmpty ?? true ? 'Vui lòng nhập mô tả' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _youtubeController,
          decoration: const InputDecoration(
            labelText: 'YouTube video (ID hoặc URL)',
            border: OutlineInputBorder(),
            hintText: 'e.g. dQw4w9WgXcQ hoặc https://youtu.be/dQw4w9WgXcQ',
          ),
          validator: (value) {
            if (value == null || value.trim().isEmpty) return null;
            final v = value.trim();
            if (v.length < 11 && !v.contains('youtube')) {
              return 'Nhập ID hoặc URL YouTube hợp lệ';
            }
            return null;
          },
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _stateController,
          decoration: const InputDecoration(
            labelText: 'Tỉnh/Bang',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _zipCodeController,
          decoration: const InputDecoration(
            labelText: 'Mã bưu điện',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: TextFormField(
                controller: _priceController,
                decoration: const InputDecoration(
                  labelText: 'Giá/đêm (₫) *',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                validator: (value) {
                  if (value?.isEmpty ?? true) return 'Vui lòng nhập giá';
                  if (double.tryParse(value!) == null) return 'Giá không hợp lệ';
                  return null;
                },
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: TextFormField(
                controller: _maxGuestsController,
                decoration: const InputDecoration(
                  labelText: 'Số khách *',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                validator: (value) {
                  if (value?.isEmpty ?? true) return 'Vui lòng nhập';
                  if (int.tryParse(value!) == null) return 'Không hợp lệ';
                  return null;
                },
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: TextFormField(
                controller: _bedroomsController,
                decoration: const InputDecoration(
                  labelText: 'Phòng ngủ *',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                validator: (value) {
                  if (value?.isEmpty ?? true) return 'Vui lòng nhập';
                  if (int.tryParse(value!) == null) return 'Không hợp lệ';
                  return null;
                },
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: TextFormField(
                controller: _bathroomsController,
                decoration: const InputDecoration(
                  labelText: 'Phòng tắm *',
                  border: OutlineInputBorder(),
                ),
                keyboardType: TextInputType.number,
                validator: (value) {
                  if (value?.isEmpty ?? true) return 'Vui lòng nhập';
                  if (int.tryParse(value!) == null) return 'Không hợp lệ';
                  return null;
                },
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildLocationSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Vị trí',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        TextFormField(
          controller: _addressController,
          decoration: const InputDecoration(
            labelText: 'Địa chỉ *',
            border: OutlineInputBorder(),
          ),
          validator: (value) => value?.isEmpty ?? true ? 'Vui lòng nhập địa chỉ' : null,
        ),
        const SizedBox(height: 16),
        TextFormField(
          controller: _cityController,
          decoration: const InputDecoration(
            labelText: 'Thành phố *',
            border: OutlineInputBorder(),
          ),
          validator: (value) => value?.isEmpty ?? true ? 'Vui lòng nhập thành phố' : null,
        ),
        const SizedBox(height: 16),
        Container(
          height: 200,
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey[400]!),
            borderRadius: BorderRadius.circular(12),
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: GoogleMap(
              initialCameraPosition: CameraPosition(
                target: LatLng(_latitude, _longitude),
                zoom: 15,
              ),
              markers: {
                Marker(
                  markerId: const MarkerId('homestay'),
                  position: LatLng(_latitude, _longitude),
                  draggable: true,
                  onDragEnd: (newPosition) {
                    setState(() {
                      _latitude = newPosition.latitude;
                      _longitude = newPosition.longitude;
                    });
                  },
                ),
              },
              onTap: (position) {
                setState(() {
                  _latitude = position.latitude;
                  _longitude = position.longitude;
                });
              },
            ),
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'Latitude: ${_latitude.toStringAsFixed(6)}, Longitude: ${_longitude.toStringAsFixed(6)}',
          style: TextStyle(fontSize: 12, color: Colors.grey[600]),
        ),
      ],
    );
  }

  Widget _buildAmenitiesSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Tiện nghi',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        if (_amenitiesLoading)
          const SizedBox(height: 48, child: Center(child: CircularProgressIndicator()))
        else if (_allAmenities.isEmpty)
          Row(
            children: [
              const Text('Không tìm thấy tiện nghi'),
              const SizedBox(width: 8),
              IconButton(
                onPressed: _loadAmenities,
                icon: const Icon(Icons.refresh),
                tooltip: 'Tải lại tiện nghi',
              ),
            ],
          )
        else
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: _allAmenities.map((amenity) {
              final isSelected = _selectedAmenityIds.contains(amenity.id);
              return FilterChip(
                label: Text(amenity.name),
                selected: isSelected,
                onSelected: (selected) {
                  setState(() {
                    if (selected) {
                      _selectedAmenityIds.add(amenity.id);
                    } else {
                      _selectedAmenityIds.remove(amenity.id);
                    }
                  });
                },
                selectedColor: AppColors.primary.withAlpha(77),
                checkmarkColor: AppColors.primary,
              );
            }).toList(),
          ),
      ],
    );
  }

  @override
  void dispose() {
  _nameController.dispose();
  _descriptionController.dispose();
  _youtubeController.dispose();
  _addressController.dispose();
  _cityController.dispose();
  _stateController.dispose();
  _zipCodeController.dispose();
  _priceController.dispose();
  _maxGuestsController.dispose();
  _bedroomsController.dispose();
  _bathroomsController.dispose();
  super.dispose();
  }
}
