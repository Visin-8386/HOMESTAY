import 'package:google_sign_in/google_sign_in.dart';

class GoogleAuthService {
  /// If you want the Android client to request an idToken that is meant for
  /// your server (so server can verify audience), pass the serverClientId
  /// when calling signIn().
  GoogleSignIn _createClient({String? serverClientId}) {
    return GoogleSignIn(
      scopes: ['email', 'profile'],
      // On Android, requestIdToken is controlled by serverClientId
      clientId: serverClientId,
    );
  }

  Future<GoogleSignInAccount?> signIn({String? serverClientId}) async {
    final client = _createClient(serverClientId: serverClientId);
    try {
      final account = await client.signIn();
      return account;
    } catch (e) {
      rethrow;
    }
  }

  Future<void> signOut() async {
    // Sign-out from default client
    final client = _createClient();
    try {
      await client.signOut();
    } catch (e) {
      // ignore
    }
  }
}
