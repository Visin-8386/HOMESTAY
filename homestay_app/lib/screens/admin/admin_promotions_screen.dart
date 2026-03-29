import 'package:flutter/material.dart';

import '../../services/promotion_service.dart';

class AdminPromotionsScreen extends StatefulWidget {
  const AdminPromotionsScreen({super.key});

  @override
  State<AdminPromotionsScreen> createState() => _AdminPromotionsScreenState();
}

class _AdminPromotionsScreenState extends State<AdminPromotionsScreen> {
  final PromotionService _service = PromotionService();
  bool _isLoading = false;
  List<Map<String, dynamic>> _promos = [];

  @override
  void initState() {
    super.initState();
    _loadPromos();
  }

  Future<void> _loadPromos() async {
    setState(() => _isLoading = true);
    try {
      final list = await _service.getAll();
      setState(() => _promos = list);
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không tải được danh sách: $e')));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _showEditDialog([Map<String, dynamic>? promo]) async {
    final codeCtrl = TextEditingController(text: promo?['code'] ?? promo?['Code'] ?? '');
    final nameCtrl = TextEditingController(text: promo?['name'] ?? promo?['Name'] ?? '');
    final descriptionCtrl = TextEditingController(text: promo?['description'] ?? promo?['Description'] ?? '');
    final valueCtrl = TextEditingController(text: (promo?['value'] ?? promo?['Value'] ?? '').toString());
    final minOrderCtrl = TextEditingController(text: (promo?['minOrderAmount'] ?? promo?['MinOrderAmount'] ?? '').toString());
    final maxDiscountCtrl = TextEditingController(text: (promo?['maxDiscountAmount'] ?? promo?['MaxDiscountAmount'] ?? '').toString());
    final usageLimitCtrl = TextEditingController(text: (promo?['usageLimit'] ?? promo?['UsageLimit'] ?? '').toString());
    bool isActive = (promo?['isActive'] ?? promo?['IsActive'] ?? true) as bool;
    // Promotion type: 0 = Percentage, 1 = FixedAmount. Try to parse from existing promo (could be string or int)
    int selectedType = 0;
    try {
      final rawType = promo?['type'] ?? promo?['Type'];
      if (rawType is int) selectedType = rawType;
      else if (rawType is String) {
        selectedType = rawType.toLowerCase().contains('fixed') ? 1 : 0;
      }
    } catch (_) { selectedType = 0; }

    final result = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(promo == null ? 'Tạo khuyến mãi' : 'Sửa khuyến mãi'),
        content: SingleChildScrollView(
              child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(controller: codeCtrl, decoration: const InputDecoration(labelText: 'Mã (Code)')),
              TextField(controller: nameCtrl, decoration: const InputDecoration(labelText: 'Tên')),
              TextField(controller: descriptionCtrl, decoration: const InputDecoration(labelText: 'Mô tả (tùy chọn)')),
              DropdownButtonFormField<int>(
                value: selectedType,
                decoration: const InputDecoration(labelText: 'Loại khuyến mãi'),
                items: const [
                  DropdownMenuItem(value: 0, child: Text('Phần trăm (%)')),
                  DropdownMenuItem(value: 1, child: Text('Cố định (đ)')),
                ],
                onChanged: (v) => setState(() => selectedType = v ?? 0),
              ),
              TextField(controller: valueCtrl, decoration: const InputDecoration(labelText: 'Giá trị (số)'), keyboardType: TextInputType.number),
              TextField(controller: minOrderCtrl, decoration: const InputDecoration(labelText: 'Giá trị tối thiểu áp dụng (MinOrderAmount)'), keyboardType: TextInputType.number),
              TextField(controller: maxDiscountCtrl, decoration: const InputDecoration(labelText: 'Giá trị tối đa giảm (MaxDiscountAmount)'), keyboardType: TextInputType.number),
              TextField(controller: usageLimitCtrl, decoration: const InputDecoration(labelText: 'Giới hạn sử dụng (UsageLimit)'), keyboardType: TextInputType.number),
              SwitchListTile(value: isActive, onChanged: (v) => setState(() => isActive = v), title: const Text('Kích hoạt')),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Hủy')),
          ElevatedButton(onPressed: () => Navigator.pop(context, true), child: const Text('Lưu')),
        ],
      ),
    );

    if (result != true) return;

    final dto = {
      'code': codeCtrl.text.trim(),
      'name': nameCtrl.text.trim(),
      'description': descriptionCtrl.text.trim().isEmpty ? null : descriptionCtrl.text.trim(),
      'type': selectedType, // 0 = Percentage, 1 = FixedAmount
      'value': double.tryParse(valueCtrl.text.trim()) ?? 0,
      'minOrderAmount': double.tryParse(minOrderCtrl.text.trim()),
      'maxDiscountAmount': double.tryParse(maxDiscountCtrl.text.trim()),
      'usageLimit': int.tryParse(usageLimitCtrl.text.trim()),
      'isActive': isActive,
      'startDate': DateTime.now().toIso8601String(),
      'endDate': DateTime.now().add(const Duration(days: 30)).toIso8601String(),
    };

    try {
      if (promo == null) {
        await _service.create(dto);
      } else {
        final id = promo['id'] ?? promo['Id'] ?? promo['Id'.toLowerCase()];
        // Include Id in payload because server expects UpdatePromotionDto.Id (required)
        dto['id'] = id is int ? id : int.tryParse(id.toString());
        await _service.update(id as int, dto);
      }
      await _loadPromos();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi lưu khuyến mãi: $e')));
    }
  }

  Future<void> _deletePromo(Map<String, dynamic> promo) async {
    final id = promo['id'] ?? promo['Id'];
    if (id == null) return;
    final ok = await showDialog<bool>(context: context, builder: (c) => AlertDialog(
      title: const Text('Xóa khuyến mãi?'),
      content: const Text('Bạn có chắc muốn xóa hoặc tắt khuyến mãi này?'),
      actions: [TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Hủy')), ElevatedButton(onPressed: () => Navigator.pop(c, true), child: const Text('Xóa'))],
    ));
    if (ok != true) return;
    try {
      await _service.delete(id as int);
      await _loadPromos();
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Xóa thất bại: $e')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Quản lý khuyến mãi')), 
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadPromos,
              child: ListView.builder(
                itemCount: _promos.length,
                itemBuilder: (context, index) {
                  final p = _promos[index];
                  final code = p['code'] ?? p['Code'] ?? '';
                  final name = p['name'] ?? p['Name'] ?? '';
                  final value = p['value'] ?? p['Value'] ?? 0;
                  final active = p['isActive'] ?? p['IsActive'] ?? true;
                  return Card(
                    child: ListTile(
                      title: Text('$name (${code.toString().toUpperCase()})'),
                      subtitle: Text('Giá trị: $value - ${active ? 'Active' : 'Inactive'}'),
                      trailing: Row(mainAxisSize: MainAxisSize.min, children: [
                        IconButton(icon: const Icon(Icons.edit), onPressed: () => _showEditDialog(p)),
                        IconButton(icon: const Icon(Icons.delete), onPressed: () => _deletePromo(p)),
                      ]),
                    ),
                  );
                },
              ),
            ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showEditDialog(),
        child: const Icon(Icons.add),
      ),
    );
  }
}
