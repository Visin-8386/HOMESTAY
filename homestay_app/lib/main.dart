import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:provider/provider.dart';

import 'models/user.dart';
import 'providers/auth_provider_fixed.dart';
import 'providers/booking_provider.dart';
import 'providers/comparison_provider.dart';
import 'providers/favorites_provider.dart';
import 'providers/homestay_provider.dart';
import 'providers/locale_provider.dart';
import 'providers/theme_provider.dart';
import 'routes.dart';
import 'screens/call/call_screen.dart';
import 'services/call_service.dart';

final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  try {
    // Load environment variables from .env file
    await dotenv.load(fileName: ".env");
    print('✅ .env file loaded successfully');
  } catch (e) {
    print('⚠️ Error loading .env file: $e');
  }

  // In debug builds we may need to accept self-signed / dev certificates.
  // This sets a global HttpOverrides that allows bad certificates only when
  // running in debug mode. DO NOT enable this in production.
  if (kDebugMode) {
    HttpOverrides.global = _DebugHttpOverrides();
  }

  // Attempt to connect SignalR hub early so incoming call notifications can be received.
  // This will silently fail if user isn't authenticated yet.
  try {
    await CallService().connectHub();
  // Register a global incoming call handler so the app can show incoming UI
  CallService().onIncomingCall = (String callId, CallType type, User caller) {
      // Push CallScreen as incoming using navigatorKey if available
      try {
        navigatorKey.currentState?.push(
          MaterialPageRoute(
            builder: (_) => CallScreen(
              callId: callId,
              callType: type,
              remoteUser: caller,
              isIncoming: true,
            ),
          ),
        );
      } catch (_) {}
    };
  } catch (_) {}

  runApp(const MyApp());
}

class _DebugHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    final client = super.createHttpClient(context);
    client.badCertificateCallback = (X509Certificate cert, String host, int port) => true;
    return client;
  }
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    print('🚀 MyApp building...');
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => BookingProvider()),
        ChangeNotifierProvider(create: (_) => HomestayProvider()),
  ChangeNotifierProvider(create: (_) => FavoritesProvider()),
        ChangeNotifierProvider(create: (_) => LocaleProvider()),
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
        ChangeNotifierProvider(create: (_) => ComparisonProvider()),
      ],
      child: MaterialApp(
        title: 'Homestay - Đom Đóm Dream',
        navigatorKey: navigatorKey,
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          primarySwatch: Colors.blue,
          primaryColor: const Color(0xFF667eea),
          colorScheme: ColorScheme.fromSeed(
            seedColor: const Color(0xFF667eea),
          ),
          useMaterial3: true,
        ),
        initialRoute: AppRoutes.splash,
        routes: AppRoutes.getRoutes(),
        onGenerateRoute: AppRoutes.onGenerateRoute,
      ),
    );
  }
}
