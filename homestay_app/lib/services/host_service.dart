import '../config/api_config.dart';
import 'api_service.dart';

class HostService {
  final ApiService _apiService = ApiService();

  /// Lấy thống kê dashboard cho host
  Future<Map<String, dynamic>> getDashboardStats() async {
    try {
      final response = await _apiService.get('${ApiConfig.baseUrl}/api/host/dashboard');
      return response;
    } catch (e) {
      print('Error getting dashboard stats: $e');
      rethrow;
    }
  }

  /// Lấy danh sách homestay của host
  Future<List<dynamic>> getMyHomestays() async {
    try {
      final response = await _apiService.get('${ApiConfig.baseUrl}/api/host/homestays');
      if (response['data'] != null) {
        return response['data'] as List<dynamic>;
      }
      return [];
    } catch (e) {
      print('Error getting host homestays: $e');
      rethrow;
    }
  }

  /// Lấy danh sách booking của host
  Future<List<dynamic>> getMyBookings({String? status}) async {
    try {
      String url = '${ApiConfig.baseUrl}/api/host/bookings';
      if (status != null && status.isNotEmpty) {
        url += '?status=$status';
      }
      
      final response = await _apiService.get(url);
      if (response['data'] != null) {
        return response['data'] as List<dynamic>;
      }
      return [];
    } catch (e) {
      print('Error getting host bookings: $e');
      rethrow;
    }
  }

  /// Cập nhật trạng thái booking
  Future<bool> updateBookingStatus(int bookingId, String status) async {
    try {
      await _apiService.put(
        '${ApiConfig.baseUrl}/api/host/bookings/$bookingId/status',
        {'status': status},
      );
      return true;
    } catch (e) {
      print('Error updating booking status: $e');
      rethrow;
    }
  }

  /// Lấy thống kê doanh thu
  Future<Map<String, dynamic>> getRevenueStats({
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    try {
      String url = '${ApiConfig.baseUrl}/api/host/revenue';
      final params = <String>[];
      
      if (startDate != null) {
        params.add('startDate=${startDate.toIso8601String()}');
      }
      if (endDate != null) {
        params.add('endDate=${endDate.toIso8601String()}');
      }
      
      if (params.isNotEmpty) {
        url += '?${params.join('&')}';
      }
      
      final response = await _apiService.get(url);
      return response;
    } catch (e) {
      print('Error getting revenue stats: $e');
      rethrow;
    }
  }
}
