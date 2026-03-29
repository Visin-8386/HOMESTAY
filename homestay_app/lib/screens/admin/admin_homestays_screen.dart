import 'package:flutter/material.dart';

import '../../screens/home/homestay_detail_screen.dart' as provider_detail;
import '../../services/admin_service.dart';

class AdminHomestaysScreen extends StatefulWidget {
  const AdminHomestaysScreen({super.key});

  @override
  State<AdminHomestaysScreen> createState() => _AdminHomestaysScreenState();
}

class _AdminHomestaysScreenState extends State<AdminHomestaysScreen> with TickerProviderStateMixin {
  final AdminService _admin = AdminService();
  List<dynamic> _items = [];
  bool _loading = true;
  String _status = 'all';
  int _page = 1;
  int _pageSize = 20;
  int _total = 0;
  String _query = '';
  Set<int> _selected = {};
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
      final data = await _admin.searchHomestays(page: _page, pageSize: _pageSize, status: _status, q: _query);
      if (!mounted) return;
      setState(() {
        _items = data['items'] ?? [];
        _total = data['total'] ?? 0;
      });
      if (mounted) _fadeController.forward(from: 0.0);
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Lỗi khi tải homestay: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _approve(int id, bool isApproved) async {
    try {
      await _admin.approveHomestay(id, !isApproved);
      await _load();
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Không thể cập nhật: $e'),
          backgroundColor: Colors.orangeAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  Future<void> _delete(int id) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Text('Xác nhận xóa'),
        content: const Text('Bạn có chắc muốn xóa homestay này?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy', style: TextStyle(color: Colors.grey)),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.red,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
            ),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );

