import 'package:flutter/material.dart';

import '../../services/auth_service.dart';

class TwoFactorSetupScreen extends StatefulWidget {
  const TwoFactorSetupScreen({Key? key}) : super(key: key);

  @override
  State<TwoFactorSetupScreen> createState() => _TwoFactorSetupScreenState();
}

class _TwoFactorSetupScreenState extends State<TwoFactorSetupScreen> {
  final _passwordController = TextEditingController();
  final _authService = AuthService();
  bool _isLoading = false;
  Map<String, dynamic>? _setupData;
  bool _showRecoveryCodes = false;

  @override
  void dispose() {
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _enableTwoFactor() async {
    if (_passwordController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập mật khẩu')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      final result = await _authService.enableTwoFactor(_passwordController.text);
      if (result['success'] == true) {
        setState(() => _setupData = result['data']);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(result['message'] ?? 'Không thể bật 2FA')),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _enableTwoFactorByEmail() async {
    setState(() => _isLoading = true);
    try {
      final result = await _authService.enableTwoFactorByEmail();
      if (result['success'] == true) {
        setState(() => _setupData = {'emailEnabled': true});
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('2FA bằng email đã được bật.')),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(result['message'] ?? 'Không thể bật 2FA bằng email')),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi: $e')),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _completeSetup() async {
    // Mark setup as complete and navigate back
    Navigator.of(context).pop(true); // Return true to indicate success
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Thiết lập xác thực hai lớp'),
        backgroundColor: Theme.of(context).primaryColor,
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Bảo mật tài khoản của bạn với xác thực hai lớp (2FA)',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              '2FA thêm một lớp bảo mật bổ sung bằng cách yêu cầu mã xác thực từ thiết bị của bạn ngoài mật khẩu.',
              style: TextStyle(fontSize: 14, color: Colors.grey),
            ),
            const SizedBox(height: 24),

            if (_setupData == null) ...[
              // Step 1: Enter password
              const Text(
                'Bước 1: Xác nhận mật khẩu',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: _passwordController,
                decoration: const InputDecoration(
                  labelText: 'Mật khẩu hiện tại',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.lock),
                ),
                obscureText: true,
              ),
              const SizedBox(height: 16),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _isLoading ? null : _enableTwoFactorByEmail,
                  child: _isLoading
                      ? const CircularProgressIndicator()
                      : const Text('Bật 2FA bằng Email'),
                ),
              ),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton(
                  onPressed: _isLoading ? null : _enableTwoFactor,
                  child: const Text('Bật 2FA bằng Authenticator (cũ)'),
                ),
              ),
            ] else ...[
              if ((_setupData?['emailEnabled'] as bool? ?? false)) ...[
                const SizedBox(height: 8),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.green[50],
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.green[200]!),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: const [
                      Text('2FA bằng Email đã được bật cho tài khoản của bạn.', style: TextStyle(fontWeight: FontWeight.bold)),
                      SizedBox(height: 8),
                      Text('Khi đăng nhập, bạn sẽ nhận mã xác thực qua email. Hãy kiểm tra email để nhận mã.'),
                    ],
                  ),
                ),
              ] else ...[
                // Show authenticator instructions if user enabled authenticator flow
                const Text(
                  'Bước 2: Thiết lập ứng dụng Authenticator',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.grey[100],
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Column(
                    children: [
                      const Text(
                        '1. Tải ứng dụng Authenticator (Google Authenticator, Microsoft Authenticator, Authy,...)',
                        style: TextStyle(fontSize: 14),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        '2. Thêm tài khoản mới và chọn "Enter a setup key":',
                        style: TextStyle(fontSize: 14),
                      ),
                      const SizedBox(height: 16),
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          border: Border.all(color: Colors.grey),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Column(
                          children: [
                            const Text(
                              'Tên tài khoản: Homestay App',
                              style: TextStyle(fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Mã bí mật:',
                              style: TextStyle(fontSize: 14),
                            ),
                            const SizedBox(height: 4),
                            Container(
                              padding: const EdgeInsets.all(8),
                              color: Colors.grey[200],
                              child: const Text(
                                'JBSWY3DPEHPK3PXP', // This would be generated by backend
                                style: TextStyle(
                                  fontFamily: 'monospace',
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  letterSpacing: 2,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 24),

              // Step 3: Recovery codes
              const Text(
                'Bước 3: Lưu mã khôi phục',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Lưu các mã này ở nơi an toàn. Chúng có thể được sử dụng để khôi phục quyền truy cập nếu bạn mất thiết bị.',
                style: TextStyle(fontSize: 14, color: Colors.grey),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () => setState(() => _showRecoveryCodes = !_showRecoveryCodes),
                child: Text(_showRecoveryCodes ? 'Ẩn mã khôi phục' : 'Hiển thị mã khôi phục'),
              ),
              if (_showRecoveryCodes) ...[
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.yellow[50],
                    border: Border.all(color: Colors.yellow[300]!),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Mã khôi phục của bạn:',
                        style: TextStyle(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 8),
                      // Display recovery codes
                      ...(_setupData?['recoveryCodes'] as List<String>? ?? []).map(
                        (code) => Padding(
                          padding: const EdgeInsets.symmetric(vertical: 2),
                          child: Text(
                            code,
                            style: const TextStyle(
                              fontFamily: 'monospace',
                              fontSize: 16,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 24),

              // Complete setup
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _completeSetup,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.green,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                  ),
                  child: const Text('Hoàn thành thiết lập'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}