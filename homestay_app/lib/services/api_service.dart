import 'dart:async';
import 'dart:convert';
import 'dart:math' as math;

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

import '../config/api_config.dart';
import '../models/homestay.dart';
import '../models/review.dart';
class TokenExpiredException implements Exception {
  final String message;
  TokenExpiredException(this.message);
  
  @override
  String toString() => message;
}

class ApiService {
  static final ApiService _instance = ApiService._internal();
  factory ApiService() => _instance;
  ApiService._internal();

  final _storage = const FlutterSecureStorage();
  String? _accessToken;
  String? _refreshToken;

  Future<void> setTokens(String accessToken, String refreshToken) async {
    _accessToken = accessToken;
    _refreshToken = refreshToken;
    await _storage.write(key: 'access_token', value: accessToken);
    await _storage.write(key: 'refresh_token', value: refreshToken);
    // Backwards compatibility: some parts of the app expect 'jwt_token'
    await _storage.write(key: 'jwt_token', value: accessToken);
  }

  Future<void> loadTokens() async {
    _accessToken = await _storage.read(key: 'access_token');
    _refreshToken = await _storage.read(key: 'refresh_token');
  }

  Future<void> clearTokens() async {
    _accessToken = null;
    _refreshToken = null;
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
    // Remove legacy jwt_token key as well
    await _storage.delete(key: 'jwt_token');
  }

  bool get isAuthenticated => _accessToken != null;

  Map<String, String> _getHeaders({bool includeAuth = true}) {
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
    
    if (includeAuth && _accessToken != null) {
      headers['Authorization'] = 'Bearer $_accessToken';
    }
    
    return headers;
  }

  Future<dynamic> get(String url, {bool requireAuth = true}) async {
    try {
      await loadTokens();
      final headers = _getHeaders(includeAuth: requireAuth);
      // Debug logging
      print('ApiService GET --> $url');
      print('ApiService GET headers: $headers');
      final response = await http.get(
        Uri.parse(url),
        headers: headers,
      ).timeout(ApiConfig.connectionTimeout);

      // Debug: log response status and truncated body
      try {
        final len = response.body.length;
        final show = math.min(len, 2000);
        print('ApiService GET response: ${response.statusCode} ${len} chars');
        print('ApiService GET body (truncated ${show}): ${response.body.substring(0, show)}');
      } catch (_) {}

      return _handleResponse(response);
    } on TokenExpiredException {
      // Try to refresh token and retry
      if (await refreshAccessToken()) {
        final headers = _getHeaders(includeAuth: requireAuth);
        print('ApiService GET (retry) --> $url');
        print('ApiService GET (retry) headers: $headers');
        final response = await http.get(
          Uri.parse(url),
          headers: headers,
        ).timeout(ApiConfig.connectionTimeout);

        try {
          final len = response.body.length;
          final show = math.min(len, 2000);
          print('ApiService GET (retry) response: ${response.statusCode} ${len} chars');
          print('ApiService GET (retry) body (truncated ${show}): ${response.body.substring(0, show)}');
        } catch (_) {}

        return _handleResponse(response);
      }
      throw ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    } catch (e) {
      throw _handleError(e);
    }
  }

  Future<dynamic> post(
    String url,
    Map<String, dynamic> body, {
    bool requireAuth = true,
  }) async {
    try {
      await loadTokens();
      final headers = _getHeaders(includeAuth: requireAuth);
      print('ApiService POST --> $url');
      print('ApiService POST headers: $headers');
      print('ApiService POST body: ${jsonEncode(body)}');
      final response = await http.post(
        Uri.parse(url),
        headers: headers,
        body: jsonEncode(body),
      ).timeout(ApiConfig.connectionTimeout);
      try {
        print('ApiService POST response: ${response.statusCode} ${response.body.length} chars');
      } catch (_) {}

      return _handleResponse(response);
    } on TokenExpiredException {
      // Try to refresh token and retry
      if (await refreshAccessToken()) {
        final headers = _getHeaders(includeAuth: requireAuth);
        print('ApiService POST (retry) --> $url');
        final response = await http.post(
          Uri.parse(url),
          headers: headers,
          body: jsonEncode(body),
        ).timeout(ApiConfig.connectionTimeout);
        try {
          print('ApiService POST (retry) response: ${response.statusCode} ${response.body.length} chars');
        } catch (_) {}
        return _handleResponse(response);
      }
      throw ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    } catch (e) {
      throw _handleError(e);
    }
  }

