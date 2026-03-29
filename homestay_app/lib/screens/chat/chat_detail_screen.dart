import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher_string.dart';

import '../../config/api_config.dart';
import '../../models/user.dart';
import '../../providers/auth_provider_fixed.dart';
import '../../services/api_service.dart';
import '../../services/call_service.dart';
import '../../services/translation_service.dart';
import '../../services/tts_stt_service.dart';
import '../../widgets/user_gradient_background.dart';
import '../call/call_screen.dart';

class ChatDetailScreen extends StatefulWidget {
  final int chatId;

  const ChatDetailScreen({Key? key, required this.chatId}) : super(key: key);

  @override
  _ChatDetailScreenState createState() => _ChatDetailScreenState();
}

class _ChatDetailScreenState extends State<ChatDetailScreen> {
  // use ApiService for token/refresh handling
  final TextEditingController _messageController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  final TranslationService _translationService = TranslationService();
  final TtsSttService _ttsSttService = TtsSttService();
  bool _isListening = false;

  // store translated text per message index
  final Map<int, String> _translatedTexts = {};
  final Map<int, bool> _isTranslating = {};

  bool _isLoading = true;
  List<dynamic> _messages = [];
  bool _isSending = false;
  String? _dialingCallId;
  String? _remoteName;

  @override
  void initState() {
    super.initState();
    _loadMessages();
    // try to resolve participant name early for appbar
    _resolveParticipantId();
    // preload common translation models to reduce latency on first translate
    _preloadTranslationModels();
  }

  @override
  void dispose() {
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _loadMessages() async {
    if (mounted) setState(() => _isLoading = true);

    try {
      final api = ApiService();
      final response = await api.get('${ApiConfig.baseUrl}/api/conversations/${widget.chatId}/messages');

      if (mounted) {
        setState(() => _messages = response['data'] ?? []);
        _scrollToBottom();
        if (kDebugMode) {
          try {
            final ids = _messages.map((m) => m['senderId']?.toString() ?? 'n/a').toList();
            print('[chat_detail] loaded ${_messages.length} messages senderIds=$ids');
          } catch (_) {}
        }
      }

      // Mark messages as read
      await api.post('${ApiConfig.baseUrl}/api/conversations/${widget.chatId}/read', {});
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi kết nối: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  /// Resolve the participant user id (GUID) for this conversation by calling
  /// the conversations list endpoint and finding the matching conversation.
  /// Returns null if not found or on error.
  Future<String?> _resolveParticipantId() async {
    try {
      final api = ApiService();
      final resp = await api.get('${ApiConfig.baseUrl}/api/conversations');
      final list = resp['data'] as List<dynamic>?;
      if (list == null) return null;
      for (final item in list) {
        try {
          if (item is Map && item['id'] == widget.chatId) {
            final pid = item['participantId']?.toString();
            // also try to fetch remote user's display name
            if (pid != null) {
              try {
                final userResp = await api.get('${ApiConfig.baseUrl}/api/users/$pid');
                final display = userResp['displayName'] ?? userResp['data']?['displayName'] ?? userResp['data']?['fullName'] ?? userResp['data']?['name'];
                if (display != null && mounted) {
                  setState(() => _remoteName = display.toString());
                }
              } catch (_) {}
            }
            return pid;
          }
        } catch (_) {}
      }
      return null;
    } catch (e) {
      print('[chat_detail] _resolveParticipantId error: $e');
      return null;
    }
  }

  Future<void> _sendMessage() async {
    final content = _messageController.text.trim();
    if (content.isEmpty) return;

  if (mounted) setState(() => _isSending = true);

    try {
      final api = ApiService();
      await api.post('${ApiConfig.baseUrl}/api/conversations/${widget.chatId}/messages', {
        'content': content,
      });

      // After sending, reload messages
      if (mounted) {
        _messageController.clear();
        await _loadMessages();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi kết nối: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isSending = false);
    }
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _showDialingDialog(CallService callService, String callId) async {
    if (!mounted) return;
    setState(() => _dialingCallId = callId);
    await showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => WillPopScope(
        onWillPop: () async => false,
        child: AlertDialog(
          title: const Text('Đang gọi...'),
          content: const SizedBox(
            height: 60,
            child: Center(child: CircularProgressIndicator()),
          ),
          actions: [
            TextButton(
              onPressed: () async {
                try {
                  await callService.endCall(callId);
                } catch (_) {}
                Navigator.of(context).pop();
              },
              child: const Text('Hủy'),
            ),
          ],
        ),
      ),
    );
    if (mounted) setState(() => _dialingCallId = null);
  }

  /// Try to fetch phone number for a given user id and show call options.
  Future<void> _onMessageCallPressed(Map<String, dynamic> message) async {
    final senderId = message['senderId']?.toString();
    if (senderId == null || senderId.isEmpty) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không tìm thấy thông tin người gửi')));
      return;
    }

    // Try to fetch user's phone from API
    try {
      final api = ApiService();
      final resp = await api.get('${ApiConfig.baseUrl}/api/users/$senderId');

      String? phone;
      if (resp is Map) {
        phone = resp['phoneNumber'] ?? resp['phone'] ?? resp['phone_number'] ?? resp['mobile'];
      }

      // If no phone found, inform user
      if (phone == null || phone.toString().trim().isEmpty) {
        if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Số điện thoại không khả dụng')));
        return;
      }

      phone = phone.toString().trim();

      // show chooser: native dialer or in-app call
      if (!mounted) return;
      final choice = await showDialog<String?>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Cách gọi'),
          content: Text('Gọi tới: $phone'),
          actions: [
            TextButton(onPressed: () => Navigator.of(ctx).pop('native'), child: const Text('Gọi qua điện thoại')),
            TextButton(onPressed: () => Navigator.of(ctx).pop('app'), child: const Text('Gọi bằng ứng dụng')),
            TextButton(onPressed: () => Navigator.of(ctx).pop(null), child: const Text('Hủy')),
          ],
        ),
      );

      if (choice == 'native') {
        // open native dialer
        final tel = 'tel:$phone';
        try {
          await launchUrlString(tel);
        } catch (e) {
          if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể mở ứng dụng gọi: $e')));
        }
        return;
      }

      if (choice == 'app') {
        // start in-app call using existing flow
        final participantId = senderId;
        await _startCallFlow(participantId, CallType.audio, remoteName: message['senderName']?.toString() ?? 'Remote');
        return;
      }
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi khi lấy số điện thoại: $e')));
    }
  }

