import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_rating_bar/flutter_rating_bar.dart';
import 'package:photo_view/photo_view.dart';
import 'package:photo_view/photo_view_gallery.dart';
import 'package:provider/provider.dart';
import 'package:webview_flutter/webview_flutter.dart';

import '../../config/api_config.dart';
import '../../config/app_colors.dart';
import '../../models/booking.dart';
import '../../models/homestay.dart';
import '../../models/weather.dart';
import '../../providers/auth_provider_fixed.dart';
import '../../providers/favorites_provider.dart';
import '../../providers/homestay_provider.dart';
import '../../services/booking_service.dart';
// Add missing imports for chat feature
import '../../services/chat_service.dart';
import '../../services/share_service.dart';
import '../../services/weather_service.dart';
import '../../widgets/booking_calendar.dart';
import '../../widgets/user_gradient_background.dart';
import '../chat/chat_screen.dart';

class HomestayDetailScreen extends StatefulWidget {
  final int homestayId;

  const HomestayDetailScreen({super.key, required this.homestayId});

  @override
  State<HomestayDetailScreen> createState() => _HomestayDetailScreenState();
}

class _HomestayDetailScreenState extends State<HomestayDetailScreen> {
  // FavoriteService removed
  DateTime? _checkIn;
  DateTime? _checkOut;
  int _guests = 1;
  // favorite state removed