  Future<dynamic> put(
    String url,
    Map<String, dynamic> body, {
    bool requireAuth = true,
  }) async {
    try {
      await loadTokens();
      final headers = _getHeaders(includeAuth: requireAuth);
      print('ApiService PUT --> $url');
      print('ApiService PUT headers: $headers');
      final response = await http.put(
        Uri.parse(url),
        headers: headers,
        body: jsonEncode(body),
      ).timeout(ApiConfig.connectionTimeout);
      try {
        print('ApiService PUT response: ${response.statusCode} ${response.body.length} chars');
      } catch (_) {}

      return _handleResponse(response);
    } on TokenExpiredException {
      // Try to refresh token and retry
      if (await refreshAccessToken()) {
        final headers = _getHeaders(includeAuth: requireAuth);
        print('ApiService PUT (retry) --> $url');
        final response = await http.put(
          Uri.parse(url),
          headers: headers,
          body: jsonEncode(body),
        ).timeout(ApiConfig.connectionTimeout);
        try {
          print('ApiService PUT (retry) response: ${response.statusCode} ${response.body.length} chars');
        } catch (_) {}
        return _handleResponse(response);
      }
      throw ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    } catch (e) {
      throw _handleError(e);
    }
  }

  Future<dynamic> delete(String url, {bool requireAuth = true}) async {
    try {
      await loadTokens();
      final headers = _getHeaders(includeAuth: requireAuth);
      print('ApiService DELETE --> $url');
      print('ApiService DELETE headers: $headers');
      final response = await http.delete(
        Uri.parse(url),
        headers: headers,
      ).timeout(ApiConfig.connectionTimeout);

      try {
        print('ApiService DELETE response: ${response.statusCode} ${response.body.length} chars');
      } catch (_) {}

      return _handleResponse(response);
    } on TokenExpiredException {
      // Try to refresh token and retry
      if (await refreshAccessToken()) {
        final headers = _getHeaders(includeAuth: requireAuth);
        print('ApiService DELETE (retry) --> $url');
        final response = await http.delete(
          Uri.parse(url),
          headers: headers,
        ).timeout(ApiConfig.connectionTimeout);
        try {
          print('ApiService DELETE (retry) response: ${response.statusCode} ${response.body.length} chars');
        } catch (_) {}
        return _handleResponse(response);
      }
      throw ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    } catch (e) {
      throw _handleError(e);
    }
  }

  /// NEW: Upload multiple files with multipart/form-data
  Future<dynamic> uploadFiles(
    String url,
    List<String> filePaths, {
    String fieldName = 'files',
    bool requireAuth = true,
  }) async {
    try {
      await loadTokens();
      
      var request = http.MultipartRequest('POST', Uri.parse(url));
      
      // Add authorization header
      if (requireAuth && _accessToken != null) {
        request.headers['Authorization'] = 'Bearer $_accessToken';
      }
      
      // Add files
      for (var filePath in filePaths) {
        request.files.add(await http.MultipartFile.fromPath(fieldName, filePath));
      }
      
      final streamedResponse = await request.send().timeout(ApiConfig.connectionTimeout);
      final response = await http.Response.fromStream(streamedResponse);
      
      return _handleResponse(response);
    } on TokenExpiredException {
      // Try to refresh token and retry
      if (await refreshAccessToken()) {
        var request = http.MultipartRequest('POST', Uri.parse(url));
        
        if (requireAuth && _accessToken != null) {
          request.headers['Authorization'] = 'Bearer $_accessToken';
        }
        
        for (var filePath in filePaths) {
          request.files.add(await http.MultipartFile.fromPath(fieldName, filePath));
        }
        
        final streamedResponse = await request.send().timeout(ApiConfig.connectionTimeout);
        final response = await http.Response.fromStream(streamedResponse);
        return _handleResponse(response);
      }
      throw ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    } catch (e) {
      throw _handleError(e);
    }
  }

  /// Upload multipart form with fields and files. Supports POST and PUT
  Future<dynamic> uploadMultipart(
    String url,
    Map<String, dynamic> fields, {
    List<String>? filePaths,
    String fileFieldName = 'Images',
    String method = 'POST',
    bool requireAuth = true,
  }) async {
    try {
      await loadTokens();

      var request = http.MultipartRequest(method, Uri.parse(url));

      if (requireAuth && _accessToken != null) {
        request.headers['Authorization'] = 'Bearer $_accessToken';
      }

      // Add scalar fields
      fields.forEach((key, value) {
        if (value == null) return;

        if (value is List) {
          for (var v in value) {
            request.fields[key] = request.fields[key] == null ? v.toString() : '${request.fields[key]},${v.toString()}';
          }
        } else {
          request.fields[key] = value.toString();
        }
      });

      // Add files
      if (filePaths != null) {
        for (var filePath in filePaths) {
          request.files.add(await http.MultipartFile.fromPath(fileFieldName, filePath));
        }
      }

      final streamedResponse = await request.send().timeout(ApiConfig.connectionTimeout);
      final response = await http.Response.fromStream(streamedResponse);
      return _handleResponse(response);
    } on TokenExpiredException {
      if (await refreshAccessToken()) {
        var request = http.MultipartRequest(method, Uri.parse(url));
        if (requireAuth && _accessToken != null) {
          request.headers['Authorization'] = 'Bearer $_accessToken';
        }
        fields.forEach((key, value) {
          if (value == null) return;
          if (value is List) {
            for (var v in value) {
              request.fields[key] = request.fields[key] == null ? v.toString() : '${request.fields[key]},${v.toString()}';
            }
          } else {
            request.fields[key] = value.toString();
          }
        });
        if (filePaths != null) {
          for (var filePath in filePaths) {
            request.files.add(await http.MultipartFile.fromPath(fileFieldName, filePath));
          }
        }
        final streamedResponse = await request.send().timeout(ApiConfig.connectionTimeout);
        final response = await http.Response.fromStream(streamedResponse);
        return _handleResponse(response);
      }
      throw ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    } catch (e) {
      throw _handleError(e);
    }
  }