  Future<void> _startCallFlow(String participantId, CallType callType, {String remoteName = 'Remote'}) async {
    final callService = CallService();
    ScaffoldMessenger.of(context).hideCurrentSnackBar();
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đang khởi tạo cuộc gọi...')));

    final result = await callService.initiateCall(participantId, callType);
    ScaffoldMessenger.of(context).hideCurrentSnackBar();

    if (result['success'] == true && result['callId'] != null) {
      final callId = result['callId'] as String;

      final remoteUser = User(
        id: participantId,
        userName: remoteName,
        email: '',
        avatarUrl: null,
        createdAt: DateTime.now(),
      );

      void cleanupHandlers() {
        callService.onCallAccepted = null;
        callService.onCallRejected = null;
      }

      callService.onCallAccepted = (acceptedCallId) {
        if (acceptedCallId == callId) {
          cleanupHandlers();
          try {
            if (_dialingCallId == callId) Navigator.of(context).pop();
          } catch (_) {}
          if (!mounted) return;
          Navigator.of(context).push(
            MaterialPageRoute(
              builder: (_) => CallScreen(
                callId: callId,
                callType: callType,
                remoteUser: remoteUser,
                isIncoming: false,
              ),
            ),
          );
        }
      };

      callService.onCallRejected = (rejectedCallId) {
        if (rejectedCallId == callId) {
          cleanupHandlers();
          try {
            if (_dialingCallId == callId) Navigator.of(context).pop();
          } catch (_) {}
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Cuộc gọi bị từ chối')));
          }
        }
      };

      _showDialingDialog(callService, callId);

      Future.delayed(const Duration(seconds: 30), () {
        if (callService.currentCallId == callId) return;
        cleanupHandlers();
        try {
          if (_dialingCallId == callId) Navigator.of(context).pop();
        } catch (_) {}
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không có phản hồi từ người nhận')));
        }
      });
    } else {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(result['message'] ?? 'Không thể khởi tạo cuộc gọi')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_remoteName ?? 'Chat'),
        backgroundColor: Colors.blue,
        actions: [
          IconButton(
            tooltip: 'Gọi điện',
            icon: const Icon(Icons.call),
            onPressed: () async {
              // Show chooser: native dialer or in-app call
              final participantId = await _resolveParticipantId();
              if (participantId == null) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không tìm thấy người nhận cuộc gọi')));
                return;
              }

              try {
                final api = ApiService();
                final resp = await api.get('${ApiConfig.baseUrl}/api/users/$participantId');
                String? phone;
                if (resp is Map) phone = resp['phoneNumber'] ?? resp['phone'] ?? resp['data']?['phoneNumber'];

                if (!mounted) return;
                final choice = await showDialog<String?>(
                  context: context,
                  builder: (ctx) => AlertDialog(
                    title: const Text('Cách gọi'),
                    content: Text(phone != null && phone.toString().isNotEmpty ? 'Gọi tới: $phone' : 'Gọi bằng ứng dụng hoặc số điện thoại không khả dụng'),
                    actions: [
                      if (phone != null && phone.toString().isNotEmpty)
                        TextButton(onPressed: () => Navigator.of(ctx).pop('native'), child: const Text('Gọi qua điện thoại')),
                      TextButton(onPressed: () => Navigator.of(ctx).pop('app'), child: const Text('Gọi bằng ứng dụng')),
                      TextButton(onPressed: () => Navigator.of(ctx).pop(null), child: const Text('Hủy')),
                    ],
                  ),
                );

                if (choice == 'native' && phone != null && phone.toString().isNotEmpty) {
                  await launchUrlString('tel:${phone.toString()}');
                  return;
                }

                if (choice == 'app') {
                  // reuse existing flow: initiate in-app audio call
                  final callService = CallService();
                  final result = await callService.initiateCall(participantId, CallType.audio);
                  if (result['success'] == true && result['callId'] != null) {
                    final callId = result['callId'] as String;
                    final remoteUser = User(id: participantId, userName: _remoteName ?? 'Remote', email: '', avatarUrl: null, createdAt: DateTime.now());

                    void cleanupHandlers() {
                      callService.onCallAccepted = null;
                      callService.onCallRejected = null;
                    }

                    callService.onCallAccepted = (acceptedCallId) {
                      if (acceptedCallId == callId) {
                        cleanupHandlers();
                        try { if (_dialingCallId == callId) Navigator.of(context).pop(); } catch (_) {}
                        if (!mounted) return;
                        Navigator.of(context).push(MaterialPageRoute(builder: (_) => CallScreen(callId: callId, callType: CallType.audio, remoteUser: remoteUser, isIncoming: false)));
                      }
                    };

                    callService.onCallRejected = (rejectedCallId) {
                      if (rejectedCallId == callId) {
                        cleanupHandlers();
                        try { if (_dialingCallId == callId) Navigator.of(context).pop(); } catch (_) {}
                        if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Cuộc gọi bị từ chối')));
                      }
                    };

                    _showDialingDialog(callService, callId);
                    Future.delayed(const Duration(seconds: 30), () {
                      cleanupHandlers();
                      try { if (_dialingCallId == callId) Navigator.of(context).pop(); } catch (_) {}
                      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không có phản hồi từ người nhận')));
                    });
                  } else {
                    if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(result['message'] ?? 'Không thể khởi tạo cuộc gọi')));
                  }
                }
              } catch (e) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi khi lấy số điện thoại: $e')));
              }
            },
          ),
          // One-tap native dialer: directly open device dialer with participant's phone
          IconButton(
            tooltip: 'Gọi bằng số điện thoại',
            icon: const Icon(Icons.phone_enabled),
            onPressed: () async {
              final participantId = await _resolveParticipantId();
              if (participantId == null) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không tìm thấy người nhận cuộc gọi')));
                return;
              }

              try {
                final api = ApiService();
                final resp = await api.get('${ApiConfig.baseUrl}/api/users/$participantId');
                String? phone;
                if (resp is Map) phone = resp['phoneNumber'] ?? resp['phone'] ?? resp['data']?['phoneNumber'];
                if (phone == null || phone.toString().trim().isEmpty) {
                  if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Số điện thoại không khả dụng')));
                  return;
                }
                await launchUrlString('tel:${phone.toString().trim()}');
              } catch (e) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể mở dialer: $e')));
              }
            },
          ),
          IconButton(
            tooltip: 'Gọi video',
            icon: const Icon(Icons.videocam),
            onPressed: () async {
              ScaffoldMessenger.of(context).hideCurrentSnackBar();
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Đang khởi tạo cuộc gọi video...')),
              );

              final participantId = await _resolveParticipantId();
              if (participantId == null) {
                ScaffoldMessenger.of(context).hideCurrentSnackBar();
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Không tìm thấy người nhận cuộc gọi')),
                );
                return;
              }

              final callService = CallService();
              final result = await callService.initiateCall(participantId, CallType.video);
              ScaffoldMessenger.of(context).hideCurrentSnackBar();

              if (result['success'] == true && result['callId'] != null) {
                final callId = result['callId'] as String;

                final remoteUser = User(
                  id: participantId,
                  userName: _remoteName ?? 'Remote',
                  email: '',
                  avatarUrl: null,
                  createdAt: DateTime.now(),
                );

                // Wait for callee to accept before opening CallScreen
                void cleanupHandlers() {
                  callService.onCallAccepted = null;
                  callService.onCallRejected = null;
                }

                callService.onCallAccepted = (acceptedCallId) {
                  if (acceptedCallId == callId) {
                    cleanupHandlers();
                    // close dialing dialog if shown
                    try {
                      if (_dialingCallId == callId) Navigator.of(context).pop();
                    } catch (_) {}
                    if (!mounted) return;
                    Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => CallScreen(
                          callId: callId,
                          callType: CallType.video,
                          remoteUser: remoteUser,
                          isIncoming: false,
                        ),
                      ),
                    );
                  }
                };

                callService.onCallRejected = (rejectedCallId) {
                  if (rejectedCallId == callId) {
                    cleanupHandlers();
                    // close dialing dialog if shown
                    try {
                      if (_dialingCallId == callId) Navigator.of(context).pop();
                    } catch (_) {}
                    if (mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(content: Text('Cuộc gọi bị từ chối')),
                      );
                    }
                  }
                };

                // show dialing dialog (non-blocking)
                _showDialingDialog(callService, callId);

                Future.delayed(const Duration(seconds: 30), () {
                  cleanupHandlers();
                  // close dialog if present
                  try {
                    if (_dialingCallId == callId) Navigator.of(context).pop();
                  } catch (_) {}
                  if (mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(content: Text('Không có phản hồi từ người nhận')),
                    );
                  }
                });
              } else {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text(result['message'] ?? 'Không thể khởi tạo cuộc gọi video')),
                );
              }
            },
          ),
        ],
      ),
      body: UserGradientBackground(
        child: Stack(
          children: [
            Column(
            children: [
              Expanded(
                child: _isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : _messages.isEmpty
                        ? const Center(
                            child: Text(
                              'Chưa có tin nhắn nào\nHãy bắt đầu cuộc trò chuyện!',
                              textAlign: TextAlign.center,
                              style: TextStyle(color: Colors.grey),
                            ),
                          )
                        : ListView.builder(
                            controller: _scrollController,
                            padding: const EdgeInsets.all(16),
                            itemCount: _messages.length,
                            itemBuilder: (context, index) {
                              final message = _messages[index];
                              final authProvider = Provider.of<AuthProvider>(context, listen: false);
                              final currentUser = authProvider.currentUser;
                              final isMine = currentUser != null && message['senderId']?.toString() == currentUser.id;

                              return Align(
                                alignment: isMine ? Alignment.centerRight : Alignment.centerLeft,
                                child: Container(
                                  margin: const EdgeInsets.only(bottom: 8),
                                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                                  decoration: BoxDecoration(
                                    color: isMine ? Colors.blue : Colors.grey[300],
                                    borderRadius: BorderRadius.circular(18),
                                  ),
                                  constraints: BoxConstraints(
                                    maxWidth: MediaQuery.of(context).size.width * 0.7,
                                  ),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      if (!isMine)
                                        Text(
                                          // prefer a resolved remote name when the message senderName is a generic placeholder
                                          _resolveDisplayName(message['senderName'] as String?),
                                          style: TextStyle(
                                            fontSize: 12,
                                            color: isMine ? Colors.white70 : Colors.black54,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      Text(
                                        message['content'] ?? '',
                                        style: TextStyle(
                                          color: isMine ? Colors.white : Colors.black,
                                        ),
                                      ),
                                      const SizedBox(height: 6),

                                      // translated text (if any)
                                      if (_translatedTexts.containsKey(index))
                                        Container(
                                          margin: const EdgeInsets.only(top: 4),
                                          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                                          decoration: BoxDecoration(
                                            color: isMine ? Colors.blue[700] : Colors.grey[200],
                                            borderRadius: BorderRadius.circular(12),
                                          ),
                                          child: Text(
                                            _translatedTexts[index] ?? '',
                                            style: TextStyle(
                                              color: isMine ? Colors.white70 : Colors.black87,
                                              fontStyle: FontStyle.italic,
                                            ),
                                          ),
                                        ),

                                      const SizedBox(height: 6),

                                      Row(
                                        mainAxisSize: MainAxisSize.min,
                                        children: [
                                          Text(
                                            _formatTime(message['sentAt']),
                                            style: TextStyle(
                                              fontSize: 10,
                                              color: isMine ? Colors.white70 : Colors.black54,
                                            ),
                                          ),
                                          const SizedBox(width: 8),
                                          // translate button per message
                                          Row(
                                            mainAxisSize: MainAxisSize.min,
                                            children: [
                                              SizedBox(
                                                width: 36,
                                                height: 28,
                                                child: IconButton(
                                                  padding: EdgeInsets.zero,
                                                  tooltip: _isTranslating[index] == true
                                                      ? 'Đang dịch...'
                                                      : (_translatedTexts.containsKey(index) ? 'Ẩn bản dịch' : 'Dịch'),
                                                  icon: _isTranslating[index] == true
                                                      ? const SizedBox(
                                                          width: 16,
                                                          height: 16,
                                                          child: CircularProgressIndicator(strokeWidth: 2),
                                                        )
                                                      : Icon(
                                                          Icons.translate,
                                                          size: 18,
                                                          color: _translatedTexts.containsKey(index) ? Colors.greenAccent : (isMine ? Colors.white70 : Colors.black45),
                                                        ),
                                                  onPressed: () async {
                                                    // toggle translation for this message index
                                                    if (_translatedTexts.containsKey(index)) {
                                                      setState(() => _translatedTexts.remove(index));
                                                      return;
                                                    }

                                                    final text = message['content'] ?? '';
                                                    if (text.trim().isEmpty) return;

                                                    setState(() => _isTranslating[index] = true);
                                                    try {
                                                      // detect language and translate to the other language
                                                      final detected = _detectLanguage(text);
                                                      final from = detected;
                                                      final to = detected == 'vi' ? 'en' : 'vi';
                                                      final translated = await _translationService.translate(text, from: from, to: to);
                                                      if (mounted) setState(() => _translatedTexts[index] = translated);
                                                    } catch (e) {
                                                      if (mounted) {
                                                        ScaffoldMessenger.of(context).showSnackBar(
                                                          SnackBar(content: Text('Dịch thất bại: $e')),
                                                        );
                                                      }
                                                    } finally {
                                                      if (mounted) setState(() => _isTranslating[index] = false);
                                                    }
                                                  },
                                                ),
                                              ),
                                              // call button for messages from other user
                                              if (!isMine)
                                                SizedBox(
                                                  width: 36,
                                                  height: 28,
                                                  child: IconButton(
                                                    padding: EdgeInsets.zero,
                                                    tooltip: 'Gọi số này',
                                                    icon: Icon(Icons.phone, size: 18, color: Colors.green),
                                                    onPressed: () async {
                                                      await _onMessageCallPressed(message as Map<String, dynamic>);
                                                    },
                                                  ),
                                                ),
                                            ],
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              );
                            },
                          ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                decoration: BoxDecoration(
                  color: Colors.white,
                  border: Border(top: BorderSide(color: Colors.grey[300]!)),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _messageController,
                        decoration: const InputDecoration(
                          hintText: 'Nhập tin nhắn...',
                          border: InputBorder.none,
                        ),
                        maxLines: null,
                        textInputAction: TextInputAction.send,
                        onSubmitted: (_) => _sendMessage(),
                      ),
                    ),
                    // Mic button for speech-to-text
                    Padding(
                      padding: const EdgeInsets.only(right: 8.0),
                      child: GestureDetector(
                        onTap: () async {
                          if (_isListening) {
                            await _ttsSttService.stopListening();
                            if (mounted) setState(() => _isListening = false);
                            return;
                          }

                          final ok = await _ttsSttService.initSpeech();
                          if (!ok) {
                            if (mounted) ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(content: Text('Microphone không khả dụng. Vui lòng bật quyền Micro trong Cài đặt.')),
                            );
                            return;
                          }

                          if (mounted) setState(() => _isListening = true);
                          await _ttsSttService.startListening(onResult: (text, isFinal) {
                            if (!mounted) return;
                            setState(() {
                              _messageController.text = text;
                              _messageController.selection = TextSelection.fromPosition(TextPosition(offset: _messageController.text.length));
                            });
                            if (isFinal) {
                              _ttsSttService.stopListening();
                              if (mounted) setState(() => _isListening = false);
                            }
                          });
                        },
                        child: Stack(
                          alignment: Alignment.center,
                          children: [
                            Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                color: Colors.blue,
                                shape: BoxShape.circle,
                              ),
                              child: Icon(_isListening ? Icons.mic : Icons.mic_none, color: Colors.white),
                            ),
                            // listening indicator (small red dot)
                            if (_isListening)
                              Positioned(
                                right: 0,
                                top: 0,
                                child: Container(
                                  width: 10,
                                  height: 10,
                                  decoration: BoxDecoration(
                                    color: Colors.redAccent,
                                    shape: BoxShape.circle,
                                    boxShadow: [BoxShadow(color: Colors.redAccent.withOpacity(0.4), blurRadius: 6, spreadRadius: 1)],
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ),
                    ),
                    // Send button
                    IconButton(
                      onPressed: _isSending ? null : _sendMessage,
                      icon: _isSending
                          ? const SizedBox(
                              width: 24,
                              height: 24,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.send, color: Colors.blue),
                    ),
                  ],
                ),
              ),
            ],
          ),
            // debug overlay removed to avoid covering app UI; keep console logs for debugging
          ],
        ),
      ),
    );
  }

  /// Try to preload common translation models (english/vietnamese).
  void _preloadTranslationModels() async {
    try {
      setState(() {
        _isTranslating[-1] = true; // use -1 as global preload flag
      });
      await _translationService.preloadModels(['en', 'vi']);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không thể tải mô hình dịch trước: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isTranslating.remove(-1));
    }
  }

  /// Very small heuristic to detect Vietnamese vs English to choose direction.
  String _detectLanguage(String text) {
    if (text.trim().isEmpty) return 'en';
    const viChars = 'àáạảãâấầậẩẫăắằặẳẵèéẻẽẹêếềệểễìíỉĩịòóỏõọôốồộổỗơớờợởỡùúủũụưứừựửữỳýỷỹỵđÀÁẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴÈÉẺẼẸÊẾỀỆỂỄÌÍỈĨỊÒÓỎÕỌÔỐỒỘỔỖƠỚỜỢỞỠÙÚỦŨỤƯỨỪỰỬỮỲÝỶỸỴĐ';
    for (final r in text.runes) {
      if (viChars.contains(String.fromCharCode(r))) return 'vi';
    }
    // fallback: if has lots of ascii letters assume english
    return 'en';
  }

  /// Resolve a display name to avoid showing generic placeholders like 'Host' or 'Guest'.
  String _resolveDisplayName(String? name) {
    bool isPlaceholder(String? n) {
      if (n == null) return true;
      final lower = n.trim().toLowerCase();
      return lower.isEmpty || lower == 'host' || lower == 'guest' || lower == 'khách' || lower == 'unknown';
    }

    if (!isPlaceholder(name)) return name!;
    if (_remoteName != null && _remoteName!.trim().isNotEmpty) return _remoteName!;
    return 'Unknown';
  }

  String _formatTime(String? dateTimeString) {
    if (dateTimeString == null) return '';

    try {
      final dateTime = DateTime.parse(dateTimeString);
      return '${dateTime.hour.toString().padLeft(2, '0')}:${dateTime.minute.toString().padLeft(2, '0')}';
    } catch (e) {
      return '';
    }
  }
}
