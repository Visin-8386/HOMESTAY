import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/api_config.dart';

class FptService {
  static final FptService _instance = FptService._internal();
  factory FptService() => _instance;
  FptService._internal();

  /// Call FPT ID recognition with a local file path.
  /// The apiKey can be provided via ApiConfig or passed explicitly.
  Future<Map<String, dynamic>> recognizeIdFromFile(String filePath, {String? apiKey}) async {
    final uri = Uri.parse('${ApiConfig.fptBaseUrl}/vision/idr/vnm');
    final key = apiKey ?? ApiConfig.fptApiKey;

    if (key.isEmpty) {
      throw Exception('FPT API key not configured');
    }

    var request = http.MultipartRequest('POST', uri);
    request.headers['api-key'] = key;

    request.files.add(await http.MultipartFile.fromPath('image', filePath));

    final streamed = await request.send();
    final resp = await http.Response.fromStream(streamed);

    if (resp.statusCode >= 200 && resp.statusCode < 300) {
      if (resp.body.isEmpty) return {'success': true};
      return jsonDecode(resp.body) as Map<String, dynamic>;
    }

    throw Exception('FPT API call failed: ${resp.statusCode} ${resp.body}');
  }
}

// Usage example (comment):
// final result = await FptService().recognizeIdFromFile('/path/to/id.jpg');
// print(result);
