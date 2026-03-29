import '../config/api_config.dart';
import 'api_service.dart';

class PromotionService {
  final ApiService _api = ApiService();

  Future<List<Map<String, dynamic>>> getAll() async {
    try {
  final dynamic response = await _api.get('${ApiConfig.baseUrl}/api/promotions');
      // Depending on controller, response might be a raw list or wrapped
      // Normalize response to a List<dynamic> in the common shapes we expect:
      // - raw list: [{...}, {...}]
      // - envelope: { success: true, data: [...] }
      List<dynamic> rawList = [];
      if (response is List) {
        rawList = response;
      } else if (response is Map<String, dynamic>) {
        final maybeData = response['data'] ?? response.values.firstWhere((v) => v is List, orElse: () => null);
        if (maybeData is List) rawList = maybeData;
      }

      // Keep only map-like items and convert to Map<String,dynamic>
  final maps = rawList.whereType<Map>().map((m) => Map<String, dynamic>.from(m)).toList();

      if (maps.length != rawList.length) {
        // Log a warning (printed to console) to help debugging unexpected shapes.
        print('PromotionService.getAll: ignored ${rawList.length - maps.length} non-object items in response');
      }

      return maps;
    } catch (e) {
      // If ApiService produced an ApiException, surface its message
      if (e is ApiException) {
        throw 'Lỗi tải khuyến mãi: ${e.message}';
      }
      throw 'Lỗi tải khuyến mãi: $e';
    }
  }

  Future<Map<String, dynamic>> getById(int id) async {
    final response = await _api.get('${ApiConfig.baseUrl}/api/promotions/$id');
    final data = response['data'] ?? response;
    if (data is Map<String, dynamic>) return data;
    return {};
  }

  Future<Map<String, dynamic>> create(Map<String, dynamic> dto) async {
    final response = await _api.post('${ApiConfig.baseUrl}/api/promotions', dto);
    return response['data'] ?? response;
  }

  Future<Map<String, dynamic>> update(int id, Map<String, dynamic> dto) async {
    final response = await _api.put('${ApiConfig.baseUrl}/api/promotions/$id', dto);
    return response['data'] ?? response;
  }

  Future<bool> delete(int id) async {
    await _api.delete('${ApiConfig.baseUrl}/api/promotions/$id');
    return true;
  }
}
