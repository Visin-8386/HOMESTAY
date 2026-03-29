import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:signalr_netcore/signalr_client.dart';

import '../config/api_config.dart';
import '../models/user.dart';
import 'storage_service.dart';

enum CallType { audio, video }

class CallService {
  // Singleton pattern
  static final CallService _instance = CallService._internal();
  factory CallService() => _instance;
  CallService._internal();

  // Call state management
  bool _isInCall = false;
  String? _currentCallId;
  CallType? _currentCallType;
  User? _remoteUser;

  bool get isInCall => _isInCall;
  String? get currentCallId => _currentCallId;
  CallType? get currentCallType => _currentCallType;
  User? get remoteUser => _remoteUser;

  // Call event callbacks
  Function(String callId, CallType type, User caller)? onIncomingCall;
  Function(String callId)? onCallAccepted;
  Function(String callId)? onCallRejected;
  Function(String callId)? onCallEnded;
  // Signaling callbacks
  Function(String callId, Map<String, dynamic> offer)? onOfferReceived;
  Function(String callId, Map<String, dynamic> answer)? onAnswerReceived;
  Function(String callId, Map<String, dynamic> candidate)? onIceReceived;

  // SignalR hub connection for signaling (incoming call notifications and relay)
  HubConnection? _hubConnection;

  bool get isHubConnected => _hubConnection != null && _hubConnection!.state.toString().toLowerCase().contains('connected');

