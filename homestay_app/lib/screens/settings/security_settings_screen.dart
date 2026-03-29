import 'package:flutter/material.dart';

import '../../services/auth_service.dart';
import '../../services/biometric_service.dart';
import '../../widgets/user_gradient_background.dart';

class SecuritySettingsScreen extends StatefulWidget {
  const SecuritySettingsScreen({super.key});

  @override
  State<SecuritySettingsScreen> createState() => _SecuritySettingsScreenState();
}

class _SecuritySettingsScreenState extends State<SecuritySettingsScreen> {
  final BiometricService _biometricService = BiometricService();
  final AuthService _authService = AuthService();
  bool _isLoading = true;
  bool _biometricAvailable = false;
  bool _biometricEnabled = false;
  bool _twoFactorEnabled = false;

  @override
  void initState() {
    super.initState();
    _checkBiometric();
    _checkTwoFactor();
  }

  Future<void> _checkBiometric() async {
    final available = await _biometricService.canCheckBiometrics();
    final enabled = await _biometricService.isBiometricEnabled();
    final twoFactorResult = await _authService.isTwoFactorEnabled();
    final twoFactorEnabled = twoFactorResult['success'] == true ? twoFactorResult['enabled'] : false;
    
    if (mounted) {
      setState(() {
        _biometricAvailable = available;
        _biometricEnabled = enabled;
        _twoFactorEnabled = twoFactorEnabled;
        _isLoading = false;
      });
    }
  }

  Future<void> _checkTwoFactor() async {
    final result = await _authService.isTwoFactorEnabled();
    final enabled = result['success'] == true ? result['enabled'] : false;
    
    if (mounted) {
      setState(() {
        _twoFactorEnabled = enabled;
      });
    }
  }

