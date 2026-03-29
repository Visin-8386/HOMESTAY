import '../config/api_config.dart';

class Homestay {
  // Thêm các trường phụ trợ cho state, zipCode nếu có trong backend
  // Nếu backend không trả về, các getter này sẽ trả về rỗng
  String get state => '';
  String get zipCode => '';
  int get bedrooms => numberOfBedrooms;
  int get bathrooms => numberOfBathrooms;
  final int id;
  final String name;
  final String description;
  final String address;
  final String city;
  final String? district;
  final String? ward;
  final double latitude;
  final double longitude;
  final double pricePerNight;
  final int maxGuests;
  final int numberOfBedrooms;
  final int numberOfBathrooms;
  final String? youtubeVideoId;
  final List<String> images;
  final List<Amenity> amenities;
  final double? averageRating;
  final int reviewCount;
  final String hostId;
  final String hostName;
  final String? hostAvatar;
  final bool isActive;
  final DateTime createdAt;

  Homestay({
    required this.id,
    required this.name,
    required this.description,
    required this.address,
    required this.city,
    this.district,
    this.ward,
    required this.latitude,
    required this.longitude,
    required this.pricePerNight,
    required this.maxGuests,
    required this.numberOfBedrooms,
    required this.numberOfBathrooms,
  this.youtubeVideoId,
    this.images = const [],
    this.amenities = const [],
    this.averageRating,
    this.reviewCount = 0,
    required this.hostId,
    required this.hostName,
    this.hostAvatar,
    this.isActive = true,
    required this.createdAt,
  });

  factory Homestay.fromJson(Map<String, dynamic> json) {
    try {
      return Homestay(
        id: json['id'] ?? 0,
        name: json['name']?.toString() ?? '',
        description: json['description']?.toString() ?? '',
        address: json['address']?.toString() ?? '',
        city: json['city']?.toString() ?? '',
        district: json['district']?.toString(),
        ward: json['ward']?.toString(),
        latitude: (json['latitude'] ?? 0).toDouble(),
        longitude: (json['longitude'] ?? 0).toDouble(),
        pricePerNight: (json['pricePerNight'] ?? 0).toDouble(),
        maxGuests: json['maxGuests'] ?? 1,
        numberOfBedrooms: json['bedrooms'] ?? json['numberOfBedrooms'] ?? 1,
        numberOfBathrooms: json['bathrooms'] ?? json['numberOfBathrooms'] ?? 1,
        images: json['images'] != null 
            ? (json['images'] as List).map((img) {
                // Backend returns HomestayImageDto objects with imageUrl field
                String imageUrl = '';
                if (img is String) {
                  imageUrl = img;
                } else {
                  imageUrl = img['imageUrl']?.toString() ?? '';
                }
                
                // Convert relative URLs to absolute URLs
                if (imageUrl.isNotEmpty && !imageUrl.startsWith('http')) {
                  final originalUrl = imageUrl;
                  imageUrl = ApiConfig.baseUrl + imageUrl;
                  print('🖼️ Image URL converted: $originalUrl → $imageUrl');
                } else if (imageUrl.isNotEmpty) {
                  print('🖼️ Image URL (already absolute): $imageUrl');
                }
                
                return imageUrl;
              }).where((url) => url.isNotEmpty).toList().cast<String>()
            : [],
        amenities: json['amenities'] != null
            ? (json['amenities'] as List).map((a) => Amenity.fromJson(a)).toList()
            : [],
        averageRating: json['averageRating']?.toDouble(),
        reviewCount: json['reviewCount'] ?? 0,
        hostId: json['hostId']?.toString() ?? '',
        hostName: json['hostName']?.toString() ?? '',
        hostAvatar: json['hostAvatar']?.toString(),
        youtubeVideoId: json['youTubeVideoId']?.toString() ?? json['YouTubeVideoId']?.toString(),
        isActive: json['isActive'] ?? true,
        createdAt: DateTime.parse(json['createdAt'] ?? DateTime.now().toIso8601String()),
      );
    } catch (e) {
      print('Error parsing Homestay: $e');
      print('JSON data: $json');
      rethrow;
    }
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description,
      'address': address,
      'city': city,
      'district': district,
      'ward': ward,
      'latitude': latitude,
      'longitude': longitude,
      'pricePerNight': pricePerNight,
      'maxGuests': maxGuests,
      'numberOfBedrooms': numberOfBedrooms,
      'numberOfBathrooms': numberOfBathrooms,
      'images': images,
      'amenities': amenities.map((a) => a.toJson()).toList(),
      'averageRating': averageRating,
      'reviewCount': reviewCount,
      'hostId': hostId,
      'hostName': hostName,
      'hostAvatar': hostAvatar,
      'isActive': isActive,
      'createdAt': createdAt.toIso8601String(),
      'youTubeVideoId': youtubeVideoId,
    };
  }

  String get fullAddress {
    final parts = <String>[];
    if (ward != null && ward!.isNotEmpty) parts.add(ward!);
    if (district != null && district!.isNotEmpty) parts.add(district!);
    parts.add(city);
    return parts.join(', ');
  }

  String get priceDisplay => '${pricePerNight.toStringAsFixed(0)}đ/đêm';
  String get guestsDisplay => '$maxGuests khách';
  String get bedroomsDisplay => '$numberOfBedrooms phòng ngủ';
  String get bathroomsDisplay => '$numberOfBathrooms phòng tắm';
}

class Amenity {
  final int id;
  final String name;
  final String? icon;

  Amenity({
    required this.id,
    required this.name,
    this.icon,
  });

  factory Amenity.fromJson(Map<String, dynamic> json) {
    return Amenity(
      id: json['id'] ?? 0,
      name: json['name']?.toString() ?? '',
      icon: json['icon']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'icon': icon,
    };
  }
}
