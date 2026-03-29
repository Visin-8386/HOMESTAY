import 'package:flutter/material.dart';
import 'package:flutter_rating_bar/flutter_rating_bar.dart';

import '../config/app_colors.dart';
import '../models/homestay.dart';
import '../services/booking_service.dart';
import 'homestay_image_widget.dart';

class HomestayCard extends StatelessWidget {
  final Homestay homestay;
  final VoidCallback onTap;
  final Widget? favoriteButton;
  final VoidCallback? onShare;
  final VoidCallback? onCompare;

  const HomestayCard({
    super.key,
    required this.homestay,
    required this.onTap,
    this.favoriteButton,
    this.onShare,
    this.onCompare,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      margin: const EdgeInsets.only(bottom: 16),
      child: InkWell(
        onTap: onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image with favorite button overlay
            Stack(
              children: [
                SizedBox(
                  height: 200,
                  width: double.infinity,
                  child: homestay.images.isNotEmpty
                  ? HomestayImageWidget(
                      imageUrl: homestay.images.first,
                      height: 200,
                      fit: BoxFit.cover,
                    )
                  : const NoImagePlaceholder(
                      height: 200,
                    ),
                ),
                if (favoriteButton != null)
                  Positioned(
                    top: 8,
                    right: 8,
                    child: favoriteButton!,
                  ),
                if (onShare != null)
                  Positioned(
                    top: 8,
                    left: 8,
                    child: Material(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(20),
                      elevation: 2,
                      child: InkWell(
                        borderRadius: BorderRadius.circular(20),
                        onTap: () {
                          onShare!();
                        },
                        child: const Padding(
                          padding: EdgeInsets.all(8),
                          child: Icon(
                            Icons.share,
                            size: 20,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    homestay.name,
                    style: Theme.of(context).textTheme.titleLarge,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Icon(Icons.location_on, size: 16, color: AppColors.textSecondary),
                      const SizedBox(width: 4),
                      Expanded(
                        child: Text(
                          homestay.fullAddress,
                          style: Theme.of(context).textTheme.bodyMedium,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      if (homestay.averageRating != null && (homestay.averageRating ?? 0) > 0) ...[
                        RatingBarIndicator(
                          rating: homestay.averageRating!,
                          itemBuilder: (context, index) => const Icon(
                            Icons.star,
                            color: AppColors.rating,
                          ),
                          itemCount: 5,
                          itemSize: 16,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          '${homestay.averageRating!.toStringAsFixed(1)} (${homestay.reviewCount})',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ] else ...[
                        // Fallback: fetch reviews for this homestay (from bookings) and compute average
                        FutureBuilder<List<dynamic>>(
                          future: BookingService().getHomestayReviews(homestay.id, page: 1, pageSize: 10),
                          builder: (context, snapshot) {
                            if (!snapshot.hasData) return const SizedBox();
                            final reviews = snapshot.data!;
                            if (reviews.isEmpty) return const SizedBox();
                            final sum = reviews.fold<double>(0.0, (prev, r) {
                              try {
                                final rating = (r as dynamic).rating;
                                if (rating is num) return prev + rating.toDouble();
                                final parsed = double.tryParse(rating.toString());
                                return prev + (parsed ?? 0.0);
                              } catch (_) {
                                return prev;
                              }
                            });
                            final avg = sum / reviews.length;
                            return Row(
                              children: [
                                RatingBarIndicator(
                                  rating: avg,
                                  itemBuilder: (context, index) => const Icon(Icons.star, color: AppColors.rating),
                                  itemCount: 5,
                                  itemSize: 16,
                                ),
                                const SizedBox(width: 4),
                                Text(
                                  '${avg.toStringAsFixed(1)} (${reviews.length})',
                                  style: Theme.of(context).textTheme.bodySmall,
                                ),
                              ],
                            );
                          },
                        ),
                      ],
                      const Spacer(),
                      Text(
                        homestay.priceDisplay,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          color: AppColors.primary,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      _buildInfoChip(Icons.people, homestay.guestsDisplay),
                      _buildInfoChip(Icons.bed, homestay.bedroomsDisplay),
                      _buildInfoChip(Icons.bathroom, homestay.bathroomsDisplay),
                    ],
                  ),
                  if (onCompare != null) ...[
                    const SizedBox(height: 8),
                    OutlinedButton.icon(
                      onPressed: onCompare,
                      icon: const Icon(Icons.compare_arrows, size: 16),
                      label: const Text('So sánh', style: TextStyle(fontSize: 12)),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.primary,
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                        minimumSize: const Size(0, 32),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoChip(IconData icon, String label) {
    return Chip(
      avatar: Icon(icon, size: 16),
      label: Text(label, style: const TextStyle(fontSize: 12)),
      padding: EdgeInsets.zero,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
    );
  }
}
