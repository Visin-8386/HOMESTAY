import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../services/cccd_service.dart';
import '../../widgets/user_gradient_background.dart';

/// Screen to capture/select CCCD image, call FPT.AI IDR and show/edit parsed fields.
///
/// Usage:
/// Navigator.push(context, MaterialPageRoute(builder: (_) => CccdScanScreen()));
/// The screen returns a Map<String, dynamic> when saved (or null if cancelled).
class CccdScanScreen extends StatefulWidget {
  const CccdScanScreen({Key? key}) : super(key: key);

  @override
  State<CccdScanScreen> createState() => _CccdScanScreenState();
}

class _CccdScanScreenState extends State<CccdScanScreen> {
  final ImagePicker _picker = ImagePicker();
  File? _imageFile;
  bool _loading = false;
  FptIdrResult? _result;

  // Editable controllers
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _idController = TextEditingController();
  final TextEditingController _dobController = TextEditingController();
  final TextEditingController _genderController = TextEditingController();
  final TextEditingController _addressController = TextEditingController();

  @override
  void dispose() {
    _nameController.dispose();
    _idController.dispose();
    _dobController.dispose();
    _genderController.dispose();
    _addressController.dispose();
    super.dispose();
  }

  Future<void> _pick(ImageSource source) async {
    final picked = await _picker.pickImage(source: source, imageQuality: 85);
    if (picked == null) return;
    setState(() {
      _imageFile = File(picked.path);
      _result = null;
    });
  }

  Future<void> _recognize() async {
    if (_imageFile == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Vui lòng chọn ảnh trước')));
      return;
    }

    setState(() => _loading = true);
    try {
      final svc = FptIdrService();
      final r = await svc.recognizeId(imageFile: _imageFile!);
      setState(() {
        _result = r;
        _nameController.text = r.fullName ?? '';
        _idController.text = r.idNumber ?? '';
        _dobController.text = r.dateOfBirth ?? '';
        _genderController.text = r.gender ?? '';
        _addressController.text = r.address ?? '';
      });
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi nhận dạng: $e')));
    } finally {
      setState(() => _loading = false);
    }
  }

  void _save() {
    final out = <String, dynamic>{
      'fullName': _nameController.text.trim(),
      'idNumber': _idController.text.trim(),
      'dateOfBirth': _dobController.text.trim(),
      'gender': _genderController.text.trim(),
      'address': _addressController.text.trim(),
      'raw': _result?.raw,
    };
    Navigator.of(context).pop(out);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Quét CCCD / CMND'),
        actions: [
          IconButton(
            icon: const Icon(Icons.check),
            onPressed: _result == null ? null : _save,
            tooltip: 'Lưu kết quả',
          ),
        ],
      ),
      body: UserGradientBackground(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_imageFile != null)
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.file(
                    _imageFile!,
                    height: 220,
                    fit: BoxFit.cover,
                  ),
                )
              else
                Container(
                  height: 220,
                  decoration: BoxDecoration(borderRadius: BorderRadius.circular(8), color: Colors.grey[200]),
                  child: const Center(child: Text('Chưa chọn ảnh')),
                ),

              const SizedBox(height: 12),

              Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.photo_library),
                      label: const Text('Chọn ảnh'),
                      onPressed: () => _pick(ImageSource.gallery),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.camera_alt),
                      label: const Text('Chụp ảnh'),
                      onPressed: () => _pick(ImageSource.camera),
                    ),
                  ),
                ],
              ),

              const SizedBox(height: 12),

              ElevatedButton.icon(
                icon: _loading ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.search),
                label: Text(_loading ? 'Đang nhận dạng...' : 'Nhận dạng bằng FPT.AI'),
                onPressed: _loading ? null : _recognize,
              ),

              const SizedBox(height: 16),

              if (_result != null) ...[
                const Text('Kết quả (chỉnh sửa nếu cần):', style: TextStyle(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),
                TextField(controller: _nameController, decoration: const InputDecoration(labelText: 'Họ và tên')),
                TextField(controller: _idController, decoration: const InputDecoration(labelText: 'Số CCCD/CMND')),
                TextField(controller: _dobController, decoration: const InputDecoration(labelText: 'Ngày sinh')),
                TextField(controller: _genderController, decoration: const InputDecoration(labelText: 'Giới tính')),
                TextField(controller: _addressController, decoration: const InputDecoration(labelText: 'Địa chỉ'), maxLines: 2),

                const SizedBox(height: 12),
                ElevatedButton.icon(
                  icon: const Icon(Icons.save),
                  label: const Text('Lưu & Trả về'),
                  onPressed: _save,
                ),
                const SizedBox(height: 8),
                OutlinedButton(
                  onPressed: () => showDialog(
                    context: context,
                    builder: (_) => AlertDialog(
                      title: const Text('Raw JSON'),
                      content: SingleChildScrollView(child: Text(_result!.raw.toString())),
                      actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Đóng'))],
                    ),
                  ),
                  child: const Text('Xem raw JSON'),
                ),
              ] else ...[
                const SizedBox(height: 24),
                const Text('Chưa có kết quả. Chọn hoặc chụp ảnh rồi bấm "Nhận dạng bằng FPT.AI".', style: TextStyle(color: Colors.grey)),
              ],
            ],
          ),
        ),
      ),
    );
  }
}