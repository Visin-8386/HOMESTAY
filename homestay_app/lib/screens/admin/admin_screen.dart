import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider_fixed.dart';
import 'admin_bookings_screen.dart';
import 'admin_homestays_screen.dart';
import 'admin_promotions_screen.dart';
import 'admin_stats_screen.dart';
import 'admin_users_screen.dart';

class AdminScreen extends StatefulWidget {
  const AdminScreen({super.key});

  @override
  State<AdminScreen> createState() => _AdminScreenState();
}

class _AdminScreenState extends State<AdminScreen> {
  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);

    void _push(Widget page) => Navigator.push(context, MaterialPageRoute(builder: (_) => page));

    Widget cardItem({required IconData icon, required String title, required String subtitle, required VoidCallback onTap}) {
      return Card(
        child: ListTile(
          leading: Icon(icon, color: Colors.teal),
          title: Text(title),
          subtitle: Text(subtitle),
          trailing: const Icon(Icons.arrow_forward_ios),
          onTap: onTap,
        ),
      );
    }

    final adminName = authProvider.currentUser?.fullName ?? 'Unknown';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Quản lý Admin'),
        backgroundColor: Colors.red,
        foregroundColor: Colors.white,
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              cardItem(
                icon: Icons.people,
                title: 'Quản lý người dùng',
                subtitle: 'Xem và quản lý tất cả người dùng',
                onTap: () => _push(const AdminUsersScreen()),
              ),
              const SizedBox(height: 16),
              cardItem(
                icon: Icons.business,
                title: 'Quản lý Homestay',
                subtitle: 'Duyệt và quản lý các homestay',
                onTap: () => _push(const AdminHomestaysScreen()),
              ),
              const SizedBox(height: 16),
              cardItem(
                icon: Icons.local_offer,
                title: 'Quản lý khuyến mãi',
                subtitle: 'Tạo / sửa / xóa mã giảm giá',
                onTap: () => _push(const AdminPromotionsScreen()),
              ),
              const SizedBox(height: 16),
              cardItem(
                icon: Icons.book,
                title: 'Quản lý đặt phòng',
                subtitle: 'Xem tất cả đơn đặt phòng',
                onTap: () => _push(const AdminBookingsScreen()),
              ),
              const SizedBox(height: 16),
              cardItem(
                icon: Icons.analytics,
                title: 'Thống kê',
                subtitle: 'Xem báo cáo và thống kê',
                onTap: () => _push(const AdminStatsScreen()),
              ),
              const SizedBox(height: 24),
              Card(
                color: Colors.red.shade50,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.admin_panel_settings, color: Colors.red),
                          const SizedBox(width: 8),
                          Text(
                            'Admin: $adminName',
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              color: Colors.red,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Bạn có quyền quản trị viên. Các tính năng quản lý sẽ được phát triển trong phiên bản tương lai.',
                        style: TextStyle(color: Colors.red),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}