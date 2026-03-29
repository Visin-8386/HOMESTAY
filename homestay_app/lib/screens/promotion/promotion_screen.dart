import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_rating_bar/flutter_rating_bar.dart';

import '../../config/app_colors.dart';
import '../../models/homestay.dart';
import '../../services/api_service.dart';
// import '../../routes.dart'; // removed - unused

class PromotionScreen extends StatefulWidget {
  const PromotionScreen({super.key});

  @override
  State<PromotionScreen> createState() => _PromotionScreenState();
}

class _PromotionScreenState extends State<PromotionScreen> {
  final ApiService _apiService = ApiService();
  List<Homestay> _promotionalHomestays = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadPromotionalHomestays();
  }

  Future<void> _loadPromotionalHomestays() async {
    setState(() {
      _isLoading = true;
    });

    try {
      // For now, we'll load all homestays and simulate promotional ones
      // In a real app, you'd have a separate API endpoint for promotions
      final homestays = await _apiService.getHomestays();
      // Simulate promotional homestays by taking the first few
      final promotional = homestays.take(3).toList();

      setState(() {
        _promotionalHomestays = promotional;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _isLoading = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể tải khuyến mãi: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Khuyến mãi'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _promotionalHomestays.isEmpty
              ? _buildEmptyState()
              : _buildPromotionsList(),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.local_offer_outlined,
            size: 64,
            color: Colors.grey[400],
          ),
          const SizedBox(height: 16),
          Text(
            'Không có khuyến mãi nào',
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'Hiện tại chưa có chương trình khuyến mãi nào đang diễn ra',
            style: TextStyle(
              color: Colors.grey[500],
              fontSize: 14,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }

  Widget _buildPromotionsList() {
    return RefreshIndicator(
      onRefresh: _loadPromotionalHomestays,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _promotionalHomestays.length,
        itemBuilder: (context, index) {
          return _buildPromotionCard(_promotionalHomestays[index]);
        },
      ),
    );
  }

  Widget _buildPromotionCard(Homestay homestay) {
  // Simulate discount for promotional homestays
  const discountPercentage = 20.0; // Fixed 20% discount for demo
    final originalPrice = homestay.pricePerNight;
    final discountedPrice = originalPrice * (1 - discountPercentage / 100);

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: () => _navigateToHomestayDetail(homestay),
        borderRadius: BorderRadius.circular(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image with discount badge
            Stack(
              children: [
                ClipRRect(
                  borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
                  child: homestay.images.isNotEmpty
                      ? CachedNetworkImage(
                          imageUrl: homestay.images.first,
                          height: 200,
                          width: double.infinity,
                          fit: BoxFit.cover,
                          placeholder: (context, url) => Container(
                            height: 200,
                            color: Colors.grey[300],
                            child: const Center(child: CircularProgressIndicator()),
                          ),
                          errorWidget: (context, url, error) => Container(
                            height: 200,
                            color: Colors.grey[300],
                            child: const Icon(Icons.error),
                          ),
                        )
                      : Container(
                          height: 200,
                          color: Colors.grey[300],
                          child: const Icon(Icons.image, size: 50),
                        ),
                ),

                // Discount badge
                if (discountPercentage > 0)
                  Positioned(
                    top: 12,
                    left: 12,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.red,
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Text(
                        '-${discountPercentage.toInt()}%',
                        style: const TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  ),

                // Promotion label
                Positioned(
                  top: 12,
                  right: 12,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: AppColors.primary,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: const Text(
                      'KHUYẾN MÃI',
                      style: TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 10,
                      ),
                    ),
                  ),
                ),
              ],
            ),

            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Title and location
                  Text(
                    homestay.name,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),

                  const SizedBox(height: 4),

                  Row(
                    children: [
                      Icon(
                        Icons.location_on,
                        size: 16,
                        color: Colors.grey[600],
                      ),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          homestay.fullAddress,
                          style: TextStyle(
                            color: Colors.grey[600],
                            fontSize: 14,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 8),

                  // Rating
                  Row(
                    children: [
                      RatingBarIndicator(
                        rating: homestay.averageRating ?? 0.0,
                        itemBuilder: (context, index) => const Icon(
                          Icons.star,
                          color: Colors.amber,
                        ),
                        itemCount: 5,
                        itemSize: 16,
                      ),
                      const SizedBox(width: 8),
                      Text(
                        '${(homestay.averageRating ?? 0.0).toStringAsFixed(1)} (${homestay.reviewCount} đánh giá)',
                        style: TextStyle(
                          color: Colors.grey[600],
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 12),

                  // Price section
                  Row(
                    children: [
                      if (discountPercentage > 0) ...[
                        Text(
                          '${originalPrice.toStringAsFixed(0)}đ/đêm',
                          style: TextStyle(
                            color: Colors.grey[500],
                            fontSize: 14,
                            decoration: TextDecoration.lineThrough,
                          ),
                        ),
                        const SizedBox(width: 8),
                      ],
                      Text(
                        '${discountedPrice.toStringAsFixed(0)}đ/đêm',
                        style: TextStyle(
                          color: AppColors.primary,
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 8),

                  // Amenities preview
                  if (homestay.amenities.isNotEmpty)
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      children: homestay.amenities.take(3).map((amenity) {
                        return Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: Colors.grey[100],
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text(
                            amenity.name,
                            style: TextStyle(
                              color: Colors.grey[700],
                              fontSize: 12,
                            ),
                          ),
                        );
                      }).toList(),
                    ),

                  const SizedBox(height: 12),

                  // Book now button
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: () => _navigateToBooking(homestay),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      child: const Text(
                        'Đặt ngay',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _navigateToHomestayDetail(Homestay homestay) {
    // Navigate to homestay detail screen
    // This would typically go to a homestay detail screen
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Xem chi tiết ${homestay.name}')),
    );
  }

  void _navigateToBooking(Homestay homestay) {
    // Navigate to booking screen
    // This would typically go to the booking flow
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Đặt phòng ${homestay.name}')),
    );
  }
}
