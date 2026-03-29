import 'dart:convert';
import 'dart:io';

import '../config/api_config.dart';
import '../models/user.dart';
import 'api_service.dart';

class UserService {
  final ApiService _apiService = ApiService();

  Future<User> getProfile() async {
    final response = await _apiService.get(ApiConfig.profileUrl);
    return User.fromJson(response);
  }

  Future<User> updateProfile({
    String? fullName,
    String? phoneNumber,
    DateTime? dateOfBirth,
    String? address,
    String? bio,
  }) async {
    final data = <String, dynamic>{};
    if (fullName != null) data['fullName'] = fullName;
    if (phoneNumber != null) data['phoneNumber'] = phoneNumber;
    if (dateOfBirth != null) data['dateOfBirth'] = dateOfBirth.toIso8601String();
    if (address != null) data['address'] = address;
    if (bio != null) data['bio'] = bio;

    final response = await _apiService.put(ApiConfig.profileUrl, data);
    return User.fromJson(response);
  }

  Future<String> updateAvatar(File imageFile) async {
    final bytes = await imageFile.readAsBytes();
    final base64Image = base64Encode(bytes);

    final response = await _apiService.post(
      ApiConfig.updateAvatarUrl,
      {'imageBase64': base64Image},
    );

    return response['avatarUrl'] ?? '';
  }

  Future<Map<String, dynamic>> changePassword({
    required String oldPassword,
    required String newPassword,
  }) async {
    try {
      final response = await _apiService.put(
        '${ApiConfig.baseUrl}/api/User/ChangePassword',
        {
          'oldPassword': oldPassword,
          'newPassword': newPassword,
        },
      );
      return {
        'success': true,
        'message': 'Đổi mật khẩu thành công',
        'data': response,
      };
    } catch (e) {
      return {
        'success': false,
        'message': e.toString(),
      };
    }
  }
}
