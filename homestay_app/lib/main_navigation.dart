import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'providers/auth_provider_fixed.dart';
import 'screens/admin/admin_screen.dart';
import 'screens/booking/my_bookings_screen.dart';
import 'screens/chat/chat_list_screen.dart';
import 'screens/home/home_screen.dart';
import 'screens/host/host_bookings_screen.dart';
import 'screens/host/host_dashboard_screen.dart';
import 'screens/host/manage_homestays_screen.dart';
import 'screens/profile/profile_screen.dart';
import 'screens/videos/videos_screen.dart';

class MainNavigationScreen extends StatefulWidget {
  const MainNavigationScreen({super.key});

  @override
  State<MainNavigationScreen> createState() => _MainNavigationScreenState();
}

class _MainNavigationScreenState extends State<MainNavigationScreen> {
  int _currentIndex = 0;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Reset navigation index when dependencies change (e.g., user role changes)
    final navigationItems = _getNavigationItems(context);
    if (_currentIndex >= navigationItems.length) {
      setState(() => _currentIndex = 0);
    }
  }

  // Get navigation items and screens based on user role
  List<BottomNavigationBarItem> _getNavigationItems(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final isHost = authProvider.isHost;
    final isAdmin = authProvider.isAdmin;

    if (isHost) {
      return const [
        BottomNavigationBarItem(
          icon: Icon(Icons.home),
          label: 'Trang chủ',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.business),
          label: 'Homestay',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.book),
          label: 'Đơn đặt',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.chat),
          label: 'Tin nhắn',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.dashboard),
          label: 'Dashboard',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.person),
          label: 'Tài khoản',
        ),
      ];
    } else if (isAdmin) {
      return const [
        BottomNavigationBarItem(
          icon: Icon(Icons.home),
          label: 'Trang chủ',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.admin_panel_settings),
          label: 'Quản lý',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.book),
          label: 'Tất cả đơn',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.chat),
          label: 'Tin nhắn',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.person),
          label: 'Tài khoản',
        ),
      ];
    } else {
      // Regular user (Favorites removed)
      return const [
        BottomNavigationBarItem(
          icon: Icon(Icons.home),
          label: 'Trang chủ',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.video_library),
          label: 'Video',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.book),
          label: 'Đơn đặt',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.chat),
          label: 'Tin nhắn',
        ),
        BottomNavigationBarItem(
          icon: Icon(Icons.person),
          label: 'Tài khoản',
        ),
      ];
    }
  }

  List<Widget> _getScreens(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final isHost = authProvider.isHost;
    final isAdmin = authProvider.isAdmin;

    if (isHost) {
      return const [
        HomeScreen(),
        ManageHomestaysScreen(),
        HostBookingsScreen(),
        ChatListScreen(),
        HostDashboardScreen(),
        ProfileScreen(),
      ];
    } else if (isAdmin) {
      return const [
        HomeScreen(),
        AdminScreen(),
        MyBookingsScreen(), // Placeholder for all bookings
        ChatListScreen(),
        ProfileScreen(),
      ];
    } else {
      // Regular user (Favorites removed)
      return const [
        HomeScreen(),
        VideosScreen(),
        MyBookingsScreen(),
        ChatListScreen(),
        ProfileScreen(),
      ];
    }
  }

  @override
  Widget build(BuildContext context) {
    final navigationItems = _getNavigationItems(context);
    final screens = _getScreens(context);

    // Ensure current index is within bounds
    final safeIndex = _currentIndex < screens.length ? _currentIndex : 0;

    return Scaffold(
      body: screens[safeIndex],
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: safeIndex,
        onTap: (index) {
          setState(() => _currentIndex = index);
        },
        type: BottomNavigationBarType.fixed,
        selectedItemColor: const Color(0xFF667eea),
        unselectedItemColor: Colors.grey,
        items: navigationItems,
      ),
    );
  }
}
