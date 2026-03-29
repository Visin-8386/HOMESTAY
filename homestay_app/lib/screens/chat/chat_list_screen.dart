import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/api_config.dart';
import '../../providers/auth_provider_fixed.dart';
import '../../routes.dart';
import '../../services/api_service.dart';
import '../../widgets/user_gradient_background.dart';

class ChatListScreen extends StatefulWidget {
  const ChatListScreen({Key? key}) : super(key: key);

  @override
  _ChatListScreenState createState() => _ChatListScreenState();
}

class _ChatListScreenState extends State<ChatListScreen> {
  bool _isLoading = true;
  List<dynamic> _chats = [];
  int _unreadCount = 0;
  String? _currentUserId;
  // cache for user display names fetched from server
  final Map<String, String> _userNameCache = {};

  @override
  void initState() {
    super.initState();
    _loadChats();
  }

  Future<void> _loadChats() async {
    if (mounted) setState(() => _isLoading = true);

    try {
      final api = ApiService();

      // Get current user info
      final userResponse = await api.get(ApiConfig.profileUrl);
      final fetchedUserId = userResponse['data']?['id']?.toString();
      String? effectiveUserId = fetchedUserId;
      // fallback to AuthProvider if profile endpoint didn't return id
      if ((effectiveUserId == null || effectiveUserId.isEmpty) && mounted) {
        try {
          final auth = Provider.of<AuthProvider>(context, listen: false);
          effectiveUserId = auth.user?.id;
        } catch (_) {}
      }

      final response = await api.get('${ApiConfig.baseUrl}/api/conversations');
      final fetchedChats = response['data'] ?? [];

      // Load unread count
      final unreadResponse = await api.get('${ApiConfig.baseUrl}/api/conversations/unread-count');
      final fetchedUnread = unreadResponse['data']?['unreadCount'] ?? 0;

      if (mounted) {
        _currentUserId = effectiveUserId ?? fetchedUserId;
        setState(() => _chats = fetchedChats);
        setState(() => _unreadCount = fetchedUnread);
      }
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

  String _getOtherUserName(dynamic chat, String currentUserId) {
    // Support two conversation shapes:
    // - new API: { participantId, participantName, participantAvatar }
    // - old API: { hostId, guestId, hostName, guestName }
    final participantId = chat['participantId']?.toString();
    final participantName = chat['participantName'] as String?;
    final hostId = chat['hostId']?.toString();
    final guestId = chat['guestId']?.toString();

    String? name;
    // Helper to decide if a provided name is just a generic placeholder
    bool isPlaceholder(String? n) {
      if (n == null) return true;
      final lower = n.trim().toLowerCase();
      return lower.isEmpty || lower == 'host' || lower == 'guest' || lower == 'khách' || lower == 'unknown';
    }

    // If API provided participantId/participantName, prefer that (newer payload)
    if (participantId != null && participantId.isNotEmpty) {
      // participant is the other user in this conversation (server already resolved)
      if (!isPlaceholder(participantName)) return participantName!;
      if (_userNameCache.containsKey(participantId)) return _userNameCache[participantId]!;
      _fetchUserName(participantId);
      return 'Người dùng';
    }

    if (hostId == currentUserId) {
      name = chat['guestName'] as String?;
      if (!isPlaceholder(name)) return name!;
      // fallback to cache
      if (guestId != null && _userNameCache.containsKey(guestId)) return _userNameCache[guestId]!;
      // otherwise fetch in background and return a localized placeholder
      if (guestId != null) _fetchUserName(guestId);
      return 'Khách';
    } else {
      name = chat['hostName'] as String?;
      if (!isPlaceholder(name)) return name!;
      if (hostId != null && _userNameCache.containsKey(hostId)) return _userNameCache[hostId]!;
      if (hostId != null) _fetchUserName(hostId);
      return 'Host';
    }
  }

  String _getOtherUserAvatar(dynamic chat, String currentUserId) {
    // Prefer participantAvatar when present (newer payload)
    final participantAvatar = chat['participantAvatar'] as String?;
    if (participantAvatar != null && participantAvatar.isNotEmpty) return participantAvatar;

    final hostId = chat['hostId']?.toString();
    if (hostId == currentUserId) {
      return chat['guestAvatar'] ?? '';
    } else {
      return chat['hostAvatar'] ?? '';
    }
  }

  Future<void> _fetchUserName(String userId) async {
    if (userId.isEmpty || _userNameCache.containsKey(userId)) return;
    try {
      final api = ApiService();
      final resp = await api.get('${ApiConfig.baseUrl}/api/users/$userId');
      final display = resp['displayName'] ?? resp['data']?['displayName'] ?? resp['data']?['fullName'] ?? resp['data']?['name'];
      if (display != null && display.toString().trim().isNotEmpty) {
        _userNameCache[userId] = display.toString();
        if (mounted) setState(() {});
      }
    } catch (_) {
      // ignore fetch errors silently
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Tin nhắn (${_unreadCount > 0 ? _unreadCount : ''})'),
        backgroundColor: Colors.blue,
        actions: [
          IconButton(
            icon: const Icon(Icons.smart_toy), // AI icon
            onPressed: () {
              Navigator.pushNamed(context, '/ai-chat');
            },
            tooltip: 'Chat với AI',
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadChats,
          ),
        ],
      ),
      body: UserGradientBackground(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _loadChats,
                child: _chats.isEmpty
                    ? const Center(
                        child: Text(
                          'Chưa có cuộc trò chuyện nào\nNhấn nút + để bắt đầu chat với host',
                          textAlign: TextAlign.center,
                          style: TextStyle(fontSize: 16, color: Colors.grey),
                        ),
                      )
                    : ListView.builder(
                        padding: const EdgeInsets.all(16),
                        itemCount: _chats.length,
                        itemBuilder: (context, index) {
                          final chat = _chats[index];
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              leading: CircleAvatar(
                                backgroundColor: Colors.grey[300],
                                child: _getOtherUserAvatar(chat, _currentUserId ?? '').isNotEmpty
                                    ? ClipOval(
                                        child: Image.network(
                                          _getOtherUserAvatar(chat, _currentUserId ?? ''),
                                          width: 40,
                                          height: 40,
                                          fit: BoxFit.cover,
                                          errorBuilder: (context, error, stackTrace) =>
                                              const Icon(Icons.person, size: 20),
                                        ),
                                      )
                                    : const Icon(Icons.person, color: Colors.grey),
                              ),
                              title: Text(
                                _getOtherUserName(chat, _currentUserId ?? ''),
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    chat['homestayName'] ?? 'Homestay',
                                    style: const TextStyle(fontSize: 12, color: Colors.grey),
                                  ),
                                  if (chat['lastMessage'] != null)
                                    Text(
                                      chat['lastMessage'],
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                      style: TextStyle(
                                        color: chat['unreadCount'] > 0 ? Colors.black : Colors.grey,
                                        fontWeight: chat['unreadCount'] > 0 ? FontWeight.bold : FontWeight.normal,
                                      ),
                                    ),
                                ],
                              ),
                              trailing: Column(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  if (chat['lastMessageAt'] != null)
                                    Text(
                                      _formatTime(chat['lastMessageAt']),
                                      style: const TextStyle(fontSize: 12, color: Colors.grey),
                                    ),
                                  if (chat['unreadCount'] > 0)
                                    Container(
                                      padding: const EdgeInsets.all(6),
                                      decoration: const BoxDecoration(
                                        color: Colors.blue,
                                        shape: BoxShape.circle,
                                      ),
                                      child: Text(
                                        chat['unreadCount'].toString(),
                                        style: const TextStyle(
                                          color: Colors.white,
                                          fontSize: 12,
                                          fontWeight: FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                ],
                              ),
                              onTap: () {
                                Navigator.pushNamed(
                                  context,
                                  AppRoutes.chatDetail,
                                  arguments: chat['id'],
                                );
                              },
                            ),
                          );
                        },
                      ),
              ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          // TODO: Navigate to start new chat screen
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Tính năng bắt đầu chat mới đang phát triển')),
          );
        },
        backgroundColor: Colors.blue,
        child: const Icon(Icons.add),
      ),
    );
  }

  String _formatTime(String? dateTimeString) {
    if (dateTimeString == null) return '';

    try {
      final dateTime = DateTime.parse(dateTimeString);
      final now = DateTime.now();
      final difference = now.difference(dateTime);

      if (difference.inDays > 0) {
        return '${difference.inDays}d';
      } else if (difference.inHours > 0) {
        return '${difference.inHours}h';
      } else if (difference.inMinutes > 0) {
        return '${difference.inMinutes}m';
      } else {
        return 'now';
      }
    } catch (e) {
      return '';
    }
  }
}
