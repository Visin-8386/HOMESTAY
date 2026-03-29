import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../providers/comparison_provider.dart';

class ComparisonScreen extends StatelessWidget {
  const ComparisonScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('So sánh homestay'),
        backgroundColor: AppColors.primary,
        actions: [
          Consumer<ComparisonProvider>(
            builder: (context, provider, child) {
              if (provider.count > 0) {
                return TextButton(
                  onPressed: () => provider.clearAll(),
                  child: const Text(
                    'Xóa tất cả',
                    style: TextStyle(color: Colors.white),
                  ),
                );
              }
              return const SizedBox.shrink();
            },
          ),
        ],
      ),
      body: Consumer<ComparisonProvider>(
        builder: (context, provider, child) {
          if (!provider.canCompare) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(
                    Icons.compare_arrows,
                    size: 80,
                    color: Colors.grey,
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Chưa có homestay nào để so sánh',
                    style: TextStyle(fontSize: 18, color: Colors.grey),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Thêm ít nhất 2 homestay để so sánh',
                    style: TextStyle(color: Colors.grey),
                  ),
                  const SizedBox(height: 24),
                  ElevatedButton.icon(
                    onPressed: () => Navigator.pop(context),
                    icon: const Icon(Icons.search),
                    label: const Text('Tìm homestay'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 24,
                        vertical: 12,
                      ),
                    ),
                  ),
                ],
              ),
            );
          }

          return SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: SingleChildScrollView(
              child: _buildComparisonTable(provider.selectedHomestays),
            ),
          );
        },
      ),
    );
  }

  Widget _buildComparisonTable(List<Homestay> homestays) {
    return DataTable(
      columnSpacing: 20,
      headingRowHeight: 50,
      dataRowHeight: 80,
      columns: [
        const DataColumn(
          label: Text(
            'Tiêu chí',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 16,
            ),
          ),
        ),
        ...homestays.map((h) => DataColumn(
              label: SizedBox(
                width: 150,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    if (h.images.isNotEmpty)
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: CachedNetworkImage(
                          imageUrl: h.images.first,
                          height: 40,
                          width: 150,
                          fit: BoxFit.cover,
                          placeholder: (context, url) => Container(
                            color: Colors.grey[300],
                            height: 40,
                          ),
                          errorWidget: (context, url, error) =>
                              const Icon(Icons.error),
                        ),
                      ),
                  ],
                ),
              ),
            )),
      ],
      rows: [
        // Name
        DataRow(
          cells: [
            const DataCell(Text('Tên', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  SizedBox(
                    width: 150,
                    child: Text(
                      h.name,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                )),
          ],
        ),
        // Price
        DataRow(
          cells: [
            const DataCell(Text('Giá/đêm', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Text(
                    h.priceDisplay,
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                )),
          ],
        ),
        // Rating
        DataRow(
          cells: [
            const DataCell(Text('Đánh giá', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Row(
                    children: [
                      const Icon(Icons.star, color: Colors.amber, size: 16),
                      const SizedBox(width: 4),
                      Text(
                        h.averageRating?.toStringAsFixed(1) ?? 'N/A',
                        style: const TextStyle(fontWeight: FontWeight.bold),
                      ),
                      Text(' (${h.reviewCount})'),
                    ],
                  ),
                )),
          ],
        ),
        // Location
        DataRow(
          cells: [
            const DataCell(Text('Địa điểm', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  SizedBox(
                    width: 150,
                    child: Text(
                      h.city,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                )),
          ],
        ),
        // Max Guests
        DataRow(
          cells: [
            const DataCell(Text('Số khách', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Row(
                    children: [
                      const Icon(Icons.people, size: 16),
                      const SizedBox(width: 4),
                      Text('${h.maxGuests}'),
                    ],
                  ),
                )),
          ],
        ),
        // Bedrooms
        DataRow(
          cells: [
            const DataCell(Text('Phòng ngủ', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Row(
                    children: [
                      const Icon(Icons.bed, size: 16),
                      const SizedBox(width: 4),
                      Text('${h.numberOfBedrooms}'),
                    ],
                  ),
                )),
          ],
        ),
        // Bathrooms
        DataRow(
          cells: [
            const DataCell(Text('Phòng tắm', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Row(
                    children: [
                      const Icon(Icons.bathroom, size: 16),
                      const SizedBox(width: 4),
                      Text('${h.numberOfBathrooms}'),
                    ],
                  ),
                )),
          ],
        ),
        // Amenities
        DataRow(
          cells: [
            const DataCell(Text('Tiện nghi', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  SizedBox(
                    width: 150,
                    child: Wrap(
                      spacing: 4,
                      runSpacing: 4,
                      children: h.amenities.take(3).map((a) => Chip(
                            label: Text(
                              a.name,
                              style: const TextStyle(fontSize: 10),
                            ),
                            materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            visualDensity: VisualDensity.compact,
                          )).toList(),
                    ),
                  ),
                )),
          ],
        ),
        // Host
        DataRow(
          cells: [
            const DataCell(Text('Chủ nhà', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Text(h.hostName),
                )),
          ],
        ),
        // Actions
        DataRow(
          cells: [
            const DataCell(Text('Hành động', style: TextStyle(fontWeight: FontWeight.bold))),
            ...homestays.map((h) => DataCell(
                  Builder(
                    builder: (context) => Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        ElevatedButton(
                          onPressed: () {
                            Navigator.pushNamed(
                              context,
                              '/homestay-detail',
                              arguments: h.id,
                            );
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                            minimumSize: const Size(100, 32),
                          ),
                          child: const Text('Xem chi tiết', style: TextStyle(fontSize: 11)),
                        ),
                        const SizedBox(height: 4),
                        TextButton(
                          onPressed: () {
                            Provider.of<ComparisonProvider>(context, listen: false)
                                .removeHomestay(h.id);
                          },
                          style: TextButton.styleFrom(
                            foregroundColor: Colors.red,
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                            minimumSize: const Size(100, 28),
                          ),
                          child: const Text('Xóa', style: TextStyle(fontSize: 11)),
                        ),
                      ],
                    ),
                  ),
                )),
          ],
        ),
      ],
    );
  }
}
