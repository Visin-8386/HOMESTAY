import '../config/api_config.dart';
import 'api_service.dart';

class AdminService {
  final ApiService _api = ApiService();

  Future<Map<String, dynamic>> getStats() async {
    final resp = await _api.get('${ApiConfig.baseUrl}/api/admin/stats');
    return resp['data'] ?? resp;
  }

  Future<Map<String, dynamic>> getUsers({int page = 1, int pageSize = 20}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/api/admin/users').replace(queryParameters: {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    });
    final resp = await _api.get(uri.toString());
    return resp['data'] ?? resp;
  }

  Future<void> updateUserStatus(String id, bool isActive) async {
    await _api.put('${ApiConfig.baseUrl}/api/admin/users/$id/status', {'isActive': isActive});
  }

  Future<void> updateUserRole(String id, String role) async {
    await _api.put('${ApiConfig.baseUrl}/api/admin/users/$id/role', {'role': role});
  }

  Future<Map<String, dynamic>> getHomestays({int page = 1, int pageSize = 20, String status = ''}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/api/admin/homestays').replace(queryParameters: {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      'status': status
    });
    final resp = await _api.get(uri.toString());
    return resp['data'] ?? resp;
  }

  Future<Map<String, dynamic>> searchHomestays({int page = 1, int pageSize = 20, String status = '', String q = ''}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/api/admin/homestays').replace(queryParameters: {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      'status': status,
      'q': q,
    });
    final resp = await _api.get(uri.toString());
    return resp['data'] ?? resp;
  }

  Future<void> approveHomestay(int id, bool isApproved) async {
    await _api.put('${ApiConfig.baseUrl}/api/admin/homestays/$id/approve', {'isApproved': isApproved});
  }

  Future<void> rejectHomestay(int id) async {
    await _api.post('${ApiConfig.baseUrl}/api/admin/homestays/$id/reject', {});
  }

  Future<void> activateHomestay(int id) async {
    await _api.put('${ApiConfig.baseUrl}/api/admin/homestays/$id/activate', {});
  }

  Future<void> deactivateHomestay(int id) async {
    await _api.put('${ApiConfig.baseUrl}/api/admin/homestays/$id/deactivate', {});
  }

  Future<Map<String, dynamic>> getBookings({int page = 1, int pageSize = 20}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/api/admin/bookings').replace(queryParameters: {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    });
    final resp = await _api.get(uri.toString());
    return resp['data'] ?? resp;
  }

  Future<void> updateBookingStatus(int id, String status) async {
    await _api.put('${ApiConfig.baseUrl}/api/admin/bookings/$id/status', {'status': status});
  }

  Future<void> confirmBooking(int id) async {
    await _api.post('${ApiConfig.baseUrl}/api/admin/bookings/$id/confirm', {});
  }

  Future<void> cancelBookingAdmin(int id) async {
    await _api.post('${ApiConfig.baseUrl}/api/admin/bookings/$id/cancel', {});
  }

  Future<void> completeBookingAdmin(int id) async {
    await _api.post('${ApiConfig.baseUrl}/api/admin/bookings/$id/complete', {});
  }

  Future<Map<String, dynamic>> searchBookings({int page = 1, int pageSize = 20, String status = 'all', String q = ''}) async {
    final uri = Uri.parse('${ApiConfig.baseUrl}/api/admin/bookings').replace(queryParameters: {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      'status': status,
      'q': q,
    });
    final resp = await _api.get(uri.toString());
    return resp['data'] ?? resp;
  }

  Future<Map<String, dynamic>> bulkApproveHomestays(List<int> ids, bool isApproved) async {
    final resp = await _api.put('${ApiConfig.baseUrl}/api/admin/homestays/bulk-approve', {'ids': ids, 'isApproved': isApproved});
    return resp['data'] ?? resp;
  }

  Future<void> deleteUser(String id) async {
    await _api.delete('${ApiConfig.baseUrl}/api/admin/users/$id');
  }

  Future<void> deleteHomestay(int id) async {
    await _api.delete('${ApiConfig.baseUrl}/api/admin/homestays/$id');
  }
}
