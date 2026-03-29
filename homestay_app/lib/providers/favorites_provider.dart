import 'package:flutter/material.dart';

import '../services/favorite_service.dart';

class FavoritesProvider extends ChangeNotifier {
  final FavoriteService _service = FavoriteService();
  final Set<int> _favoriteIds = {};
  bool _isLoaded = false;

  bool get isLoaded => _isLoaded;

  List<int> get favoriteIds => _favoriteIds.toList(growable: false);

  Future<void> loadFavorites() async {
    final ids = await _service.getFavoriteHomestayIds();
    _favoriteIds.clear();
    _favoriteIds.addAll(ids);
    _isLoaded = true;
    notifyListeners();
  }

  bool isFavorite(int homestayId) => _favoriteIds.contains(homestayId);

  Future<void> toggleFavorite(int homestayId) async {
    final isFav = _favoriteIds.contains(homestayId);
    if (isFav) {
      final ok = await _service.removeFavorite(homestayId);
      if (ok) {
        _favoriteIds.remove(homestayId);
        notifyListeners();
      }
    } else {
      final ok = await _service.addFavorite(homestayId);
      if (ok) {
        _favoriteIds.add(homestayId);
        notifyListeners();
      }
    }
  }
}
