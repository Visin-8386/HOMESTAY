import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';



class YouTubeScreen extends StatefulWidget {
  const YouTubeScreen({super.key});

  @override
  State<YouTubeScreen> createState() => _YouTubeScreenState();
}

class _YouTubeScreenState extends State<YouTubeScreen> {
  final List<Map<String, dynamic>> _videos = [
    {
      'id': '1',
      'title': 'Khám phá Homestay Đà Lạt - Kỳ nghỉ dưỡng hoàn hảo',
      'description': 'Khám phá những homestay đẹp nhất tại Đà Lạt với view núi rừng tuyệt đẹp.',
      'thumbnail': 'https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
      'url': 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      'duration': '12:34',
      'views': '125K',
      'uploadDate': '2 tuần trước',
    },
    {
      'id': '2',
      'title': 'Homestay Sapa - Trải nghiệm bản làng độc đáo',
      'description': 'Tham quan và trải nghiệm cuộc sống của người dân tộc tại Sapa.',
      'thumbnail': 'https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
      'url': 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      'duration': '8:45',
      'views': '89K',
      'uploadDate': '1 tháng trước',
    },
    {
      'id': '3',
      'title': 'Ẩm thực đường phố Hà Nội - Homestay gần phố cổ',
      'description': 'Khám phá ẩm thực đường phố Hà Nội và các homestay tiện lợi.',
      'thumbnail': 'https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
      'url': 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      'duration': '15:20',
      'views': '203K',
      'uploadDate': '3 tuần trước',
    },
    {
      'id': '4',
      'title': 'Du lịch biển Nha Trang - Homestay view biển',
      'description': 'Các homestay tuyệt đẹp với view biển trực tiếp tại Nha Trang.',
      'thumbnail': 'https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
      'url': 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      'duration': '10:15',
      'views': '156K',
      'uploadDate': '1 tuần trước',
    },
    {
      'id': '5',
      'title': 'Homestay Phú Quốc - Resort mini giá rẻ',
      'description': 'Trải nghiệm nghỉ dưỡng cao cấp với giá homestay tại Phú Quốc.',
      'thumbnail': 'https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
      'url': 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
      'duration': '9:30',
      'views': '98K',
      'uploadDate': '2 tháng trước',
    },
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        title: const Text('Video Homestay'),
        backgroundColor: Colors.transparent,
        elevation: 0,
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.search),
            onPressed: () {
              // TODO: Implement search functionality
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Tính năng tìm kiếm đang được phát triển')),
              );
            },
          ),
        ],
      ),
      body: Container(
        width: double.infinity,
        height: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFF667eea), Color(0xFF764ba2)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
        ),
        child: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isSmall = constraints.maxWidth < 420;
              final horizontalPadding = isSmall ? 8.0 : 16.0;
              // Add extra bottom padding so the list won't overflow behind a
              // persistent bottom navigation bar (common in the app).
              final bottomPadding = MediaQuery.of(context).padding.bottom + kBottomNavigationBarHeight + 16;

              return ListView.builder(
                padding: EdgeInsets.only(
                  left: horizontalPadding,
                  right: horizontalPadding,
                  top: 16,
                  bottom: bottomPadding,
                ),
                itemCount: _videos.length,
                itemBuilder: (context, index) {
                  return _buildVideoCard(_videos[index], isSmall: isSmall);
                },
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _buildVideoCard(Map<String, dynamic> video, {bool isSmall = false}) {
    final double cardPadding = isSmall ? 8 : 12;
    // Responsive fonts
    final double titleFont = isSmall ? 14 : 16;
    final double descFont = isSmall ? 12 : 14;
    final double buttonFont = isSmall ? 13 : 15;
    // Calculate a capped thumbnail height so cards don't become too tall on narrow screens
    final screenWidth = MediaQuery.of(context).size.width;
    final horizontalGutter = isSmall ? 16.0 : 32.0; // approximate padding used by parent
    final contentWidth = (screenWidth - horizontalGutter).clamp(120.0, screenWidth);
    final maxThumbHeight = isSmall ? 160.0 : 220.0;
    final thumbHeight = (contentWidth * 9.0 / 16.0).clamp(0.0, maxThumbHeight);
    return Card(
      margin: EdgeInsets.only(bottom: cardPadding),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: () => _openVideo(video['url']),
        borderRadius: BorderRadius.circular(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Video thumbnail with centered play button (no full dark overlay)
            ClipRRect(
              borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
              child: Stack(
                children: [
                  // Use a capped SizedBox to avoid very tall thumbnails on narrow devices
                  SizedBox(
                    width: double.infinity,
                    height: thumbHeight,
                    child: CachedNetworkImage(
                      // Use a smaller thumbnail variant when available to reduce memory
                      imageUrl: (video['thumbnail'] as String).replaceAll('maxresdefault', 'mqdefault'),
                      width: double.infinity,
                      fit: BoxFit.cover,
                      placeholder: (context, url) => Container(
                        color: Colors.grey[300],
                        child: const Center(child: CircularProgressIndicator()),
                      ),
                      errorWidget: (context, url, error) => Container(
                        color: Colors.grey[300],
                        child: const Icon(Icons.video_library, size: 50),
                      ),
                    ),
                  ),

                  // Centered circular play button so thumbnail remains visible
                  Positioned.fill(
                    child: Align(
                      alignment: Alignment.center,
                      child: Container(
                        width: isSmall ? 56 : 72,
                        height: isSmall ? 56 : 72,
                        decoration: BoxDecoration(
                          color: Colors.black.withOpacity(0.45),
                          shape: BoxShape.circle,
                        ),
                        child: Icon(Icons.play_arrow, color: Colors.white, size: isSmall ? 32 : 40),
                      ),
                    ),
                  ),

                  // Duration badge
                  Positioned(
                    bottom: 8,
                    right: 8,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: Colors.black.withAlpha(200),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        video['duration'],
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 12,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            Padding(
              padding: EdgeInsets.all(cardPadding),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Video title (limit lines on small screens)
                  Text(
                    video['title'],
                    style: TextStyle(
                      fontSize: titleFont,
                      fontWeight: FontWeight.w600,
                      height: 1.25,
                    ),
                    maxLines: isSmall ? 2 : 3,
                    overflow: TextOverflow.ellipsis,
                  ),

                  SizedBox(height: isSmall ? 6 : 8),

                  // Video description
                  Text(
                    video['description'],
                    style: TextStyle(
                      color: Colors.grey[700],
                      fontSize: descFont,
                      height: 1.35,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),

                  SizedBox(height: isSmall ? 8 : 12),

                  // Video stats (wrapped to avoid overflow on small screens)
                  Wrap(
                    spacing: 12,
                    runSpacing: 4,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.visibility, size: 16, color: Colors.grey[600]),
                          const SizedBox(width: 6),
                          Flexible(child: Text('${video['views']} lượt xem', style: const TextStyle(fontSize: 12, color: Colors.grey))),
                        ],
                      ),
                      Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.access_time, size: 16, color: Colors.grey[600]),
                          const SizedBox(width: 6),
                          Flexible(child: Text(video['uploadDate'], style: const TextStyle(fontSize: 12, color: Colors.grey))),
                        ],
                      ),
                    ],
                  ),

                  SizedBox(height: isSmall ? 8 : 12),

                  // Action buttons
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: () => _openVideo(video['url']),
                          icon: const Icon(Icons.play_arrow, size: 18),
                          label: Text('Xem video', style: TextStyle(fontSize: buttonFont)),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.red,
                            foregroundColor: Colors.white,
                            padding: EdgeInsets.symmetric(vertical: isSmall ? 8 : 10),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(8),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      IconButton(
                        onPressed: () => _shareVideo(video),
                        icon: const Icon(Icons.share),
                        tooltip: 'Chia sẻ',
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openVideo(String url) async {
    try {
      final Uri uri = Uri.parse(url);
      if (await canLaunchUrl(uri)) {
        await launchUrl(uri, mode: LaunchMode.externalApplication);
      } else {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Không thể mở video')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi khi mở video: $e')),
        );
      }
    }
  }

  void _shareVideo(Map<String, dynamic> video) {
    // TODO: Implement share functionality
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Chia sẻ: ${video['title']}')),
    );
  }

  // Favorites removed
}
