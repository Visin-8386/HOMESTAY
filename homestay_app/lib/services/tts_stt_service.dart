import 'package:flutter/foundation.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:speech_to_text/speech_to_text.dart' as stt;

class TtsSttService {
  static final TtsSttService _instance = TtsSttService._internal();
  factory TtsSttService() => _instance;

  late final FlutterTts _tts;
  final stt.SpeechToText _stt = stt.SpeechToText();

  bool _isTtsInitialized = false;
  bool _isSttAvailable = false;

  /// When true, automatically speak translated text after translation completes.
  bool autoSpeakTranslated = true;

  TtsSttService._internal() {
    _tts = FlutterTts();
    _initTts();
  }

  Future<void> _initTts() async {
    try {
      await _tts.setSharedInstance(true);
      // Default to Vietnamese voice if available; fall back to system default.
      try {
        await _tts.setLanguage('vi-VN');
      } catch (_) {
        // ignore if language not supported
      }
      await _tts.setSpeechRate(0.45);
      await _tts.setPitch(1.0);
      _isTtsInitialized = true;
    } catch (e) {
      if (kDebugMode) print('TTS init error: $e');
      _isTtsInitialized = false;
    }
  }

  Future<void> speak(String text, {String? lang}) async {
    if (!_isTtsInitialized) await _initTts();
    if (lang != null) {
      try {
        await _tts.setLanguage(lang);
      } catch (_) {}
    }
    try {
      await _tts.speak(text);
    } catch (e) {
      if (kDebugMode) print('TTS speak error: $e');
    }
  }

  Future<void> stop() async {
    if (_isTtsInitialized) await _tts.stop();
  }

  /// Initialize speech recognition; returns true if available/permission granted
  Future<bool> initSpeech() async {
    try {
      _isSttAvailable = await _stt.initialize();
      return _isSttAvailable;
    } catch (e) {
      if (kDebugMode) print('STT init error: $e');
      _isSttAvailable = false;
      return false;
    }
  }

  /// Start listening and call onResult for partial/final transcripts
  Future<void> startListening({required void Function(String text, bool isFinal) onResult, String localeId = 'vi_VN'}) async {
    if (!_isSttAvailable) {
      final ok = await initSpeech();
      if (!ok) return;
    }

    await _stt.listen(onResult: (result) {
      onResult(result.recognizedWords, result.finalResult);
    }, localeId: localeId, listenMode: stt.ListenMode.dictation);
  }

  Future<void> stopListening() async {
    if (_isSttAvailable && _stt.isListening) {
      await _stt.stop();
    }
  }

  bool get isListening => _stt.isListening;
}