  @override
  void initState() {
    super.initState();
  // favorites removed - no status to check
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = Provider.of<HomestayProvider>(context, listen: false);
      provider.loadHomestayById(widget.homestayId);
      // load favorites into provider
      final fav = Provider.of<FavoritesProvider>(context, listen: false);
      if (!fav.isLoaded) fav.loadFavorites();
    });
  }

  // favorites removed - toggle and check functions removed

  Future<void> _shareHomestay(Homestay homestay) async {
    try {
      await ShareService.shareHomestay(homestay);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi khi chia sẻ: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: UserGradientBackground(
        child: Consumer<HomestayProvider>(
          builder: (context, provider, child) {
            if (provider.isLoading) {
              return const Center(child: CircularProgressIndicator());
            }

            final homestay = provider.selectedHomestay;
            if (homestay == null) {
              return const Center(child: Text('Không tìm thấy thông tin'));
            }

            return CustomScrollView(
              slivers: [
                SliverAppBar(
                  expandedHeight: 300,
                  pinned: true,
                  actions: [
                    IconButton(
                      onPressed: () => _shareHomestay(homestay),
                      icon: const Icon(Icons.share, color: Colors.white),
                    ),
                    Consumer<FavoritesProvider>(
                      builder: (context, favProv, child) {
                        final isFav = favProv.isFavorite(homestay.id);
                        return IconButton(
                          onPressed: () async {
                            await favProv.toggleFavorite(homestay.id);
                            if (mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text(isFav ? 'Đã xóa khỏi yêu thích' : 'Đã thêm vào yêu thích'),
                                ),
                              );
                            }
                          },
                          icon: Icon(
                            isFav ? Icons.favorite : Icons.favorite_border,
                            color: Colors.white,
                          ),
                        );
                      },
                    ),
                  ],
                  flexibleSpace: FlexibleSpaceBar(
                    background: _buildImageGallery(homestay),
                  ),
                ),
                SliverToBoxAdapter(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _buildBasicInfo(homestay),
                      _buildVideoSection(homestay),
                      _buildWeatherSection(homestay),
                      const Divider(thickness: 8),
                      _buildHostInfo(homestay),
                      const Divider(thickness: 8),
                      _buildAmenities(homestay),
                      const Divider(thickness: 8),
                      _buildDescription(homestay),
                      const Divider(thickness: 8),
                      _buildReviews(homestay),
                      const SizedBox(height: 100),
                    ],
                  ),
                ),
              ],
            );
          },
        ),
      ),
      bottomNavigationBar: _buildBookingBar(),
    );
  }

  Widget _buildImageGallery(Homestay homestay) {
    if (homestay.images.isEmpty) {
      return Container(
        color: Colors.grey[300],
        child: const Center(child: Icon(Icons.home_work, size: 80)),
      );
    }

    return GestureDetector(
      onTap: () => _showImageGallery(homestay.images),
      child: Builder(builder: (context) {
        // Ensure the image URL is absolute
        var raw = homestay.images.first;
        var imageUrl = raw.startsWith('http') ? raw : '${ApiConfig.baseUrl}$raw';
        return CachedNetworkImage(
          imageUrl: imageUrl,
          fit: BoxFit.cover,
          placeholder: (context, url) => const Center(child: CircularProgressIndicator()),
          errorWidget: (context, url, error) => Container(
            color: Colors.grey[300],
            child: const Center(child: Icon(Icons.broken_image, size: 48)),
          ),
        );
      }),
    );
  }

  Widget _buildVideoSection(Homestay homestay) {
    final id = homestay.youtubeVideoId;
    if (id == null || id.isEmpty) return const SizedBox.shrink();

    final embedUrl = 'https://www.youtube.com/embed/$id?rel=0&modestbranding=1';

    final controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..loadRequest(Uri.parse(embedUrl));

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: AspectRatio(
        aspectRatio: 16 / 9,
        child: ClipRRect(
          borderRadius: BorderRadius.circular(12),
          child: WebViewWidget(controller: controller),
        ),
      ),
    );
  }

  Widget _buildWeatherSection(Homestay homestay) {
    // Use a FutureBuilder so we don't need to manage extra state here
    return FutureBuilder<Weather?>(
      future: WeatherService().getCurrentWeather(homestay.latitude, homestay.longitude),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Padding(
            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: SizedBox(
              height: 56,
              child: Center(child: SizedBox(width: 28, height: 28, child: CircularProgressIndicator())),
            ),
          );
        }

        if (!snapshot.hasData || snapshot.data == null) {
          // No weather available — do not take space
          return const SizedBox.shrink();
        }

        final weather = snapshot.data!;
        final code = int.tryParse(weather.weatherCode) ?? -1;

        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
            child: Padding(
              padding: const EdgeInsets.all(12.0),
              child: Row(
                children: [
                  _weatherIcon(code),
                  const SizedBox(width: 12),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('${weather.temperatureC.toStringAsFixed(1)}°C', style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 4),
                      Text('Gió: ${weather.windSpeed} m/s', style: Theme.of(context).textTheme.bodyMedium),
                      const SizedBox(height: 4),
                      Text('Cập nhật: ${weather.time}', style: Theme.of(context).textTheme.bodySmall),
                    ],
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _weatherIcon(int code) {
    // Map Open-Meteo weather codes to icons (simplified)
    IconData icon;
    Color color = Colors.orange.shade700;
    if (code == 0) {
      icon = Icons.wb_sunny;
      color = Colors.orange;
    } else if (code == 1 || code == 2 || code == 3) {
      icon = Icons.wb_cloudy;
      color = Colors.blueGrey;
    } else if (code == 45 || code == 48) {
      icon = Icons.cloud;
      color = Colors.grey;
    } else if ((code >= 51 && code <= 67) || (code >= 80 && code <= 82)) {
      icon = Icons.grain; // rain-like
      color = Colors.blue;
    } else if ((code >= 71 && code <= 77) || (code >= 85 && code <= 86)) {
      icon = Icons.ac_unit; // snow
      color = Colors.lightBlue;
    } else if (code >= 95) {
      icon = Icons.flash_on; // thunder
      color = Colors.deepPurple;
    } else {
      icon = Icons.cloud_queue;
      color = Colors.grey;
    }

    return Container(
      padding: const EdgeInsets.all(6),
      decoration: BoxDecoration(color: color.withOpacity(0.12), borderRadius: BorderRadius.circular(8)),
      child: Icon(icon, size: 42, color: color),
    );
  }

  void _showImageGallery(List<String> images) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => Scaffold(
          appBar: AppBar(backgroundColor: Colors.black),
          body: PhotoViewGallery.builder(
            itemCount: images.length,
            builder: (context, index) {
              return PhotoViewGalleryPageOptions(
                imageProvider: CachedNetworkImageProvider(images[index]),
                minScale: PhotoViewComputedScale.contained,
                maxScale: PhotoViewComputedScale.covered * 2,
              );
            },
            backgroundDecoration: const BoxDecoration(color: Colors.black),
          ),
        ),
      ),
    );
  }

  Widget _buildBasicInfo(Homestay homestay) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(homestay.name, style: Theme.of(context).textTheme.headlineMedium),
          const SizedBox(height: 8),
          Row(
            children: [
              if (homestay.averageRating != null) ...[
                RatingBarIndicator(
                  rating: homestay.averageRating!,
                  itemBuilder: (context, index) => const Icon(Icons.star, color: AppColors.rating),
                  itemCount: 5,
                  itemSize: 20,
                ),
                const SizedBox(width: 8),
                Text('${homestay.averageRating!.toStringAsFixed(1)} (${homestay.reviewCount} đánh giá)'),
              ],
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              const Icon(Icons.location_on, size: 20, color: AppColors.textSecondary),
              const SizedBox(width: 4),
              Expanded(child: Text(homestay.fullAddress)),
            ],
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            children: [
              _buildInfoItem(Icons.people, homestay.guestsDisplay),
              _buildInfoItem(Icons.bed, homestay.bedroomsDisplay),
              _buildInfoItem(Icons.bathroom, homestay.bathroomsDisplay),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInfoItem(IconData icon, String label) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 20, color: AppColors.primary),
        const SizedBox(width: 4),
        Text(label),
      ],
    );
  }

  Widget _buildHostInfo(Homestay homestay) {
    return ListTile(
      leading: CircleAvatar(
        backgroundImage: homestay.hostAvatar != null
            ? NetworkImage(homestay.hostAvatar!)
            : null,
        child: homestay.hostAvatar == null ? const Icon(Icons.person) : null,
      ),
      title: Text('Chủ nhà: ${homestay.hostName}'),
      subtitle: const Text('Xem hồ sơ'),
      trailing: const Icon(Icons.chevron_right),
      onTap: () async {
        // Tạo hoặc mở cuộc trò chuyện với chủ nhà
        final chatService = ChatService();
        final authProvider = Provider.of<AuthProvider>(context, listen: false);
        final currentUser = authProvider.user;
        if (currentUser == null) {
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Bạn cần đăng nhập để nhắn tin với chủ nhà.')));
          return;
        }
        // Tạo hoặc lấy conversation giữa currentUser và host
        final conversation = await chatService.createOrGetConversation(currentUser.id, homestay.hostId);
        if (conversation == null) {
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không thể tạo cuộc trò chuyện.')));
          return;
        }
        if (!mounted) return;
        Navigator.of(context).push(MaterialPageRoute(
          builder: (_) => ChatScreen(conversation: conversation),
        ));
      },
    );
  }

  Widget _buildAmenities(Homestay homestay) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Tiện nghi', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 16),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: homestay.amenities.map((amenity) {
              return Chip(label: Text(amenity.name));
            }).toList(),
          ),
        ],
      ),
    );
  }

  Widget _buildDescription(Homestay homestay) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Mô tả', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          Text(homestay.description),
        ],
      ),
    );
  }

  Widget _buildReviews(Homestay homestay) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Đánh giá', style: Theme.of(context).textTheme.titleLarge),
                if (homestay.reviewCount > 0)
                TextButton(
                  onPressed: () {
                    Navigator.pushNamed(
                      context,
                      '/all-reviews',
                      arguments: {'homestayId': homestay.id},
                    );
                  },
                  child: Text('Xem tất cả (${homestay.reviewCount})'),
                ),
            ],
          ),
          const SizedBox(height: 16),
          
          // Rating Summary
          if (homestay.reviewCount > 0) ...[
            Row(
              children: [
                Text(
                  (homestay.averageRating ?? 0.0).toStringAsFixed(1),
                  style: const TextStyle(
                    fontSize: 48,
                    fontWeight: FontWeight.bold,
                    color: AppColors.primary,
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      RatingBarIndicator(
                        rating: homestay.averageRating ?? 0.0,
                        itemBuilder: (context, _) => const Icon(
                          Icons.star,
                          color: AppColors.rating,
                        ),
                        itemCount: 5,
                        itemSize: 24,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${homestay.reviewCount} đánh giá',
                        style: TextStyle(
                          color: Colors.grey[600],
                          fontSize: 14,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            
            // Recent Reviews Preview (Show first 2)
            FutureBuilder<List<Review>>(
              future: _loadReviews(homestay.id),
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const Center(
                    child: Padding(
                      padding: EdgeInsets.all(16),
                      child: CircularProgressIndicator(),
                    ),
                  );
                }
                
                if (!snapshot.hasData || snapshot.data!.isEmpty) {
                  return const SizedBox();
                }
                
                final reviews = snapshot.data!.take(2).toList();
                
                return Column(
                  children: reviews.map((review) => _buildReviewCard(review)).toList(),
                );
              },
            ),
          ] else ...[
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: Colors.grey[100],
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                children: [
                  Icon(Icons.star_border, size: 48, color: Colors.grey[400]),
                  const SizedBox(height: 12),
                  Text(
                    'Chưa có đánh giá',
                    style: TextStyle(
                      fontSize: 16,
                      color: Colors.grey[600],
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Hãy là người đầu tiên đánh giá homestay này',
                    style: TextStyle(
                      fontSize: 13,
                      color: Colors.grey[500],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
  
  Future<List<Review>> _loadReviews(int homestayId) async {
    try {
      final bookingService = BookingService();
      return await bookingService.getHomestayReviews(homestayId, page: 1, pageSize: 2);
    } catch (e) {
      return [];
    }
  }
  
  Widget _buildReviewCard(Review review) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundImage: review.userAvatar != null
                      ? NetworkImage(review.userAvatar!)
                      : null,
                  child: review.userAvatar == null
                      ? Text(review.userName[0].toUpperCase())
                      : null,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        review.userName,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 15,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        _formatDate(review.createdAt),
                        style: TextStyle(
                          color: Colors.grey[600],
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                RatingBarIndicator(
                  rating: review.rating.toDouble(),
                  itemBuilder: (context, _) => const Icon(
                    Icons.star,
                    color: AppColors.rating,
                  ),
                  itemCount: 5,
                  itemSize: 16,
                ),
              ],
            ),
            if (review.comment != null && review.comment!.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(
                review.comment!,
                style: const TextStyle(fontSize: 14, height: 1.5),
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final difference = now.difference(date);
    
    if (difference.inDays == 0) {
      return 'Hôm nay';
    } else if (difference.inDays == 1) {
      return 'Hôm qua';
    } else if (difference.inDays < 7) {
      return '${difference.inDays} ngày trước';
    } else if (difference.inDays < 30) {
      return '${(difference.inDays / 7).floor()} tuần trước';
    } else if (difference.inDays < 365) {
      return '${(difference.inDays / 30).floor()} tháng trước';
    } else {
      return '${date.day}/${date.month}/${date.year}';
    }
  }

  Widget _buildBookingBar() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 8,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        child: Consumer<HomestayProvider>(
          builder: (context, provider, child) {
            final homestay = provider.selectedHomestay;
            if (homestay == null) return const SizedBox();

            return Row(
              children: [
                Expanded(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        homestay.priceDisplay,
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          color: AppColors.primary,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      if (_checkIn != null && _checkOut != null)
                        Text(
                          'Tổng: ${_calculateTotal(homestay.pricePerNight)}đ',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      if (homestay.reviewCount == 0)
                        Text(
                          'Homestay chưa có đánh giá',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Colors.orange,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                    ],
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: ElevatedButton(
                    onPressed: () => _showBookingSheet(homestay),
                    child: const Text('Đặt phòng'),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  int _calculateTotal(double pricePerNight) {
    if (_checkIn == null || _checkOut == null) return 0;
    final nights = _checkOut!.difference(_checkIn!).inDays;
    return (pricePerNight * nights).toInt();
  }

  void _showBookingSheet(Homestay homestay) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) {
          return DraggableScrollableSheet(
            expand: false,
            initialChildSize: 0.72,
            minChildSize: 0.35,
            maxChildSize: 0.95,
            builder: (context, scrollController) {
              return SingleChildScrollView(
                controller: scrollController,
                child: Padding(
                  padding: EdgeInsets.only(
                    bottom: MediaQuery.of(context).viewInsets.bottom,
                    top: 16,
                    left: 16,
                    right: 16,
                  ),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text('Đặt phòng', style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 16),
                      // Calendar for date selection - constrain height so modal doesn't overflow
                      Builder(builder: (ctx) {
                        final available = MediaQuery.of(ctx).size.height;
                        final calendarHeight = (available * 0.55).clamp(240.0, 520.0);
                        return SizedBox(
                          height: calendarHeight,
                          child: BookingCalendar(
                            homestayId: homestay.id,
                            initialCheckIn: _checkIn,
                            initialCheckOut: _checkOut,
                            onDateRangeSelected: (checkIn, checkOut) {
                              setState(() {
                                _checkIn = checkIn;
                                _checkOut = checkOut;
                              });
                            },
                          ),
                        );
                      }),
                      const SizedBox(height: 16),
                      ListTile(
                        title: const Text('Số khách'),
                        subtitle: Text('$_guests khách'),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: const Icon(Icons.remove_circle_outline),
                              onPressed: _guests > 1
                                  ? () => setState(() => _guests--)
                                  : null,
                            ),
                            Text('$_guests'),
                            IconButton(
                              icon: const Icon(Icons.add_circle_outline),
                              onPressed: _guests < homestay.maxGuests
                                  ? () => setState(() => _guests++)
                                  : null,
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton(
                          onPressed: _checkIn != null && _checkOut != null
                              ? () {
                                  Navigator.pop(context);
                                  Navigator.pushNamed(
                                    context,
                                    '/booking',
                                    arguments: {
                                      'homestayId': homestay.id,
                                      'checkIn': _checkIn!,
                                      'checkOut': _checkOut!,
                                      'guests': _guests,
                                    },
                                  );
                                }
                              : null,
                          child: const Text('Tiếp tục'),
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}
