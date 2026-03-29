// Top-level helpers for defensive parsing so multiple classes can reuse them
DateTime _parseDateTime(dynamic input) {
  try {
    if (input == null) return DateTime.now();
    if (input is DateTime) return input;
    if (input is int) return DateTime.fromMillisecondsSinceEpoch(input);
    if (input is double) return DateTime.fromMillisecondsSinceEpoch(input.toInt());
    final s = input.toString();
    return DateTime.parse(s);
  } catch (e) {
    return DateTime.now();
  }
}

double _parseDouble(dynamic input) {
  try {
    if (input == null) return 0.0;
    if (input is double) return input;
    if (input is int) return input.toDouble();
    return double.tryParse(input.toString()) ?? 0.0;
  } catch (e) {
    return 0.0;
  }
}

int _parseInt(dynamic input) {
  try {
    if (input == null) return 0;
    if (input is int) return input;
    if (input is double) return input.toInt();
    return int.tryParse(input.toString()) ?? 0;
  } catch (e) {
    return 0;
  }
}

String _parseString(dynamic input, {String defaultValue = ''}) {
  try {
    if (input == null) return defaultValue;
    return input.toString();
  } catch (e) {
    return defaultValue;
  }
}

String _statusFromValue(dynamic input) {
  // Backend may return numeric enum (0..3) or string value.
  try {
    if (input == null) return 'Pending';
    if (input is int) {
      switch (input) {
        case 0:
          return 'Pending';
        case 1:
          return 'Paid';
        case 2:
          return 'Cancelled';
        case 3:
          return 'Completed';
        default:
          return 'Pending';
      }
    }
    final s = input.toString();
    // if numeric string
    final asInt = int.tryParse(s);
    if (asInt != null) return _statusFromValue(asInt);
    // normalize common textual variants
    final lower = s.toLowerCase();
    if (lower.contains('pending')) return 'Pending';
    if (lower.contains('paid') || lower.contains('confirmed')) return 'Paid';
    if (lower.contains('cancel') || lower.contains('cancelled')) return 'Cancelled';
    if (lower.contains('complete')) return 'Completed';
    // fallback to input string
    return s;
  } catch (e) {
    return 'Pending';
  }
}

double _extractTotalPrice(Map<String, dynamic> json) {
  final candidates = [
    'finalAmount',
    'FinalAmount',
    'totalPrice',
    'TotalPrice',
    'total_amount',
    'total',
    'amount',
  ];
  for (final key in candidates) {
    if (json.containsKey(key)) {
      final val = json[key];
      final parsed = _parseDouble(val);
      if (parsed != 0.0) return parsed;
    }
  }
  return 0.0;
}

String _extractGuestName(Map<String, dynamic> json) {
  final candidates = [
    'guestName',
    'guest_name',
    'customerName',
    'customer_name',
    'guest',
    'customer',
    'user',
    'userName',
    'UserName',
    'username',
  ];

  for (final key in candidates) {
    if (!json.containsKey(key)) continue;
    final val = json[key];
    if (val == null) continue;

    // If it's a string, return it
    if (val is String && val.trim().isNotEmpty) return val.trim();

    // If it's a map, try common properties
    if (val is Map) {
      final m = Map<String, dynamic>.from(val);
      if (m.containsKey('fullName') && m['fullName'] != null) return m['fullName'].toString();
      if (m.containsKey('name') && m['name'] != null) return m['name'].toString();
      if (m.containsKey('firstName') || m.containsKey('lastName')) {
        final first = m['firstName']?.toString() ?? '';
        final last = m['lastName']?.toString() ?? '';
        final combined = ('$first $last').trim();
        if (combined.isNotEmpty) return combined;
      }
    }
  }

  // Fallback to empty string
  return '';
}

class Booking {
  final int id;
  final int homestayId;
  final String homestayName;
  final String? homestayImage;
  final String userId;
  final String guestName;
  final String? guestAvatar;
  final DateTime checkInDate;
  final DateTime checkOutDate;
  final int numberOfGuests;
  final double totalPrice;
  final String status; // Pending, Confirmed, Cancelled, Completed
  final String? specialRequests;
  final DateTime createdAt;
  final Review? review;

  Booking({
    required this.id,
    required this.homestayId,
    required this.homestayName,
    this.homestayImage,
    required this.userId,
    required this.guestName,
    this.guestAvatar,
    required this.checkInDate,
    required this.checkOutDate,
    required this.numberOfGuests,
    required this.totalPrice,
    required this.status,
    this.specialRequests,
    required this.createdAt,
    this.review,
  });

