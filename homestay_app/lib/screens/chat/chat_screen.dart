import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher_string.dart';

import '../../config/api_config.dart';
import '../../config/app_colors.dart';
import '../../models/message.dart';
import '../../models/user.dart';
import '../../providers/auth_provider_fixed.dart';
import '../../services/api_service.dart';
import '../../services/call_service.dart';
import '../../services/chat_service.dart';
import '../../services/storage_service.dart';
import '../../services/translation_service.dart';
import '../../services/tts_stt_service.dart';
import '../../widgets/user_gradient_background.dart';
import '../call/call_screen.dart';

class ChatScreen extends StatefulWidget {
  final Conversation conversation;

  const ChatScreen({Key? key, required this.conversation}) : super(key: key);

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final ChatService _chatService = ChatService();
  final TextEditingController _messageController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  
  List<Message> _messages = [];
  bool _isLoading = true;
  bool _isSending = false;
  bool _isOtherUserTyping = false;
  final TranslationService _translationService = TranslationService();
  String? _dialingCallId;
  final TtsSttService _ttsSttService = TtsSttService();
  // Track which messages are currently being translated (show spinner)
  final Set<String> _translatingMessageIds = {};
  // Remember last requested translation target per message id (e.g. 'en' or 'vi')
  final Map<String, String> _lastTranslationTargetByMessageId = {};
  // When true, automatically translate incoming messages to Vietnamese
  bool _autoTranslateIncoming = false;
  // Fallback local user id read from secure storage (used when AuthProvider.user is not yet available)
  String? _localUserId;
  // Fallback local userName read from secure storage (used when AuthProvider.user is not yet available)
  String? _localUserName;

  @override
  void initState() {
    super.initState();
    _setupChat();
  }

