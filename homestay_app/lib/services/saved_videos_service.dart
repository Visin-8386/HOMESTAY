import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../models/video.dart';

class SavedVideosService {
  static const _key = 'saved_videos';

  Future<List<Video>> getSavedVideos() async {
    final sp = await SharedPreferences.getInstance();
    final jsonStr = sp.getString(_key);
    if (jsonStr == null || jsonStr.isEmpty) return [];
    try {
      final list = json.decode(jsonStr) as List<dynamic>;
      return list.map((e) => Video.fromJson(Map<String, dynamic>.from(e as Map))).toList();
    } catch (_) {
      return [];
    }
  }

  Future<bool> saveVideo(Video v) async {
    final videos = await getSavedVideos();
    if (videos.any((e) => e.videoId == v.videoId)) return true;
    videos.add(v);
    final sp = await SharedPreferences.getInstance();
    return sp.setString(_key, json.encode(videos.map((e) => e.toJson()).toList()));
  }

  Future<bool> removeVideo(String videoId) async {
    final videos = await getSavedVideos();
    videos.removeWhere((e) => e.videoId == videoId);
    final sp = await SharedPreferences.getInstance();
    return sp.setString(_key, json.encode(videos.map((e) => e.toJson()).toList()));
  }

  Future<bool> isSaved(String videoId) async {
    final videos = await getSavedVideos();
    return videos.any((e) => e.videoId == videoId);
  }
}
