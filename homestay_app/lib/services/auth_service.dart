import '../config/api_config.dart';
import '../models/user.dart';
import 'api_service.dart';

class AuthService {
  final ApiService _apiService = ApiService();

  Future<Map<String, dynamic>> login(String email, String password) async {
    final response = await _apiService.post(
      ApiConfig.loginUrl,
      {
        'email': email,
        'password': password,
      },
      requireAuth: false,
    );

    // Backend returns: { "success": true, "data": { "token": "...", "refreshToken": "..." } }
    // Need to extract from 'data' field
    final data = response['data'];
    if (data != null && data['token'] != null && data['refreshToken'] != null) {
      // Check if 2FA is required
      if (data['token'] == '2FA_REQUIRED') {
        return {
          'success': true,
          'requiresTwoFactor': true,
          'user': data['user'],
          'message': 'Vui lòng nhập mã 2FA để tiếp tục',
        };
      }

      await _apiService.setTokens(
        data['token'],
        data['refreshToken'],
      );
    }

    return response;
  }

  /// Send OTP to email for login (server: /api/auth/send-otp)
  Future<Map<String, dynamic>> sendOtp(String email) async {
    try {
      final response = await _apiService.post(
        ApiConfig.sendOtpUrl,
        {'email': email},
        requireAuth: false,
      );

      return response;
    } catch (e) {
      return {'success': false, 'message': e.toString()};
    }
  }

  /// Verify OTP sent to email and obtain tokens (server: /api/auth/verify-otp)
  Future<Map<String, dynamic>> verifyOtp(
    String email,
    String code,
    bool rememberMachine,
  ) async {
    try {
      final response = await _apiService.post(
        ApiConfig.verifyOtpUrl,
        {
          'email': email,
          'code': code,
          'rememberMachine': rememberMachine,
        },
        requireAuth: false,
      );

      final data = response['data'];
      if (data != null && data['token'] != null && data['refreshToken'] != null) {
        await _apiService.setTokens(
          data['token'],
          data['refreshToken'],
        );
      }

      return response;
    } catch (e) {
      return {'success': false, 'message': e.toString()};
    }
  }

  Future<Map<String, dynamic>> register({
    required String userName,
    required String email,
    required String password,
    String? fullName,
    String? phoneNumber,
    String? role,
  }) async {
    // Split fullName into firstName and lastName if provided
    String? firstName;
    String? lastName;
    if (fullName != null && fullName.trim().isNotEmpty) {
      final parts = fullName.trim().split(RegExp(r"\s+"));
      firstName = parts.first;
      lastName = parts.length > 1 ? parts.sublist(1).join(' ') : '';
    }

    final body = {
      'userName': userName,
      'email': email,
      'password': password,
      'confirmPassword': password,
      'firstName': firstName ?? '',
      'lastName': lastName ?? '',
      'phoneNumber': phoneNumber,
    };
    if (role != null) body['role'] = role;

    return await _apiService.post(
      ApiConfig.registerUrl,
      body,
      requireAuth: false,
    );
  }

  Future<void> logout() async {
    try {
      await _apiService.post(ApiConfig.logoutUrl, {});
    } catch (e) {
      // Continue with local logout even if API call fails
    } finally {
      await _apiService.clearTokens();
    }
  }

  Future<User> getCurrentUser() async {
    final response = await _apiService.get(ApiConfig.profileUrl);
    // Backend returns: { "success": true, "data": {...user...} }
    final userData = response['data'] ?? response;
    return User.fromJson(userData);
  }

  Future<bool> isLoggedIn() async {
    await _apiService.loadTokens();
    return _apiService.isAuthenticated;
  }

  Future<Map<String, dynamic>> forgotPassword(String email) async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}/api/Account/ForgotPassword',
        {'email': email},
        requireAuth: false,
      );
      return {
        'success': true,
        'message': 'Email khôi phục đã được gửi',
        'data': response,
      };
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  Future<Map<String, dynamic>> resetPassword({
    required String email,
    required String token,
    required String newPassword,
  }) async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}/api/Account/ResetPassword',
        {
          'email': email,
          'token': token,
          'newPassword': newPassword,
        },
        requireAuth: false,
      );
      return {
        'success': true,
        'message': 'Đặt lại mật khẩu thành công',
        'data': response,
      };
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  // Two-Factor Authentication methods
  Future<Map<String, dynamic>> enableTwoFactor(String password) async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}${ApiConfig.authEndpoint}/2fa/enable',
        {'password': password},
      );
      return {
        'success': true,
        'message': '2FA đã được bật thành công',
        'data': response['data'],
      };
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  Future<Map<String, dynamic>> enableTwoFactorByEmail() async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}${ApiConfig.authEndpoint}/2fa/enable-email',
        {},
      );
      return {'success': true, 'message': response['message'] ?? 'Email 2FA enabled', 'data': response['data']};
    } catch (e) {
      return {'success': false, 'message': e.toString()};
    }
  }

  Future<Map<String, dynamic>> disableTwoFactor(String password) async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}${ApiConfig.authEndpoint}/2fa/disable',
        {'password': password},
      );
      return {
        'success': true,
        'message': '2FA đã được tắt thành công',
        'data': response,
      };
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  Future<Map<String, dynamic>> getTwoFactorStatus() async {
    try {
      final response = await _apiService.get(
        '${ApiConfig.baseUrl}${ApiConfig.authEndpoint}/2fa/status',
      );
      return {
        'success': true,
        'data': response['data'],
      };
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  Future<Map<String, dynamic>> loginWithTwoFactor(
    String email,
    String password,
    String twoFactorCode,
    bool rememberMachine,
  ) async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}${ApiConfig.authEndpoint}/login-2fa',
        {
          'email': email,
          'password': password,
          'twoFactorCode': twoFactorCode,
          'rememberMachine': rememberMachine,
        },
        requireAuth: false,
      );

      final data = response['data'];
      if (data != null && data['token'] != null && data['refreshToken'] != null) {
        await _apiService.setTokens(
          data['token'],
          data['refreshToken'],
        );
      }

      return response;
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  Future<Map<String, dynamic>> isTwoFactorEnabled() async {
    try {
      final response = await _apiService.get(
        '${ApiConfig.baseUrl}${ApiConfig.authEndpoint}/2fa/status',
      );
      return {
        'success': true,
        // Server may return TwoFactorEnabled (PascalCase) or twoFactorEnabled (camelCase)
        'enabled': response['data']['twoFactorEnabled'] ?? response['data']['TwoFactorEnabled'] ?? false,
      };
    } catch (e) {
      return {
        'success': false,
        'enabled': false,
        'message': e.toString(),
      };
    }
  }
}
