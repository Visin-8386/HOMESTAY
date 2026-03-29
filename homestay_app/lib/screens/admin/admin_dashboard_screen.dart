import 'package:flutter/material.dart';

import '../../services/admin_service.dart';

class AdminDashboardScreen extends StatefulWidget {
  const AdminDashboardScreen({super.key});

  @override
  State<AdminDashboardScreen> createState() => _AdminDashboardScreenState();
}

class _AdminDashboardScreenState extends State<AdminDashboardScreen> with TickerProviderStateMixin {
  final AdminService _admin = AdminService();
  Map<String, dynamic>? _stats;
  bool _loading = true;
  late AnimationController _fadeController;
  late Animation<double> _fadeAnimation;
  late AnimationController _scaleController;
  late Animation<double> _scaleAnimation;

  @override
  void initState() {
    super.initState();
    _fadeController = AnimationController(vsync: this, duration: const Duration(milliseconds: 800));
    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(CurvedAnimation(parent: _fadeController, curve: Curves.easeInOut));

    _scaleController = AnimationController(vsync: this, duration: const Duration(milliseconds: 600));
    _scaleAnimation = Tween<double>(begin: 0.8, end: 1.0).animate(CurvedAnimation(parent: _scaleController, curve: Curves.elasticOut));

    _load();
  }

