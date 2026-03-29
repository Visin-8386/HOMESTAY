import '../config/api_config.dart';

class ImageHelper {
  /// Get full image URL from relative path
  static String getFullImageUrl(String? imageUrl) {
    if (imageUrl == null || imageUrl.isEmpty) {
      return homestayPlaceholder;
    }
    
    // If already full URL
    if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) {
      return imageUrl;
    }
    
    // If relative path starting with /
    if (imageUrl.startsWith('/')) {
      return '${ApiConfig.baseUrl}$imageUrl';
    }
    
    // Otherwise prepend base URL and /images/
    return '${ApiConfig.baseUrl}/images/$imageUrl';
  }
  
  /// Get logo URL
  static String get logoUrl => '${ApiConfig.baseUrl}/images/admin/logo.png';
  
  /// Get placeholder for homestay
  static String get homestayPlaceholder => '${ApiConfig.baseUrl}/images/placeholder-homestay.svg';
  
  /// Get default homestay image
  static String get defaultHomestay => '${ApiConfig.baseUrl}/images/default-homestay.jpg';
  
  /// Get default avatar
  static String get defaultAvatar => '${ApiConfig.baseUrl}/images/default-avatar.svg';
  
  /// Get default avatar JPG
  static String get defaultAvatarJpg => '${ApiConfig.baseUrl}/images/default-avatar.jpg';
  
  /// Get no image placeholder
  static String get noImage => '${ApiConfig.baseUrl}/images/no-image.jpg';
  
  /// Get Tudong background images (for splash screen or slideshow)
  static List<String> get tudongImages => List.generate(
    7,
    (index) => '${ApiConfig.baseUrl}/images/Tudong/${index + 1}.jpg',
  );
  
  /// Get primary image from list or return placeholder
  static String getPrimaryImage(List<dynamic>? images) {
    if (images == null || images.isEmpty) {
      return homestayPlaceholder;
    }
    
    // Try to find primary image
    try {
      final primary = images.firstWhere(
        (img) => img['isPrimary'] == true,
        orElse: () => images.first,
      );
      return getFullImageUrl(primary['imageUrl']);
    } catch (e) {
      return homestayPlaceholder;
    }
  }
  
  /// Get all image URLs from image list
  static List<String> getImageUrls(List<dynamic>? images) {
    if (images == null || images.isEmpty) {
      return [homestayPlaceholder];
    }
    
    return images
        .map((img) => getFullImageUrl(img['imageUrl'] as String?))
        .toList();
  }
}
