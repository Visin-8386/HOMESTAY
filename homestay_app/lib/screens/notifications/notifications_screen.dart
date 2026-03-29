import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../config/app_colors.dart';
import '../../models/payment.dart';
import '../../services/payment_service.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({Key? key}) : super(key: key);

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final PaymentService _paymentService = PaymentService();
  List<AppNotification> _notifications = [];
  bool _isLoading = true;
  int _unreadCount = 0;

  @override
  void initState() {
    super.initState();
    _loadNotifications();
  }

  Future<void> _loadNotifications() async {
    setState(() => _isLoading = true);
    
    try {
      final result = await _paymentService.getNotifications();
      setState(() {
        _notifications = result['notifications'] as List<AppNotification>;
        _unreadCount = result['unreadCount'] as int;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải thông báo: $e')),
        );
      }
    }
  }

  Future<void> _markAsRead(String id) async {
    try {
      await _paymentService.markNotificationAsRead(id);
      setState(() {
        final index = _notifications.indexWhere((n) => n.id == id);
        if (index != -1 && !_notifications[index].isRead) {
          _notifications[index] = AppNotification(
            id: _notifications[index].id,
            userId: _notifications[index].userId,
            title: _notifications[index].title,
            message: _notifications[index].message,
            type: _notifications[index].type,
            isRead: true,
            createdAt: _notifications[index].createdAt,
            relatedUrl: _notifications[index].relatedUrl,
          );
          _unreadCount = (_unreadCount - 1).clamp(0, 9999);
        }
      });
    } catch (e) {
      print('Error marking notification as read: $e');
    }
  }

  Future<void> _markAllAsRead() async {
    try {
      await _paymentService.markAllNotificationsAsRead();
      setState(() {
        _notifications = _notifications.map((n) => AppNotification(
          id: n.id,
          userId: n.userId,
          title: n.title,
          message: n.message,
          type: n.type,
          isRead: true,
          createdAt: n.createdAt,
          relatedUrl: n.relatedUrl,
        )).toList();
        _unreadCount = 0;
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi đánh dấu đã đọc: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            const Text('Thông báo'),
            if (_unreadCount > 0) ...[
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.error,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  '$_unreadCount',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ],
        ),
        actions: [
          if (_unreadCount > 0)
            TextButton(
              onPressed: _markAllAsRead,
              child: const Text('Đọc tất cả'),
            ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_notifications.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.notifications_none, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'Chưa có thông báo nào',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadNotifications,
      child: ListView.separated(
        itemCount: _notifications.length,
        separatorBuilder: (context, index) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final notification = _notifications[index];
          return _buildNotificationTile(notification);
        },
      ),
    );
  }

  Widget _buildNotificationTile(AppNotification notification) {
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      leading: Container(
        width: 48,
        height: 48,
        decoration: BoxDecoration(
          color: _getNotificationColor(notification.type).withOpacity(0.1),
          shape: BoxShape.circle,
        ),
        child: Icon(
          _getNotificationIcon(notification.type),
          color: _getNotificationColor(notification.type),
        ),
      ),
      title: Row(
        children: [
          Expanded(
            child: Text(
              notification.title,
              style: TextStyle(
                fontWeight: notification.isRead ? FontWeight.normal : FontWeight.bold,
              ),
            ),
          ),
          Text(
            _formatTime(notification.createdAt),
            style: TextStyle(
              fontSize: 12,
              color: Colors.grey[600],
            ),
          ),
        ],
      ),
      subtitle: Text(
        notification.message,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: notification.isRead ? Colors.grey[600] : Colors.black87,
        ),
      ),
      tileColor: notification.isRead ? null : AppColors.primary.withOpacity(0.05),
      onTap: () {
        if (!notification.isRead) {
          _markAsRead(notification.id.toString());
        }
        // Handle navigation based on relatedUrl if needed
        if (notification.relatedUrl != null) {
          // Navigate to related screen
          print('Navigate to: ${notification.relatedUrl}');
        }
      },
    );
  }

  IconData _getNotificationIcon(String type) {
    switch (type.toLowerCase()) {
      case 'booking':
        return Icons.book_online;
      case 'payment':
        return Icons.payment;
      case 'review':
        return Icons.star;
      case 'message':
        return Icons.message;
      case 'system':
        return Icons.info;
      default:
        return Icons.notifications;
    }
  }

  Color _getNotificationColor(String type) {
    switch (type.toLowerCase()) {
      case 'booking':
        return Colors.blue;
      case 'payment':
        return Colors.green;
      case 'review':
        return Colors.orange;
      case 'message':
        return AppColors.primary;
      case 'system':
        return Colors.grey;
      default:
        return AppColors.primary;
    }
  }

  String _formatTime(DateTime time) {
    final now = DateTime.now();
    final difference = now.difference(time);

    if (difference.inMinutes < 1) {
      return 'Vừa xong';
    } else if (difference.inHours < 1) {
      return '${difference.inMinutes} phút trước';
    } else if (difference.inDays < 1) {
      return '${difference.inHours} giờ trước';
    } else if (difference.inDays < 7) {
      return '${difference.inDays} ngày trước';
    } else {
      return DateFormat('dd/MM/yyyy').format(time);
    }
  }
}
