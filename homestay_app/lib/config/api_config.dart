import 'package:flutter_dotenv/flutter_dotenv.dart';

class ApiConfig {
  // IMPORTANT: Update API_BASE_URL in .env file daily as Conveyor URL changes
  // Default to localhost for development when .env not set.
  // For Android emulator use 10.0.2.2 to reach host machine. Server listens on HTTP port 5189 and HTTPS 7097 by default.
  static String get baseUrl => dotenv.env['API_BASE_URL'] ?? 'http://10.0.2.2:5189';
  
  // SignalR Hub URL
  static String get signalRHubUrl => '$baseUrl/chatHub';
  
  // API Endpoints
  static const String authEndpoint = '/api/auth';
  static const String homestaysEndpoint = '/api/homestays';
  static const String bookingsEndpoint = '/api/bookings';
  static const String userEndpoint = '/api/user';
  static const String reviewsEndpoint = '/api/reviews';
  static const String paymentsEndpoint = '/api/payments';
  static const String amenitiesEndpoint = '/api/amenities';
  static const String notificationsEndpoint = '/api/notifications';
  
  // Auth URLs
  static String get loginUrl => '$baseUrl$authEndpoint/login';
  static String get registerUrl => '$baseUrl$authEndpoint/register';
  static String get refreshTokenUrl => '$baseUrl$authEndpoint/refresh-token';
  static String get logoutUrl => '$baseUrl$authEndpoint/logout';
  static String get sendOtpUrl => '$baseUrl$authEndpoint/send-otp';
  static String get verifyOtpUrl => '$baseUrl$authEndpoint/verify-otp';
  
  // Homestay URLs
  static String get homestaysUrl => '$baseUrl$homestaysEndpoint';
  static String get searchHomestaysUrl => '$baseUrl$homestaysEndpoint'; // Backend uses GET /api/homestays with query params
  static String get myHomestaysUrl => '$baseUrl$homestaysEndpoint/my-homestays';
  static String homestayDetailUrl(int id) => '$baseUrl$homestaysEndpoint/$id';
  
  // Booking URLs
  static String get bookingsUrl => '$baseUrl$bookingsEndpoint';
  static String get myBookingsUrl => '$baseUrl$bookingsEndpoint/my-bookings';
  static String get hostBookingsUrl => '$baseUrl$bookingsEndpoint/host-bookings';
  static String bookingDetailUrl(int id) => '$baseUrl$bookingsEndpoint/$id';
  static String get checkAvailabilityUrl => '$baseUrl$bookingsEndpoint/check-availability';
  
  // User URLs
  static String get profileUrl => '$baseUrl$userEndpoint/profile';
  static String get updateAvatarUrl => '$baseUrl$userEndpoint/avatar';
  
  // Review URLs
  static String homestayReviewsUrl(int homestayId) => '$baseUrl$reviewsEndpoint/homestay/$homestayId';
  
  // Payment URLs
  static String get createPaymentUrl => '$baseUrl$paymentsEndpoint';
  static String paymentByBookingUrl(int bookingId) => '$baseUrl$paymentsEndpoint/booking/$bookingId';
  static String get paymentCallbackUrl => '$baseUrl$paymentsEndpoint/callback';
  
  // Amenities URLs
  static String get amenitiesUrl => '$baseUrl$amenitiesEndpoint';
  
  // Notifications URLs
  static String get notificationsUrl => '$baseUrl$notificationsEndpoint';
  static String get unreadCountUrl => '$baseUrl$notificationsEndpoint/unread-count';
  static String get markReadUrl => '$baseUrl$notificationsEndpoint/mark-read';
  static String get markAllReadUrl => '$baseUrl$notificationsEndpoint/mark-all-read';
  
  // Google Maps
  static String get googleMapsApiKey => dotenv.env['GOOGLE_MAPS_API_KEY'] ?? '';
  // Google Sign-In server client id (for Android to request idToken aud)
  static String get googleServerClientId => dotenv.env['GOOGLE_SERVER_CLIENT_ID'] ?? '';

  // FPT.ai settings for vision/ID recognition
  static String get fptApiKey => dotenv.env['FPT_API_KEY'] ?? '';
  static String get fptBaseUrl => dotenv.env['FPT_BASE_URL'] ?? 'https://api.fpt.ai';
  
  // Timeouts
  static const Duration connectionTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);

  // RTC / WebRTC configuration
  // Use environment variables to configure TURN server when testing across NAT/Internet.
  // Set TURN_URL to turn:turn.example.com:3478, TURN_USERNAME and TURN_CREDENTIAL for auth.
  static List<Map<String, dynamic>> get rtcIceServers {
    final List<Map<String, dynamic>> servers = [
      {'urls': 'stun:stun.l.google.com:19302'},
    ];

    final turnUrl = dotenv.env['TURN_URL'];
    final turnUsername = dotenv.env['TURN_USERNAME'];
    final turnCredential = dotenv.env['TURN_CREDENTIAL'];

    if (turnUrl != null && turnUrl.isNotEmpty) {
      final turnEntry = <String, dynamic>{'urls': turnUrl};
      if (turnUsername != null && turnUsername.isNotEmpty) turnEntry['username'] = turnUsername;
      if (turnCredential != null && turnCredential.isNotEmpty) turnEntry['credential'] = turnCredential;
      servers.add(turnEntry);
    }

    return servers;
  }
}
