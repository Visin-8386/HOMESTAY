import 'package:flutter/foundation.dart';
import 'package:google_mlkit_translation/google_mlkit_translation.dart';

import 'gemini_service.dart';
import 'tts_stt_service.dart';

class TranslationService {
  final GeminiService _gemini = GeminiService();
  final onDeviceModels = OnDeviceTranslatorModelManager();

  /// Try ML Kit on-device translation first. If model not available or an error occurs,
  /// fallback to Gemini (if configured) and finally to a deterministic mock.
  Future<String> translate(String text, {String from = 'vi', String to = 'en'}) async {
    final sourceLang = _mapLangCodeToMlKit(from);
    final targetLang = _mapLangCodeToMlKit(to);

    try {
      // Check and download model if needed
      // Ensure models are downloaded. If not available or download fails, we'll catch and fallback.
      final sourceCode = _mlKitLangToCode(sourceLang);
      final targetCode = _mlKitLangToCode(targetLang);

      if (!await onDeviceModels.isModelDownloaded(sourceCode)) {
        await onDeviceModels.downloadModel(sourceCode);
      }
      if (!await onDeviceModels.isModelDownloaded(targetCode)) {
        await onDeviceModels.downloadModel(targetCode);
      }

      final translator = OnDeviceTranslator(
        sourceLanguage: sourceLang,
        targetLanguage: targetLang,
      );

      final translated = await translator.translateText(text);
      await translator.close();
      if (translated.isNotEmpty) {
        // Auto-speak the translated text when target is Vietnamese and the
        // TTS service has autoSpeak enabled.
        try {
          if (to == 'vi' && TtsSttService().autoSpeakTranslated) {
            await TtsSttService().speak(translated, lang: 'vi-VN');
          }
        } catch (_) {}
        return translated;
      }
    } catch (e) {
      if (kDebugMode) print('ML Kit translate error or model unavailable: $e');
      // fallthrough to Gemini / mock
    }

    // Next fallback: Gemini cloud-based translation if configured
    try {
      final useGemini = await _gemini.checkConnection();
      if (useGemini) {
        final res = await _gemini.translate(text, from: from, to: to);
        if (res['success'] == true && res['message'] != null) {
          return res['message'] as String;
        }
      }
    } catch (e) {
      if (kDebugMode) print('Gemini fallback error: $e');
    }

    // Final fallback: deterministic mock
    if (from == 'vi' && to.startsWith('en')) {
      return '[EN] $text';
    }
    if (from.startsWith('en') && to == 'vi') {
      final mock = '[VI] $text';
      // Auto speak mock translated text if configured
      try {
        if (TtsSttService().autoSpeakTranslated) {
          await TtsSttService().speak(mock, lang: 'vi-VN');
        }
      } catch (_) {}
      return mock;
    }
    final res = '[$to] $text';
    try {
      if (to == 'vi' && TtsSttService().autoSpeakTranslated) {
        await TtsSttService().speak(res, lang: 'vi-VN');
      }
    } catch (_) {}
    return res;
  }

  /// Preload on-device models for the given simple language codes (e.g. 'en','vi').
  /// This helps reduce latency when translating later by downloading models in advance.
  Future<void> preloadModels(List<String> codes) async {
    for (final code in codes) {
      try {
        final lang = _mapLangCodeToMlKit(code);
        final modelCode = _mlKitLangToCode(lang);
        if (!await onDeviceModels.isModelDownloaded(modelCode)) {
          await onDeviceModels.downloadModel(modelCode);
        }
      } catch (e) {
        if (kDebugMode) print('Preload model $code failed: $e');
      }
    }
  }

  /// Convert TranslateLanguage enum to language code string expected by model manager (e.g., 'en', 'vi')
  String _mlKitLangToCode(TranslateLanguage lang) {
    switch (lang) {
      case TranslateLanguage.english:
        return 'en';
      case TranslateLanguage.vietnamese:
        return 'vi';
      case TranslateLanguage.french:
        return 'fr';
      case TranslateLanguage.spanish:
        return 'es';
      case TranslateLanguage.german:
        return 'de';
      case TranslateLanguage.japanese:
        return 'ja';
      case TranslateLanguage.korean:
        return 'ko';
      case TranslateLanguage.chinese:
        return 'zh';
      default:
        return 'en';
    }
  }

  /// Map simple codes like 'vi' or 'en' to ML Kit's TranslateLanguage values
  TranslateLanguage _mapLangCodeToMlKit(String code) {
    final c = code.toLowerCase();
    switch (c) {
      case 'en':
      case 'eng':
        return TranslateLanguage.english;
      case 'vi':
      case 'vie':
        return TranslateLanguage.vietnamese;
      case 'fr':
        return TranslateLanguage.french;
      case 'es':
        return TranslateLanguage.spanish;
      case 'de':
        return TranslateLanguage.german;
      case 'ja':
        return TranslateLanguage.japanese;
      case 'ko':
        return TranslateLanguage.korean;
      case 'zh':
      case 'zh-cn':
      case 'zh_tw':
        return TranslateLanguage.chinese;
      default:
        // default to english when unknown
        return TranslateLanguage.english;
    }
  }
}