    if (ok == true) {
      try {
        await _admin.deleteHomestay(id);
        await _load();
      } catch (e) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Không thể xóa: $e'),
            backgroundColor: Colors.redAccent,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  Future<void> _bulkApprove(bool approve) async {
    if (_selected.isEmpty) return;
    try {
      await _admin.bulkApproveHomestays(_selected.toList(), approve);
      final count = _selected.length;
      _selected.clear();
      await _load();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Đã ${approve ? 'duyệt' : 'từ chối'} $count homestay'),
          backgroundColor: Colors.greenAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Bulk action failed: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
        ),
      );
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
          title: const Text('Quản lý Homestay', style: TextStyle(fontWeight: FontWeight.bold)),
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
                      // Search and filters
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
                        child: Column(
                          children: [
                            // Search bar
                            TextField(
                              decoration: InputDecoration(
                                hintText: 'Tìm kiếm homestay...',
                                prefixIcon: const Icon(Icons.search, color: Colors.blue),
                                filled: true,
                                fillColor: Colors.grey.shade100,
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(12),
                                  borderSide: BorderSide.none,
                                ),
                                contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                              ),
                              onSubmitted: (v) {
                                setState(() {
                                  _query = v.trim();
                                  _page = 1;
                                });
                                _load();
                              },
                            ),
                            const SizedBox(height: 12),
                            // Filters row
                            Row(
                              children: [
                                const Text('Trạng thái:', style: TextStyle(fontWeight: FontWeight.w500)),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: DropdownButtonFormField<String>(
                                    value: _status,
                                    decoration: InputDecoration(
                                      filled: true,
                                      fillColor: Colors.grey.shade100,
                                      border: OutlineInputBorder(
                                        borderRadius: BorderRadius.circular(8),
                                        borderSide: BorderSide.none,
                                      ),
                                      contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                                    ),
                                    items: const [
                                      DropdownMenuItem(value: 'all', child: Text('Tất cả')),
                                      DropdownMenuItem(value: 'pending', child: Text('Chờ duyệt')),
                                      DropdownMenuItem(value: 'approved', child: Text('Đã duyệt')),
                                      DropdownMenuItem(value: 'inactive', child: Text('Không hoạt động')),
                                    ],
                                    onChanged: (v) {
                                      setState(() {
                                        _status = v ?? 'all';
                                        _page = 1;
                                      });
                                      _load();
                                    },
                                  ),
                                ),
                                const SizedBox(width: 8),
                                const Text('Hiển thị:', style: TextStyle(fontWeight: FontWeight.w500)),
                                const SizedBox(width: 8),
                                DropdownButton<int>(
                                  value: _pageSize,
                                  underline: const SizedBox(),
                                  items: const [
                                    DropdownMenuItem(value: 10, child: Text('10')),
                                    DropdownMenuItem(value: 20, child: Text('20')),
                                    DropdownMenuItem(value: 50, child: Text('50')),
                                  ],
                                  onChanged: (v) {
                                    setState(() {
                                      _pageSize = v ?? 20;
                                      _page = 1;
                                    });
                                    _load();
                                  },
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            // Bulk actions
                            Row(
                              children: [
                                Expanded(
                                  child: ElevatedButton.icon(
                                    onPressed: () => _bulkApprove(true),
                                    icon: const Icon(Icons.check_circle),
                                    label: const Text('Duyệt hàng loạt'),
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: Colors.green,
                                      foregroundColor: Colors.white,
                                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                      padding: const EdgeInsets.symmetric(vertical: 12),
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: ElevatedButton.icon(
                                    onPressed: () => _bulkApprove(false),
                                    icon: const Icon(Icons.cancel),
                                    label: const Text('Từ chối hàng loạt'),
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: Colors.red,
                                      foregroundColor: Colors.white,
                                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                      padding: const EdgeInsets.symmetric(vertical: 12),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                      // Pagination
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            IconButton(
                              onPressed: () {
                                if (_page > 1) {
                                  setState(() => _page = 1);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.first_page),
                              color: Colors.blue,
                            ),
                            IconButton(
                              onPressed: () {
                                if ((_page - 1) * _pageSize > 0) {
                                  setState(() => _page--);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.chevron_left),
                              color: Colors.blue,
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                              decoration: BoxDecoration(
                                color: Colors.blue.shade50,
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(
                                'Trang $_page / ${(_total / _pageSize).ceil().clamp(1, 9999)}',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                            ),
                            IconButton(
                              onPressed: () {
                                if (_page * _pageSize < _total) {
                                  setState(() => _page++);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.chevron_right),
                              color: Colors.blue,
                            ),
                            IconButton(
                              onPressed: () {
                                final last = (_total / _pageSize).ceil();
                                if (_page < last) {
                                  setState(() => _page = last);
                                  _load();
                                }
                              },
                              icon: const Icon(Icons.last_page),
                              color: Colors.blue,
                            ),
                          ],
                        ),
                      ),
                      // List
                      Expanded(
                        child: FadeTransition(
                          opacity: _fadeAnimation,
                          child: ListView.builder(
                            padding: const EdgeInsets.all(16),
                            itemCount: _items.length,
                            itemBuilder: (context, index) {
                              final h = _items[index];
                              final hid = h['id'] is int ? h['id'] as int : int.parse(h['id'].toString());
                              final selected = _selected.contains(hid);
                              final thumbnail = h['thumbnail'] as String?;
                              final created = h['createdAt']?.toString() ?? h['created']?.toString() ?? '';
                              return Card(
                                elevation: 4,
                                margin: const EdgeInsets.only(bottom: 12),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                child: InkWell(
                                  borderRadius: BorderRadius.circular(12),
                                  onTap: () {
                                    Navigator.push(
                                      context,
                                      MaterialPageRoute(
                                        builder: (_) => provider_detail.HomestayDetailScreen(homestayId: hid),
                                      ),
                                    );
                                  },
                                  child: Padding(
                                    padding: const EdgeInsets.all(16),
                                    child: Row(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        // Thumbnail
                                        Container(
                                          width: 80,
                                          height: 80,
                                          decoration: BoxDecoration(
                                            borderRadius: BorderRadius.circular(8),
                                            color: Colors.grey.shade200,
                                          ),
                                          child: thumbnail != null
                                              ? ClipRRect(
                                                  borderRadius: BorderRadius.circular(8),
                                                  child: Image.network(
                                                    thumbnail,
                                                    fit: BoxFit.cover,
                                                    errorBuilder: (context, error, stackTrace) =>
                                                        const Icon(Icons.home, color: Colors.grey),
                                                  ),
                                                )
                                              : const Icon(Icons.home, color: Colors.grey),
                                        ),
                                        const SizedBox(width: 16),
                                        // Content
                                        Expanded(
                                          child: Column(
                                            crossAxisAlignment: CrossAxisAlignment.start,
                                            children: [
                                              Text(
                                                h['name'] ?? 'No name',
                                                style: const TextStyle(
                                                  fontSize: 18,
                                                  fontWeight: FontWeight.bold,
                                                ),
                                              ),
                                              const SizedBox(height: 4),
                                              Text(
                                                h['address'] ?? '',
                                                style: TextStyle(color: Colors.grey.shade600),
                                              ),
                                              const SizedBox(height: 8),
                                              Row(
                                                children: [
                                                  Icon(Icons.person, size: 16, color: Colors.blue),
                                                  const SizedBox(width: 4),
                                                  Expanded(
                                                    child: Text(
                                                      'Host: ${h['hostEmail'] ?? h['ownerEmail'] ?? '—'}',
                                                      style: const TextStyle(fontSize: 12),
                                                    ),
                                                  ),
                                                ],
                                              ),
                                              const SizedBox(height: 4),
                                              Row(
                                                children: [
                                                  Icon(Icons.calendar_today, size: 16, color: Colors.blue),
                                                  const SizedBox(width: 4),
                                                  Expanded(
                                                    child: Text(
                                                      'Created: $created',
                                                      style: const TextStyle(fontSize: 12),
                                                      overflow: TextOverflow.ellipsis,
                                                    ),
                                                  ),
                                                ],
                                              ),
                                            ],
                                          ),
                                        ),
                                        // Actions
                                        Column(
                                          children: [
                                            Checkbox(
                                              value: selected,
                                              onChanged: (v) {
                                                setState(() {
                                                  if (v == true)
                                                    _selected.add(hid);
                                                  else
                                                    _selected.remove(hid);
                                                });
                                              },
                                              activeColor: Colors.blue,
                                            ),
                                            PopupMenuButton<String>(
                                              onSelected: (val) async {
                                                if (val == 'approve') await _approve(hid, h['isApproved'] ?? false);
                                                if (val == 'delete') await _delete(hid);
                                                if (val == 'reject') {
                                                  await _admin.rejectHomestay(hid);
                                                  await _load();
                                                }
                                                if (val == 'activate') {
                                                  await _admin.activateHomestay(hid);
                                                  await _load();
                                                }
                                                if (val == 'deactivate') {
                                                  await _admin.deactivateHomestay(hid);
                                                  await _load();
                                                }
                                              },
                                              itemBuilder: (_) => [
                                                PopupMenuItem(
                                                  value: 'approve',
                                                  child: Text((h['isApproved'] ?? false) ? 'Hủy duyệt' : 'Duyệt'),
                                                ),
                                                const PopupMenuItem(value: 'delete', child: Text('Xóa')),
                                                const PopupMenuItem(value: 'reject', child: Text('Từ chối')),
                                                const PopupMenuItem(value: 'activate', child: Text('Kích hoạt')),
                                                const PopupMenuItem(value: 'deactivate', child: Text('Vô hiệu hóa')),
                                              ],
                                              icon: const Icon(Icons.more_vert),
                                            ),
                                          ],
                                        ),
                                      ],
                                    ),
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
}
