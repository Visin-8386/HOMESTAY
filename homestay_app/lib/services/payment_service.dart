import '../config/api_config.dart';
import '../models/payment.dart';
import 'api_service.dart';

class PaymentService {
  final ApiService _apiService = ApiService();

  // Get payment checkout info
  Future<Map<String, dynamic>> getCheckoutInfo(int bookingId) async {
    try {
      final response = await _apiService.get(
        '${ApiConfig.baseUrl}/Payment/Checkout?bookingId=$bookingId'
      );
      return response;
    } catch (e) {
      print('Error getting checkout info: $e');
      rethrow;
    }
  }

  // Process payment and get payment URL
  Future<Map<String, dynamic>> processPayment({
    required int bookingId,
    required String paymentMethod, // VNPay, MoMo, PayPal, Free
  }) async {
    try {
      final response = await _apiService.post(
        '${ApiConfig.baseUrl}/Payment/ProcessPayment',
        {
          'bookingId': bookingId,
          'paymentMethod': paymentMethod,
        },
      );
      
      if (response['success'] == true) {
        return {
          'success': true,
          'paymentUrl': response['paymentUrl'],
        };
      } else {
        return {
          'success': false,
          'message': response['message'] ?? 'Payment failed',
        };
      }
    } catch (e) {
      print('Error processing payment: $e');
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }

  Future<Map<String, dynamic>> createPayment({
    required int bookingId,
    required String method,
  }) async {
    // Deprecated - use processPayment instead
    return await processPayment(bookingId: bookingId, paymentMethod: method);
  }

  Future<Payment> getPaymentByBooking(int bookingId) async {
    final response = await _apiService.get(
      ApiConfig.paymentByBookingUrl(bookingId),
    );
    return Payment.fromJson(response);
  }

  // Notification methods integrated into PaymentService
  Future<Map<String, dynamic>> getNotifications({
    int page = 1,
    int pageSize = 20,
  }) async {
    final uri = Uri.parse(ApiConfig.notificationsUrl).replace(
      queryParameters: {
        'page': page.toString(),
        'pageSize': pageSize.toString(),
      },
    );

    final response = await _apiService.get(uri.toString());
    final List<dynamic> notificationsData = response['data']?['notifications'] ?? [];
    final notifications = notificationsData.map((json) => AppNotification.fromJson(json)).toList();
    final unreadCount = response['data']?['unreadCount'] ?? 0;
    
    return {
      'notifications': notifications,
      'unreadCount': unreadCount,
    };
  }

  Future<int> getUnreadCount() async {
    final response = await _apiService.get(ApiConfig.unreadCountUrl);
    return response['count'] ?? 0;
  }

  Future<void> markNotificationAsRead(String id) async {
    await _apiService.post(
      '${ApiConfig.notificationsUrl}/$id/read',
      {},
    );
  }

  Future<void> markAllNotificationsAsRead() async {
    await _apiService.post('${ApiConfig.notificationsUrl}/mark-all-read', {});
  }

  /// Lấy lịch sử thanh toán
  Future<List<Map<String, dynamic>>> getPaymentHistory({
    int page = 1,
    int pageSize = 20,
    String? status,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    try {
      String url = '${ApiConfig.baseUrl}/api/payments/history?page=$page&pageSize=$pageSize';
      
      if (status != null && status.isNotEmpty) {
        url += '&status=$status';
      }
      if (startDate != null) {
        url += '&startDate=${startDate.toIso8601String()}';
      }
      if (endDate != null) {
        url += '&endDate=${endDate.toIso8601String()}';
      }
      
      final response = await _apiService.get(url);
      
      if (response['data'] != null) {
        return List<Map<String, dynamic>>.from(response['data']);
      }
      return [];
    } catch (e) {
      print('Error getting payment history: $e');
      rethrow;
    }
  }

  /// Xuất báo cáo thanh toán
  Future<String> exportPaymentReport({
    DateTime? startDate,
    DateTime? endDate,
    String format = 'pdf', // pdf or csv
  }) async {
    try {
      String url = '${ApiConfig.baseUrl}/api/payments/export?format=$format';
      
      if (startDate != null) {
        url += '&startDate=${startDate.toIso8601String()}';
      }
      if (endDate != null) {
        url += '&endDate=${endDate.toIso8601String()}';
      }
      
      final response = await _apiService.get(url);
      return response['downloadUrl'] ?? '';
    } catch (e) {
      print('Error exporting payment report: $e');
      rethrow;
    }
  }
}
