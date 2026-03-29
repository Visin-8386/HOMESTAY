class Weather {
  final double temperatureC;
  final String weatherCode; // code from Open-Meteo
  final double windSpeed;
  final String time;

  Weather({
    required this.temperatureC,
    required this.weatherCode,
    required this.windSpeed,
    required this.time,
  });

  factory Weather.fromOpenMeteo(Map<String, dynamic> json) {
    // expecting hourly or current_weather structure
    if (json.containsKey('current_weather')) {
      final cw = json['current_weather'];
      return Weather(
        temperatureC: (cw['temperature'] ?? 0).toDouble(),
        weatherCode: (cw['weathercode'] ?? '').toString(),
        windSpeed: (cw['windspeed'] ?? 0).toDouble(),
        time: (cw['time'] ?? '').toString(),
      );
    }

    // fallback: try hourly
    if (json.containsKey('hourly')) {
      final hourly = json['hourly'];
      final temps = hourly['temperature_2m'] as List<dynamic>;
      final times = hourly['time'] as List<dynamic>;
      final codes = hourly['weathercode'] as List<dynamic>?;
      if (temps.isNotEmpty) {
        return Weather(
          temperatureC: (temps[0] ?? 0).toDouble(),
          weatherCode: codes != null && codes.isNotEmpty ? codes[0].toString() : '',
          windSpeed: 0.0,
          time: times.isNotEmpty ? times[0].toString() : '',
        );
      }
    }

    throw Exception('Unexpected Open-Meteo response');
  }
}
