import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../config/api_config.dart';
import '../models/message.dart';
import 'api_service.dart';

class ChatService {

  /// Get or create a direct conversation between two users (guest and host)
  Future<Conversation?> createOrGetConversation(String userId, String hostId) async {
    try {
      // 1. Try to find an existing conversation between these two users
      final conversations = await getConversations();
      Conversation? existing;
      try {
        existing = conversations.firstWhere(
          (c) => (c.hostId == hostId && c.guestId == userId) || (c.hostId == userId && c.guestId == hostId),
        );
      } catch (_) {
        existing = null;
      }
      if (existing != null) return existing;

      // 2. If not found, create a new direct conversation (API must support this)
      final response = await _apiService.post(
        '/api/conversations/start-direct',
        {'hostId': hostId, 'guestId': userId},
      );
      if (response['data'] != null) {
        return Conversation.fromJson(response['data']);
      }
      return null;
    } catch (e) {
      print('Error creating or getting conversation: $e');
      return null;
    }
  }
  static final ChatService _instance = ChatService._internal();
  factory ChatService() => _instance;
  ChatService._internal();

  HubConnection? _hubConnection;
  final ApiService _apiService = ApiService();
  final _storage = const FlutterSecureStorage();
  
  // Callbacks
  Function(Message)? onMessageReceived;
  Function(String conversationId)? onTyping;
  Function()? onConnected;
  Function()? onDisconnected;

  Future<void> connect() async {
    if (_hubConnection?.state == HubConnectionState.Connected) {
      return;
    }

    try {
      final token = await _storage.read(key: 'access_token');
      if (token == null) throw Exception('No authentication token');

      _hubConnection = HubConnectionBuilder()
          .withUrl(
            ApiConfig.signalRHubUrl,
            options: HttpConnectionOptions(
              accessTokenFactory: () async => token,
            ),
          )
          .withAutomaticReconnect()
          .build();

      // Register message handlers
      _hubConnection!.on('ReceiveMessage', _handleReceiveMessage);
      _hubConnection!.on('UserTyping', _handleUserTyping);
      
      _hubConnection!.onclose(({error}) {
        print('Connection closed: $error');
        onDisconnected?.call();
      });

      _hubConnection!.onreconnecting(({error}) {
        print('Reconnecting: $error');
      });

      _hubConnection!.onreconnected(({connectionId}) {
        print('Reconnected: $connectionId');
        onConnected?.call();
      });

      await _hubConnection!.start();
      print('SignalR Connected');
      onConnected?.call();
    } catch (e) {
      print('SignalR Connection Error: $e');
      rethrow;
    }
  }

  void _handleReceiveMessage(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return;
    
    try {
      final raw = arguments[0];
      Map<String, dynamic> messageData;

      if (raw is Map<String, dynamic>) {
        messageData = raw;
      } else if (raw is Map) {
        messageData = Map<String, dynamic>.from(raw.cast<String, dynamic>());
      } else {
        // Try to parse if server sends JSON string
        try {
          messageData = Map<String, dynamic>.from(raw as Map);
        } catch (_) {
          print('Unhandled message payload type: ${raw.runtimeType}');
          return;
        }
      }

      // Normalize numeric ids to strings
      if (messageData['conversationId'] != null && messageData['conversationId'] is int) {
        messageData['conversationId'] = messageData['conversationId'].toString();
      }
      if (messageData['senderId'] != null && messageData['senderId'] is int) {
        messageData['senderId'] = messageData['senderId'].toString();
      }

      final message = Message.fromJson(messageData);
      onMessageReceived?.call(message);
    } catch (e) {
      print('Error handling message: $e');
    }
  }

  void _handleUserTyping(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return;
    
    try {
      final conversationId = arguments[0] as String;
      onTyping?.call(conversationId);
    } catch (e) {
      print('Error handling typing: $e');
    }
  }

  Future<void> sendMessage(String conversationId, String content) async {
    if (_hubConnection?.state != HubConnectionState.Connected) {
      await connect();
    }

    try {
      await _hubConnection!.invoke('SendMessage', args: [conversationId, content]);
    } catch (e) {
      print('Error sending message: $e');
      rethrow;
    }
  }

  Future<void> sendTypingIndicator(String conversationId) async {
    if (_hubConnection?.state != HubConnectionState.Connected) return;

    try {
      await _hubConnection!.invoke('SendTypingIndicator', args: [conversationId]);
    } catch (e) {
      print('Error sending typing indicator: $e');
    }
  }

  Future<void> joinConversation(String conversationId) async {
    if (_hubConnection?.state != HubConnectionState.Connected) {
      await connect();
    }

    try {
      await _hubConnection!.invoke('JoinConversation', args: [conversationId]);
    } catch (e) {
      print('Error joining conversation: $e');
      rethrow;
    }
  }

  Future<void> leaveConversation(String conversationId) async {
    if (_hubConnection?.state != HubConnectionState.Connected) return;

    try {
      await _hubConnection!.invoke('LeaveConversation', args: [conversationId]);
    } catch (e) {
      print('Error leaving conversation: $e');
    }
  }

  Future<void> disconnect() async {
    if (_hubConnection?.state == HubConnectionState.Connected) {
      await _hubConnection!.stop();
    }
    _hubConnection = null;
  }

  // REST API methods
  Future<List<Conversation>> getConversations() async {
    try {
    final response = await _apiService.get('/api/conversations');
      final List<dynamic> data = response['data'] ?? [];
      return data.map((json) => Conversation.fromJson(json)).toList();
    } catch (e) {
      print('Error fetching conversations: $e');
      return [];
    }
  }

  Future<List<Message>> getMessages(String conversationId, {int page = 1, int pageSize = 50}) async {
    try {
      final response = await _apiService.get(
        '/api/conversations/$conversationId/messages?page=$page&pageSize=$pageSize'
      );
      final List<dynamic> data = response['data'] ?? [];
      return data.map((json) => Message.fromJson(json)).toList();
    } catch (e) {
      print('Error fetching messages: $e');
      return [];
    }
  }

  Future<Conversation?> createConversation(String bookingId) async {
    try {
      final response = await _apiService.post(
        '/api/conversations/start',
        {'bookingId': bookingId},
      );
      return Conversation.fromJson(response['data']);
    } catch (e) {
      print('Error creating conversation: $e');
      return null;
    }
  }

  Future<void> markAsRead(String conversationId) async {
    try {
  await _apiService.post('/api/conversations/$conversationId/read', {});
    } catch (e) {
      print('Error marking as read: $e');
    }
  }
}
