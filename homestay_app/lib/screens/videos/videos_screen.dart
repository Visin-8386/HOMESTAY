import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher_string.dart';
import 'package:webview_flutter/webview_flutter.dart';

import '../../models/video.dart';
import '../../services/saved_videos_service.dart';
import '../../services/youtube_service.dart';
import '../../widgets/user_gradient_background.dart';

class VideosScreen extends StatefulWidget {
  const VideosScreen({super.key});

  @override
  State<VideosScreen> createState() => _VideosScreenState();
}

class _VideosScreenState extends State<VideosScreen> {
  final YoutubeService _yt = YoutubeService();
  final SavedVideosService _saved = SavedVideosService();
  List<Video> _videos = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
    });
    final v = await _yt.getHomestayVideos(location: 'Vietnam', maxResults: 12);
    setState(() {
      _videos = v;
      _loading = false;
    });
  }

  void _play(Video v) {
    final embedUrl =
        'https://www.youtube.com/embed/${v.videoId}?autoplay=1&enablejsapi=1&playsinline=1&rel=0&origin=https://www.youtube.com';

    final controller = WebViewController();

    controller.setJavaScriptMode(JavaScriptMode.unrestricted);
    controller.setNavigationDelegate(NavigationDelegate(
      onPageFinished: (url) async {
        try {
          final dyn =
              await controller.runJavaScriptReturningResult('document.body.innerText');
          final txt = dyn.toString();
          if (txt.contains('Video player configuration error') ||
              txt.contains('This video is unavailable')) {
            try {
              if (mounted) {
                Navigator.of(context, rootNavigator: true).pop();
              }
            } catch (_) {}
            await launchUrlString('https://www.youtube.com/watch?v=${v.videoId}');
          }
        } catch (_) {}
      },
    ));

    controller.loadRequest(Uri.parse(embedUrl));

    showDialog(
      context: context,
      builder: (ctx) => Dialog(
        insetPadding: const EdgeInsets.all(12),
        child: AspectRatio(
          aspectRatio: 16 / 9,
          child: WebViewWidget(controller: controller),
        ),
      ),
    ).then((_) => setState(() {}));
  }

  Widget _buildCard(Video v) {
    return GestureDetector(
      onTap: () => _play(v),
      child: Card(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        elevation: 3,
        clipBehavior: Clip.antiAlias,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Thumbnail - chiếm 65% chiều cao của card
            if (v.thumbnail != null)
              Expanded(
                flex: 65,
                child: ClipRRect(
                  borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
                  child: Image.network(
                    v.thumbnail!,
                    width: double.infinity,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) => Container(
                      color: Colors.grey[200],
                      alignment: Alignment.center,
                      child: const Icon(
                        Icons.broken_image,
                        size: 40,
                        color: Colors.grey,
                      ),
                    ),
                  ),
                ),
              ),
            // Text area - chiếm 35% chiều cao
            Expanded(
              flex: 35,
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8.0, vertical: 6.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // Title
                    Expanded(
                      child: Text(
                        v.title,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 14,
                          height: 1.2,
                        ),
                      ),
                    ),
                    const SizedBox(height: 4),
                    // Channel và bookmark button
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            v.channelTitle ?? '',
                            style: TextStyle(
                              color: Colors.grey[600],
                              fontSize: 11,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        IconButton(
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                          iconSize: 18,
                          icon: const Icon(Icons.bookmark_add_outlined),
                          onPressed: () async {
                            final saved = await _saved.saveVideo(v);
                            if (!mounted) return;
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text(
                                  saved ? 'Đã lưu video' : 'Không thể lưu',
                                ),
                                duration: const Duration(seconds: 2),
                              ),
                            );
                            setState(() {});
                          },
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: AppBar(
        title: const Text('Video Homestay'),
        elevation: 0,
      ),
      body: UserGradientBackground(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _load,
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final width = constraints.maxWidth;
                    
                    // Số cột dựa trên chiều rộng màn hình
                    final crossAxisCount = width < 500 ? 1 : width < 800 ? 2 : 3;

                    // Tỷ lệ khung hình (width/height) - giảm để có nhiều không gian hơn cho text
                    final childAspect = width < 500 ? 0.75 : width < 800 ? 0.8 : 0.85;

                    return GridView.builder(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.all(12),
                      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: crossAxisCount,
                        crossAxisSpacing: 12,
                        mainAxisSpacing: 12,
                        childAspectRatio: childAspect,
                      ),
                      itemCount: _videos.length,
                      itemBuilder: (context, i) => _buildCard(_videos[i]),
                    );
                  },
                ),
              ),
      ),
    );
  }
}