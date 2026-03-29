import 'package:flutter/material.dart';

import '../models/booking.dart';
import '../services/booking_service.dart';

class BookingProvider with ChangeNotifier {
  final BookingService _bookingService = BookingService();

  List<Booking> _myBookings = [];
  List<Booking> _hostBookings = [];
  Booking? _selectedBooking;
  bool _isLoading = false;
  String? _error;

  List<Booking> get myBookings => _myBookings;
  List<Booking> get hostBookings => _hostBookings;
  Booking? get selectedBooking => _selectedBooking;
  bool get isLoading => _isLoading;
  String? get error => _error;

  Future<bool> checkAvailability({
    required int homestayId,
    required DateTime checkIn,
    required DateTime checkOut,
  }) async {
    try {
      final result = await _bookingService.checkAvailability(
        homestayId: homestayId,
        checkIn: checkIn,
        checkOut: checkOut,
      );
      return result['isAvailable'] ?? false;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    }
  }

  Future<Booking?> createBooking({
    required int homestayId,
    required DateTime checkIn,
    required DateTime checkOut,
    required int guests,
    String? specialRequests,
    String? promotionCode,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final booking = await _bookingService.createBooking(
        homestayId: homestayId,
        checkIn: checkIn,
        checkOut: checkOut,
        guests: guests,
        specialRequests: specialRequests,
        promotionCode: promotionCode,
      );
      _myBookings.insert(0, booking);
      _isLoading = false;
      notifyListeners();
      return booking;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return null;
    }
  }

  Future<void> loadMyBookings() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _myBookings = await _bookingService.getMyBookings();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadHostBookings() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _hostBookings = await _bookingService.getHostBookings();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadBookingById(int id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _selectedBooking = await _bookingService.getBookingById(id);
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> updateBookingStatus(int id, String status) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final booking = await _bookingService.updateBookingStatus(id, status);
      
      // Update in myBookings list
      final myIndex = _myBookings.indexWhere((b) => b.id == id);
      if (myIndex != -1) {
        _myBookings[myIndex] = booking;
      }
      
      // Update in hostBookings list
      final hostIndex = _hostBookings.indexWhere((b) => b.id == id);
      if (hostIndex != -1) {
        _hostBookings[hostIndex] = booking;
      }
      
      if (_selectedBooking?.id == id) {
        _selectedBooking = booking;
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

  Future<bool> cancelBooking(int id) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _bookingService.cancelBooking(id);
      await loadMyBookings(); // Reload to get updated status
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

  Future<bool> createReview({
    required int bookingId,
    required int rating,
    String? comment,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _bookingService.createReview(
        bookingId: bookingId,
        rating: rating,
        comment: comment,
      );
      await loadMyBookings(); // Reload to show review
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
