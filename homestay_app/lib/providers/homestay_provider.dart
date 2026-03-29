import 'package:flutter/material.dart';

import '../models/homestay.dart';
import '../services/homestay_service.dart';

class HomestayProvider with ChangeNotifier {
  final HomestayService _homestayService = HomestayService();

  List<Homestay> _homestays = [];
  List<Homestay> _myHomestays = [];
  List<Amenity> _amenities = [];
  Homestay? _selectedHomestay;
  bool _isLoading = false;
  String? _error;
  
  // Search filters
  String? _searchCity;
  DateTime? _checkInDate;
  DateTime? _checkOutDate;
  int? _numberOfGuests;
  double? _minPrice;
  double? _maxPrice;
  List<int> _selectedAmenityIds = [];

  // Getters
  List<Homestay> get homestays => _homestays;
  List<Homestay> get myHomestays => _myHomestays;
  List<Amenity> get amenities => _amenities;
  Homestay? get selectedHomestay => _selectedHomestay;
  bool get isLoading => _isLoading;
  String? get error => _error;
  
  String? get searchCity => _searchCity;
  DateTime? get checkInDate => _checkInDate;
  DateTime? get checkOutDate => _checkOutDate;
  int? get numberOfGuests => _numberOfGuests;
  double? get minPrice => _minPrice;
  double? get maxPrice => _maxPrice;
  List<int> get selectedAmenityIds => _selectedAmenityIds;

  Future<void> loadAmenities() async {
    try {
      _amenities = await _homestayService.getAmenities();
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  void setSearchFilters({
    String? city,
    DateTime? checkIn,
    DateTime? checkOut,
    int? guests,
    double? minPrice,
    double? maxPrice,
    List<int>? amenityIds,
  }) {
    _searchCity = city;
    _checkInDate = checkIn;
    _checkOutDate = checkOut;
    _numberOfGuests = guests;
    _minPrice = minPrice;
    _maxPrice = maxPrice;
    _selectedAmenityIds = amenityIds ?? [];
    notifyListeners();
  }

  void clearSearchFilters() {
    _searchCity = null;
    _checkInDate = null;
    _checkOutDate = null;
    _numberOfGuests = null;
    _minPrice = null;
    _maxPrice = null;
    _selectedAmenityIds = [];
    notifyListeners();
  }

  Future<void> searchHomestays() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _homestays = await _homestayService.searchHomestays(
        city: _searchCity,
        checkIn: _checkInDate,
        checkOut: _checkOutDate,
        guests: _numberOfGuests,
        minPrice: _minPrice,
        maxPrice: _maxPrice,
        amenityIds: _selectedAmenityIds,
      );
      _error = null; // Clear error on success
    } catch (e) {
      print('Error searching homestays: $e');
      _error = 'Không thể tải danh sách homestay. Vui lòng thử lại.';
      _homestays = []; // Clear homestays on error
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadHomestayById(int id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _selectedHomestay = await _homestayService.getHomestayById(id);
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMyHomestays() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _myHomestays = await _homestayService.getMyHomestays();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> createHomestay(Map<String, dynamic> data) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final homestay = await _homestayService.createHomestay(data);
      _myHomestays.insert(0, homestay);
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> updateHomestay(int id, Map<String, dynamic> data) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final homestay = await _homestayService.updateHomestay(id, data);
      final index = _myHomestays.indexWhere((h) => h.id == id);
      if (index != -1) {
        _myHomestays[index] = homestay;
      }
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> deleteHomestay(int id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _homestayService.deleteHomestay(id);
      _myHomestays.removeWhere((h) => h.id == id);
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
