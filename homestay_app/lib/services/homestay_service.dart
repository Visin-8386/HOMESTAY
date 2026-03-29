import '../config/api_config.dart';
import '../models/homestay.dart';
import 'api_service.dart';

class HomestayService {
  final ApiService _apiService = ApiService();

  Future<List<Homestay>> searchHomestays({
    String? city,
    DateTime? checkIn,
    DateTime? checkOut,
    int? guests,
    double? minPrice,
    double? maxPrice,
    List<int>? amenityIds,
    int page = 1,
    int pageSize = 10,
  }) async {
    final queryParams = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };

    if (city != null && city.isNotEmpty) queryParams['city'] = city;
    if (checkIn != null) queryParams['checkIn'] = checkIn.toIso8601String();
    if (checkOut != null) queryParams['checkOut'] = checkOut.toIso8601String();
    if (guests != null) queryParams['guests'] = guests.toString();
    if (minPrice != null) queryParams['minPrice'] = minPrice.toString();
    if (maxPrice != null) queryParams['maxPrice'] = maxPrice.toString();
    if (amenityIds != null && amenityIds.isNotEmpty) {
      queryParams['amenityIds'] = amenityIds.join(',');
    }

    final uri = Uri.parse(ApiConfig.searchHomestaysUrl).replace(
      queryParameters: queryParams,
    );

    final response = await _apiService.get(uri.toString(), requireAuth: false);
    
    // Backend returns: { "success": true, "data": { "items": [...], "totalCount": N, ... } }
    final data = response['data'] ?? response;
    
    // PagedResponse structure
    final List<dynamic> homestays = data is List 
        ? data 
        : (data['items'] ?? data['homestays'] ?? []);
    
