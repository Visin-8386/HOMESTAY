import 'package:flutter/material.dart';

import '../../config/app_colors.dart';

class FilterDialog extends StatefulWidget {
  final double? minPrice;
  final double? maxPrice;
  final List<int>? selectedAmenities;
  final double? minRating;
  final double? locationRadius;

  const FilterDialog({
    Key? key,
    this.minPrice,
    this.maxPrice,
    this.selectedAmenities,
    this.minRating,
    this.locationRadius,
  }) : super(key: key);

  @override
  State<FilterDialog> createState() => _FilterDialogState();
}

class _FilterDialogState extends State<FilterDialog> {
  late RangeValues _priceRange;
  late List<int> _selectedAmenities;
  late double _minRating;
  late double _locationRadius;

  // Danh sách amenities mẫu
  final List<Map<String, dynamic>> _availableAmenities = [
    {'id': 1, 'name': 'WiFi', 'icon': Icons.wifi},
    {'id': 2, 'name': 'Máy lạnh', 'icon': Icons.ac_unit},
    {'id': 3, 'name': 'Bếp', 'icon': Icons.kitchen},
    {'id': 4, 'name': 'Bãi đỗ xe', 'icon': Icons.local_parking},
    {'id': 5, 'name': 'Hồ bơi', 'icon': Icons.pool},
    {'id': 6, 'name': 'TV', 'icon': Icons.tv},
    {'id': 7, 'name': 'Máy giặt', 'icon': Icons.local_laundry_service},
    {'id': 8, 'name': 'Phòng gym', 'icon': Icons.fitness_center},
  ];