  @override
  void dispose() {
    _fadeController.dispose();
    _scaleController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    if (!mounted) return;
    setState(() => _loading = true);
    try {
      final s = await _admin.getStats();
      if (!mounted) return;
      setState(() => _stats = s);
      if (mounted) _fadeController.forward(from: 0.0);
      if (mounted) _scaleController.forward(from: 0.0);
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Lỗi khi tải thống kê: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Theme(
      data: Theme.of(context).copyWith(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: Colors.blue,
          brightness: Brightness.light,
        ),
      ),
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Bảng điều khiển Admin', style: TextStyle(fontWeight: FontWeight.bold)),
          flexibleSpace: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Colors.blue, Colors.indigo, Colors.purple],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
            ),
          ),
          elevation: 4,
          shadowColor: Colors.blue.withOpacity(0.3),
        ),
        body: Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [Colors.grey.shade50, Colors.white],
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
            ),
          ),
          child: _loading
              ? const Center(
                  child: CircularProgressIndicator(
                    valueColor: AlwaysStoppedAnimation<Color>(Colors.blue),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  color: Colors.blue,
                  child: SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.all(16),
                    child: FadeTransition(
                      opacity: _fadeAnimation,
                      child: ScaleTransition(
                        scale: _scaleAnimation,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            // Welcome section
                            Container(
                              padding: const EdgeInsets.all(20),
                              decoration: BoxDecoration(
                                gradient: const LinearGradient(
                                  colors: [Colors.blue, Colors.indigo],
                                  begin: Alignment.topLeft,
                                  end: Alignment.bottomRight,
                                ),
                                borderRadius: BorderRadius.circular(16),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.blue.withOpacity(0.3),
                                    spreadRadius: 2,
                                    blurRadius: 8,
                                    offset: const Offset(0, 4),
                                  ),
                                ],
                              ),
                              child: const Row(
                                children: [
                                  Icon(
                                    Icons.admin_panel_settings,
                                    color: Colors.white,
                                    size: 48,
                                  ),
                                  SizedBox(width: 16),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          'Chào mừng Admin!',
                                          style: TextStyle(
                                            color: Colors.white,
                                            fontSize: 24,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                        SizedBox(height: 4),
                                        Text(
                                          'Quản lý hệ thống homestay một cách hiệu quả',
                                          style: TextStyle(
                                            color: Colors.white70,
                                            fontSize: 14,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 24),

                            // Statistics section
                            const Text(
                              'Thống kê tổng quan',
                              style: TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.bold,
                                color: Colors.black87,
                              ),
                            ),
                            const SizedBox(height: 16),

                            if (_stats != null) ...[
                              // Modern stat row (responsive)
                              LayoutBuilder(
                                builder: (context, constraints) {
                                  final max = constraints.maxWidth;
                                  final isSmall = max < 600;
                                  return Wrap(
                                    spacing: 16,
                                    runSpacing: 16,
                                    children: [
                                      _buildModernStat(
                                        title: 'Người dùng',
                                        value: (_stats!['totalUsers'] ?? _stats!['usersCount'] ?? 0).toString(),
                                        icon: Icons.people,
                                        color: Colors.blue,
                                        subtitle: 'Tổng tài khoản',
                                        width: isSmall ? max : (max - 48) / 4,
                                      ),
                                      _buildModernStat(
                                        title: 'Homestay',
                                        value: (_stats!['totalHomestays'] ?? _stats!['homestaysCount'] ?? 0).toString(),
                                        icon: Icons.home,
                                        color: Colors.green,
                                        subtitle: 'Đã đăng ký',
                                        width: isSmall ? max : (max - 48) / 4,
                                      ),
                                      _buildModernStat(
                                        title: 'Đặt phòng',
                                        value: (_stats!['totalBookings'] ?? _stats!['bookingsCount'] ?? 0).toString(),
                                        icon: Icons.book_online,
                                        color: Colors.orange,
                                        subtitle: 'Tổng booking',
                                        width: isSmall ? max : (max - 48) / 4,
                                      ),
                                      _buildModernStat(
                                        title: 'Doanh thu',
                                        value: '₫${(_stats!['totalRevenue'] ?? _stats!['revenue'] ?? 0).toString()}',
                                        icon: Icons.attach_money,
                                        color: Colors.purple,
                                        subtitle: 'Tổng thu nhập',
                                        width: isSmall ? max : (max - 48) / 4,
                                      ),
                                    ],
                                  );
                                },
                              ),
                              const SizedBox(height: 24),

                              // Quick actions section
                              const Text(
                                'Thao tác nhanh',
                                style: TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.black87,
                                ),
                              ),
                              const SizedBox(height: 16),

                              // Action buttons grid
                              GridView.count(
                                crossAxisCount: 2,
                                shrinkWrap: true,
                                physics: const NeverScrollableScrollPhysics(),
                                crossAxisSpacing: 16,
                                mainAxisSpacing: 16,
                                children: [
                                  _buildActionCard(
                                    'Quản lý người dùng',
                                    Icons.people,
                                    Colors.blue,
                                    () => Navigator.pushNamed(context, '/admin/users'),
                                  ),
                                  _buildActionCard(
                                    'Quản lý Homestay',
                                    Icons.home,
                                    Colors.green,
                                    () => Navigator.pushNamed(context, '/admin/homestays'),
                                  ),
                                  _buildActionCard(
                                    'Quản lý đặt phòng',
                                    Icons.book_online,
                                    Colors.orange,
                                    () => Navigator.pushNamed(context, '/admin/bookings'),
                                  ),
                                  _buildActionCard(
                                    'Cài đặt hệ thống',
                                    Icons.settings,
                                    Colors.purple,
                                    () {
                                      // TODO: Navigate to settings
                                      ScaffoldMessenger.of(context).showSnackBar(
                                        const SnackBar(
                                          content: Text('Tính năng cài đặt sẽ được thêm sau'),
                                          backgroundColor: Colors.blueAccent,
                                          behavior: SnackBarBehavior.floating,
                                        ),
                                      );
                                    },
                                  ),
                                ],
                              ),
                            ] else ...[
                              // No data state
                              Center(
                                child: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(
                                      Icons.error_outline,
                                      size: 64,
                                      color: Colors.grey.shade400,
                                    ),
                                    const SizedBox(height: 16),
                                    Text(
                                      'Không thể tải dữ liệu thống kê',
                                      style: TextStyle(
                                        fontSize: 16,
                                        color: Colors.grey.shade600,
                                      ),
                                    ),
                                    const SizedBox(height: 16),
                                    ElevatedButton.icon(
                                      onPressed: _load,
                                      icon: const Icon(Icons.refresh),
                                      label: const Text('Thử lại'),
                                      style: ElevatedButton.styleFrom(
                                        backgroundColor: Colors.blue,
                                        foregroundColor: Colors.white,
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(8),
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
        ),
        floatingActionButton: FloatingActionButton(
          onPressed: _load,
          backgroundColor: Colors.blue,
          child: const Icon(Icons.refresh),
        ),
      ),
    );
  }

  

  Widget _buildModernStat({
    required String title,
    required String value,
    required IconData icon,
    required Color color,
    required String subtitle,
    double width = 200,
  }) {
    return SizedBox(
      width: width,
      child: Card(
        elevation: 6,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: color.withOpacity(0.12),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: color, size: 28),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(value, style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: color)),
                    const SizedBox(height: 4),
                    Text(title, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                    const SizedBox(height: 2),
                    Text(subtitle, style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
                  ],
                ),
              )
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildActionCard(String title, IconData icon, Color color, VoidCallback onTap) {
    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            gradient: LinearGradient(
              colors: [color.withOpacity(0.1), color.withOpacity(0.05)],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                icon,
                size: 32,
                color: color,
              ),
              const SizedBox(height: 12),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: Colors.black87,
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
