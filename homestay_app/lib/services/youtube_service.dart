import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/video.dart';

class YoutubeService {
  /// Try to fetch homestay videos from WebHS controller endpoints.
  /// Will fallback to a demo list if requests fail.
  Future<List<Video>> getHomestayVideos({String location = 'Vietnam', String category = 'all', int maxResults = 12}) async {
    final List<Video> result = [];

    // Primary endpoint: controller action
    final uri1 = Uri.parse('${ApiConfig.baseUrl}/YouTube/GetHomestayVideos?location=${Uri.encodeComponent(location)}&category=${Uri.encodeComponent(category)}&maxResults=$maxResults');
    try {
      final res = await http.get(uri1).timeout(ApiConfig.connectionTimeout);
      if (res.statusCode == 200) {
        final body = json.decode(res.body);
        final videos = body['videos'] as List<dynamic>?;
        if (videos != null) {
          for (final v in videos) {
            result.add(Video.fromJson(Map<String, dynamic>.from(v as Map)));
          }
          return result;
        }
      }
    } catch (_) {
      // ignore and try fallback
    }

    // Fallback endpoint: api route
    final uri2 = Uri.parse('${ApiConfig.baseUrl}/api/YouTube/homestay-videos/${Uri.encodeComponent(location)}');
    try {
      final res = await http.get(uri2).timeout(ApiConfig.connectionTimeout);
      if (res.statusCode == 200) {
        final body = json.decode(res.body);
        final videos = body['videos'] as List<dynamic>?;
        if (videos != null) {
          for (final v in videos) {
            result.add(Video.fromJson(Map<String, dynamic>.from(v as Map)));
          }
          return result;
        }
      }
    } catch (_) {
      // final fallback to demo data below
    }

    // Demo fallback (client-side) to avoid failing UI when backend not available
    return _demoVideos(location);
  }

  List<Video> _demoVideos(String location) {
    final now = DateTime.now();
    return [
      Video(videoId: 'dQw4w9WgXcQ', title: '$location - Homestay Highlights', thumbnail: 'https://img.youtube.com/vi/dQw4w9WgXcQ/mqdefault.jpg', channelTitle: 'Demo Channel', publishedAt: now.toIso8601String()),
      Video(videoId: 'M7lc1UVf-VE', title: 'Local Food & Homestay', thumbnail: 'https://img.youtube.com/vi/M7lc1UVf-VE/mqdefault.jpg', channelTitle: 'Demo Channel', publishedAt: now.subtract(const Duration(days:3)).toIso8601String()),
      Video(videoId: 'ScMzIvxBSi4', title: 'Family Friendly Homestays', thumbnail: 'https://img.youtube.com/vi/ScMzIvxBSi4/mqdefault.jpg', channelTitle: 'Demo Channel', publishedAt: now.subtract(const Duration(days:7)).toIso8601String()),
    ];
  }
}
