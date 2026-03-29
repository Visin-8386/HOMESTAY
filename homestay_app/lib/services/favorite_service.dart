import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

class FavoriteService {
  static const _prefsKey = 'favorite_homestay_ids';

  Future<SharedPreferences> _prefs() async => await SharedPreferences.getInstance();

  Future<List<int>> _loadIds() async {
    final p = await _prefs();
    final raw = p.getString(_prefsKey);
    if (raw == null || raw.isEmpty) return [];
    try {
      final list = List<dynamic>.from(json.decode(raw));
      return list.map((e) => (e as num).toInt()).toList();
    } catch (e) {
      return [];
    }
  }

  Future<void> _saveIds(List<int> ids) async {
    final p = await _prefs();
    await p.setString(_prefsKey, json.encode(ids));
  }

  /// Trả về danh sách id homestay yêu thích (client có thể map sang model)
  Future<List<int>> getFavoriteHomestayIds() async {
    return await _loadIds();
  }

  Future<bool> isFavorite(int homestayId) async {
    final ids = await _loadIds();
    return ids.contains(homestayId);
  }

  Future<bool> addFavorite(int homestayId) async {
    try {
      final ids = await _loadIds();
      if (!ids.contains(homestayId)) ids.add(homestayId);
      await _saveIds(ids);
      return true;
    } catch (e) {
      print('Error adding favorite: $e');
      return false;
    }
  }

  Future<bool> removeFavorite(int homestayId) async {
    try {
      final ids = await _loadIds();
      ids.remove(homestayId);
      await _saveIds(ids);
      return true;
    } catch (e) {
      print('Error removing favorite: $e');
      return false;
    }
  }

  Future<bool> toggleFavorite(int homestayId) async {
    final isFav = await isFavorite(homestayId);
    if (isFav) {
      await removeFavorite(homestayId);
      return false;
    } else {
      await addFavorite(homestayId);
      return true;
    }
  }
}