    return homestays.map((json) => Homestay.fromJson(json)).toList();
  }

  Future<Homestay> getHomestayById(int id) async {
    final response = await _apiService.get(
      ApiConfig.homestayDetailUrl(id),
      requireAuth: false,
    );
    // Backend returns: { "success": true, "data": {...} }
    final data = response['data'] ?? response;
    return Homestay.fromJson(data);
  }

  Future<List<Homestay>> getMyHomestays() async {
    final response = await _apiService.get(ApiConfig.myHomestaysUrl);
    // Backend returns: { "success": true, "data": [...] }
    final data = response['data'] ?? response;
    final List<dynamic> homestays = data is List ? data : (data['homestays'] ?? data['items'] ?? []);
    return homestays.map((json) => Homestay.fromJson(json)).toList();
  }

  Future<List<Homestay>> getHostHomestays() async {
    final response = await _apiService.get('${ApiConfig.baseUrl}/api/host/homestays');
    // Backend returns: { "success": true, "data": [...] }
    final data = response['data'] ?? response;
    final List<dynamic> homestays = data is List ? data : (data['homestays'] ?? data['items'] ?? []);
    return homestays.map((json) => Homestay.fromJson(json)).toList();
  }

  Future<Homestay> createHomestay(Map<String, dynamic> data) async {
    // WebHS expects multipart form with scalar fields and IFormFile images.
    final List<String>? imagePaths = data.remove('imagePaths')?.cast<String>();

    // Some backend versions expect JSON for homestay creation and a separate
    // endpoint to upload images. To be compatible we first create the
    // homestay with JSON, then upload images (if any) to
    // POST /api/homestays/{id}/images using the existing helper.
    final response = await _apiService.post(ApiConfig.homestaysUrl, data);
    final responseData = response['data'] ?? response;
    final created = Homestay.fromJson(responseData);

    if (imagePaths != null && imagePaths.isNotEmpty) {
      try {
        await uploadHomestayImages(created.id, imagePaths);
        // Re-fetch homestay to get updated image URLs
        return await getHomestayById(created.id);
      } catch (e) {
        // If image upload fails, still return the created homestay but
        // surface the error to the caller.
        throw 'Homestay created but failed to upload images: $e';
      }
    }

    return created;
  }

  Future<Homestay> updateHomestay(int id, Map<String, dynamic> data) async {
    // The server's EditHomestayViewModel supports Images (IFormFile list) and ImagesToDelete (int[])
    final List<String>? imagePaths = data.remove('imagePaths')?.cast<String>();
    // ImagesToDelete should be passed as comma-separated field 'ImagesToDelete'
    final List<int>? imagesToDelete = data['imagesToDelete']?.cast<int>();

    // Debug: print outgoing data (non-file fields) to help trace coordinate updates
    try {
      final preview = Map<String, dynamic>.from(data);
      preview.remove('Images');
      preview.remove('imagePaths');
      print('🔁 updateHomestay sending (id=$id): ${preview}');
    } catch (_) {}

    if (imagePaths != null && imagePaths.isNotEmpty) {
      final response = await _apiService.uploadMultipart(
        ApiConfig.homestayDetailUrl(id),
        {
          ...data,
          if (imagesToDelete != null) 'ImagesToDelete': imagesToDelete,
        },
        filePaths: imagePaths,
        fileFieldName: 'Images',
        method: 'PUT',
      );

      final responseData = response['data'] ?? response;
      try { print('🔁 updateHomestay responseData (multipart): $responseData'); } catch (_) {}
      return Homestay.fromJson(responseData);
    } else {
      final response = await _apiService.put(
        ApiConfig.homestayDetailUrl(id),
        data,
      );
      final responseData = response['data'] ?? response;
      try { print('🔁 updateHomestay responseData (put): $responseData'); } catch (_) {}
      return Homestay.fromJson(responseData);
    }
  }

  /// Fetch raw homestay JSON (no parsing) for edit screen prefill
  Future<Map<String, dynamic>> getHomestayDetailRaw(int id) async {
    final response = await _apiService.get(ApiConfig.homestayDetailUrl(id));
    final data = response['data'] ?? response;
    if (data is Map<String, dynamic>) return data;
    return Map<String, dynamic>.from(data as Map);
  }

  Future<void> deleteHomestay(int id) async {
    try {
      final res = await _apiService.delete(ApiConfig.homestayDetailUrl(id));
      // Debug print
      try { print('🗑️ deleteHomestay response: $res'); } catch (_) {}
    } catch (e) {
      // Surface error for callers
      print('Error in deleteHomestay: $e');
      rethrow;
    }
  }

  Future<List<Amenity>> getAmenities() async {
    final response = await _apiService.get(
      ApiConfig.amenitiesUrl,
      requireAuth: false,
    );
    // response may be: { data: [...]} or { amenities: [...] } or raw list
    List<dynamic> amenities = [];
    try {
      if (response is List) {
        amenities = response;
      } else if (response is Map && response.containsKey('data')) {
        final d = response['data'];
        if (d is List) amenities = d;
      } else if (response is Map && response.containsKey('amenities')) {
        final a = response['amenities'];
        if (a is List) amenities = a;
      } else if (response is Map) {
        // try common keys
        if (response['items'] is List) amenities = response['items'];
      }
    } catch (_) {
      amenities = [];
    }

    return amenities.map((json) => Amenity.fromJson(Map<String, dynamic>.from(json))).toList();
  }

  /// NEW: Upload multiple images to an existing homestay
  Future<List<String>> uploadHomestayImages(int homestayId, List<String> imagePaths) async {
    // Create multipart request
    final uri = Uri.parse('${ApiConfig.homestaysUrl}/$homestayId/images');
    
    final response = await _apiService.uploadFiles(
      uri.toString(),
      imagePaths,
      fieldName: 'images',
    );
    
    // Backend returns: { "success": true, "data": ["/uploads/homestays/1/abc.jpg", ...] }
    final data = response['data'] ?? response;
    return List<String>.from(data is List ? data : []);
  }

  /// NEW: Set primary image for homestay
  Future<void> setPrimaryImage(int homestayId, int imageId) async {
    await _apiService.put(
      '${ApiConfig.homestaysUrl}/$homestayId/images/$imageId/primary',
      {},
    );
  }

  /// NEW: Delete homestay image
  Future<void> deleteHomestayImage(int homestayId, int imageId) async {
    await _apiService.delete(
      '${ApiConfig.homestaysUrl}/$homestayId/images/$imageId',
    );
  }
}
