import '../config/api_config.dart';
import '../models/booking.dart';
import 'api_service.dart';

class BookingService {
  final ApiService _apiService = ApiService();

  /// NEW: Get all booked dates for a homestay (for calendar display)
  Future<List<DateTime>> getBookedDates(int homestayId) async {
    final response = await _apiService.get(
      '${ApiConfig.bookingsUrl}/homestays/$homestayId/booked-dates',
      requireAuth: false,
    );
    
    // Backend returns: { "success": true, "data": ["2025-01-15", "2025-01-16", ...] }
    final data = response['data'] ?? response;
    final List<dynamic> dates = data is List ? data : [];
    
    return dates.map((dateStr) => DateTime.parse(dateStr)).toList();
  }

  /// NEW: Calculate booking amount with optional promotion code
  Future<Map<String, dynamic>> calculateAmount({
    required int homestayId,
    required DateTime checkIn,
    required DateTime checkOut,
    String? promotionCode,
  }) async {
    final response = await _apiService.post(
      '${ApiConfig.bookingsUrl}/calculate-amount',
      {
        'homestayId': homestayId,
        'checkIn': checkIn.toIso8601String(),
        'checkOut': checkOut.toIso8601String(),
        if (promotionCode != null && promotionCode.isNotEmpty)
          'promotionCode': promotionCode,
      },
      requireAuth: false,
    );
    
    // Backend returns: { "success": true, "data": { "subtotal": 300, "discount": 30, ... } }
    return response['data'] ?? response;
  }

  /// Get active promotions (returns raw list of promotion JSON objects)
  Future<List<Map<String, dynamic>>> getActivePromotions({int limit = 50}) async {
    final response = await _apiService.get('${ApiConfig.baseUrl}/api/promotions/active', requireAuth: false);
    // ApiService._handleResponse may return either a Map (with data/items) or a raw List
    dynamic data;
    if (response is Map && response.containsKey('data')) {
      data = response['data'];
    } else {
      data = response;
    }

    // Normalize to list of dynamic items
    final List<dynamic> promos =
        data is List ? data : (data is Map ? (data['items'] ?? data['promotions'] ?? []) : []);

    return promos.map((p) => Map<String, dynamic>.from(p as Map)).toList();
  }

  Future<Map<String, dynamic>> checkAvailability({
    required int homestayId,
    required DateTime checkIn,
    required DateTime checkOut,
  }) async {
    final response = await _apiService.post(
      ApiConfig.checkAvailabilityUrl,
      {
        'homestayId': homestayId,
        'checkInDate': checkIn.toIso8601String(),
        'checkOutDate': checkOut.toIso8601String(),
      },
    );
    return response;
  }

  Future<Booking> createBooking({
    required int homestayId,
    required DateTime checkIn,
    required DateTime checkOut,
    required int guests,
    String? specialRequests,
    String? promotionCode,
  }) async {
    final response = await _apiService.post(
      ApiConfig.bookingsUrl,
      {
        'homestayId': homestayId,
        'checkInDate': checkIn.toIso8601String(),
        'checkOutDate': checkOut.toIso8601String(),
        'numberOfGuests': guests,
        // backend DTO uses 'notes'
        'notes': specialRequests,
        if (promotionCode != null && promotionCode.isNotEmpty)
          'promotionCode': promotionCode,
      },
    );
    // Backend returns: { "success": true, "data": {...} }
    final data = response['data'] ?? response;
    return Booking.fromJson(data);
  }

  Future<Booking> getBookingById(int id) async {
    final response = await _apiService.get(
      ApiConfig.bookingDetailUrl(id),
    );
    // Backend returns: { "success": true, "data": {...} }
    final data = response['data'] ?? response;
    return Booking.fromJson(data);
  }

  Future<List<Booking>> getMyBookings({
    int page = 1,
    int pageSize = 10,
  }) async {
    final uri = Uri.parse(ApiConfig.myBookingsUrl).replace(
      queryParameters: {
        'page': page.toString(),
        'pageSize': pageSize.toString(),
      },
    );

    final response = await _apiService.get(uri.toString());
    // Backend returns: { "success": true, "data": [...] }
    final data = response['data'] ?? response;
    final List<dynamic> bookings = data is List ? data : (data['bookings'] ?? data['items'] ?? []);
    return bookings.map((json) => Booking.fromJson(json)).toList();
  }

  Future<List<Booking>> getHostBookings({
    int page = 1,
    int pageSize = 10,
  }) async {
    final uri = Uri.parse(ApiConfig.hostBookingsUrl).replace(
      queryParameters: {
        'page': page.toString(),
        'pageSize': pageSize.toString(),
      },
    );

    final response = await _apiService.get(uri.toString());
    // Backend returns: { "success": true, "data": [...] }
    final data = response['data'] ?? response;
    final List<dynamic> bookings = data is List ? data : (data['bookings'] ?? data['items'] ?? []);
    // Diagnostic: if bookings are empty but response had content, print raw response for debugging
    if (bookings.isEmpty && (data is Map && data.isNotEmpty)) {
      // Avoid importing dart:developer to keep things simple; use print for now
      print('DEBUG getHostBookings: unexpected response shape: $response');
    }
    return bookings.map((json) => Booking.fromJson(json)).toList();
  }

  Future<Booking> updateBookingStatus(int id, String status) async {
    final uri = Uri.parse('${ApiConfig.bookingsUrl}/$id/status');
    final response = await _apiService.put(uri.toString(), {'status': status});
    // Backend returns: { "success": true, "data": {...} }
    final data = response['data'] ?? response;
    return Booking.fromJson(data);
  }

  Future<void> cancelBooking(int id) async {
    final uri = Uri.parse('${ApiConfig.bookingsUrl}/$id/cancel');
    await _apiService.post(uri.toString(), {});
  }

  Future<Review> createReview({
    required int bookingId,
    required int rating,
    String? comment,
  }) async {
    final uri = Uri.parse('${ApiConfig.bookingsUrl}/$bookingId/review');
    final response = await _apiService.post(
      uri.toString(),
      {
        'rating': rating,
        'comment': comment,
      },
    );
    // Backend returns: { "success": true, "data": {...} }
    final data = response['data'] ?? response;
    return Review.fromJson(data);
  }

  Future<List<Review>> getHomestayReviews(
    int homestayId, {
    int page = 1,
    int pageSize = 10,
  }) async {
    final uri = Uri.parse(ApiConfig.homestayReviewsUrl(homestayId)).replace(
      queryParameters: {
        'page': page.toString(),
        'pageSize': pageSize.toString(),
      },
    );

    final response = await _apiService.get(uri.toString(), requireAuth: false);
    // Backend returns: { "success": true, "data": [...] }
    final data = response['data'] ?? response;
    final List<dynamic> reviews = data is List ? data : (data['reviews'] ?? data['items'] ?? []);
    return reviews.map((json) => Review.fromJson(json)).toList();
  }
}
