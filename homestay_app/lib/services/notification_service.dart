import '../config/api_config.dart';
import 'api_service.dart';

class NotificationService {
  final ApiService _apiService = ApiService();

  /// Lấy danh sách thông báo
  Future<List<Map<String, dynamic>>> getNotifications({
    int page = 1,
    int pageSize = 20,
    String? type,
    bool? isRead,
  }) async {
    try {
      String url = '${ApiConfig.baseUrl}/api/notifications?page=$page&pageSize=$pageSize';
      
      if (type != null && type.isNotEmpty) {
        url += '&type=$type';
      }
      if (isRead != null) {
        url += '&isRead=$isRead';
      }
      
      final response = await _apiService.get(url);
      
      if (response['data'] != null) {
        return List<Map<String, dynamic>>.from(response['data']);
      }
      return [];
    } catch (e) {
      print('Error getting notifications: $e');
      rethrow;
    }
  }

  /// Lấy số lượng thông báo chưa đọc
  Future<int> getUnreadCount() async {
    try {
      final response = await _apiService.get('${ApiConfig.baseUrl}/api/notifications/unread-count');
      return response['count'] ?? 0;
    } catch (e) {
      print('Error getting unread count: $e');
      return 0;
    }
  }

  /// Đánh dấu thông báo đã đọc
  Future<bool> markAsRead(int notificationId) async {
    try {
      await _apiService.put(
        '${ApiConfig.baseUrl}/api/notifications/$notificationId/mark-read',
        {},
      );
      return true;
    } catch (e) {
      print('Error marking notification as read: $e');
      rethrow;
    }
  }

  /// Đánh dấu tất cả đã đọc
  Future<bool> markAllAsRead() async {
    try {
      await _apiService.put(
        '${ApiConfig.baseUrl}/api/notifications/mark-all-read',
        {},
      );
      return true;
    } catch (e) {
      print('Error marking all as read: $e');
      rethrow;
    }
  }

  /// Xóa thông báo
  Future<bool> deleteNotification(int notificationId) async {
    try {
      await _apiService.delete('${ApiConfig.baseUrl}/api/notifications/$notificationId');
      return true;
    } catch (e) {
      print('Error deleting notification: $e');
      rethrow;
    }
  }

  /// Xóa tất cả thông báo
  Future<bool> deleteAllNotifications() async {
    try {
      await _apiService.delete('${ApiConfig.baseUrl}/api/notifications');
      return true;
    } catch (e) {
      print('Error deleting all notifications: $e');
      rethrow;
    }
  }
}