  /// Connect to SignalR hub and join user group for notifications.
  Future<void> connectHub() async {
    if (isHubConnected) return;
    final hubUrl = '${ApiConfig.baseUrl.replaceAll(RegExp(r'\/\$'), '')}/hubs/call';

    // Use a lazy token factory so SignalR negotiation always gets the freshest token
    _hubConnection = HubConnectionBuilder()
        .withUrl(hubUrl, options: HttpConnectionOptions(accessTokenFactory: () async => await _getToken() ?? ''))
        .build();

    _hubConnection?.onclose(({error}) {
      // reset and attempt a simple reconnect after a short delay
      // debug
      // ignore: avoid_print
      print('[CallService] Hub connection closed: $error');
      _hubConnection = null;
      // try a single reconnect after 3 seconds using a fresh token
      Future.delayed(const Duration(seconds: 3), () async {
        try {
          await connectHub();
        } catch (e) {
          // ignore reconnect errors for now
          // ignore: avoid_print
          print('[CallService] reconnect failed: $e');
        }
      });
    });

    _hubConnection?.on('IncomingCall', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          final callerId = payload['callerUserId']?.toString() ?? '';
          final callTypeStr = payload['callType']?.toString() ?? 'audio';
          final callType = callTypeStr == 'video' ? CallType.video : CallType.audio;

          // We only have callerId from hub; higher-level code can fetch user details if needed.
          final caller = User(id: callerId, userName: '', email: '', fullName: '', createdAt: DateTime.now());
          // debug
          // ignore: avoid_print
          print('[CallService] IncomingCall from: $callerId callId: $callId type: $callTypeStr');
          handleIncomingCall(callId, callType, caller);
        }
      } catch (e) {
        // ignore
      }
    });

    // SDP / ICE handlers
    _hubConnection?.on('ReceiveOffer', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          final offer = Map<String, dynamic>.from(payload['offer'] ?? {});
          // debug
          // ignore: avoid_print
          print('[CallService] ReceiveOffer callId:$callId offer_keys:${offer.keys.toList()}');
          if (onOfferReceived != null) onOfferReceived!(callId, offer);
        }
      } catch (e) {}
    });

    _hubConnection?.on('ReceiveAnswer', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          final answer = Map<String, dynamic>.from(payload['answer'] ?? {});
          // debug
          // ignore: avoid_print
          print('[CallService] ReceiveAnswer callId:$callId answer_keys:${answer.keys.toList()}');
          if (onAnswerReceived != null) onAnswerReceived!(callId, answer);
        }
      } catch (e) {}
    });

    _hubConnection?.on('ReceiveIce', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          final candidate = Map<String, dynamic>.from(payload['candidate'] ?? {});
          // debug
          // ignore: avoid_print
          print('[CallService] ReceiveIce callId:$callId candidate_keys:${candidate.keys.toList()}');
          if (onIceReceived != null) onIceReceived!(callId, candidate);
        }
      } catch (e) {}
    });

    // Call lifecycle notifications from server
    _hubConnection?.on('CallAccepted', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          // debug
          // ignore: avoid_print
          print('[CallService] CallAccepted for callId:$callId');
          if (onCallAccepted != null) onCallAccepted!(callId);
        }
      } catch (e) {}
    });

    _hubConnection?.on('CallRejected', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          // debug
          // ignore: avoid_print
          print('[CallService] CallRejected for callId:$callId');
          if (onCallRejected != null) onCallRejected!(callId);
        }
      } catch (e) {}
    });

    _hubConnection?.on('CallEnded', (arguments) {
      try {
        final payload = (arguments != null && arguments.isNotEmpty) ? arguments[0] : null;
        if (payload != null && payload is Map) {
          final callId = payload['callId']?.toString() ?? '';
          // debug
          // ignore: avoid_print
          print('[CallService] CallEnded for callId:$callId');
          if (onCallEnded != null) onCallEnded!(callId);
        }
      } catch (e) {}
    });

    try {
      await _hubConnection?.start();
      final userId = await StorageService().getUserId();
      if (userId != null) {
        await _hubConnection?.invoke('JoinUserGroup', args: [userId]);
      }
    } catch (e) {
      // log and clear connection so callers know it's not connected
      // ignore: avoid_print
      print('[CallService] connectHub error: $e');
      _hubConnection = null;
    }
  }

  // Signaling senders (invoke hub methods)
  Future<void> sendOffer(String recipientId, String callId, Map<String, dynamic> offer) async {
    try {
      if (!isHubConnected) await connectHub();
      // debug
      // ignore: avoid_print
      print('[CallService] sendOffer to:$recipientId callId:$callId');
      await _hubConnection?.invoke('SendOffer', args: [recipientId, callId, offer]);
    } catch (e) {
      // ignore send errors for now
    }
  }

  Future<void> sendAnswer(String recipientId, String callId, Map<String, dynamic> answer) async {
    try {
      if (!isHubConnected) await connectHub();
      // debug
      // ignore: avoid_print
      print('[CallService] sendAnswer to:$recipientId callId:$callId');
      await _hubConnection?.invoke('SendAnswer', args: [recipientId, callId, answer]);
    } catch (e) {}
  }

  Future<void> sendIce(String recipientId, String callId, Map<String, dynamic> candidate) async {
    try {
      if (!isHubConnected) await connectHub();
      // debug
      // ignore: avoid_print
      print('[CallService] sendIce to:$recipientId callId:$callId');
      await _hubConnection?.invoke('SendIce', args: [recipientId, callId, candidate]);
    } catch (e) {}
  }

  Future<void> disconnectHub() async {
    try {
      final userId = await StorageService().getUserId();
      if (userId != null) {
        await _hubConnection?.invoke('LeaveUserGroup', args: [userId]);
      }
      await _hubConnection?.stop();
      _hubConnection = null;
    } catch (e) {
      // ignore
    }
  }

  Future<Map<String, dynamic>> initiateCall(String recipientId, CallType callType) async {
    try {
      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/calls/initiate'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ${await _getToken()}',
        },
        body: jsonEncode({
          'recipientId': recipientId,
          'callType': callType == CallType.audio ? 'audio' : 'video',
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        _isInCall = true;
        _currentCallId = data['callId'];
        _currentCallType = callType;
        return {'success': true, 'callId': data['callId']};
      } else {
        return {'success': false, 'message': 'Không thể khởi tạo cuộc gọi'};
      }
    } catch (e) {
      return {'success': false, 'message': 'Lỗi kết nối: $e'};
    }
  }

  Future<Map<String, dynamic>> acceptCall(String callId) async {
    try {
      final body = jsonEncode({'callerId': _remoteUser?.id});
      // debug
      // ignore: avoid_print
      print('[CallService] acceptCall callId:$callId callerId:${_remoteUser?.id}');
      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/calls/$callId/accept'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ${await _getToken()}',
        },
        body: body,
      );

      if (response.statusCode == 200) {
        _isInCall = true;
        _currentCallId = callId;
        if (onCallAccepted != null) {
          onCallAccepted!(callId);
        }
        return {'success': true};
      } else {
        return {'success': false, 'message': 'Không thể chấp nhận cuộc gọi'};
      }
    } catch (e) {
      return {'success': false, 'message': 'Lỗi kết nối: $e'};
    }
  }

  Future<Map<String, dynamic>> rejectCall(String callId) async {
    try {
      final body = jsonEncode({'callerId': _remoteUser?.id});
      // debug
      // ignore: avoid_print
      print('[CallService] rejectCall callId:$callId callerId:${_remoteUser?.id}');
      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/calls/$callId/reject'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ${await _getToken()}',
        },
        body: body,
      );

      if (response.statusCode == 200) {
        _resetCallState();
        if (onCallRejected != null) {
          onCallRejected!(callId);
        }
        return {'success': true};
      } else {
        return {'success': false, 'message': 'Không thể từ chối cuộc gọi'};
      }
    } catch (e) {
      return {'success': false, 'message': 'Lỗi kết nối: $e'};
    }
  }

  Future<Map<String, dynamic>> endCall(String callId) async {
    try {
      final body = jsonEncode({'callerId': _remoteUser?.id});
      // debug
      // ignore: avoid_print
      print('[CallService] endCall callId:$callId callerId:${_remoteUser?.id}');
      final response = await http.post(
        Uri.parse('${ApiConfig.baseUrl}/api/calls/$callId/end'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ${await _getToken()}',
        },
        body: body,
      );

      _resetCallState();
      if (onCallEnded != null) {
        onCallEnded!(callId);
      }

      if (response.statusCode == 200) {
        return {'success': true};
      } else {
        return {'success': false, 'message': 'Không thể kết thúc cuộc gọi'};
      }
    } catch (e) {
      _resetCallState();
      return {'success': false, 'message': 'Lỗi kết nối: $e'};
    }
  }

  void _resetCallState() {
    _isInCall = false;
    _currentCallId = null;
    _currentCallType = null;
    _remoteUser = null;
  }

  Future<String?> _getToken() async {
    // Get token from secure storage using StorageService
    try {
      final token = await StorageService().getToken();
      return token;
    } catch (e) {
      // If storage read fails, return null so callers handle unauthenticated state
      return null;
    }
  }

  // Handle incoming call notification
  void handleIncomingCall(String callId, CallType callType, User caller) {
    _remoteUser = caller;
    _currentCallType = callType;
    _currentCallId = callId;

    if (onIncomingCall != null) {
      onIncomingCall!(callId, callType, caller);
    }
  }
}