import 'package:flutter/material.dart';

import '../../services/admin_service.dart';

class AdminUsersScreen extends StatefulWidget {
  const AdminUsersScreen({super.key});

  @override
  State<AdminUsersScreen> createState() => _AdminUsersScreenState();
}

class _AdminUsersScreenState extends State<AdminUsersScreen> with TickerProviderStateMixin {
  final AdminService _admin = AdminService();
  List<dynamic> _users = [];
  bool _loading = true;
  String _searchQuery = '';
  late AnimationController _fadeController;
  late Animation<double> _fadeAnimation;

  @override
  void initState() {
    super.initState();
    _fadeController = AnimationController(vsync: this, duration: const Duration(milliseconds: 500));
    _fadeAnimation = Tween<double>(begin: 0.0, end: 1.0).animate(CurvedAnimation(parent: _fadeController, curve: Curves.easeIn));
    _load();
  }

  @override
  void dispose() {
    _fadeController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    if (!mounted) return;
    setState(() => _loading = true);
    try {
      final data = await _admin.getUsers();
      if (!mounted) return;
      setState(() {
        _users = data['items'] ?? [];
      });
      if (mounted) _fadeController.forward(from: 0.0);
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Lỗi khi tải người dùng: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _toggleActive(String id, bool isActive) async {
    try {
      await _admin.updateUserStatus(id, !isActive);
      await _load();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Đã ${!isActive ? 'kích hoạt' : 'vô hiệu hóa'} người dùng'),
          backgroundColor: Colors.greenAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Không thể cập nhật trạng thái: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  Future<void> _changeRole(String id) async {
    final role = await showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Text('Chọn vai trò mới'),
        content: const Text('Chọn vai trò mới cho người dùng này:'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, 'User'),
            child: const Text('User'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, 'Host'),
            child: const Text('Host'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, 'Admin'),
            child: const Text('Admin'),
          ),
        ],
      ),
    );

    if (role != null) {
      try {
        await _admin.updateUserRole(id, role);
        await _load();
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Đã cập nhật vai trò thành $role'),
            backgroundColor: Colors.greenAccent,
            behavior: SnackBarBehavior.floating,
          ),
        );
      } catch (e) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Không thể cập nhật role: $e'),
            backgroundColor: Colors.redAccent,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  List<dynamic> get _filteredUsers {
    if (_searchQuery.isEmpty) return _users;
    return _users.where((u) {
      final email = u['email']?.toString().toLowerCase() ?? '';
      final name = u['name']?.toString().toLowerCase() ?? '';
      final query = _searchQuery.toLowerCase();
      return email.contains(query) || name.contains(query);
    }).toList();
  }

