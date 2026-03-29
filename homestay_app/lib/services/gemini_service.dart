import 'dart:convert';

import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;

class GeminiService {
  final String _apiKey;
  final String _baseUrl;

  GeminiService()
      : _apiKey = dotenv.env['GEMINI_API_KEY'] ?? '',
        _baseUrl = dotenv.env['GEMINI_BASE_URL'] ??
            'https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent';

  /// Send a message to Gemini AI and get response
  Future<Map<String, dynamic>> chat(String message) async {
    try {
      if (_apiKey.isEmpty) {
        return {
          'success': false,
          'error': 'Gemini API key not configured',
        };
      }

      final response = await http.post(
        Uri.parse('$_baseUrl?key=$_apiKey'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'contents': [
            {
              'parts': [
                {'text': message}
              ]
            }
          ],
          'generationConfig': {
            'temperature': 0.7,
            'topK': 40,
            'topP': 0.95,
            'maxOutputTokens': 1024,
          }
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        
        // Extract text from Gemini response
        final text = data['candidates']?[0]?['content']?['parts']?[0]?['text'] ?? 
                     'Không có phản hồi từ AI';

        return {
          'success': true,
          'message': text,
          'timestamp': DateTime.now().toIso8601String(),
        };
      } else {
        final errorData = jsonDecode(response.body);
        return {
          'success': false,
          'error': errorData['error']?['message'] ?? 'Unknown error',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'error': e.toString(),
      };
    }
  }

  /// Chat with Gemini AI with specific context about homestay booking
  Future<Map<String, dynamic>> chatWithContext(String message) async {
    final contextPrompt = '''
Bạn là trợ lý AI thông minh cho ứng dụng đặt homestay tại Việt Nam.

Nhiệm vụ của bạn:
- Hỗ trợ người dùng tìm kiếm homestay phù hợp
- Tư vấn về quy trình đặt phòng
- Giải đáp thắc mắc về thanh toán (VNPay, MoMo, PayPal)
- Hướng dẫn sử dụng tính năng ứng dụng
- Đưa ra gợi ý về địa điểm du lịch

Hãy trả lời bằng tiếng Việt, thân thiện, nhiệt tình và chuyên nghiệp.

User: $message
''';

    return await chat(contextPrompt);
  }

  /// Ask Gemini for homestay recommendations based on criteria
  Future<Map<String, dynamic>> getHomestayRecommendations({
    required String location,
    required int guests,
    required double budget,
    String? preferences,
  }) async {
    final prompt = '''
Tôi cần gợi ý homestay tại Việt Nam với tiêu chí:
- Địa điểm: $location
- Số khách: $guests người
- Ngân sách: ${budget.toStringAsFixed(0)}đ/đêm
${preferences != null ? '- Yêu cầu thêm: $preferences' : ''}

Hãy đưa ra:
1. 3-5 gợi ý về loại homestay phù hợp
2. Các tiện nghi nên có
3. Khu vực/quận huyết cụ thể nên tìm
4. Lời khuyên về thời gian đặt phòng

Trả lời ngắn gọn, dễ hiểu.
''';

    return await chat(prompt);
  }

  /// Ask Gemini to explain payment methods
  Future<Map<String, dynamic>> explainPaymentMethod(String method) async {
    final prompt = '''
Hãy giải thích cách thanh toán bằng $method khi đặt homestay:
- Các bước thực hiện
- Ưu điểm
- Lưu ý bảo mật
- Thời gian xử lý

Trả lời ngắn gọn bằng tiếng Việt.
''';

    return await chat(prompt);
  }

  /// Get travel tips for a location
  Future<Map<String, dynamic>> getTravelTips(String location) async {
    final prompt = '''
Tôi sắp đi du lịch $location. Hãy cho tôi:
1. Top 3 địa điểm nên đi
2. Món ăn đặc sản phải thử
3. Mẹo tiết kiệm chi phí
4. Thời tiết và thời điểm nên đi

Trả lời ngắn gọn, bullet points.
''';

    return await chat(prompt);
  }

  /// Translate text to English (for international users)
  Future<Map<String, dynamic>> translate(String text, {String from = 'vi', String to = 'en'}) async {
    final prompt = 'Translate from $from to $to: "$text"';
    return await chat(prompt);
  }

  /// Check if Gemini API is configured and working
  Future<bool> checkConnection() async {
    if (_apiKey.isEmpty) return false;
    
    final result = await chat('Hi');
    return result['success'] == true;
  }
}