  @override
  void initState() {
    super.initState();
    _priceRange = RangeValues(
      widget.minPrice ?? 0,
      widget.maxPrice ?? 10000000,
    );
    _selectedAmenities = widget.selectedAmenities ?? [];
    _minRating = widget.minRating ?? 0;
    _locationRadius = widget.locationRadius ?? 50;
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      child: Container(
        constraints: const BoxConstraints(maxHeight: 600),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildHeader(),
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _buildPriceRangeSection(),
                    const SizedBox(height: 24),
                    _buildAmenitiesSection(),
                    const SizedBox(height: 24),
                    _buildRatingSection(),
                    const SizedBox(height: 24),
                    _buildLocationRadiusSection(),
                  ],
                ),
              ),
            ),
            _buildFooter(),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.primary,
        borderRadius: const BorderRadius.only(
          topLeft: Radius.circular(16),
          topRight: Radius.circular(16),
        ),
      ),
      child: Row(
        children: [
          const Icon(Icons.filter_list, color: Colors.white),
          const SizedBox(width: 12),
          const Expanded(
            child: Text(
              'Bộ lọc tìm kiếm',
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.close, color: Colors.white),
            onPressed: () => Navigator.pop(context),
          ),
        ],
      ),
    );
  }

  Widget _buildPriceRangeSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Icon(Icons.attach_money, size: 20, color: AppColors.primary),
            const SizedBox(width: 8),
            Text(
              'Khoảng giá',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Tối thiểu', style: TextStyle(fontSize: 12)),
                  Text(
                    '${_formatPrice(_priceRange.start)}',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                    ),
                  ),
                ],
              ),
            ),
            const Icon(Icons.arrow_forward, size: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  const Text('Tối đa', style: TextStyle(fontSize: 12)),
                  Text(
                    '${_formatPrice(_priceRange.end)}',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        RangeSlider(
          values: _priceRange,
          min: 0,
          max: 10000000,
          divisions: 100,
          activeColor: AppColors.primary,
          labels: RangeLabels(
            _formatPrice(_priceRange.start),
            _formatPrice(_priceRange.end),
          ),
          onChanged: (RangeValues values) {
            setState(() {
              _priceRange = values;
            });
          },
        ),
      ],
    );
  }

  Widget _buildAmenitiesSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Icon(Icons.check_circle_outline, size: 20, color: AppColors.primary),
            const SizedBox(width: 8),
            Text(
              'Tiện nghi',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: _availableAmenities.map((amenity) {
            final isSelected = _selectedAmenities.contains(amenity['id']);
            return FilterChip(
              selected: isSelected,
              label: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    amenity['icon'] as IconData,
                    size: 16,
                    color: isSelected ? Colors.white : AppColors.textSecondary,
                  ),
                  const SizedBox(width: 6),
                  Text(amenity['name'] as String),
                ],
              ),
              selectedColor: AppColors.primary,
              labelStyle: TextStyle(
                color: isSelected ? Colors.white : AppColors.textPrimary,
              ),
              onSelected: (bool selected) {
                setState(() {
                  if (selected) {
                    _selectedAmenities.add(amenity['id'] as int);
                  } else {
                    _selectedAmenities.remove(amenity['id']);
                  }
                });
              },
            );
          }).toList(),
        ),
      ],
    );
  }

  Widget _buildRatingSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Icon(Icons.star, size: 20, color: AppColors.primary),
            const SizedBox(width: 8),
            Text(
              'Đánh giá tối thiểu',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: List.generate(5, (index) {
            final rating = index + 1;
            final isSelected = _minRating >= rating;
            return Expanded(
              child: GestureDetector(
                onTap: () {
                  setState(() {
                    _minRating = rating.toDouble();
                  });
                },
                child: Container(
                  margin: const EdgeInsets.symmetric(horizontal: 4),
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  decoration: BoxDecoration(
                    color: isSelected ? AppColors.primary : Colors.grey[200],
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Column(
                    children: [
                      Icon(
                        Icons.star,
                        color: isSelected ? Colors.white : Colors.grey,
                        size: 20,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '$rating+',
                        style: TextStyle(
                          color: isSelected ? Colors.white : Colors.grey,
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            );
          }),
        ),
        const SizedBox(height: 8),
        Center(
          child: TextButton(
            onPressed: () {
              setState(() {
                _minRating = 0;
              });
            },
            child: const Text('Xóa bộ lọc đánh giá'),
          ),
        ),
      ],
    );
  }

  Widget _buildLocationRadiusSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Icon(Icons.location_on, size: 20, color: AppColors.primary),
            const SizedBox(width: 8),
            Text(
              'Bán kính tìm kiếm',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Text(
          '${_locationRadius.toInt()} km',
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            color: AppColors.primary,
          ),
        ),
        Slider(
          value: _locationRadius,
          min: 1,
          max: 100,
          divisions: 99,
          activeColor: AppColors.primary,
          label: '${_locationRadius.toInt()} km',
          onChanged: (double value) {
            setState(() {
              _locationRadius = value;
            });
          },
        ),
      ],
    );
  }

  Widget _buildFooter() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey[100],
        borderRadius: const BorderRadius.only(
          bottomLeft: Radius.circular(16),
          bottomRight: Radius.circular(16),
        ),
      ),
      child: Row(
        children: [
          Expanded(
            child: OutlinedButton(
              onPressed: () {
                setState(() {
                  _priceRange = const RangeValues(0, 10000000);
                  _selectedAmenities = [];
                  _minRating = 0;
                  _locationRadius = 50;
                });
              },
              style: OutlinedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 12),
              ),
              child: const Text('Đặt lại'),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            flex: 2,
            child: ElevatedButton(
              onPressed: () {
                Navigator.pop(context, {
                  'minPrice': _priceRange.start,
                  'maxPrice': _priceRange.end,
                  'selectedAmenities': _selectedAmenities,
                  'minRating': _minRating,
                  'locationRadius': _locationRadius,
                });
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                padding: const EdgeInsets.symmetric(vertical: 12),
              ),
              child: const Text('Áp dụng'),
            ),
          ),
        ],
      ),
    );
  }

  String _formatPrice(double price) {
    if (price >= 1000000) {
      return '${(price / 1000000).toStringAsFixed(1)}tr';
    } else if (price >= 1000) {
      return '${(price / 1000).toStringAsFixed(0)}k';
    }
    return price.toStringAsFixed(0);
  }
}