  factory Booking.fromJson(Map<String, dynamic> json) {
    return Booking(
      id: _parseInt(json['id']),
      homestayId: _parseInt(json['homestayId']),
      homestayName: _parseString(json['homestayName']),
      homestayImage: json['homestayImage']?.toString(),
  userId: _parseString(json['userId']),
    // Try flexible extraction first (looks for many possible keys and nested objects),
    // then fall back to common keys used by different backends: guestName, UserName, userName
    guestName: _extractGuestName(json).isNotEmpty
      ? _extractGuestName(json)
      : (_parseString(json['guestName']).isNotEmpty
        ? _parseString(json['guestName'])
        : (_parseString(json['UserName']).isNotEmpty
          ? _parseString(json['UserName'])
          : _parseString(json['userName']))),
      guestAvatar: json['guestAvatar']?.toString(),
      checkInDate: _parseDateTime(json['checkInDate']),
      checkOutDate: _parseDateTime(json['checkOutDate']),
      numberOfGuests: _parseInt(json['numberOfGuests']) == 0 ? 1 : _parseInt(json['numberOfGuests']),
      totalPrice: _extractTotalPrice(json),
  status: _statusFromValue(json['status']),
      specialRequests: json['specialRequests']?.toString(),
      createdAt: _parseDateTime(json['createdAt']),
      review: json['review'] != null && json['review'] is Map<String, dynamic>
          ? Review.fromJson(Map<String, dynamic>.from(json['review']))
          : (json['review'] != null && json['review'] is Map ? Review.fromJson(Map<String, dynamic>.from(json['review'] as Map)) : null),
    );
  }


  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'homestayId': homestayId,
      'homestayName': homestayName,
      'homestayImage': homestayImage,
      'userId': userId,
      'guestName': guestName,
      'guestAvatar': guestAvatar,
      'checkInDate': checkInDate.toIso8601String(),
      'checkOutDate': checkOutDate.toIso8601String(),
      'numberOfGuests': numberOfGuests,
      'totalPrice': totalPrice,
      'status': status,
      'specialRequests': specialRequests,
      'createdAt': createdAt.toIso8601String(),
      'review': review?.toJson(),
    };
  }

  int get numberOfNights => checkOutDate.difference(checkInDate).inDays;
  
  String get statusDisplay {
    switch (status) {
      case 'Pending':
        return 'Chờ xác nhận';
      case 'Confirmed':
        return 'Đã xác nhận';
      case 'Cancelled':
        return 'Đã hủy';
      case 'Completed':
        return 'Hoàn thành';
      default:
        return status;
    }
  }

  bool get canCancel => status == 'Pending' || status == 'Confirmed';
  bool get canReview => status == 'Completed' && review == null;
}

class Review {
  final int id;
  final int bookingId;
  final int homestayId;
  final String userId;
  final String userName;
  final String? userAvatar;
  final int rating;
  final String? comment;
  final DateTime createdAt;

  Review({
    required this.id,
    required this.bookingId,
    required this.homestayId,
    required this.userId,
    required this.userName,
    this.userAvatar,
    required this.rating,
    this.comment,
    required this.createdAt,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: _parseInt(json['id']),
      bookingId: _parseInt(json['bookingId']),
      homestayId: _parseInt(json['homestayId']),
      userId: _parseString(json['userId']),
      userName: _parseString(json['userName']),
      userAvatar: json['userAvatar']?.toString(),
      rating: _parseInt(json['rating']) == 0 ? 5 : _parseInt(json['rating']),
      comment: json['comment']?.toString(),
      createdAt: () {
        try {
          final val = json['createdAt'];
          if (val == null) return DateTime.now();
          if (val is int) return DateTime.fromMillisecondsSinceEpoch(val);
          if (val is double) return DateTime.fromMillisecondsSinceEpoch(val.toInt());
          if (val is DateTime) return val;
          return DateTime.parse(val.toString());
        } catch (e) {
          return DateTime.now();
        }
      }(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'bookingId': bookingId,
      'homestayId': homestayId,
      'userId': userId,
      'userName': userName,
      'userAvatar': userAvatar,
      'rating': rating,
      'comment': comment,
      'createdAt': createdAt.toIso8601String(),
    };
  }
}