  Future<void> _toggleBiometric(bool value) async {
    if (value) {
      // Enable biometric
      final result = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Cảnh báo bảo mật'),
          content: const Text(
            'Thông tin đăng nhập của bạn sẽ được lưu trữ an toàn trên thiết bị này. '
            'Bạn có chắc chắn muốn bật đăng nhập sinh trắc học không?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Bật'),
            ),
          ],
        ),
      );

      if (result != true) return;

      // Authenticate first
      final authenticated = await _biometricService.authenticate(
        reason: 'Xác thực để bật đăng nhập sinh trắc học',
      );

      if (!authenticated) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Xác thực thất bại'),
              backgroundColor: Colors.red,
            ),
          );
        }
        return;
      }

      // Need to get credentials from user
      final credentials = await _showCredentialsDialog();
      if (credentials == null) return;

      await _biometricService.enableBiometric(
        credentials['email']!,
        credentials['password']!,
      );

      if (mounted) {
        setState(() => _biometricEnabled = true);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đã bật đăng nhập sinh trắc học'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } else {
      // Disable biometric
      final result = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Tắt sinh trắc học?'),
          content: const Text(
            'Thông tin đăng nhập đã lưu sẽ bị xóa khỏi thiết bị này.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
              child: const Text('Tắt'),
            ),
          ],
        ),
      );

      if (result != true) return;

      await _biometricService.disableBiometric();

      if (mounted) {
        setState(() => _biometricEnabled = false);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đã tắt đăng nhập sinh trắc học'),
            backgroundColor: Colors.orange,
          ),
        );
      }
    }
  }

  Future<void> _toggleTwoFactor(bool value) async {
    if (value) {
      // Enable two-factor authentication
      final result = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Bật xác thực hai yếu tố?'),
          content: const Text(
            'Bạn sẽ được chuyển đến trang thiết lập để cấu hình ứng dụng xác thực.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Tiếp tục'),
            ),
          ],
        ),
      );

      if (result != true) return;

      // Navigate to setup screen
      if (mounted) {
        Navigator.pushNamed(context, '/two-factor-setup').then((_) {
          // Refresh status after setup
          _checkTwoFactor();
        });
      }
    } else {
      // Disable two-factor authentication
      final result = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Tắt xác thực hai yếu tố?'),
          content: const Text(
            'Xác thực hai yếu tố sẽ bị tắt cho tài khoản của bạn.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
              child: const Text('Tắt'),
            ),
          ],
        ),
      );

      if (result != true) return;

      // Get credentials for disabling 2FA
      final credentials = await _showCredentialsDialog();
      if (credentials == null) return;

      // Call API to disable 2FA
      try {
        final disableResult = await _authService.disableTwoFactor(credentials['password']!);
        if (disableResult['success'] == true) {
          if (mounted) {
            setState(() => _twoFactorEnabled = false);
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Đã tắt xác thực hai yếu tố'),
                backgroundColor: Colors.orange,
              ),
            );
          }
        } else {
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(disableResult['message'] ?? 'Không thể tắt xác thực hai yếu tố'),
                backgroundColor: Colors.red,
              ),
            );
          }
        }
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Lỗi khi tắt xác thực hai yếu tố'),
              backgroundColor: Colors.red,
            ),
          );
        }
      }
    }
  }

  Future<Map<String, String>?> _showCredentialsDialog() async {
    final emailController = TextEditingController();
    final passwordController = TextEditingController();

    final result = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Nhập thông tin đăng nhập'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: emailController,
              decoration: const InputDecoration(
                labelText: 'Email',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: 16),
            TextField(
              controller: passwordController,
              decoration: const InputDecoration(
                labelText: 'Mật khẩu',
                border: OutlineInputBorder(),
              ),
              obscureText: true,
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Xác nhận'),
          ),
        ],
      ),
    );

    if (result != true) return null;

    final email = emailController.text.trim();
    final password = passwordController.text;

    if (email.isEmpty || password.isEmpty) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Vui lòng nhập đầy đủ thông tin'),
            backgroundColor: Colors.red,
          ),
        );
      }
      return null;
    }

    return {
      'email': email,
      'password': password,
    };
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Bảo mật'),
        backgroundColor: const Color(0xFF667eea),
      ),
      body: UserGradientBackground(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Icon(
                                Icons.fingerprint,
                                size: 32,
                                color: _biometricEnabled
                                    ? Colors.green
                                    : Colors.grey,
                              ),
                              const SizedBox(width: 16),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Text(
                                      'Đăng nhập sinh trắc học',
                                      style: TextStyle(
                                        fontSize: 18,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      _biometricAvailable
                                          ? 'Sử dụng vân tay hoặc khuôn mặt để đăng nhập'
                                          : 'Thiết bị không hỗ trợ sinh trắc học',
                                      style: TextStyle(
                                        fontSize: 14,
                                        color: Colors.grey[600],
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              Switch(
                                value: _biometricEnabled,
                                onChanged: _biometricAvailable
                                    ? _toggleBiometric
                                    : null,
                                activeColor: Colors.green,
                              ),
                            ],
                          ),
                          if (_biometricEnabled) ...[
                            const Divider(height: 32),
                            Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: Colors.green.withOpacity(0.1),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Row(
                                children: [
                                  const Icon(
                                    Icons.check_circle,
                                    color: Colors.green,
                                    size: 20,
                                  ),
                                  const SizedBox(width: 8),
                                  Expanded(
                                    child: Text(
                                      'Sinh trắc học đã được bật. Bạn có thể đăng nhập nhanh chóng bằng vân tay hoặc khuôn mặt.',
                                      style: TextStyle(
                                        fontSize: 13,
                                        color: Colors.grey[800],
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Row(
                            children: [
                              Icon(Icons.security, size: 24),
                              SizedBox(width: 12),
                              Text(
                                'Thông tin bảo mật',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 16),
                          Text(
                            '• Thông tin đăng nhập được mã hóa và lưu trữ an toàn trong bộ nhớ bảo mật của thiết bị\n\n'
                            '• Chỉ bạn có thể truy cập thông tin này bằng sinh trắc học của mình\n\n'
                            '• Nếu tắt sinh trắc học, tất cả thông tin đã lưu sẽ bị xóa vĩnh viễn',
                            style: TextStyle(
                              fontSize: 14,
                              color: Colors.grey[700],
                              height: 1.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Row(
                            children: [
                              Icon(Icons.lock, size: 24),
                              SizedBox(width: 12),
                              Text(
                                'Xác thực hai yếu tố',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 16),
                          Text(
                            'Bảo vệ tài khoản của bạn bằng cách yêu cầu xác thực hai yếu tố. '
                            'Khi bật tính năng này, bạn sẽ cần cung cấp mã xác thực được gửi đến email hoặc số điện thoại của mình ngoài thông tin đăng nhập.',
                            style: TextStyle(
                              fontSize: 14,
                              color: Colors.grey[700],
                              height: 1.5,
                            ),
                          ),
                          const SizedBox(height: 16),
                          Switch(
                            value: _twoFactorEnabled,
                            onChanged: _toggleTwoFactor,
                            activeColor: Colors.green,
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}
