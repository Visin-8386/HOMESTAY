import 'package:local_auth/local_auth.dart';

import 'storage_service.dart';

class BiometricService {
  final LocalAuthentication _auth = LocalAuthentication();
  final StorageService _storage = StorageService();

  // Keys for biometric settings
  static const String _keyBiometricEnabled = 'biometric_enabled';
  static const String _keyBiometricEmail = 'biometric_email';
  static const String _keyBiometricPassword = 'biometric_password';

  /// Returns true if the device supports biometrics (fingerprint/face) or device credentials.
  Future<bool> canCheckBiometrics() async {
    try {
      return await _auth.canCheckBiometrics || await _auth.isDeviceSupported();
    } catch (e) {
      return false;
    }
  }

  /// Returns list of available biometric types (e.g. fingerprint, face).
  Future<List<BiometricType>> getAvailableBiometrics() async {
    try {
      return await _auth.getAvailableBiometrics();
    } catch (e) {
      return <BiometricType>[];
    }
  }

  /// Prompt the system biometric dialog. Returns true if authenticated.
  Future<bool> authenticate({String reason = 'Authenticate'}) async {
    try {
      final didAuthenticate = await _auth.authenticate(
        localizedReason: reason,
        options: const AuthenticationOptions(
          biometricOnly: false,
          stickyAuth: false,
        ),
      );
      return didAuthenticate;
    } catch (e, st) {
      // Save last error to secure storage for diagnostics (do not leak to UI)
      try {
        await _storage.write('biometric_last_error', e.toString());
        await _storage.write('biometric_last_stack', st.toString());
      } catch (_) {}
      return false;
    }
  }

  /// Check if biometric login is enabled
  Future<bool> isBiometricEnabled() async {
    final value = await _storage.read(_keyBiometricEnabled);
    return value == 'true';
  }

  /// Enable biometric login and save credentials securely
  Future<void> enableBiometric(String email, String password) async {
    await _storage.write(_keyBiometricEnabled, 'true');
    await _storage.write(_keyBiometricEmail, email);
    await _storage.write(_keyBiometricPassword, password);
  }

  /// Disable biometric login and clear saved credentials
  Future<void> disableBiometric() async {
    await _storage.delete(_keyBiometricEnabled);
    await _storage.delete(_keyBiometricEmail);
    await _storage.delete(_keyBiometricPassword);
  }

  /// Get saved credentials (only if biometric is enabled)
  Future<Map<String, String>?> getSavedCredentials() async {
    final enabled = await isBiometricEnabled();
    if (!enabled) return null;

    final email = await _storage.read(_keyBiometricEmail);
    final password = await _storage.read(_keyBiometricPassword);

    if (email == null || password == null) return null;

    return {
      'email': email,
      'password': password,
    };
  }
}
