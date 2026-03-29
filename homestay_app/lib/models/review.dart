class Review {
  final int id;
  final int userId;
  final int homestayId;
  final int bookingId;
  final double rating;
  final String comment;
  final List<String>? images;
  final DateTime createdAt;
  final DateTime updatedAt;
  final String userName;
  final String? userAvatar;
  final String? homestayName;

  Review({
    required this.id,
    required this.userId,
    required this.homestayId,
    required this.bookingId,
    required this.rating,
    required this.comment,
    this.images,
    required this.createdAt,
    required this.updatedAt,
    required this.userName,
    this.userAvatar,
    this.homestayName,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: json['id'] ?? 0,
      userId: json['userId'] ?? 0,
      homestayId: json['homestayId'] ?? 0,
      bookingId: json['bookingId'] ?? 0,
      rating: (json['rating'] ?? 0).toDouble(),
      comment: json['comment'] ?? '',
      images: json['images'] != null
          ? List<String>.from(json['images'])
          : null,
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'])
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.parse(json['updatedAt'])
          : DateTime.now(),
      userName: json['userName'] ?? 'Người dùng',
      userAvatar: json['userAvatar'],
      homestayName: json['homestayName'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'homestayId': homestayId,
      'bookingId': bookingId,
      'rating': rating,
      'comment': comment,
      'images': images,
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt.toIso8601String(),
      'userName': userName,
      'userAvatar': userAvatar,
      'homestayName': homestayName,
    };
  }
}