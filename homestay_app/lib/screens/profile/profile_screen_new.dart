import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../../config/api_config.dart';
import '../../services/auth_service.dart';
import '../../services/biometric_service.dart';
import '../../services/storage_service.dart';
import '../auth/login_screen.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  Map<String, dynamic>? _userInfo;
  bool _isLoading = true;
  final BiometricService _biometricService = BiometricService();
  bool _biometricAvailable = false;
  bool _biometricEnabled = false;

  @override
  void initState() {
    super.initState();
    _loadUserInfo();
    _initBiometric();
  }

  Future<void> _initBiometric() async {
    final available = await _biometricService.canCheckBiometrics();
    final enabled = await _biometricService.isBiometricEnabled();
    if (!mounted) return;
    setState(() {
      _biometricAvailable = available;
      _biometricEnabled = enabled;
    });
  }

  Future<void> _loadUserInfo() async {
    setState(() => _isLoading = true);

    try {
      final storage = StorageService();
      final userId = await storage.getUserId();
      final token = await storage.getToken();

      if (userId == null || token == null) {
        _logout();
        return;
      }

      final response = await http.get(
        Uri.parse('${ApiConfig.baseUrl}/api/user/$userId'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (mounted) {
          setState(() {
            _userInfo = data['data'];
            _isLoading = false;
          });
        }
      } else {
        if (mounted) {
          setState(() => _isLoading = false);
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
    // ensure server logout if needed
    try {
      await AuthService().logout();
    } catch (_) {}
    // clear all local secure storage including biometric data
    await StorageService().clearAll();
    if (mounted) {
      Navigator.of(context).pushAndRemoveUntil(
        MaterialPageRoute(builder: (_) => const LoginScreen()),
        (route) => false,
      );
    }
  }

  Future<void> _toggleBiometric(bool value) async {
    if (!_biometricAvailable) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Thiết bị không hỗ trợ sinh trắc học')),
      );
      return;
    }

    final email = _userInfo?['email'];
    if (email == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không có email người dùng')),
      );
      return;
    }

    if (value) {
      // Enable: ask for current password to verify identity
      final password = await showDialog<String?>(
        context: context,
        builder: (context) {
          final pwController = TextEditingController();
          return AlertDialog(
            title: const Text('Xác nhận mật khẩu'),
            content: TextField(
              controller: pwController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Mật khẩu hiện tại'),
            ),
            actions: [
              TextButton(onPressed: () => Navigator.pop(context, null), child: const Text('Hủy')),
              ElevatedButton(onPressed: () => Navigator.pop(context, pwController.text), child: const Text('Xác nhận')),
            ],
          );
        },
      );

  if (password == null || password.isEmpty) return;

      // Validate credentials with server
      final auth = AuthService();
      try {
        await auth.login(email, password);
        // login returns a map; if token set then login success
        // enable biometric storage
        await _biometricService.enableBiometric(email, password);
        if (!mounted) return;
        setState(() => _biometricEnabled = true);
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đã bật đăng nhập vân tay')));
      } catch (e) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Mật khẩu không đúng')));
      }
    } else {
      // Disable biometric
      await _biometricService.disableBiometric();
      if (!mounted) return;
      setState(() => _biometricEnabled = false);
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đã tắt đăng nhập vân tay')));
    }
  }

  void _showComingSoon(String feature) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('Chức năng $feature sẽ được bổ sung sau'),
        duration: const Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[100],
      appBar: AppBar(
        title: const Text('Hồ sơ', style: TextStyle(color: Colors.white)),
        backgroundColor: const Color(0xFF667eea),
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.settings, color: Colors.white),
            onPressed: () => _showComingSoon('Cài đặt'),
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              child: Column(
                children: [
                  // Header với avatar
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.symmetric(vertical: 30),
                    decoration: const BoxDecoration(
                      gradient: LinearGradient(
                        colors: [Color(0xFF667eea), Color(0xFF764ba2)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                    ),
                    child: Column(
                      children: [
                        CircleAvatar(
                          radius: 50,
                          backgroundColor: Colors.white,
                          child: _userInfo?['profilePicture'] != null
                              ? ClipOval(
                                  child: Image.network(
                                    '${ApiConfig.baseUrl}${_userInfo!['profilePicture']}',
                                    width: 100,
                                    height: 100,
                                    fit: BoxFit.cover,
                                    errorBuilder: (context, error, stackTrace) {
                                      return const Icon(Icons.person, size: 50, color: Color(0xFF667eea));
                                    },
                                  ),
                                )
                              : const Icon(Icons.person, size: 50, color: Color(0xFF667eea)),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          _userInfo?['fullName'] ?? 'Người dùng',
                          style: const TextStyle(
                            fontSize: 24,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          _userInfo?['email'] ?? '',
                          style: const TextStyle(
                            fontSize: 14,
                            color: Colors.white70,
                          ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 20),

                  // Thông tin cá nhân
                  _buildSection(
                    title: 'Thông tin cá nhân',
                    children: [
                      _buildInfoTile(
                        icon: Icons.phone,
                        title: 'Số điện thoại',
                        value: _userInfo?['phoneNumber'] ?? 'Chưa cập nhật',
                      ),
                      _buildInfoTile(
                        icon: Icons.location_on,
                        title: 'Địa chỉ',
                        value: _userInfo?['address'] ?? 'Chưa cập nhật',
                      ),
                      _buildInfoTile(
                        icon: Icons.info,
                        title: 'Giới thiệu',
                        value: _userInfo?['bio'] ?? 'Chưa có thông tin',
                      ),
                    ],
                  ),

                  const SizedBox(height: 12),

                  // Menu chức năng
                  _buildSection(
                    title: 'Chức năng',
                    children: [
                      _buildMenuTile(
                        icon: Icons.edit,
                        title: 'Chỉnh sửa hồ sơ',
                        onTap: () => _showComingSoon('Chỉnh sửa hồ sơ'),
                      ),
                      _buildMenuTile(
                        icon: Icons.book,
                        title: 'Đơn đặt phòng của tôi',
                        onTap: () => _showComingSoon('Đơn đặt phòng'),
                      ),
                      _buildMenuTile(
                        icon: Icons.favorite,
                        title: 'Homestay yêu thích',
                        onTap: () => _showComingSoon('Yêu thích'),
                      ),
                      _buildMenuTile(
                        icon: Icons.history,
                        title: 'Lịch sử giao dịch',
                        onTap: () => _showComingSoon('Lịch sử'),
                      ),
                      _buildMenuTile(
                        icon: Icons.lock,
                        title: 'Đổi mật khẩu',
                        onTap: () => _showComingSoon('Đổi mật khẩu'),
                      ),
                      // Biometric toggle (About Me / Security)
                      if (_biometricAvailable)
                        Padding(
                          padding: const EdgeInsets.only(top: 8.0),
                          child: Row(
                            children: [
                              const Icon(Icons.fingerprint, size: 22, color: Color(0xFF667eea)),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Text(
                                  'Đăng nhập bằng sinh trắc học',
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ),
                              Switch(
                                value: _biometricEnabled,
                                onChanged: (v) => _toggleBiometric(v),
                                activeThumbColor: const Color(0xFF667eea),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),

                  const SizedBox(height: 12),

                  // Trợ giúp
                  _buildSection(
                    title: 'Trợ giúp',
                    children: [
                      _buildMenuTile(
                        icon: Icons.help,
                        title: 'Trung tâm trợ giúp',
                        onTap: () => _showComingSoon('Trợ giúp'),
                      ),
                      _buildMenuTile(
                        icon: Icons.privacy_tip,
                        title: 'Chính sách bảo mật',
                        onTap: () => _showComingSoon('Chính sách'),
                      ),
                      _buildMenuTile(
                        icon: Icons.description,
                        title: 'Điều khoản sử dụng',
                        onTap: () => _showComingSoon('Điều khoản'),
                      ),
                    ],
                  ),

                  const SizedBox(height: 12),

                  // Nút đăng xuất
                  Container(
                    margin: const EdgeInsets.all(16),
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: _logout,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.red,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                        elevation: 0,
                      ),
                      child: const Text(
                        'Đăng xuất',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),

                  const SizedBox(height: 20),
                ],
              ),
            ),
    );
  }

  Widget _buildSection({required String title, required List<Widget> children}) {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: const Color.fromRGBO(0, 0, 0, 0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: Color(0xFF667eea),
            ),
          ),
          const SizedBox(height: 12),
          ...children,
        ],
      ),
    );
  }

  Widget _buildInfoTile({
    required IconData icon,
    required String title,
    required String value,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: Colors.grey[600]),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMenuTile({
    required IconData icon,
    required String title,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: Row(
          children: [
            Icon(icon, size: 22, color: const Color(0xFF667eea)),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                title,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
            Icon(Icons.chevron_right, color: Colors.grey[400]),
          ],
        ),
      ),
    );
  }
}
