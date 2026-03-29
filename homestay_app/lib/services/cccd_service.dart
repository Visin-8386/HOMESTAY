import 'dart:convert';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

/// Service to call FPT.AI ID Recognition (IDR) API for Vietnam ID cards (CCCD/CMND).
///
/// Usage:
///  - Put your API key in `.env` as `FPT_API_KEY=your_key_here` or pass it to `recognizeId`.
///  - Call `FptIdrService().recognizeId(imageFile: myFile)` to get a `FptIdrResult`.
class FptIdrService {
  final Dio _dio;

  FptIdrService({Dio? dio}) : _dio = dio ?? Dio();

  /// Recognize an ID card image.
  ///
  /// [imageFile] must be a local file (picked with image_picker or similar).
  /// [apiKey] if omitted, will be read from environment variable `FPT_API_KEY` using flutter_dotenv.
  Future<FptIdrResult> recognizeId({required File imageFile, String? apiKey, CancelToken? cancelToken}) async {
    final key = apiKey ?? dotenv.env['FPT_API_KEY'];
    if (key == null || key.isEmpty) {
      throw ArgumentError('FPT API key is missing. Set FPT_API_KEY in .env or pass apiKey param.');
    }

    final url = 'https://api.fpt.ai/vision/idr/vnm';

    final fileName = imageFile.path.split(Platform.pathSeparator).last;

    final form = FormData.fromMap({
      'image': await MultipartFile.fromFile(imageFile.path, filename: fileName),
    });

    try {
      final response = await _dio.post(
        url,
        data: form,
        options: Options(headers: {
          'api-key': key,
          'Accept': 'application/json',
        }),
        cancelToken: cancelToken,
      );

      // FPT returns JSON. Keep raw body and attempt to extract common fields.
      final data = response.data;

      Map<String, dynamic> jsonBody;
      if (data is String) {
        jsonBody = json.decode(data) as Map<String, dynamic>;
      } else if (data is Map<String, dynamic>) {
        jsonBody = data;
      } else {
        // sometimes Dio returns a parsed map inside a Map<Object, Object>
        jsonBody = Map<String, dynamic>.from(data as Map);
      }

      final result = FptIdrResult.fromRaw(jsonBody);
      return result;
    } on DioError catch (e) {
      if (e.response != null) {
        throw Exception('FPT IDR request failed: ${e.response?.statusCode} ${e.response?.statusMessage} ${e.response?.data}');
      }
      throw Exception('FPT IDR request error: ${e.message}');
    }
  }
}

/// A small wrapper representing a best-effort parsed result from FPT.AI IDR.
class FptIdrResult {
  /// Raw JSON returned by the FPT API.
  final Map<String, dynamic> raw;

  final String? fullName;
  final String? idNumber;
  final String? dateOfBirth;
  final String? gender;
  final String? address;

  FptIdrResult({required this.raw, this.fullName, this.idNumber, this.dateOfBirth, this.gender, this.address});

  /// Create a result from raw JSON with some heuristics to locate common fields.
  factory FptIdrResult.fromRaw(Map<String, dynamic> raw) {
    // Heuristic search helpers
    String? searchForKeys(Map m, List<String> variants) {
      for (final k in m.keys) {
        final kl = k.toString().toLowerCase();
        for (final v in variants) {
          if (kl.contains(v)) {
            final val = m[k];
            if (val == null) continue;
            if (val is String) return val;
            return val.toString();
          }
        }
      }

      for (final v in m.values) {
        if (v is Map) {
          final r = searchForKeys(v, variants);
          if (r != null) return r;
        } else if (v is List) {
          for (final e in v) {
            if (e is Map) {
              final r = searchForKeys(e, variants);
              if (r != null) return r;
            }
          }
        }
      }
      return null;
    }

    String? normalize(String? s) {
      if (s == null) return null;
      return s.replaceAll(RegExp(r"\s+"), ' ').trim();
    }

    final fullName = normalize(searchForKeys(raw, ['name', 'full_name', 'hoten', 'ho_ten', 'ten']));
    final idNumber = normalize(searchForKeys(raw, ['number', 'id', 'so', 'cmnd', 'cccd', 'id_number']));
    final dob = normalize(searchForKeys(raw, ['dob', 'date_of_birth', 'ngay_sinh', 'birth']));
    String? mapGender(String? s) {
      final n = normalize(s);
      if (n == null) return null;
      final low = n.toLowerCase();
      if (low.contains('nam') || low.contains('male') || low.contains('m')) return 'Nam';
      if (low.contains('nữ') || low.contains('nu') || low.contains('female') || low.contains('f')) return 'Nữ';
      return n;
    }

    // include 'sex' which some OCR providers return
    final rawGender = normalize(searchForKeys(raw, ['gender', 'gioi_tinh', 'sex']));
    final gender = mapGender(rawGender);
    final address = normalize(searchForKeys(raw, ['address', 'dia_chi', 'addr']));

    return FptIdrResult(raw: raw, fullName: fullName, idNumber: idNumber, dateOfBirth: dob, gender: gender, address: address);
  }

  /// Convenience: create from the `images`/`fields` structure which some OCR APIs use.
  /// This will convert a list of field maps like {"field":"Name","inferText":"Nguyen Van A"}
  /// into the raw map and run heuristics.
  static FptIdrResult fromFptStyle(Map<String, dynamic> body) {
    // Try to transform common structures to a flat map.
    final flat = <String, dynamic>{};

    void collect(dynamic node) {
      if (node is Map) {
        // Some responses have 'fields' array with objects that contain 'field'/'name' and 'inferText'/'value'
        if (node.containsKey('fields') && node['fields'] is List) {
          for (final f in node['fields']) {
            if (f is Map) {
              final key = (f['field'] ?? f['name'] ?? f['label'])?.toString() ?? '';
              final val = f['inferText'] ?? f['value'] ?? f['text'] ?? f['infer_text'] ?? f['raw'] ?? '';
              if (key != '') flat[key] = val;
            }
          }
        }
        node.forEach((k, v) {
          if (v is String || v is num || v is bool) {
            flat[k] = v;
          } else {
            collect(v);
          }
        });
      } else if (node is List) {
        for (final e in node) collect(e);
      }
    }

    collect(body);
    return FptIdrResult.fromRaw(flat);
  }

  Map<String, dynamic> toJson() => {
        'raw': raw,
        'fullName': fullName,
        'idNumber': idNumber,
        'dateOfBirth': dateOfBirth,
        'gender': gender,
        'address': address,
      };
}

// Example usage (not executed here):
//
// final file = await ImagePicker().pickImage(source: ImageSource.camera);
// final result = await FptIdrService().recognizeId(imageFile: File(file.path));
// print(result.fullName);
