class Payment {
  final int id;
  final int bookingId;
  final double amount;
  final String method; // VNPay, Momo, BankTransfer
  final String status; // Pending, Completed, Failed, Refunded
  final String? transactionId;
  final DateTime? completedAt;
  final DateTime createdAt;

  Payment({
    required this.id,
    required this.bookingId,
    required this.amount,
    required this.method,
    required this.status,
    this.transactionId,
    this.completedAt,
    required this.createdAt,
  });

  factory Payment.fromJson(Map<String, dynamic> json) {
    return Payment(
      id: json['id'] ?? 0,
      bookingId: json['bookingId'] ?? 0,
      amount: (json['amount'] ?? 0).toDouble(),
      method: json['method'] ?? '',
      status: json['status'] ?? 'Pending',
      transactionId: json['transactionId'],
      completedAt: json['completedAt'] != null 
          ? DateTime.parse(json['completedAt']) 
          : null,
      createdAt: DateTime.parse(json['createdAt'] ?? DateTime.now().toIso8601String()),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'bookingId': bookingId,
      'amount': amount,
      'method': method,
      'status': status,
      'transactionId': transactionId,
      'completedAt': completedAt?.toIso8601String(),
      'createdAt': createdAt.toIso8601String(),
    };
  }

  String get statusDisplay {
    switch (status) {
      case 'Pending':
        return 'Chờ thanh toán';
      case 'Completed':
        return 'Đã thanh toán';
      case 'Failed':
        return 'Thất bại';
      case 'Refunded':
        return 'Đã hoàn tiền';
      default:
        return status;
    }
  }

  String get methodDisplay {
    switch (method) {
      case 'VNPay':
        return 'VNPay';
      case 'Momo':
        return 'MoMo';
      case 'BankTransfer':
        return 'Chuyển khoản';
      default:
        return method;
    }
  }
}

class AppNotification {
  final int id;
  final String userId;
  final String type;
  final String title;
  final String message;
  final String? relatedUrl;
  final bool isRead;
  final DateTime createdAt;

  AppNotification({
    required this.id,
    required this.userId,
    required this.type,
    required this.title,
    required this.message,
    this.relatedUrl,
    this.isRead = false,
    required this.createdAt,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    return AppNotification(
      id: json['id'] ?? 0,
      userId: json['userId'] ?? '',
      type: json['type'] ?? '',
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      relatedUrl: json['relatedUrl'],
      isRead: json['isRead'] ?? false,
      createdAt: DateTime.parse(json['createdAt'] ?? DateTime.now().toIso8601String()),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'type': type,
      'title': title,
      'message': message,
      'relatedUrl': relatedUrl,
      'isRead': isRead,
      'createdAt': createdAt.toIso8601String(),
    };
  }
}
