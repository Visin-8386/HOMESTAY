import 'package:flutter/material.dart';

import '../models/user.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/biometric_service.dart';
import '../services/call_service.dart';
import '../services/storage_service.dart';

class AuthProvider with ChangeNotifier {
  final AuthService _authService = AuthService();

  User? _currentUser;
  bool _isLoading = false;
  String? _error;

  User? get currentUser => _currentUser;
  User? get user => _currentUser; // Alias for compatibility
  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get isAuthenticated => _currentUser != null;
  bool get isHost => _currentUser?.isHost ?? false;
  bool get isAdmin => _currentUser?.isAdmin ?? false;

  Future<void> checkAuthStatus() async {
    _isLoading = true;
    notifyListeners();

    try {
      final isLoggedIn = await _authService.isLoggedIn();
      if (isLoggedIn) {
        _currentUser = await _authService.getCurrentUser();
        if (_currentUser != null) {
          final storage = StorageService();
          await storage.saveUserId(_currentUser!.id);
          await storage.saveUserEmail(_currentUser!.email);
          await storage.saveUserName(_currentUser!.userName);
          // ensure SignalR hub is connected for this user
          try {
            await CallService().connectHub();
          } catch (e) {
            // ignore but log
            // ignore: avoid_print
            print('[AuthProvider] connectHub after login failed: $e');
          }
        }
      } else {
        // Try to refresh tokens (if refresh token present)
        final api = ApiService();
        final refreshed = await api.refreshAccessToken();
        if (refreshed) {
          _currentUser = await _authService.getCurrentUser();
          if (_currentUser != null) {
            final storage = StorageService();
            await storage.saveUserId(_currentUser!.id);
            await storage.saveUserEmail(_currentUser!.email);
            await storage.saveUserName(_currentUser!.userName);
          }
        }
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<Map<String, dynamic>> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _authService.login(email, password);

      if (result['success'] == true) {
        if (result['requiresTwoFactor'] == true) {
          // Send OTP via email (server should expose send-otp). Prefer server-provided user email
          final userMap = result['user'] as Map<String, dynamic>?;
          final userEmail = userMap != null && userMap['email'] != null ? userMap['email'].toString() : email;
          try {
            final sendResult = await _authService.sendOtp(userEmail);
            // Debug: print sendResult so developer can see server response in logs
            // ignore: avoid_print
            print('sendOtp result: $sendResult');
            // include a hint that otp was sent so UI can adapt
            result['otpSent'] = (sendResult['success'] == true);
            result['otpDelivery'] = 'email';
          } catch (e) {
            // ignore: avoid_print
            print('sendOtp failed: $e');
            result['otpSent'] = false;
          }

          _isLoading = false;
          notifyListeners();
          return result; // 2FA required
        }

        _currentUser = await _authService.getCurrentUser();
        if (_currentUser != null) {
          final storage = StorageService();
          await storage.saveUserId(_currentUser!.id);
          await storage.saveUserEmail(_currentUser!.email);
          await storage.saveUserName(_currentUser!.userName);
          try {
            await CallService().connectHub();
          } catch (e) {
            // ignore: avoid_print
            print('[AuthProvider] connectHub after 2FA login failed: $e');
          }
        }
      }

      _isLoading = false;
      notifyListeners();
      return result;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return {'success': false, 'message': _error};
    }
  }

  Future<Map<String, dynamic>> loginWithTwoFactor(
    String email,
    String password,
    String twoFactorCode,
    bool rememberMachine,
  ) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _authService.loginWithTwoFactor(
        email,
        password,
        twoFactorCode,
        rememberMachine,
      );

      if (result['success'] == true) {
        _currentUser = await _authService.getCurrentUser();
        if (_currentUser != null) {
          final storage = StorageService();
          await storage.saveUserId(_currentUser!.id);
          await storage.saveUserEmail(_currentUser!.email);
          await storage.saveUserName(_currentUser!.userName);
          try {
            await CallService().connectHub();
          } catch (e) {
            // ignore: avoid_print
            print('[AuthProvider] connectHub after OTP login failed: $e');
          }
        }
      }

      _isLoading = false;
      notifyListeners();
      return result;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return {'success': false, 'message': _error};
    }
  }

  /// Verify email OTP (server /verify-otp) and finalize login
  Future<Map<String, dynamic>> loginWithOtp(
    String email,
    String code,
    bool rememberMachine,
  ) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _authService.verifyOtp(email, code, rememberMachine);

      if (result['success'] == true) {
        _currentUser = await _authService.getCurrentUser();
        if (_currentUser != null) {
          final storage = StorageService();
          await storage.saveUserId(_currentUser!.id);
          await storage.saveUserEmail(_currentUser!.email);
          await storage.saveUserName(_currentUser!.userName);
          try {
            await CallService().connectHub();
          } catch (e) {
            // ignore: avoid_print
            print('[AuthProvider] connectHub after OTP login failed: $e');
          }
        }
      }

      _isLoading = false;
      notifyListeners();
      return result;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return {'success': false, 'message': _error};
    }
  }

  Future<bool> register({
    required String userName,
    required String email,
    required String password,
    String? fullName,
    String? phoneNumber,
    String? role,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _authService.register(
        userName: userName,
        email: email,
        password: password,
        fullName: fullName,
        phoneNumber: phoneNumber,
        role: role,
      );

      // Auto login after registration
      await login(email, password);
      return true;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    await _authService.logout();
    // Clear local session-related storage but keep biometric credentials
    await StorageService().clearSession();
    _currentUser = null;
    notifyListeners();
  }

  Future<void> refreshUser() async {
    try {
      _currentUser = await _authService.getCurrentUser();
      if (_currentUser != null) {
        final storage = StorageService();
        await storage.saveUserId(_currentUser!.id);
        await storage.saveUserEmail(_currentUser!.email);
        await storage.saveUserName(_currentUser!.userName);
      }
      notifyListeners();
    } catch (e) {
      _error = e.toString();
    }
  }

  /// Attempt biometric login using saved credentials.
  /// Returns true if login succeeds and user is set.
  Future<bool> tryBiometricLogin({String reason = 'Authenticate to sign in'}) async {
    final bio = BiometricService();
    try {
      final enabled = await bio.isBiometricEnabled();
      if (!enabled) return false;

      final authenticated = await bio.authenticate(reason: reason);
      if (!authenticated) return false;

      final creds = await bio.getSavedCredentials();
      if (creds == null) return false;

  final email = creds['email'];
  final password = creds['password'];
      if (email == null || password == null) return false;

      final result = await _authService.login(email, password);
      if (result['success'] == true) {
        _currentUser = await _authService.getCurrentUser();
        // Persist user info
        if (_currentUser != null) {
          final storage = StorageService();
          await storage.saveUserId(_currentUser!.id);
          await storage.saveUserEmail(_currentUser!.email);
          await storage.saveUserName(_currentUser!.userName);
          try {
            await CallService().connectHub();
          } catch (e) {
            // ignore: avoid_print
            print('[AuthProvider] connectHub after biometric login failed: $e');
          }
        }
        notifyListeners();
        return true;
      }
    } catch (_) {
      // ignore and return false
    }
    return false;
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
