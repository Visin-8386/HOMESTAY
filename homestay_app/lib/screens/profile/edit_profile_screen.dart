import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:homestay_app/config/api_config.dart';
import 'package:http/http.dart' as http;

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});

  @override
  _EditProfileScreenState createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _storage = const FlutterSecureStorage();

  String? _firstName;
  String? _lastName;
  String? _phoneNumber;
  String? _bio;
  String? _address;
  String? _profilePicture;
  bool _isLoading = true;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Not authenticated')),
        );
        Navigator.of(context).pop();
        return;
      }

      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/user/profile'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          final profile = data['data'];
          setState(() {
            _firstName = profile['firstName'];
            _lastName = profile['lastName'];
            _phoneNumber = profile['phoneNumber'];
            _bio = profile['bio'];
            _address = profile['address'];
            _profilePicture = profile['profilePicture'];
            _isLoading = false;
          });
        }
      } else {
        throw Exception('Failed to load profile');
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error loading profile: $e')),
      );
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _saveProfile() async {
    if (!_formKey.currentState!.validate()) return;

    _formKey.currentState!.save();

    setState(() {
      _isSaving = true;
    });

    try {
      final token = await _storage.read(key: 'jwt_token');
      if (token == null) {
        throw Exception('Not authenticated');
      }

      final updateData = {
        'firstName': _firstName,
        'lastName': _lastName,
        'phoneNumber': _phoneNumber,
        'bio': _bio,
        'address': _address,
        'profilePicture': _profilePicture,
      };

      final response = await http.put(
        Uri.parse('${ApiConfig.baseUrl}/api/user/profile'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: json.encode(updateData),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Profile updated successfully')),
          );
          Navigator.of(context).pop();
        } else {
          throw Exception(data['message'] ?? 'Failed to update profile');
        }
      } else {
        throw Exception('Failed to update profile');
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error updating profile: $e')),
      );
    } finally {
      setState(() {
        _isSaving = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Chỉnh sửa hồ sơ')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Chỉnh sửa hồ sơ'),
        backgroundColor: const Color(0xFF667eea),
        elevation: 0,
        actions: [
          if (_isSaving)
            const Padding(
              padding: EdgeInsets.all(16.0),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            )
          else
            TextButton(
              onPressed: _saveProfile,
              child: const Text('Lưu', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
            ),
        ],
      ),
      body: Container(
        width: double.infinity,
        height: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFF667eea), Color(0xFF764ba2)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                // Avatar with edit button
                Center(
                  child: Stack(
                    alignment: Alignment.bottomRight,
                    children: [
                      CircleAvatar(
                        radius: 54,
                        backgroundColor: Colors.grey[200],
                        backgroundImage: (_profilePicture != null && _profilePicture!.isNotEmpty)
                            ? NetworkImage(_profilePicture!)
                            : null,
                        child: (_profilePicture == null || _profilePicture!.isEmpty)
                            ? const Icon(Icons.person, size: 54, color: Color(0xFF667eea))
                            : null,
                      ),
                      Positioned(
                        bottom: 4,
                        right: 4,
                        child: Material(
                          color: Colors.white,
                          shape: const CircleBorder(),
                          child: IconButton(
                            icon: const Icon(Icons.edit, size: 20, color: Color(0xFF667eea)),
                            onPressed: () {
                              // Future: implement image picker/upload
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(content: Text('Chức năng cập nhật ảnh sẽ sớm có!')),
                              );
                            },
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 28),
                Card(
                  elevation: 0,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  color: Colors.white,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 20),
                    child: Column(
                      children: [
                        TextFormField(
                          initialValue: _firstName,
                          decoration: const InputDecoration(
                            labelText: 'Họ',
                            border: OutlineInputBorder(),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'Vui lòng nhập họ';
                            }
                            return null;
                          },
                          onSaved: (value) => _firstName = value,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          initialValue: _lastName,
                          decoration: const InputDecoration(
                            labelText: 'Tên',
                            border: OutlineInputBorder(),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'Vui lòng nhập tên';
                            }
                            return null;
                          },
                          onSaved: (value) => _lastName = value,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          initialValue: _phoneNumber,
                          decoration: const InputDecoration(
                            labelText: 'Số điện thoại',
                            border: OutlineInputBorder(),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'Vui lòng nhập số điện thoại';
                            }
                            return null;
                          },
                          onSaved: (value) => _phoneNumber = value,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          initialValue: _bio,
                          decoration: const InputDecoration(
                            labelText: 'Giới thiệu',
                            border: OutlineInputBorder(),
                          ),
                          maxLines: 3,
                          onSaved: (value) => _bio = value,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          initialValue: _address,
                          decoration: const InputDecoration(
                            labelText: 'Địa chỉ',
                            border: OutlineInputBorder(),
                          ),
                          maxLines: 2,
                          onSaved: (value) => _address = value,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          initialValue: _profilePicture,
                          decoration: const InputDecoration(
                            labelText: 'Ảnh đại diện (URL)',
                            border: OutlineInputBorder(),
                          ),
                          onSaved: (value) => _profilePicture = value,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 32),
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton(
                    onPressed: _isSaving ? null : _saveProfile,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF667eea),
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    child: const Text(
                      'Lưu thay đổi',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
                    ),
                  ),
                ),
                const SizedBox(height: 16),
              ],
            ),
          ),
        ),
      ),
    );
  }
}