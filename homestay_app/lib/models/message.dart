class Message {
  final String id;
  final String conversationId;
  final String senderId;
  final String senderName;
  final String? senderAvatar;
  final String content;
  final String? imageUrl;
  final DateTime timestamp;
  final bool isRead;
  // In-memory translations keyed by target language code (e.g. 'en')
  Map<String, String>? translations;

  Message({
    required this.id,
    required this.conversationId,
    required this.senderId,
    required this.senderName,
    this.senderAvatar,
    required this.content,
    this.imageUrl,
    required this.timestamp,
    this.isRead = false,
    this.translations,
  });

  factory Message.fromJson(Map<String, dynamic> json) {
    // Support different server field names: sentAt, timestamp, sent_at
    DateTime parseTimestamp(dynamic v) {
      if (v == null) return DateTime.now();
      try {
        if (v is DateTime) return v;
        if (v is String && v.isNotEmpty) return DateTime.parse(v);
        if (v is int) return DateTime.fromMillisecondsSinceEpoch(v);
      } catch (_) {}
      return DateTime.now();
    }

    final tsCandidates = [
      json['timestamp'],
      json['sentAt'],
      json['sent_at'],
      json['sentAtUtc'],
      json['sent_at_utc'],
      json['sent_at_iso'],
      json['sentAtIso']
    ];

    DateTime ts = DateTime.now();
    for (final c in tsCandidates) {
      if (c != null) {
        ts = parseTimestamp(c);
        break;
      }
    }

    return Message(
      id: json['id']?.toString() ?? '',
      conversationId: json['conversationId']?.toString() ?? json['conversationId']?.toString() ?? '',
      senderId: json['senderId']?.toString() ?? '',
      senderName: json['senderName'] ?? json['sender'] ?? 'Unknown',
      senderAvatar: json['senderAvatar'] ?? json['sender_avatar'],
      content: json['content'] ?? json['message'] ?? '',
      imageUrl: json['imageUrl'] ?? json['image_url'],
      timestamp: ts,
      isRead: json['isRead'] ?? json['is_read'] ?? false,
      translations: json['translations'] != null && json['translations'] is Map
          ? Map<String, String>.from(json['translations'])
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'conversationId': conversationId,
      'senderId': senderId,
      'senderName': senderName,
      'senderAvatar': senderAvatar,
      'content': content,
      'imageUrl': imageUrl,
      'timestamp': timestamp.toIso8601String(),
      'isRead': isRead,
      'translations': translations,
    };
  }

  /// Attach or update a translation for a language code (e.g. 'en')
  void setTranslation(String lang, String translatedText) {
    translations = translations ?? {};
    translations![lang] = translatedText;
  }
}

class Conversation {
  final String id;
  final String bookingId;
  final String hostId;
  final String hostName;
  final String? hostAvatar;
  final String guestId;
  final String guestName;
  final String? guestAvatar;
  final String? lastMessage;
  final DateTime? lastMessageTime;
  final int unreadCount;
  final DateTime createdAt;

  Conversation({
    required this.id,
    required this.bookingId,
    required this.hostId,
    required this.hostName,
    this.hostAvatar,
    required this.guestId,
    required this.guestName,
    this.guestAvatar,
    this.lastMessage,
    this.lastMessageTime,
    this.unreadCount = 0,
    required this.createdAt,
  });

  factory Conversation.fromJson(Map<String, dynamic> json) {
    return Conversation(
      id: json['id']?.toString() ?? '',
      bookingId: json['bookingId']?.toString() ?? '',
      hostId: json['hostId']?.toString() ?? '',
      hostName: json['hostName'] ?? 'Host',
      hostAvatar: json['hostAvatar'],
      guestId: json['guestId']?.toString() ?? '',
      guestName: json['guestName'] ?? 'Guest',
      guestAvatar: json['guestAvatar'],
      lastMessage: json['lastMessage'],
      lastMessageTime: json['lastMessageTime'] != null 
        ? DateTime.parse(json['lastMessageTime']) 
        : null,
      unreadCount: json['unreadCount'] ?? 0,
      createdAt: json['createdAt'] != null 
        ? DateTime.parse(json['createdAt']) 
        : DateTime.now(),
    );
  }
}
