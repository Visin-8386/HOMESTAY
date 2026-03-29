class User {
  final String id;
  final String userName;
  final String email;
  final String? fullName;
  final String? phoneNumber;
  final String? avatarUrl;
  final DateTime? dateOfBirth;
  final String? address;
  final String? bio;
  final List<String> roles;
  final DateTime createdAt;

  User({
    required this.id,
    required this.userName,
    required this.email,
    this.fullName,
    this.phoneNumber,
    this.avatarUrl,
    this.dateOfBirth,
    this.address,
    this.bio,
    this.roles = const [],
    required this.createdAt,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['id']?.toString() ?? '',
      userName: json['userName'] ?? '',
      email: json['email'] ?? '',
      fullName: json['fullName'],
      phoneNumber: json['phoneNumber'],
      avatarUrl: json['avatarUrl'],
      dateOfBirth: json['dateOfBirth'] != null 
          ? DateTime.parse(json['dateOfBirth']) 
          : null,
      address: json['address'],
      bio: json['bio'],
      roles: json['roles'] != null ? List<String>.from(json['roles']) : [],
      createdAt: DateTime.parse(json['createdAt'] ?? DateTime.now().toIso8601String()),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userName': userName,
      'email': email,
      'fullName': fullName,
      'phoneNumber': phoneNumber,
      'avatarUrl': avatarUrl,
      'dateOfBirth': dateOfBirth?.toIso8601String(),
      'address': address,
      'bio': bio,
      'roles': roles,
      'createdAt': createdAt.toIso8601String(),
    };
  }

  bool get isHost => roles.contains('Host');
  bool get isAdmin => roles.contains('Admin');
}
