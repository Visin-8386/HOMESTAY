import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/weather.dart';

class WeatherService {
  /// Use Open-Meteo (no API key) to get current weather by lat/lon
  /// Example: https://api.open-meteo.com/v1/forecast?latitude=10.762623&longitude=106.660172&current_weather=true
  Future<Weather?> getCurrentWeather(double latitude, double longitude) async {
    final uri = Uri.https('api.open-meteo.com', '/v1/forecast', {
      'latitude': latitude.toString(),
      'longitude': longitude.toString(),
      'current_weather': 'true',
      'timezone': 'UTC'
    });

    try {
      final res = await http.get(uri).timeout(const Duration(seconds: 10));
      if (res.statusCode == 200) {
        final data = json.decode(res.body) as Map<String, dynamic>;
        return Weather.fromOpenMeteo(data);
      }
    } catch (e) {
      // Ignore errors; caller can handle null
    }

    return null;
  }
}