  Color _getRoleColor(String role) {
    switch (role.toLowerCase()) {
      case 'admin':
        return Colors.red;
      case 'host':
        return Colors.orange;
      case 'user':
        return Colors.blue;
      default:
        return Colors.grey;
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
          title: const Text('Quản lý người dùng', style: TextStyle(fontWeight: FontWeight.bold)),
          flexibleSpace: Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                colors: [Colors.blue, Colors.indigo],
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
                  child: Column(
                    children: [
                      // Search bar
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: const BorderRadius.only(
                            bottomLeft: Radius.circular(20),
                            bottomRight: Radius.circular(20),
                          ),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.grey.withOpacity(0.1),
                              spreadRadius: 2,
                              blurRadius: 8,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: TextField(
                          decoration: InputDecoration(
                            hintText: 'Tìm kiếm người dùng...',
                            prefixIcon: const Icon(Icons.search, color: Colors.blue),
                            filled: true,
                            fillColor: Colors.grey.shade100,
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(12),
                              borderSide: BorderSide.none,
                            ),
                            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                          ),
                          onChanged: (value) {
                            setState(() {
                              _searchQuery = value;
                            });
                          },
                        ),
                      ),
                      // Stats
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                          children: [
                            _buildStatCard('Tổng số', _users.length.toString(), Icons.people, Colors.blue),
                            _buildStatCard('Admin', _users.where((u) => (u['roles'] as List?)?.contains('Admin') ?? false).length.toString(), Icons.admin_panel_settings, Colors.red),
                            _buildStatCard('Host', _users.where((u) => (u['roles'] as List?)?.contains('Host') ?? false).length.toString(), Icons.business, Colors.orange),
                            _buildStatCard('User', _users.where((u) => (u['roles'] as List?)?.contains('User') ?? false).length.toString(), Icons.person, Colors.green),
                          ],
                        ),
                      ),
                      // List
                      Expanded(
                        child: FadeTransition(
                          opacity: _fadeAnimation,
                          child: ListView.builder(
                            padding: const EdgeInsets.all(16),
                            itemCount: _filteredUsers.length,
                            itemBuilder: (context, index) {
                              final u = _filteredUsers[index];
                              final uid = u['id']?.toString() ?? '';
                              final email = u['email']?.toString() ?? 'Unknown';
                              final name = u['name']?.toString() ?? '';
                              final roles = (u['roles'] as List?) ?? [];
                              final isActive = u['isActive'] ?? true;
                              final createdAt = u['createdAt']?.toString() ?? '';

                              return Card(
                                elevation: 4,
                                margin: const EdgeInsets.only(bottom: 12),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                child: Padding(
                                  padding: const EdgeInsets.all(16),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          CircleAvatar(
                                            backgroundColor: isActive ? Colors.green.shade100 : Colors.grey.shade100,
                                            child: Icon(
                                              isActive ? Icons.check_circle : Icons.cancel,
                                              color: isActive ? Colors.green : Colors.grey,
                                            ),
                                          ),
                                          const SizedBox(width: 12),
                                          Expanded(
                                            child: Column(
                                              crossAxisAlignment: CrossAxisAlignment.start,
                                              children: [
                                                Text(
                                                  name.isNotEmpty ? name : email,
                                                  style: const TextStyle(
                                                    fontSize: 18,
                                                    fontWeight: FontWeight.bold,
                                                  ),
                                                ),
                                                const SizedBox(height: 4),
                                                Text(
                                                  email,
                                                  style: TextStyle(color: Colors.grey.shade600),
                                                ),
                                              ],
                                            ),
                                          ),
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                            decoration: BoxDecoration(
                                              color: isActive ? Colors.green.shade100 : Colors.red.shade100,
                                              borderRadius: BorderRadius.circular(12),
                                            ),
                                            child: Text(
                                              isActive ? 'Hoạt động' : 'Vô hiệu',
                                              style: TextStyle(
                                                color: isActive ? Colors.green : Colors.red,
                                                fontSize: 12,
                                                fontWeight: FontWeight.w500,
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 12),
                                      Wrap(
                                        spacing: 8,
                                        runSpacing: 8,
                                        children: roles.map<Widget>((role) {
                                          return Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                                            decoration: BoxDecoration(
                                              color: _getRoleColor(role).withOpacity(0.1),
                                              borderRadius: BorderRadius.circular(16),
                                              border: Border.all(color: _getRoleColor(role)),
                                            ),
                                            child: Text(
                                              role,
                                              style: TextStyle(
                                                color: _getRoleColor(role),
                                                fontWeight: FontWeight.w500,
                                              ),
                                            ),
                                          );
                                        }).toList(),
                                      ),
                                      const SizedBox(height: 8),
                                      Row(
                                        children: [
                                          Icon(Icons.calendar_today, size: 16, color: Colors.grey),
                                          const SizedBox(width: 4),
                                          Text(
                                            'Tham gia: $createdAt',
                                            style: const TextStyle(fontSize: 12),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 12),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.end,
                                        children: [
                                          PopupMenuButton<String>(
                                            onSelected: (val) async {
                                              if (val == 'toggle') await _toggleActive(uid, isActive);
                                              if (val == 'role') await _changeRole(uid);
                                            },
                                            itemBuilder: (_) => [
                                              PopupMenuItem(
                                                value: 'toggle',
                                                child: Text(isActive ? 'Vô hiệu hóa' : 'Kích hoạt'),
                                              ),
                                              const PopupMenuItem(value: 'role', child: Text('Thay đổi vai trò')),
                                            ],
                                            child: Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                                              decoration: BoxDecoration(
                                                color: Colors.blue.shade50,
                                                borderRadius: BorderRadius.circular(8),
                                              ),
                                              child: const Row(
                                                mainAxisSize: MainAxisSize.min,
                                                children: [
                                                  Text('Thao tác', style: TextStyle(color: Colors.blue)),
                                                  Icon(Icons.arrow_drop_down, color: Colors.blue),
                                                ],
                                              ),
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              );
                            },
                          ),
                        ),
                      ),
                    ],
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

  Widget _buildStatCard(String title, String value, IconData icon, Color color) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(height: 4),
            Text(
              value,
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: color,
              ),
            ),
            Text(
              title,
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey.shade600,
              ),
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }
}
