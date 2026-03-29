import 'package:flutter/material.dart';

/// Simple container that paints the app-wide user gradient background
/// and places [child] on top. Use this to make a consistent background
/// for all user-facing screens.
class UserGradientBackground extends StatelessWidget {
  final Widget child;
  final EdgeInsets? padding;

  const UserGradientBackground({Key? key, required this.child, this.padding}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          colors: [Color(0xFFFFE082), Color(0xFFF48FB1)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      child: Padding(
        padding: padding ?? EdgeInsets.zero,
        child: child,
      ),
    );
  }
}
