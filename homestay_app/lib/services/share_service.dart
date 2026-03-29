import 'package:share_plus/share_plus.dart';

import '../models/homestay.dart';

class ShareService {
  /// Share homestay details via share sheet
  static Future<void> shareHomestay(Homestay homestay) async {
    final text = '''
🏠 ${homestay.name}

📍 ${homestay.address}, ${homestay.city}
💰 ${homestay.priceDisplay}
⭐ ${homestay.averageRating != null ? '${homestay.averageRating!.toStringAsFixed(1)} (${homestay.reviewCount} đánh giá)' : 'Chưa có đánh giá'}
👥 Tối đa ${homestay.maxGuests} khách
🛏️ ${homestay.numberOfBedrooms} phòng ngủ
🚿 ${homestay.numberOfBathrooms} phòng tắm

${homestay.description}

🔗 Xem chi tiết: ${_getHomestayDeepLink(homestay.id)}
''';

    await Share.share(
      text,
      subject: 'Homestay: ${homestay.name}',
    );
  }

  /// Share homestay with an image
  static Future<void> shareHomestayWithImage(
    Homestay homestay,
    String? imageUrl,
  ) async {
    final text = '''
🏠 ${homestay.name}

📍 ${homestay.address}
💰 ${homestay.priceDisplay}
⭐ ${homestay.averageRating?.toStringAsFixed(1) ?? 'N/A'}

🔗 ${_getHomestayDeepLink(homestay.id)}
''';

    if (imageUrl != null && imageUrl.isNotEmpty) {
      // For sharing with images, you'd need to download the image first
      // This is a simplified version
      await Share.share(text, subject: homestay.name);
    } else {
      await Share.share(text, subject: homestay.name);
    }
  }

  /// Share app referral link
  static Future<void> shareAppReferral() async {
    const text = '''
🏠 Khám phá hàng ngàn homestay tuyệt vời!

Tải app Homestay để:
✅ Tìm kiếm và đặt phòng dễ dàng
✅ Giá tốt nhất
✅ Đánh giá thật từ người dùng
✅ Hỗ trợ 24/7

📱 Tải ngay: https://homestay.app/download
''';

    await Share.share(text, subject: 'Ứng dụng Homestay');
  }

  /// Get deep link URL for homestay detail
  static String _getHomestayDeepLink(int homestayId) {
    // Replace with your actual deep link domain
    return 'https://homestay.app/homestay/$homestayId';
  }

  /// Share search results
  static Future<void> shareSearchResults({
    required String city,
    required DateTime checkIn,
    required DateTime checkOut,
    required int guests,
  }) async {
    final text = '''
🔍 Tìm homestay tại ${city}

📅 Nhận phòng: ${_formatDate(checkIn)}
📅 Trả phòng: ${_formatDate(checkOut)}
👥 Số khách: $guests

🔗 Tìm kiếm: ${_getSearchDeepLink(city, checkIn, checkOut, guests)}
''';

    await Share.share(text, subject: 'Tìm kiếm homestay tại $city');
  }

  static String _formatDate(DateTime date) {
    return '${date.day}/${date.month}/${date.year}';
  }

  static String _getSearchDeepLink(
    String city,
    DateTime checkIn,
    DateTime checkOut,
    int guests,
  ) {
    return 'https://homestay.app/search?city=$city&checkIn=${checkIn.toIso8601String()}&checkOut=${checkOut.toIso8601String()}&guests=$guests';
  }
}
