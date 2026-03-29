import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';

import '../../config/api_config.dart';
import '../../main_navigation.dart';
import '../../providers/auth_provider_fixed.dart';
import '../../services/api_service.dart';
import '../../services/biometric_service.dart';
import '../../services/google_auth_service.dart';
import '../../services/storage_service.dart';
import 'forgot_password_screen.dart';
import 'register_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;
  bool _obscurePassword = true;
  final BiometricService _biometricService = BiometricService();
  bool _biometricAvailable = false;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    _checkBiometric();
  }

  Future<void> _checkBiometric() async {
    final ok = await _biometricService.canCheckBiometrics();
    if (!mounted) return;
    setState(() => _biometricAvailable = ok);
  }

  Future<void> _askEnableBiometric() async {
    if (!_biometricAvailable) return;
    final isEnabled = await _biometricService.isBiometricEnabled();
    if (isEnabled) return;

    if (!mounted) return;

    final email = _emailController.text.trim();
    if (email.isEmpty) return;

    final promptKey = 'biometric_prompt_shown::$email';
    final alreadyAsked = (await StorageService().read(promptKey)) == 'true';
    if (alreadyAsked) return;

    await StorageService().write(promptKey, 'true');

    if (!mounted) return;

    final result = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Bật đăng nhập sinh trắc học?'),
        content: const Text(
          'Bạn có muốn sử dụng vân tay hoặc khuôn mặt để đăng nhập lần sau không?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Không'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Bật'),
          ),
        ],
      ),
    );

    if (result == true) {
      await _biometricService.enableBiometric(
        email,
        _passwordController.text,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đã bật đăng nhập sinh trắc học!'),
            backgroundColor: Colors.green,
          ),
        );
      }
    }
  }

  Future<void> _biometricLogin() async {
    if (_isLoading) return;
    setState(() => _isLoading = true);
    try {
      final ok = await _biometricService.authenticate(reason: 'Xác thực để đăng nhập');
      if (!ok) {
        if (mounted) {
          String? lastError;
          String? lastStack;
          try {
            lastError = await StorageService().read('biometric_last_error');
            lastStack = await StorageService().read('biometric_last_stack');
          } catch (_) {
            lastError = null;
            lastStack = null;
          }

          if ((lastError ?? '').isNotEmpty) {
            if (!mounted) return;
            await showDialog<void>(
              context: context,
              builder: (context) => AlertDialog(
                title: const Text('Xác thực thất bại'),
                content: SingleChildScrollView(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (lastError != null) Text('Lỗi: $lastError'),
                      const SizedBox(height: 12),
                      if (lastStack != null) 
                        Text('Chi tiết: ${lastStack.split('\n').take(5).join('\n')}'),
                      const SizedBox(height: 12),
                      const Text(
                        'Vui lòng kiểm tra thiết bị có vân tay/khuôn mặt đã được thiết lập.',
                        style: TextStyle(fontSize: 13),
                      ),
                    ],
                  ),
                ),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(context),
                    child: const Text('Đóng'),
                  ),
                ],
              ),
            );
          } else {
            if (!mounted) return;
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(
                content: Text('Xác thực thất bại'),
                backgroundColor: Colors.red,
              ),
            );
          }
        }
        return;
      }

      final credentials = await _biometricService.getSavedCredentials();
      if (credentials == null) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text(
                'Không tìm thấy thông tin đăng nhập. Vui lòng đăng nhập bằng email/mật khẩu.',
              ),
              backgroundColor: Colors.orange,
              duration: Duration(seconds: 3),
            ),
          );
        }
        return;
      }

      final authProvider = Provider.of<AuthProvider>(context, listen: false);
      final success = await authProvider.login(
        credentials['email']!,
        credentials['password']!,
      );
      if (!mounted) return;

      if (success['success'] == true) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đăng nhập thành công!'),
            backgroundColor: Colors.green,
          ),
        );
        if (!mounted) return;
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
        );
      } else {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đăng nhập thất bại. Vui lòng thử lại.'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _login() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isLoading = true);

    try {
      final authProvider = Provider.of<AuthProvider>(context, listen: false);
      final result = await authProvider.login(
        _emailController.text.trim(),
        _passwordController.text,
      );

      if (!mounted) return;

      if (result['success'] == true) {
        if (result['requiresTwoFactor'] == true) {
          if (mounted) {
            final otpSent = result['otpSent'] == true;
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(otpSent
                    ? 'Mã OTP đã được gửi tới email của bạn.'
                    : 'Vui lòng nhập mã từ ứng dụng Authenticator.'),
                duration: const Duration(seconds: 3),
              ),
            );
            await _showTwoFactorDialog(result['user'], otpSent);
          }
          return;
        }

        if (mounted) {
          await _askEnableBiometric();
          
          if (mounted) {
            Navigator.of(context).pushReplacement(
              MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
            );
          }
        }
      } else {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(result['message'] ?? 'Đăng nhập thất bại')),
          );
        }
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

  Future<void> _googleLogin() async {
    if (!mounted) return;
    setState(() => _isLoading = true);
    try {
      final serverClientId = ApiConfig.googleServerClientId;
      final account = await GoogleAuthService().signIn(
        serverClientId: serverClientId.isNotEmpty ? serverClientId : null,
      );
      if (account == null) return;

      final auth = await account.authentication;
      final idToken = auth.idToken;
      if (idToken == null) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Không lấy được token từ Google')),
          );
        }
        return;
      }

      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/auth/google'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'idToken': idToken}),
      );

      if (response.statusCode == 200) {
        final body = jsonDecode(response.body) as Map<String, dynamic>;
        final data = body['data'];
        if (data != null && data['token'] != null && data['refreshToken'] != null) {
          await ApiService().setTokens(data['token'], data['refreshToken']);
          if (!mounted) return;
          final authProvider = Provider.of<AuthProvider>(context, listen: false);
          await authProvider.refreshUser();
          if (mounted) {
            Navigator.of(context).pushReplacement(
              MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
            );
          }
          return;
        }
      }

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Đăng nhập bằng Google thất bại: ${response.body}'),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi Google Sign-In: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _showTwoFactorDialog(Map<String, dynamic> user, bool otpSent) async {
    final twoFactorCodeController = TextEditingController();

    await showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) {
          bool rememberMachine = false;
          
          return AlertDialog(
            title: const Text('Xác thực hai lớp'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  otpSent
                      ? 'Mã đã được gửi tới email của bạn. Bạn cũng có thể nhập mã từ ứng dụng Authenticator.'
                      : 'Vui lòng nhập mã xác thực từ ứng dụng Authenticator hoặc email.',
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontSize: 14),
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: twoFactorCodeController,
                  decoration: const InputDecoration(
                    labelText: 'Mã 2FA',
                    hintText: 'Nhập 6 chữ số',
                    border: OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.number,
                  maxLength: 6,
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Checkbox(
                      value: rememberMachine,
                      onChanged: (value) {
                        setDialogState(() {
                          rememberMachine = value ?? false;
                        });
                      },
                    ),
                    const Expanded(
                      child: Text(
                        'Ghi nhớ thiết bị này trong 30 ngày',
                        style: TextStyle(fontSize: 12),
                      ),
                    ),
                  ],
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Hủy'),
              ),
              ElevatedButton(
                onPressed: () async {
                  final code = twoFactorCodeController.text.trim();
                  if (code.isEmpty || code.length != 6) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Vui lòng nhập mã 6 chữ số')),
                    );
                    return;
                  }

                  final authProvider = Provider.of<AuthProvider>(context, listen: false);
                  final email = (user['email'] as String?) ?? _emailController.text.trim();

                  Map<String, dynamic> result;
                  if (otpSent) {
                    result = await authProvider.loginWithOtp(
                      email,
                      code,
                      rememberMachine,
                    );
                  } else {
                    result = await authProvider.loginWithTwoFactor(
                      email,
                      _passwordController.text,
                      code,
                      rememberMachine,
                    );
                  }

                  if (!mounted) return;

                  if (result['success'] == true) {
                    // Use dialog context to pop the dialog synchronously
                    Navigator.of(context).pop();

                    // Capture the state context for post-await navigation
                    final parentContext = this.context;
                    await _askEnableBiometric();
                    if (!mounted) return;
                    Navigator.of(parentContext).pushReplacement(
                      MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
                    );
                  } else {
                    if (!mounted) return;
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text(result['message'] ?? 'Mã 2FA không đúng'),
                      ),
                    );
                  }
                },
                child: const Text('Xác nhận'),
              ),
            ],
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: LayoutBuilder(
        builder: (context, constraints) {
          final width = constraints.maxWidth;
          final isSmall = width < 400;
          final isTablet = width > 600;
          
          // Responsive sizes
          final logoSize = isSmall ? 80.0 : isTablet ? 120.0 : 100.0;
          final titleFont = isSmall ? 26.0 : isTablet ? 38.0 : 32.0;
          final subtitleFont = isSmall ? 13.0 : isTablet ? 18.0 : 15.0;
          final fieldFont = isSmall ? 14.0 : isTablet ? 18.0 : 16.0;
          final buttonFont = isSmall ? 15.0 : isTablet ? 20.0 : 17.0;
          final contentPadding = isSmall ? 16.0 : isTablet ? 32.0 : 24.0;
          final verticalSpace = isSmall ? 16.0 : isTablet ? 28.0 : 22.0;
          final buttonHeight = isSmall ? 46.0 : isTablet ? 56.0 : 52.0;
          
          return Stack(
            children: [
              // Background gradient
              Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [Color(0xFF667eea), Color(0xFF764ba2)],
                  ),
                ),
              ),
              SafeArea(
                child: Center(
                  child: SingleChildScrollView(
                    padding: EdgeInsets.all(contentPadding),
                    child: ConstrainedBox(
                      constraints: BoxConstraints(
                        maxWidth: isTablet ? 440 : double.infinity,
                      ),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            // Logo
                            Container(
                              width: logoSize,
                              height: logoSize,
                              decoration: BoxDecoration(
                                color: Colors.white,
                                shape: BoxShape.circle,
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black.withOpacity(0.2),
                                    blurRadius: 20,
                                    offset: const Offset(0, 10),
                                  ),
                                ],
                              ),
                              child: ClipOval(
                                child: Image.network(
                                  '${ApiConfig.baseUrl}/images/admin/logo.png',
                                  width: logoSize,
                                  height: logoSize,
                                  fit: BoxFit.cover,
                                  errorBuilder: (context, error, stackTrace) {
                                    return Icon(
                                      Icons.home,
                                      size: logoSize * 0.5,
                                      color: const Color(0xFF667eea),
                                    );
                                  },
                                ),
                              ),
                            ),
                            SizedBox(height: verticalSpace),
                            
                            // Title
                            Text(
                              'Đăng Nhập',
                              style: TextStyle(
                                fontSize: titleFont,
                                fontWeight: FontWeight.bold,
                                color: Colors.white,
                              ),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              'Chào mừng bạn trở lại!',
                              style: TextStyle(
                                fontSize: subtitleFont,
                                color: Colors.white70,
                              ),
                            ),
                            SizedBox(height: verticalSpace + 8),
                            
                            // Email field
                            _buildTextField(
                              controller: _emailController,
                              label: 'Email',
                              icon: Icons.email_outlined,
                              keyboardType: TextInputType.emailAddress,
                              autofillHints: const [AutofillHints.email],
                              textInputAction: TextInputAction.next,
                              fontSize: fieldFont,
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Vui lòng nhập email';
                                }
                                if (!value.contains('@')) {
                                  return 'Email không hợp lệ';
                                }
                                return null;
                              },
                            ),
                            SizedBox(height: isSmall ? 14 : 18),
                            
                            // Password field
                            _buildTextField(
                              controller: _passwordController,
                              label: 'Mật khẩu',
                              icon: Icons.lock_outline,
                              obscureText: _obscurePassword,
                              autofillHints: const [AutofillHints.password],
                              textInputAction: TextInputAction.done,
                              fontSize: fieldFont,
                              suffixIcon: IconButton(
                                icon: Icon(
                                  _obscurePassword
                                      ? Icons.visibility_off_outlined
                                      : Icons.visibility_outlined,
                                ),
                                onPressed: () {
                                  setState(() => _obscurePassword = !_obscurePassword);
                                },
                              ),
                              validator: (value) {
                                if (value == null || value.isEmpty) {
                                  return 'Vui lòng nhập mật khẩu';
                                }
                                if (value.length < 6) {
                                  return 'Mật khẩu phải có ít nhất 6 ký tự';
                                }
                                return null;
                              },
                            ),
                            
                            // Forgot password
                            Align(
                              alignment: Alignment.centerRight,
                              child: TextButton(
                                onPressed: () {
                                  Navigator.of(context).push(
                                    MaterialPageRoute(
                                      builder: (_) => const ForgotPasswordScreen(),
                                    ),
                                  );
                                },
                                child: Text(
                                  'Quên mật khẩu?',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontSize: isSmall ? 12 : 14,
                                  ),
                                ),
                              ),
                            ),
                            SizedBox(height: isSmall ? 8 : 12),
                            
                            // Login button
                            SizedBox(
                              width: double.infinity,
                              height: buttonHeight,
                              child: ElevatedButton(
                                onPressed: _isLoading ? null : _login,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: Colors.white,
                                  foregroundColor: const Color(0xFF667eea),
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  elevation: 5,
                                ),
                                child: _isLoading
                                    ? SizedBox(
                                        width: 22,
                                        height: 22,
                                        child: const CircularProgressIndicator(
                                          strokeWidth: 2.5,
                                        ),
                                      )
                                    : Text(
                                        'Đăng Nhập',
                                        style: TextStyle(
                                          fontSize: buttonFont,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                              ),
                            ),
                            SizedBox(height: verticalSpace),
                            
                            // Divider
                            Row(
                              children: [
                                Expanded(child: Divider(color: Colors.white.withOpacity(0.3))),
                                Padding(
                                  padding: const EdgeInsets.symmetric(horizontal: 12),
                                  child: Text(
                                    'hoặc',
                                    style: TextStyle(
                                      color: Colors.white70,
                                      fontSize: isSmall ? 12 : 14,
                                    ),
                                  ),
                                ),
                                Expanded(child: Divider(color: Colors.white.withOpacity(0.3))),
                              ],
                            ),
                            SizedBox(height: verticalSpace),
                            
                            // Google button
                            SizedBox(
                              width: double.infinity,
                              height: buttonHeight,
                              child: ElevatedButton(
                                onPressed: _isLoading ? null : () => _googleLogin(),
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: Colors.white,
                                  foregroundColor: Colors.black87,
                                  shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  elevation: 3,
                                ),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Image.asset(
                                      'assets/icons/google_logo.png',
                                      width: 20,
                                      height: 20,
                                      errorBuilder: (context, error, stackTrace) {
                                        return const Icon(
                                          Icons.g_mobiledata,
                                          size: 24,
                                          color: Colors.red,
                                        );
                                      },
                                    ),
                                    SizedBox(width: isSmall ? 8 : 12),
                                    Text(
                                      'Google',
                                      style: TextStyle(
                                        fontSize: buttonFont,
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            
                            // Biometric button (if available)
                            if (_biometricAvailable) ...[
                              SizedBox(height: isSmall ? 10 : 14),
                              SizedBox(
                                width: double.infinity,
                                height: buttonHeight,
                                child: OutlinedButton(
                                  onPressed: _isLoading ? null : _biometricLogin,
                                  style: OutlinedButton.styleFrom(
                                    backgroundColor: Colors.white.withOpacity(0.1),
                                    foregroundColor: Colors.white,
                                    side: const BorderSide(color: Colors.white70, width: 1.5),
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(12),
                                    ),
                                  ),
                                  child: Row(
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: [
                                      Icon(
                                        Icons.fingerprint,
                                        size: isSmall ? 20 : 24,
                                      ),
                                      SizedBox(width: isSmall ? 8 : 10),
                                      Flexible(
                                        child: Text(
                                          isSmall ? 'Sinh trắc học' : 'Đăng nhập sinh trắc học',
                                          style: TextStyle(
                                            fontSize: buttonFont,
                                            fontWeight: FontWeight.w600,
                                          ),
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                            
                            SizedBox(height: verticalSpace),
                            
                            // Register link - question line above, button on its own line
                            Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  'Chưa có tài khoản?',
                                  style: TextStyle(
                                    color: Colors.white70,
                                    fontSize: isSmall ? 13 : 14,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                TextButton(
                                  onPressed: () {
                                    Navigator.of(context).push(
                                      MaterialPageRoute(
                                        builder: (_) => const RegisterScreen(),
                                      ),
                                    );
                                  },
                                  style: TextButton.styleFrom(
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 8,
                                      vertical: 4,
                                    ),
                                  ),
                                  child: Text(
                                    'Đăng ký ngay',
                                    style: TextStyle(
                                      color: Colors.white,
                                      fontWeight: FontWeight.bold,
                                      fontSize: isSmall ? 13 : 15,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required IconData icon,
    bool obscureText = false,
    TextInputType? keyboardType,
    List<String>? autofillHints,
    TextInputAction? textInputAction,
    required double fontSize,
    Widget? suffixIcon,
    String? Function(String?)? validator,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 10,
            offset: const Offset(0, 5),
          ),
        ],
      ),
      child: TextFormField(
        controller: controller,
        obscureText: obscureText,
        keyboardType: keyboardType,
        autofillHints: autofillHints,
        textInputAction: textInputAction,
        style: TextStyle(fontSize: fontSize),
        decoration: InputDecoration(
          labelText: label,
          labelStyle: TextStyle(fontSize: fontSize),
          prefixIcon: Icon(icon),
          suffixIcon: suffixIcon,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide.none,
          ),
          filled: true,
          fillColor: Colors.white,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 16,
          ),
        ),
        validator: validator,
      ),
    );
  }
}