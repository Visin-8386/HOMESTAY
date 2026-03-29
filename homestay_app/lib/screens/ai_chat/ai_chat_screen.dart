import 'package:flutter/material.dart';

import '../../services/gemini_service.dart';

class AIChatScreen extends StatefulWidget {
  const AIChatScreen({super.key});

  @override
  State<AIChatScreen> createState() => _AIChatScreenState();
}

class _AIChatScreenState extends State<AIChatScreen> {
  final _messageController = TextEditingController();
  final _scrollController = ScrollController();
  final _geminiService = GeminiService();
  final List<ChatMessage> _messages = [];
  bool _isLoading = false;
  bool _isConnected = false;

  @override
  void initState() {
    super.initState();
    _checkConnection();
    _addWelcomeMessage();
  }

  Future<void> _checkConnection() async {
    final connected = await _geminiService.checkConnection();
    setState(() {
      _isConnected = connected;
    });
  }

  void _addWelcomeMessage() {
    setState(() {
      _messages.add(ChatMessage(
        text: 'Xin chào! Tôi là trợ lý AI của Homestay Booking. Tôi có thể giúp bạn:\n\n'
            '• 🏠 Tìm homestay phù hợp\n'
            '• 📅 Tư vấn đặt phòng\n'
            '• 💳 Hướng dẫn thanh toán\n'
            '• 🗺️ Gợi ý điểm đến\n'
            '• ❓ Giải đáp thắc mắc\n\n'
            'Bạn cần tôi hỗ trợ gì ạ?',
        isUser: false,
        timestamp: DateTime.now(),
      ));
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.blue,
        foregroundColor: Colors.white,
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Icon(Icons.smart_toy, color: Colors.blue, size: 20),
            ),
            const SizedBox(width: 12),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('AI Assistant', style: TextStyle(fontSize: 16)),
                Row(
                  children: [
                    Container(
                      width: 8,
                      height: 8,
                      decoration: BoxDecoration(
                        color: _isConnected ? Colors.greenAccent : Colors.redAccent,
                        shape: BoxShape.circle,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Text(
                      _isConnected ? 'Gemini AI' : 'Offline',
                      style: const TextStyle(fontSize: 11, color: Colors.white70),
                    ),
                  ],
                ),
              ],
            ),
          ],
        ),
        actions: [
          PopupMenuButton<String>(
            icon: const Icon(Icons.more_vert),
            onSelected: _handleMenuAction,
            itemBuilder: (context) => [
              const PopupMenuItem(
                value: 'recommendations',
                child: Row(
                  children: [
                    Icon(Icons.recommend, color: Colors.blue),
                    SizedBox(width: 8),
                    Text('Gợi ý homestay'),
                  ],
                ),
              ),
              const PopupMenuItem(
                value: 'payment',
                child: Row(
                  children: [
                    Icon(Icons.payment, color: Colors.green),
                    SizedBox(width: 8),
                    Text('Hướng dẫn thanh toán'),
                  ],
                ),
              ),
              const PopupMenuItem(
                value: 'travel',
                child: Row(
                  children: [
                    Icon(Icons.travel_explore, color: Colors.orange),
                    SizedBox(width: 8),
                    Text('Mẹo du lịch'),
                  ],
                ),
              ),
              const PopupMenuItem(
                value: 'clear',
                child: Row(
                  children: [
                    Icon(Icons.delete, color: Colors.red),
                    SizedBox(width: 8),
                    Text('Xóa lịch sử chat'),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
      body: Column(
        children: [
          // Connection status banner
          if (!_isConnected)
            Container(
              padding: const EdgeInsets.all(8),
              color: Colors.orange[100],
              child: Row(
                children: [
                  const Icon(Icons.warning, color: Colors.orange, size: 16),
                  const SizedBox(width: 8),
                  const Expanded(
                    child: Text(
                      'Gemini AI chưa kết nối. Kiểm tra API key trong .env',
                      style: TextStyle(fontSize: 12),
                    ),
                  ),
                  TextButton(
                    onPressed: _checkConnection,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            ),

          // Messages list
          Expanded(
            child: _messages.isEmpty
                ? const Center(child: Text('Chưa có tin nhắn'))
                : ListView.builder(
                    controller: _scrollController,
                    padding: const EdgeInsets.all(16),
                    itemCount: _messages.length,
                    itemBuilder: (context, index) {
                      final message = _messages[index];
                      return _buildMessageBubble(message);
                    },
                  ),
          ),

          // Loading indicator
          if (_isLoading)
            Container(
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                  const SizedBox(width: 12),
                  const Text('AI đang suy nghĩ...', style: TextStyle(fontStyle: FontStyle.italic)),
                ],
              ),
            ),

          // Input field
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: Colors.white,
              boxShadow: [
                BoxShadow(
                  color: Colors.black12,
                  blurRadius: 4,
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
                        hintText: 'Hỏi AI về homestay...',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(24),
                        ),
                        contentPadding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 10,
                        ),
                        suffixIcon: IconButton(
                          icon: const Icon(Icons.attach_file),
                          onPressed: () {
                            // Future: attach images
                          },
                        ),
                      ),
                      maxLines: null,
                      textInputAction: TextInputAction.send,
                      onSubmitted: (_) => _sendMessage(),
                    ),
                  ),
                  const SizedBox(width: 8),
                  CircleAvatar(
                    backgroundColor: Colors.blue,
                    child: IconButton(
                      icon: const Icon(Icons.send, color: Colors.white, size: 20),
                      onPressed: _sendMessage,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMessageBubble(ChatMessage message) {
    final isUser = message.isUser;
    return Align(
      alignment: isUser ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(12),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        decoration: BoxDecoration(
          color: isUser ? Colors.blue : Colors.grey[200],
          borderRadius: BorderRadius.circular(16),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (!isUser)
              Row(
                children: [
                  Icon(Icons.smart_toy, size: 14, color: Colors.blue[700]),
                  const SizedBox(width: 4),
                  Text(
                    'Gemini AI',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 11,
                      color: Colors.blue[700],
                    ),
                  ),
                ],
              ),
            if (!isUser) const SizedBox(height: 6),
            Text(
              message.text,
              style: TextStyle(
                color: isUser ? Colors.white : Colors.black87,
                fontSize: 14,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              _formatTime(message.timestamp),
              style: TextStyle(
                fontSize: 10,
                color: isUser ? Colors.white70 : Colors.grey[600],
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _sendMessage() async {
    final text = _messageController.text.trim();
    if (text.isEmpty || !_isConnected) return;

    // Add user message
    setState(() {
      _messages.add(ChatMessage(
        text: text,
        isUser: true,
        timestamp: DateTime.now(),
      ));
      _isLoading = true;
    });

    _messageController.clear();
    _scrollToBottom();

    // Get AI response with context
    final result = await _geminiService.chatWithContext(text);

    setState(() {
      _isLoading = false;
      if (result['success']) {
        _messages.add(ChatMessage(
          text: result['message'],
          isUser: false,
          timestamp: DateTime.now(),
        ));
      } else {
        _messages.add(ChatMessage(
          text: '❌ Xin lỗi, tôi không thể trả lời lúc này.\nLỗi: ${result['error']}',
          isUser: false,
          timestamp: DateTime.now(),
        ));
      }
    });

    _scrollToBottom();
  }

  void _handleMenuAction(String action) {
    switch (action) {
      case 'recommendations':
        _askForRecommendations();
        break;
      case 'payment':
        _askAboutPayment();
        break;
      case 'travel':
        _askForTravelTips();
        break;
      case 'clear':
        _clearChat();
        break;
    }
  }

  void _askForRecommendations() {
    _messageController.text = 'Tôi muốn tìm homestay phù hợp với ngân sách 500k/đêm tại Đà Lạt cho 2 người';
    _sendMessage();
  }

  void _askAboutPayment() {
    _messageController.text = 'Hướng dẫn tôi cách thanh toán bằng VNPay';
    _sendMessage();
  }

  void _askForTravelTips() {
    _messageController.text = 'Cho tôi mẹo du lịch Đà Nẵng tiết kiệm';
    _sendMessage();
  }

  void _clearChat() {
    setState(() {
      _messages.clear();
      _addWelcomeMessage();
    });
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Đã xóa lịch sử chat')),
    );
  }

  void _scrollToBottom() {
    Future.delayed(const Duration(milliseconds: 100), () {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  String _formatTime(DateTime time) {
    return '${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}';
  }

  @override
  void dispose() {
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }
}

class ChatMessage {
  final String text;
  final bool isUser;
  final DateTime timestamp;

  ChatMessage({
    required this.text,
    required this.isUser,
    required this.timestamp,
  });
}
