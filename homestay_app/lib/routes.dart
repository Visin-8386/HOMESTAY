import 'package:flutter/material.dart';

import 'main_navigation.dart';
import 'screens/admin/admin_bookings_screen.dart';
import 'screens/admin/admin_dashboard_screen.dart';
import 'screens/admin/admin_users_screen.dart';
import 'screens/ai_chat/ai_chat_screen.dart';
import 'screens/auth/forgot_password_screen.dart';
// Import all screens
import 'screens/auth/login_screen.dart';
import 'screens/auth/register_screen.dart';
import 'screens/auth/two_factor_setup_screen.dart';
import 'screens/booking/booking_screen.dart';
import 'screens/booking/my_bookings_screen.dart';
import 'screens/chat/chat_detail_screen.dart';
import 'screens/chat/chat_list_screen.dart';
// Use the host version which includes amenities and returns the saved Homestay
import 'screens/host/create_homestay_screen.dart';
import 'screens/host/host_bookings_screen.dart';
import 'screens/host/host_dashboard_screen.dart';
import 'screens/host/host_revenue_screen.dart';
import 'screens/host/host_reviews_screen.dart';
import 'screens/host/manage_homestays_screen.dart';
import 'screens/profile/change_password_screen.dart';
import 'screens/profile/edit_profile_screen.dart';
import 'screens/promotion/promotion_screen.dart';
import 'screens/review/all_reviews_screen.dart';
import 'screens/settings/about_screen.dart';
import 'screens/settings/privacy_policy_screen.dart';
import 'screens/settings/terms_of_service_screen.dart';
import 'screens/splash_screen.dart';
import 'screens/youtube/youtube_screen.dart';

class AppRoutes {
  static const String splash = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String forgotPassword = '/forgot-password';
  static const String twoFactorSetup = '/two-factor-setup';
  static const String mainNavigation = '/main';
  static const String createHomestay = '/create-homestay';
  static const String myBookings = '/my-bookings';
  static const String editProfile = '/edit-profile';
  static const String hostDashboard = '/host-dashboard';
  static const String manageHomestays = '/manage-homestays';
  static const String hostRevenue = '/host-revenue';
  static const String hostReviews = '/host-reviews';
  static const String allReviews = '/all-reviews';
  static const String promotions = '/promotions';
  static const String youtube = '/youtube';
  static const String about = '/about';
  static const String privacyPolicy = '/privacy-policy';
  static const String termsOfService = '/terms-of-service';
  static const String adminDashboard = '/admin';
  static const String adminUsers = '/admin/users';
  static const String adminBookings = '/admin/bookings';
  static const String changePassword = '/change-password';
  static const String aiChat = '/ai-chat';
  static const String hostBookings = '/host-bookings';
  static const String chatList = '/chat-list';
  static const String chatDetail = '/chat-detail';

  static Map<String, WidgetBuilder> getRoutes() {
    return {
      splash: (context) => const SplashScreen(),
      login: (context) => const LoginScreen(),
      register: (context) => const RegisterScreen(),
      forgotPassword: (context) => const ForgotPasswordScreen(),
      twoFactorSetup: (context) => const TwoFactorSetupScreen(),
      mainNavigation: (context) => const MainNavigationScreen(),
  // Route for creating a homestay. Use the host variant so callers
  // that await the pushed route receive the saved Homestay object.
  createHomestay: (context) => const CreateHomestayScreen(),
      myBookings: (context) => const MyBookingsScreen(),
      editProfile: (context) => const EditProfileScreen(),
      hostDashboard: (context) => const HostDashboardScreen(),
      manageHomestays: (context) => const ManageHomestaysScreen(),
      hostRevenue: (context) => const HostRevenueScreen(),
      hostReviews: (context) => const HostReviewsScreen(),
  hostBookings: (context) => const HostBookingsScreen(),
      chatList: (context) => const ChatListScreen(),
  changePassword: (context) => const ChangePasswordScreen(),
      allReviews: (context) => const AllReviewsScreen(),
      promotions: (context) => const PromotionScreen(),
      youtube: (context) => const YouTubeScreen(),
      about: (context) => const AboutScreen(),
      privacyPolicy: (context) => const PrivacyPolicyScreen(),
      termsOfService: (context) => const TermsOfServiceScreen(),
  adminDashboard: (context) => const AdminDashboardScreen(),
  adminUsers: (context) => const AdminUsersScreen(),
  adminBookings: (context) => const AdminBookingsScreen(),
      aiChat: (context) => const AIChatScreen(),
    };
  }

  static Route<dynamic>? onGenerateRoute(RouteSettings settings) {
    // Handle routes with parameters
    switch (settings.name) {
      case '/booking':
        final args = settings.arguments as Map<String, dynamic>?;
        if (args != null) {
          final homestayId = args['homestayId'] as int;
          final checkIn = args['checkIn'] as DateTime;
          final checkOut = args['checkOut'] as DateTime;
          final guests = args['guests'] as int;
          return MaterialPageRoute(
            builder: (context) => BookingScreen(
              homestayId: homestayId,
              checkIn: checkIn,
              checkOut: checkOut,
              guests: guests,
            ),
          );
        }
        return null;
      case chatDetail:
        final chatId = settings.arguments as int?;
        if (chatId != null) {
          return MaterialPageRoute(
            builder: (context) => ChatDetailScreen(chatId: chatId),
          );
        }
        return null;
      default:
        return null;
    }
  }
}