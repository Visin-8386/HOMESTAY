import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_rating_bar/flutter_rating_bar.dart';

import '../../config/app_colors.dart';
import '../../models/review.dart';
import '../../services/api_service.dart';
import '../../services/booking_service.dart';

class AllReviewsScreen extends StatefulWidget {
  const AllReviewsScreen({super.key});

  @override
  State<AllReviewsScreen> createState() => _AllReviewsScreenState();
}

class _AllReviewsScreenState extends State<AllReviewsScreen> {
  final ApiService _apiService = ApiService();
  List<Review> _reviews = [];
  bool _isLoading = true;
  bool _isLoadingMore = false;
  int _currentPage = 1;
  final int _pageSize = 20;
  bool _hasMoreData = true;
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    // If route passed a homestayId, load reviews for that homestay; otherwise load all reviews
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final args = ModalRoute.of(context)?.settings.arguments;
      if (args is Map && args['homestayId'] != null) {
        _loadHomestayReviews(args['homestayId'] as int);
      } else {
        _loadReviews();
      }
    });
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels == _scrollController.position.maxScrollExtent) {
      if (!_isLoadingMore && _hasMoreData) {
        _loadMoreReviews();
      }
    }
  }

  Future<void> _loadReviews() async {
    setState(() {
      _isLoading = true;
    });

    try {
      final reviews = await _apiService.getAllReviews(page: 1, pageSize: _pageSize);
      setState(() {
        _reviews = reviews;
        _currentPage = 1;
        _hasMoreData = reviews.length == _pageSize;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _isLoading = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể tải đánh giá: $e')),
        );
      }
    }
  }

  Future<void> _loadHomestayReviews(int homestayId) async {
    setState(() => _isLoading = true);
    try {
      final bookingService = BookingService();
      final bookingReviews = await bookingService.getHomestayReviews(homestayId, page: 1, pageSize: _pageSize);
      
      // Convert from booking.Review to review.Review
      final reviews = bookingReviews.map((br) => Review(
        id: br.id,
        userId: int.tryParse(br.userId) ?? 0,
        homestayId: br.homestayId,
        bookingId: br.bookingId,
        rating: br.rating.toDouble(),
        comment: br.comment ?? '',
        images: null, // booking.Review doesn't have images field
        createdAt: br.createdAt,
        updatedAt: br.createdAt, // booking.Review doesn't have updatedAt
        userName: br.userName,
        userAvatar: br.userAvatar,
        homestayName: null, // Not available in booking.Review
      )).toList();
      
      setState(() {
        _reviews = reviews;
        _currentPage = 1;
        _hasMoreData = reviews.length == _pageSize;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể tải đánh giá: $e')));
      }
    }
  }

  Future<void> _loadMoreReviews() async {
    if (_isLoadingMore) return;

    setState(() {
      _isLoadingMore = true;
    });

    try {
      final reviews = await _apiService.getAllReviews(
        page: _currentPage + 1,
        pageSize: _pageSize,
      );

      setState(() {
        _reviews.addAll(reviews);
        _currentPage++;
        _hasMoreData = reviews.length == _pageSize;
        _isLoadingMore = false;
      });
    } catch (e) {
      setState(() {
        _isLoadingMore = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể tải thêm đánh giá: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Tất cả đánh giá'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _reviews.isEmpty
              ? _buildEmptyState()
              : _buildReviewsList(),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.star_border,
            size: 64,
            color: Colors.grey[400],
          ),
          const SizedBox(height: 16),
          Text(
            'Chưa có đánh giá nào',
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
              fontWeight: FontWeight.w500,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'Đánh giá sẽ xuất hiện ở đây khi có khách hàng chia sẻ trải nghiệm',
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

  Widget _buildReviewsList() {
    return RefreshIndicator(
      onRefresh: _loadReviews,
      child: ListView.builder(
        controller: _scrollController,
        padding: const EdgeInsets.all(16),
        itemCount: _reviews.length + (_hasMoreData ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _reviews.length) {
            return _buildLoadingMoreIndicator();
          }
          return _buildReviewCard(_reviews[index]);
        },
      ),
    );
  }

  Widget _buildLoadingMoreIndicator() {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      alignment: Alignment.center,
      child: const CircularProgressIndicator(),
    );
  }

  Widget _buildReviewCard(Review review) {
    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header with user info and rating
            Row(
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundImage: review.userAvatar != null
                      ? CachedNetworkImageProvider(review.userAvatar!)
                      : null,
                  child: review.userAvatar == null
                      ? Text(
                          review.userName.isNotEmpty
                              ? review.userName[0].toUpperCase()
                              : '?',
                          style: const TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                          ),
                        )
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
                          fontWeight: FontWeight.w600,
                          fontSize: 16,
                        ),
                      ),
                      const SizedBox(height: 4),
                      RatingBarIndicator(
                        rating: review.rating.toDouble(),
                        itemBuilder: (context, index) => const Icon(
                          Icons.star,
                          color: Colors.amber,
                        ),
                        itemCount: 5,
                        itemSize: 16,
                      ),
                    ],
                  ),
                ),
                Text(
                  _formatDate(review.createdAt),
                  style: const TextStyle(
                    color: Colors.grey,
                    fontSize: 12,
                  ),
                ),
              ],
            ),

            const SizedBox(height: 12),

            // Homestay name
            if (review.homestayName != null) ...[
              Text(
                review.homestayName!,
                style: const TextStyle(
                  color: AppColors.primary,
                  fontWeight: FontWeight.w500,
                  fontSize: 14,
                ),
              ),
              const SizedBox(height: 8),
            ],

            // Review comment
            if (review.comment.isNotEmpty) ...[
              Text(
                review.comment,
                style: const TextStyle(
                  fontSize: 14,
                  height: 1.5,
                ),
              ),
              const SizedBox(height: 8),
            ],

            // Review images
            if (review.images != null && review.images!.isNotEmpty)
              _buildReviewImages(review.images!),
          ],
        ),
      ),
    );
  }

  Widget _buildReviewImages(List<String> images) {
    return SizedBox(
      height: 80,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        itemCount: images.length,
        itemBuilder: (context, index) {
          return Container(
            width: 80,
            height: 80,
            margin: const EdgeInsets.only(right: 8),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(8),
              image: DecorationImage(
                image: CachedNetworkImageProvider(images[index]),
                fit: BoxFit.cover,
              ),
            ),
          );
        },
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
      final weeks = (difference.inDays / 7).floor();
      return '$weeks tuần trước';
    } else if (difference.inDays < 365) {
      final months = (difference.inDays / 30).floor();
      return '$months tháng trước';
    } else {
      final years = (difference.inDays / 365).floor();
      return '$years năm trước';
    }
  }
}
