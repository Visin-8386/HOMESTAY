import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../config/api_config.dart';
import '../main_navigation.dart';
import '../providers/auth_provider_fixed.dart';
import '../services/api_service.dart';
import '../services/biometric_service.dart';
import 'auth/login_screen.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({Key? key}) : super(key: key);

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _fadeAnimation;
  late Animation<double> _scaleAnimation;
  int _currentImageIndex = 0;
  Timer? _imageTimer;

  final List<String> _backgroundImages = [
    '${ApiConfig.baseUrl}/images/Tudong/1.jpg',
    '${ApiConfig.baseUrl}/images/Tudong/2.jpg',
    '${ApiConfig.baseUrl}/images/Tudong/3.jpg',
    '${ApiConfig.baseUrl}/images/Tudong/4.jpg',
    '${ApiConfig.baseUrl}/images/Tudong/5.jpg',
    '${ApiConfig.baseUrl}/images/Tudong/6.jpg',
    '${ApiConfig.baseUrl}/images/Tudong/7.jpg',
  ];

  @override
  void initState() {
    super.initState();
    print('🎬 SplashScreen initState');
    
    _controller = AnimationController(
      duration: const Duration(milliseconds: 2000),
      vsync: this,
    );

    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeIn),
    );

    _scaleAnimation = Tween<double>(begin: 0.5, end: 1.0).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeOutBack),
    );

    print('▶️ Starting animation');
    _controller.forward();

    // Auto change background images
    _imageTimer = Timer.periodic(const Duration(seconds: 2), (timer) {
      if (mounted) {
        setState(() {
          _currentImageIndex = (_currentImageIndex + 1) % _backgroundImages.length;
        });
      }
    });

    // Precache logo and first few background images to reduce flicker
    WidgetsBinding.instance.addPostFrameCallback((_) {
      for (var i = 0; i < _backgroundImages.length && i < 3; i++) {
        precacheImage(NetworkImage(_backgroundImages[i]), context);
      }
      precacheImage(NetworkImage('${ApiConfig.baseUrl}/images/admin/logo.png'), context);
    });

    // Start navigate logic after first frame to avoid setState/notifyListeners while building
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _navigateToNext();
    });
  }

  Future<void> _navigateToNext() async {
    if (!mounted) return;

    final authProvider = Provider.of<AuthProvider>(context, listen: false);

    // Fast-path: check local tokens first so the app feels instant on startup.
    // If tokens exist, navigate to main immediately and refresh user in background.
    final api = ApiService();
    await api.loadTokens();

    if (api.isAuthenticated) {
      // Navigate to main immediately to provide snappy startup.
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
      );

      // Refresh user/profile in background. If refresh fails, AuthProvider will
      // handle clearing session as needed.
      // ignore: unawaited_futures
      authProvider.refreshUser();
      return;
    }

    // No local token: fall back to the previous flow which tries to validate
    // session/server and biometric login.
    // Start auth check (network-backed) but keep biometric/login fallback as before.
    try {
      await authProvider.checkAuthStatus();
      if (!mounted) return;
      if (authProvider.isAuthenticated) {
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
        );
        return;
      }
    } catch (_) {
      // ignore and continue to biometric/login
    }

    // Try biometric auto-login when no active session
    try {
      final biometric = BiometricService();
      final enabled = await biometric.isBiometricEnabled();
      if (enabled) {
        final ok = await biometric.authenticate(reason: 'Xác thực để đăng nhập tự động');
        if (ok) {
          final creds = await biometric.getSavedCredentials();
          if (creds != null) {
            final result = await authProvider.login(creds['email']!, creds['password']!);
            if (result['success'] == true) {
              if (!mounted) return;
              Navigator.of(context).pushReplacement(
                MaterialPageRoute(builder: (_) => const MainNavigationScreen()),
              );
              return;
            }
          }
        }
      }
    } catch (e) {
      // ignore biometric failures and continue to login screen
    }

    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (_) => const LoginScreen()),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    _imageTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    print('🎨 SplashScreen building... Image index: $_currentImageIndex');
    return Scaffold(
      backgroundColor: const Color(0xFF667eea), // Fallback color
      body: Stack(
        children: [
          // Animated background images
          AnimatedSwitcher(
            duration: const Duration(milliseconds: 1000),
            child: Container(
              key: ValueKey<int>(_currentImageIndex),
              decoration: BoxDecoration(
                image: DecorationImage(
                  image: NetworkImage(_backgroundImages[_currentImageIndex]),
                  fit: BoxFit.cover,
                ),
              ),
            ),
          ),
          
          // Gradient overlay
          Container(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  const Color(0xFF667eea).withOpacity(0.7),
                  const Color(0xFF764ba2).withOpacity(0.7),
                ],
              ),
            ),
          ),
          
          // Content
          Center(
            child: FadeTransition(
              opacity: _fadeAnimation,
              child: ScaleTransition(
                scale: _scaleAnimation,
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    // Logo
                    Container(
                      width: 150,
                      height: 150,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.3),
                            blurRadius: 20,
                            offset: const Offset(0, 10),
                          ),
                        ],
                      ),
                      padding: const EdgeInsets.all(15),
                      child: Image.network(
                        '${ApiConfig.baseUrl}/images/admin/logo.png',
                        errorBuilder: (context, error, stackTrace) {
                          return const Icon(
                            Icons.home,
                            size: 80,
                            color: Color(0xFF667eea),
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: 40),
                    
                    // App Name
                    const Text(
                      'Homestay',
                      style: TextStyle(
                        fontSize: 48,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                        letterSpacing: 2,
                      ),
                    ),
                    const SizedBox(height: 10),
                    const Text(
                      'Đom Đóm Dream',
                      style: TextStyle(
                        fontSize: 24,
                        color: Colors.white70,
                        fontWeight: FontWeight.w300,
                      ),
                    ),
                    const SizedBox(height: 50),
                    
                    // Loading indicator
                    const CircularProgressIndicator(
                      valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                    ),
                    const SizedBox(height: 20),
                    const Text(
                      'Đang tải...',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 16,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
