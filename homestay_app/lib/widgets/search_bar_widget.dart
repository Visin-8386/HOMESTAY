import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../config/app_colors.dart';
import '../providers/homestay_provider.dart';

class SearchBarWidget extends StatelessWidget {
  const SearchBarWidget({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha((0.05 * 255).round()),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: InkWell(
        onTap: () => _showSearchBottomSheet(context),
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          decoration: BoxDecoration(
            color: Colors.grey[100],
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppColors.border),
          ),
          child: Row(
            children: [
              const Icon(Icons.search, color: AppColors.textSecondary),
              const SizedBox(width: 12),
              Expanded(
                child: Consumer<HomestayProvider>(
                  builder: (context, provider, child) {
                    final city = provider.searchCity;
                    final checkIn = provider.checkInDate;
                    final guests = provider.numberOfGuests;
                    
                    String searchText = 'Tìm kiếm homestay...';
                    if (city != null || checkIn != null || guests != null) {
                      final parts = <String>[];
                      if (city != null) parts.add(city);
                      if (checkIn != null) parts.add('Ngày ${checkIn.day}/${checkIn.month}');
                      if (guests != null) parts.add('$guests khách');
                      searchText = parts.join(' • ');
                    }
                    
                    return Text(
                      searchText,
                      style: TextStyle(
                        color: city != null || checkIn != null || guests != null
                            ? AppColors.textPrimary
                            : AppColors.textSecondary,
                      ),
                    );
                  },
                ),
              ),
              const Icon(Icons.tune, color: AppColors.primary),
            ],
          ),
        ),
      ),
    );
  }

  void _showSearchBottomSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (context) => const SearchFilterBottomSheet(),
    );
  }
}

class SearchFilterBottomSheet extends StatefulWidget {
  const SearchFilterBottomSheet({super.key});

  @override
  State<SearchFilterBottomSheet> createState() => _SearchFilterBottomSheetState();
}

class _SearchFilterBottomSheetState extends State<SearchFilterBottomSheet> {
  final _cityController = TextEditingController();
  DateTime? _checkIn;
  DateTime? _checkOut;
  int _guests = 1;

  @override
  void initState() {
    super.initState();
    final provider = Provider.of<HomestayProvider>(context, listen: false);
    _cityController.text = provider.searchCity ?? '';
    _checkIn = provider.checkInDate;
    _checkOut = provider.checkOutDate;
    _guests = provider.numberOfGuests ?? 1;
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.9,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) {
        return Container(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  TextButton(
                    onPressed: () {
                      final provider = Provider.of<HomestayProvider>(context, listen: false);
                      provider.clearSearchFilters();
                      _cityController.clear();
                      setState(() {
                        _checkIn = null;
                        _checkOut = null;
                        _guests = 1;
                      });
                    },
                    child: const Text('Xóa bộ lọc'),
                  ),
                  Text(
                    'Tìm kiếm',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  TextButton(
                    onPressed: () => Navigator.pop(context),
                    child: const Text('Đóng'),
                  ),
                ],
              ),
              const Divider(),
              Expanded(
                child: ListView(
                  controller: scrollController,
                  children: [
                    TextField(
                      controller: _cityController,
                      decoration: const InputDecoration(
                        labelText: 'Thành phố',
                        prefixIcon: Icon(Icons.location_city),
                      ),
                    ),
                    const SizedBox(height: 16),
                    ListTile(
                      title: const Text('Ngày nhận phòng'),
                      subtitle: Text(_checkIn != null
                          ? '${_checkIn!.day}/${_checkIn!.month}/${_checkIn!.year}'
                          : 'Chọn ngày'),
                      leading: const Icon(Icons.calendar_today),
                      onTap: () async {
                        final date = await showDatePicker(
                          context: context,
                          initialDate: _checkIn ?? DateTime.now(),
                          firstDate: DateTime.now(),
                          lastDate: DateTime.now().add(const Duration(days: 365)),
                        );
                        if (date != null) {
                          setState(() {
                            _checkIn = date;
                          });
                        }
                      },
                    ),
                    ListTile(
                      title: const Text('Ngày trả phòng'),
                      subtitle: Text(_checkOut != null
                          ? '${_checkOut!.day}/${_checkOut!.month}/${_checkOut!.year}'
                          : 'Chọn ngày'),
                      leading: const Icon(Icons.calendar_today),
                      onTap: () async {
                        final date = await showDatePicker(
                          context: context,
                          initialDate: _checkOut ?? (_checkIn ?? DateTime.now()),
                          firstDate: _checkIn ?? DateTime.now(),
                          lastDate: DateTime.now().add(const Duration(days: 365)),
                        );
                        if (date != null) {
                          setState(() {
                            _checkOut = date;
                          });
                        }
                      },
                    ),
                    const SizedBox(height: 16),
                    Text('Số khách: $_guests', style: Theme.of(context).textTheme.titleMedium),
                    Slider(
                      value: _guests.toDouble(),
                      min: 1,
                      max: 10,
                      divisions: 9,
                      label: '$_guests khách',
                      onChanged: (value) {
                        setState(() {
                          _guests = value.toInt();
                        });
                      },
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: () async {
                  Navigator.pop(context);
                  // TODO: Show FilterDialog
                  // final result = await showDialog<Map<String, dynamic>>(
                  //   context: context,
                  //   builder: (context) => FilterDialog(),
                  // );
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bộ lọc nâng cao - Đang phát triển')),
                  );
                },
                icon: const Icon(Icons.tune),
                label: const Text('Bộ lọc nâng cao (Giá, Tiện nghi, Đánh giá...)'),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () {
                    final provider = Provider.of<HomestayProvider>(context, listen: false);
                    provider.setSearchFilters(
                      city: _cityController.text.trim().isEmpty ? null : _cityController.text.trim(),
                      checkIn: _checkIn,
                      checkOut: _checkOut,
                      guests: _guests,
                    );
                    provider.searchHomestays();
                    Navigator.pop(context);
                  },
                  child: const Text('Tìm kiếm'),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
