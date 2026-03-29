import 'package:flutter/material.dart';

import '../models/homestay.dart';

class ComparisonProvider with ChangeNotifier {
  final List<Homestay> _selectedHomestays = [];
  static const int maxComparison = 3;

  List<Homestay> get selectedHomestays => _selectedHomestays;
  int get count => _selectedHomestays.length;
  bool get isFull => _selectedHomestays.length >= maxComparison;
  bool get canCompare => _selectedHomestays.length >= 2;

  bool isSelected(int homestayId) {
    return _selectedHomestays.any((h) => h.id == homestayId);
  }

  void toggleSelection(Homestay homestay) {
    final index = _selectedHomestays.indexWhere((h) => h.id == homestay.id);
    
    if (index >= 0) {
      _selectedHomestays.removeAt(index);
    } else {
      if (_selectedHomestays.length < maxComparison) {
        _selectedHomestays.add(homestay);
      }
    }
    
    notifyListeners();
  }

  void removeHomestay(int homestayId) {
    _selectedHomestays.removeWhere((h) => h.id == homestayId);
    notifyListeners();
  }

  void clearAll() {
    _selectedHomestays.clear();
    notifyListeners();
  }
}