  dynamic _handleResponse(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) {
        return {'success': true};
      }
      return jsonDecode(response.body);
    } else if (response.statusCode == 401) {
      // Token expired
      throw TokenExpiredException('Token hết hạn');
    } else {
      final error = response.body.isNotEmpty ? jsonDecode(response.body) : {'message': 'Đã xảy ra lỗi'};

      // If server returns structured error with a list of validation messages (e.g., data or errors),
      // prefer to surface them joined so client can show the reasons.
      String message;
      try {
        if (error is Map<String, dynamic>) {
          // Common shapes: { message: 'Invalid data', data: [ 'err1', 'err2' ] }
          if (error['data'] is List) {
            final list = (error['data'] as List).map((e) => e.toString()).toList();
            message = list.join('; ');
          } else if (error['errors'] is List) {
            final list = (error['errors'] as List).map((e) => e.toString()).toList();
            message = list.join('; ');
          } else if (error['message'] != null && error['message'].toString().isNotEmpty) {
            message = error['message'].toString();
          } else if (error['title'] != null) {
            message = error['title'].toString();
          } else {
            message = error.toString();
          }
        } else {
          message = error.toString();
        }
      } catch (e) {
        message = 'Đã xảy ra lỗi';
      }

      throw ApiException(message, response.statusCode);
    }
  }

  ApiException _handleError(dynamic error) {
    // Normalize different error types into ApiException so callers can
    // inspect the message and statusCode instead of receiving raw strings.
    if (error is ApiException) {
      return error;
    }
    if (error is TokenExpiredException) {
      return ApiException('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.', 401);
    }
    if (error is TimeoutException) {
      return ApiException('Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng.', 0);
    }
    // Fallback: wrap any other error
    return ApiException(error?.toString() ?? 'Đã xảy ra lỗi không xác định', 0);
  }

  Future<bool> refreshAccessToken() async {
    await loadTokens(); // Ensure tokens are loaded
    if (_refreshToken == null || _accessToken == null) return false;

    try {
      // Make direct HTTP call to avoid retry loop
      final response = await http.post(
        Uri.parse(ApiConfig.refreshTokenUrl),
        headers: {
          'Content-Type': 'application/json',
        },
        body: jsonEncode({
          'token': _accessToken,
          'refreshToken': _refreshToken,
        }),
      ).timeout(ApiConfig.connectionTimeout);

      if (response.statusCode >= 200 && response.statusCode < 300) {
        final result = jsonDecode(response.body);
        // Backend returns: { "data": { "token": "...", "refreshToken": "..." }, "success": true }
        if (result['success'] == true && result['data'] != null) {
          final data = result['data'];
          final newAccessToken = data['token'];
          final newRefreshToken = data['refreshToken'];
          
          if (newAccessToken != null && newRefreshToken != null) {
            await setTokens(newAccessToken, newRefreshToken);
            return true;
          }
        }
      }
      return false;
    } catch (e) {
      await clearTokens();
      return false;
    }
  }

  // Reviews
  Future<List<Review>> getAllReviews({int page = 1, int pageSize = 20}) async {
    try {
      final response = await get('${ApiConfig.baseUrl}/reviews?page=$page&pageSize=$pageSize');
      final data = response['data'] as List? ?? [];
      return data.map((json) => Review.fromJson(json)).toList();
    } catch (e) {
      throw 'Không thể tải danh sách đánh giá: $e';
    }
  }

  // Homestays
  Future<List<Homestay>> getHomestays() async {
    try {
      final response = await get('${ApiConfig.baseUrl}/homestays');
      final data = response['data'] as List? ?? [];
      return data.map((json) => Homestay.fromJson(json)).toList();
    } catch (e) {
      throw 'Không thể tải danh sách homestay: $e';
    }
  }
}

class ApiException implements Exception {
  final String message;
  final int statusCode;

  ApiException(this.message, this.statusCode);

  @override
  String toString() => message;
}
