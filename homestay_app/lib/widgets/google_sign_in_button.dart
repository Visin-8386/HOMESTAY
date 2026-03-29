import 'package:flutter/material.dart';
import 'package:google_sign_in/google_sign_in.dart';

typedef OnGoogleSignIn = void Function(GoogleSignInAccount account);

class GoogleSignInButton extends StatelessWidget {
  final Future<GoogleSignInAccount?> Function() onSignIn;
  final Widget? label;

  const GoogleSignInButton({super.key, required this.onSignIn, this.label});

  @override
  Widget build(BuildContext context) {
    return ElevatedButton.icon(
      style: ElevatedButton.styleFrom(backgroundColor: Colors.white, foregroundColor: Colors.black),
      onPressed: () async {
        final account = await onSignIn();
        if (account != null) {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Signed in as ${account.email}')));
        }
      },
      icon: Image.asset('assets/icons/google_logo.png', width: 20, height: 20),
      label: label ?? const Text('Sign in with Google'),
    );
  }
}