  Future<void> _setupChat() async {
    // Try to read saved user id from secure storage for offline alignment
    try {
      final id = await StorageService().getUserId();
      final name = await StorageService().getUserName();
      if (!mounted) return;
      setState(() {
        _localUserId = id;
        _localUserName = name;
      });
    } catch (_) {
      if (mounted) setState(() { _localUserId = null; _localUserName = null; });
    }
    // Setup message listener
      _chatService.onMessageReceived = (message) {
        if (message.conversationId != widget.conversation.id) return;
        if (!mounted) return;
        // Debug: log message sender and effective user id
        try {
          final effectiveUserIdDbg = Provider.of<AuthProvider>(context, listen: false).user?.id ?? _localUserId;
          print('[chat] onMessageReceived - senderId=${message.senderId} senderName=${message.senderName} effectiveUserId=$effectiveUserIdDbg');
        } catch (_) {}
        // Append to the end so messages remain in chronological order
        setState(() {
          _messages.add(message);
        });
        // Auto-translate incoming message if enabled
        final authProvider = Provider.of<AuthProvider>(context, listen: false);
        final currentUser = authProvider.user;
        if (_autoTranslateIncoming && message.senderId != currentUser?.id) {
          _autoTranslateMessage(message);
        }
        _scrollToBottom();
        _chatService.markAsRead(widget.conversation.id);
      };

    // Setup typing indicator
    _chatService.onTyping = (conversationId) {
      if (conversationId != widget.conversation.id) return;
      if (!mounted) return;
      setState(() => _isOtherUserTyping = true);
      Future.delayed(const Duration(seconds: 3), () {
        if (mounted) setState(() => _isOtherUserTyping = false);
      });
    };

    // Connect and join conversation
    try {
      await _chatService.connect();
      await _chatService.joinConversation(widget.conversation.id);
      await _loadMessages();
      await _chatService.markAsRead(widget.conversation.id);
    } catch (e) {
      print('Error setting up chat: $e');
    }

    // Show a small hint about the translate FAB so users discover the feature
    if (mounted && !_autoTranslateIncoming) {
      Future.delayed(const Duration(milliseconds: 400), () {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Bấm nút dịch (góc phải dưới) để dịch tin nhắn')), 
        );
      });
    }
  }

  Future<void> _loadMessages() async {
    setState(() => _isLoading = true);
    
    try {
      final messages = await _chatService.getMessages(widget.conversation.id);
      // Ensure messages are in ascending chronological order (oldest first)
      messages.sort((a, b) => a.timestamp.compareTo(b.timestamp));
      if (!mounted) return;
      setState(() {
        _messages = messages.toList();
        _isLoading = false;
      });
      // Debug: print effective user id and first few message ids
      try {
        final authProvider = Provider.of<AuthProvider>(context, listen: false);
        final effectiveUserIdDbg = authProvider.user?.id ?? _localUserId;
        final effectiveUserNameDbg = authProvider.user?.userName ?? _localUserName;
        print('[chat] _loadMessages effectiveUserId=$effectiveUserIdDbg total=${_messages.length}');
        print('[chat] _loadMessages effectiveUserName=$effectiveUserNameDbg conversationHostId=${widget.conversation.hostId} guestId=${widget.conversation.guestId}');
        for (var i = 0; i < _messages.length && i < 6; i++) {
          final m = _messages[i];
          print('[chat] msg[$i] senderId=${m.senderId} senderName=${m.senderName} content="${m.content}"');
        }
      } catch (_) {}
      _scrollToBottom();
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải tin nhắn: $e')),
        );
      }
    }
  }

  Future<void> _sendMessage() async {
    final content = _messageController.text.trim();
    if (content.isEmpty || _isSending) return;

    setState(() => _isSending = true);
    _messageController.clear();

    try {
      await _chatService.sendMessage(widget.conversation.id, content);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi gửi tin nhắn: $e')),
        );
      }
    } finally {
      setState(() => _isSending = false);
    }
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      Future.delayed(const Duration(milliseconds: 100), () {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      });
    }
  }

  Future<void> _startAudioCall() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    final currentUser = authProvider.user;
    final isHost = currentUser?.id == widget.conversation.hostId;
    final recipientId = isHost ? widget.conversation.guestId : widget.conversation.hostId;

    // Create remote user object
    final remoteUser = User(
      id: recipientId,
      userName: isHost ? widget.conversation.guestName : widget.conversation.hostName,
      email: '', // We don't have email in conversation
      avatarUrl: isHost ? widget.conversation.guestAvatar : widget.conversation.hostAvatar,
      createdAt: DateTime.now(),
    );

    final callService = CallService();
    final result = await callService.initiateCall(recipientId, CallType.audio);

    if (result['success'] == true && result['callId'] != null) {
      final callId = result['callId'] as String;

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
                callType: CallType.audio,
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

      // Timeout if no response
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
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(result['message'] ?? 'Không thể bắt đầu cuộc gọi')),
        );
      }
    }
  }

  Future<void> _startVideoCall() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    final currentUser = authProvider.user;
    final isHost = currentUser?.id == widget.conversation.hostId;
    final recipientId = isHost ? widget.conversation.guestId : widget.conversation.hostId;

    // Create remote user object
    final remoteUser = User(
      id: recipientId,
      userName: isHost ? widget.conversation.guestName : widget.conversation.hostName,
      email: '', // We don't have email in conversation
      avatarUrl: isHost ? widget.conversation.guestAvatar : widget.conversation.hostAvatar,
      createdAt: DateTime.now(),
    );

    final callService = CallService();
    final result = await callService.initiateCall(recipientId, CallType.video);

    if (result['success'] == true && result['callId'] != null) {
      final callId = result['callId'] as String;

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

      // Timeout if no response
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
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(result['message'] ?? 'Không thể bắt đầu cuộc gọi video')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final currentUser = authProvider.user;
    final isHost = currentUser?.id == widget.conversation.hostId;
    final otherUserName = isHost ? widget.conversation.guestName : widget.conversation.hostName;
    final otherUserAvatar = isHost ? widget.conversation.guestAvatar : widget.conversation.hostAvatar;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundImage: otherUserAvatar != null
                  ? CachedNetworkImageProvider(otherUserAvatar)
                  : null,
              child: otherUserAvatar == null
                  ? Text(otherUserName[0].toUpperCase())
                  : null,
            ),
            const SizedBox(width: 12),
            Expanded(
            child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    otherUserName,
                    style: const TextStyle(fontSize: 16),
                  ),
                  if (_isOtherUserTyping)
                    Text(
                      'Đang nhập...',
                      style: TextStyle(
                        fontSize: 12,
                        color: AppColors.primary,
                        fontStyle: FontStyle.italic,
                      ),
                    ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.call),
            onPressed: () async {
              // Offer native dialer or in-app call for conversation recipient
              final authProvider = Provider.of<AuthProvider>(context, listen: false);
              final currentUser = authProvider.user;
              final isHost = currentUser?.id == widget.conversation.hostId;
              final recipientId = isHost ? widget.conversation.guestId : widget.conversation.hostId;

              if (recipientId.isEmpty) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không tìm thấy người nhận cuộc gọi')));
                return;
              }

              try {
                final api = ApiService();
                final resp = await api.get('${ApiConfig.baseUrl}/api/users/$recipientId');
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
                  // Start in-app audio call
                  await _startAudioCall();
                }
              } catch (e) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi khi lấy số điện thoại: $e')));
              }
            },
            tooltip: 'Gọi điện',
          ),
          // Quick native dial button
          IconButton(
            icon: const Icon(Icons.phone_enabled),
            tooltip: 'Gọi bằng số điện thoại',
            onPressed: () async {
              final authProvider = Provider.of<AuthProvider>(context, listen: false);
              final currentUser = authProvider.user;
              final isHost = currentUser?.id == widget.conversation.hostId;
              final recipientId = isHost ? widget.conversation.guestId : widget.conversation.hostId;

              if (recipientId.isEmpty) {
                if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không tìm thấy người nhận cuộc gọi')));
                return;
              }

              try {
                final api = ApiService();
                final resp = await api.get('${ApiConfig.baseUrl}/api/users/$recipientId');
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
            icon: const Icon(Icons.videocam),
            onPressed: _startVideoCall,
            tooltip: 'Gọi video',
          ),
        ],
      ),
      body: UserGradientBackground(
        child: Stack(
          children: [
            Column(
              children: [
                Expanded(child: _buildMessageList(currentUser)),
                _buildMessageInput(),
              ],
            ),
            if (kDebugMode)
              Positioned(
                left: 8,
                top: 8,
                child: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(color: Colors.black54, borderRadius: BorderRadius.circular(8)),
                  child: Builder(builder: (ctx) {
                    final authProvider = Provider.of<AuthProvider>(context, listen: false);
                    final effectiveId = authProvider.user?.id ?? _localUserId ?? 'null';
                    final lastSender = _messages.isNotEmpty ? _messages.last.senderId : 'n/a';
                    return Text(
                      'DBG cur=$effectiveId host=${widget.conversation.hostId} guest=${widget.conversation.guestId} msgs=${_messages.length} lastSender=$lastSender',
                      style: const TextStyle(color: Colors.white, fontSize: 11),
                    );
                  }),
                ),
              ),
          ],
        ),
      ),
      floatingActionButtonLocation: FloatingActionButtonLocation.endFloat,
      floatingActionButton: Padding(
        padding: const EdgeInsets.only(bottom: 100.0, right: 8.0),
        child: FloatingActionButton.extended(
          heroTag: 'translate_fab',
          backgroundColor: _autoTranslateIncoming ? AppColors.primary : AppColors.primary.withAlpha((0.3 * 255).round()),
          onPressed: () {
            setState(() => _autoTranslateIncoming = !_autoTranslateIncoming);
            if (_autoTranslateIncoming) _translateExistingIncomingMessages();
            final msg = _autoTranslateIncoming ? 'Bật dịch tự động' : 'Tắt dịch tự động';
            if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
          },
          icon: Icon(Icons.translate, color: _autoTranslateIncoming ? Colors.white : AppColors.primary),
          label: Text(_autoTranslateIncoming ? 'Dịch: BẬT' : 'Dịch tin nhắn', style: TextStyle(color: _autoTranslateIncoming ? Colors.white : AppColors.primary)),
        ),
      ),
    );
  }

  /// Automatically translate a single message to Vietnamese and attach it to the message.
  Future<void> _autoTranslateMessage(Message m) async {
    if (m.content.trim().isEmpty) return;
    if (m.translations != null && m.translations!.isNotEmpty) return;
    try {
      setState(() => _translatingMessageIds.add(m.id));
      // Attempt translation: assume incoming messages are in English and translate to Vietnamese.
      final translated = await _translationService.translate(m.content, from: 'en', to: 'vi');
      if (!mounted) return;
      setState(() {
        m.setTranslation('vi', translated);
        _lastTranslationTargetByMessageId[m.id] = 'vi';
      });
    } catch (_) {
      // ignore translation failures silently
    } finally {
      if (mounted) setState(() => _translatingMessageIds.remove(m.id));
    }
  }

  /// Translate existing incoming messages (used when the user enables auto-translate)
  Future<void> _translateExistingIncomingMessages() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    final currentUser = authProvider.user;
    for (final m in _messages) {
      if (m.senderId == currentUser?.id) continue;
      if (m.translations != null && m.translations!.isNotEmpty) continue;
      await _autoTranslateMessage(m);
    }
  }

  /// Show a simple dialing dialog while waiting for callee to accept.
  /// Returns when dialog dismissed.
  Future<void> _showDialingDialog(CallService callService, String callId) async {
    if (!mounted) return;
    setState(() => _dialingCallId = callId);
    // Use a dialog that cannot be dismissed by tapping outside
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
                // End call on server (cancel)
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
    // dialog closed — clear dialing id
    if (mounted) setState(() => _dialingCallId = null);
  }

  Widget _buildMessageList(User? currentUser) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_messages.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.chat_bubble_outline, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'Chưa có tin nhắn nào',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Gửi tin nhắn đầu tiên để bắt đầu',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      controller: _scrollController,
      padding: const EdgeInsets.all(16),
      itemCount: _messages.length,
  itemBuilder: (context, index) {
  final message = _messages[index];
  // Determine effective local identity (id and username)
  final effectiveUserIdRaw = currentUser?.id ?? _localUserId;
  final effectiveUserNameRaw = currentUser?.userName ?? _localUserName;

  String? normalize(String? v) => v?.toString().trim();
  final effectiveUserId = normalize(effectiveUserIdRaw);
  final effectiveUserName = normalize(effectiveUserNameRaw)?.toLowerCase();
  final messageSenderId = normalize(message.senderId);
  final messageSenderName = normalize(message.senderName)?.toLowerCase();
  // Also compute simplified names by removing common prefixes like 'User ' or 'Host '
  String simplifyName(String? n) {
    if (n == null || n.isEmpty) return '';
    var s = n.trim();
    // If name contains spaces like 'User user1' take last token
    if (s.contains(' ')) s = s.split(' ').last;
    return s.toLowerCase();
  }
  final simplifiedEffectiveName = simplifyName(effectiveUserName);
  final simplifiedMessageName = simplifyName(messageSenderName);

  // Determine ownership using conversation mapping first (host/guest), then id/name checks
  bool isMe = false;
  try {
    final convHost = normalize(widget.conversation.hostId);
    final convGuest = normalize(widget.conversation.guestId);
    // If effective user matches host id, then messages from host are mine
    if (effectiveUserId != null && convHost != null && effectiveUserId == convHost) {
      if (messageSenderId == convHost) isMe = true;
    } else if (effectiveUserId != null && convGuest != null && effectiveUserId == convGuest) {
      if (messageSenderId == convGuest) isMe = true;
    }
  } catch (_) {}

  // If still unknown, fallback to id comparison
  if (!isMe && effectiveUserId != null && messageSenderId != null) {
    if (messageSenderId == effectiveUserId) {
      isMe = true;
    } else if (messageSenderId.toLowerCase() == effectiveUserId.toLowerCase()) {
      isMe = true;
    }
  }

  // Final fallback: compare usernames if ids don't match. Allow substring or simplified comparisons
  if (!isMe && effectiveUserName != null && messageSenderName != null) {
    if (effectiveUserName == messageSenderName) {
      isMe = true;
    } else if (messageSenderName.contains(effectiveUserName) || effectiveUserName.contains(messageSenderName)) {
      isMe = true;
    } else if (simplifiedEffectiveName.isNotEmpty && simplifiedMessageName.isNotEmpty && simplifiedEffectiveName == simplifiedMessageName) {
      isMe = true;
    }
  }
        final showTimestamp = index == 0 ||
            _messages[index - 1].timestamp.difference(message.timestamp).abs() > const Duration(minutes: 5);

        // Debug per-message identity resolution
        try {
          print('[chat] convHost=${widget.conversation.hostId} convGuest=${widget.conversation.guestId} resolvedEffectiveUserId=$effectiveUserId resolvedEffectiveUserName=$effectiveUserName');
          print('[chat] itemBuilder idx=$index senderId=${messageSenderId} senderName=${messageSenderName} effectiveUserId=${effectiveUserId} effectiveUserName=${effectiveUserName} isMe=$isMe');
        } catch (_) {}

        return Column(
          children: [
            if (showTimestamp) _buildTimestamp(message.timestamp),
            _buildMessageBubble(message, isMe),
          ],
        );
      },
    );
  }

  Widget _buildTimestamp(DateTime timestamp) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 16),
      child: Text(
        DateFormat('dd/MM/yyyy HH:mm').format(timestamp),
        style: TextStyle(
          fontSize: 12,
          color: Colors.grey[600],
        ),
      ),
    );
  }

  Widget _buildMessageBubble(Message message, bool isMe) {
    // Modern chat bubble with avatar, shadow, and smooth animation
    final bubble = AnimatedContainer(
      duration: const Duration(milliseconds: 250),
      curve: Curves.easeOut,
      margin: const EdgeInsets.symmetric(vertical: 6, horizontal: 2),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      constraints: BoxConstraints(
        maxWidth: MediaQuery.of(context).size.width * 0.78,
      ),
      decoration: BoxDecoration(
        color: isMe ? AppColors.primary : Colors.white,
        borderRadius: BorderRadius.only(
          topLeft: Radius.circular(isMe ? 18 : 6),
          topRight: Radius.circular(isMe ? 6 : 18),
          bottomLeft: const Radius.circular(18),
          bottomRight: const Radius.circular(18),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha((0.07 * 255).round()),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
        border: Border.all(
          color: isMe ? AppColors.primary.withAlpha((0.18 * 255).round()) : Colors.grey[200]!,
          width: 1.2,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Message content
          Text(
            message.content,
            style: TextStyle(
              color: isMe ? Colors.white : Colors.black87,
              fontSize: 16,
              fontWeight: FontWeight.w500,
              height: 1.35,
            ),
          ),
          const SizedBox(height: 8),
          // Translation (if any)
          if (message.translations != null && message.translations!.isNotEmpty)
            Container(
              margin: const EdgeInsets.only(top: 2, bottom: 2),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
                decoration: BoxDecoration(
                color: isMe ? AppColors.primary.withAlpha((0.13 * 255).round()) : Colors.grey[100],
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                message.translations!.values.first,
                style: TextStyle(
                  color: isMe ? Colors.white : Colors.grey[800],
                  fontSize: 14,
                  fontStyle: FontStyle.italic,
                ),
              ),
            ),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Text(
                    DateFormat('HH:mm').format(message.timestamp),
                    style: TextStyle(
                      fontSize: 11,
                      color: isMe ? Colors.white70 : Colors.grey[600],
                    ),
                  ),
                  if (isMe) ...[
                    const SizedBox(width: 8),
                    Icon(
                      message.isRead ? Icons.done_all : Icons.check,
                      size: 14,
                      color: message.isRead ? Colors.white70 : Colors.white54,
                    ),
                  ]
                ],
              ),
              Row(
                children: [
                  // Play TTS button
                  GestureDetector(
                    onTap: () async {
                      try {
                        await _ttsSttService.speak(message.content, lang: message.content.length > 0 ? 'vi-VN' : null);
                      } catch (e) {
                        if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi phát âm: $e')));
                      }
                    },
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                      decoration: BoxDecoration(
                        color: isMe ? Colors.white.withAlpha((0.13 * 255).round()) : AppColors.primary.withAlpha((0.13 * 255).round()),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Icon(Icons.play_arrow, size: 16, color: isMe ? Colors.white70 : AppColors.primary),
                    ),
                  ),
                  const SizedBox(width: 6),
                  // Phone call button (other user's messages only)
                  if (!isMe)
                    GestureDetector(
                      onTap: () async => await _onMessageCallPressed(message),
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withAlpha((0.13 * 255).round()),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Icon(Icons.phone, size: 16, color: AppColors.primary),
                      ),
                    ),
                  const SizedBox(width: 6),
                  // Translate button
                  GestureDetector(
                    onTap: () async {
                      final from = message.senderName == 'Me' || isMe ? 'vi' : 'en';
                      final to = from == 'vi' ? 'en' : 'vi';
                      setState(() => _translatingMessageIds.add(message.id));
                      try {
                        final translated = await _translationService.translate(message.content, from: from, to: to);
                        if (!mounted) return;
                        setState(() {
                          message.setTranslation(to, translated);
                          _lastTranslationTargetByMessageId[message.id] = to;
                        });
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(content: Text('Đã dịch sang ${to == 'en' ? 'Tiếng Anh' : 'Tiếng Việt'}')),
                        );
                      } catch (e) {
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi: $e')));
                      } finally {
                        setState(() => _translatingMessageIds.remove(message.id));
                      }
                    },
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 200),
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                        color: message.translations != null && message.translations!.isNotEmpty 
                            ? Colors.green
                            : (isMe ? Colors.white.withAlpha((0.8 * 255).round()) : AppColors.primary.withAlpha((0.8 * 255).round())),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.white.withAlpha((0.3 * 255).round()), width: 1),
                        boxShadow: [
                          if (message.translations != null && message.translations!.isNotEmpty)
                            BoxShadow(color: Colors.green.withAlpha((0.2 * 255).round()), blurRadius: 6, offset: Offset(0,2)),
                        ],
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            _translatingMessageIds.contains(message.id) ? Icons.hourglass_empty : Icons.translate, 
                            size: 13, 
                            color: message.translations != null && message.translations!.isNotEmpty 
                                ? Colors.white
                                : (isMe ? AppColors.primary : Colors.white)
                          ),
                          const SizedBox(width: 3),
                          Text(
                            _translatingMessageIds.contains(message.id) 
                                ? 'Đang dịch...'
                                : (message.translations != null && message.translations!.isNotEmpty ? 'Đã dịch' : 'Dịch'),
                            style: TextStyle(
                              fontSize: 10,
                              fontWeight: FontWeight.w600,
                              color: message.translations != null && message.translations!.isNotEmpty 
                                  ? Colors.white
                                  : (isMe ? AppColors.primary : Colors.white)
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    );

    // Wrap bubble in a Stack so we can show a translate button for both sides
    if (isMe) {
      return Row(
        mainAxisAlignment: MainAxisAlignment.end,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Expanded(child: Container()),
          Flexible(child: bubble),
        ],
      );
    }
    // Other user's message: avatar + bubble
    return Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Padding(
          padding: const EdgeInsets.only(right: 8.0, left: 2.0),
          child: CircleAvatar(
            radius: 18,
            backgroundImage: message.senderAvatar != null
                ? CachedNetworkImageProvider(message.senderAvatar!)
                : null,
            child: message.senderAvatar == null
                ? Text(message.senderName.isNotEmpty ? message.senderName[0].toUpperCase() : '?', style: const TextStyle(fontWeight: FontWeight.bold))
                : null,
            backgroundColor: Colors.grey[300],
          ),
        ),
        Flexible(child: bubble),
        Expanded(child: Container()),
      ],
    );
  }

  Future<void> _onMessageCallPressed(Message message) async {
    final senderId = message.senderId;
    if (senderId.isEmpty) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Không tìm thấy thông tin người gửi')));
      return;
    }

    try {
      final api = ApiService();
      final resp = await api.get('${ApiConfig.baseUrl}/api/users/$senderId');

      String? phone;
      if (resp is Map) {
        phone = resp['phoneNumber'] ?? resp['phone'] ?? resp['phone_number'] ?? resp['mobile'] ?? resp['data']?['phoneNumber'];
      }

      if (phone == null || phone.toString().trim().isEmpty) {
        if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Số điện thoại không khả dụng')));
        return;
      }

      phone = phone.toString().trim();

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
        final tel = 'tel:$phone';
        try {
          await launchUrlString(tel);
        } catch (e) {
          if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể mở ứng dụng gọi: $e')));
        }
        return;
      }

      if (choice == 'app') {
        final callService = CallService();
        final result = await callService.initiateCall(senderId, CallType.audio);

        if (result['success'] == true && result['callId'] != null) {
          final callId = result['callId'] as String;

          // Prepare remote user
          final remoteUser = User(
            id: senderId,
            userName: message.senderName,
            email: '',
            avatarUrl: message.senderAvatar,
            createdAt: DateTime.now(),
          );

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
  }

  Widget _buildMessageInput() {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha((0.05 * 255).round()),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: _messageController,
                decoration: InputDecoration(
                  hintText: 'Nhập tin nhắn...',
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(24),
                    borderSide: BorderSide.none,
                  ),
                  filled: true,
                  fillColor: Colors.grey[100],
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 20,
                    vertical: 10,
                  ),
                ),
                maxLines: null,
                textCapitalization: TextCapitalization.sentences,
                onChanged: (value) {
                  if (value.isNotEmpty) {
                    _chatService.sendTypingIndicator(widget.conversation.id);
                  }
                },
              ),
            ),
            const SizedBox(width: 8),
            // Mic button for speech-to-text
            GestureDetector(
              onTap: () async {
                if (_ttsSttService.isListening) {
                  await _ttsSttService.stopListening();
                } else {
                  final ok = await _ttsSttService.initSpeech();
                  if (!ok) {
                    if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Microphone không khả dụng')));
                    return;
                  }

                  await _ttsSttService.startListening(onResult: (text, isFinal) {
                    if (!mounted) return;
                    setState(() {
                      _messageController.text = text;
                      _messageController.selection = TextSelection.fromPosition(TextPosition(offset: _messageController.text.length));
                    });
                    if (isFinal) {
                      _ttsSttService.stopListening();
                    }
                  });
                }
              },
              child: Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  shape: BoxShape.circle,
                ),
                child: Icon(_ttsSttService.isListening ? Icons.mic : Icons.mic_none, color: Colors.white),
              ),
            ),
            const SizedBox(width: 8),
            Material(
              color: AppColors.primary,
              borderRadius: BorderRadius.circular(24),
              child: InkWell(
                borderRadius: BorderRadius.circular(24),
                onTap: _isSending ? null : _sendMessage,
                child: Container(
                  padding: const EdgeInsets.all(12),
                  child: _isSending
                      ? const SizedBox(
                          width: 24,
                          height: 24,
                          child: CircularProgressIndicator(
                            color: Colors.white,
                            strokeWidth: 2,
                          ),
                        )
                      : const Icon(Icons.send, color: Colors.white),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _chatService.leaveConversation(widget.conversation.id);
    _chatService.onMessageReceived = null;
    _chatService.onTyping = null;
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }
}
