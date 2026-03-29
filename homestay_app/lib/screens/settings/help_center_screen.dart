import 'package:flutter/material.dart';

import '../../widgets/user_gradient_background.dart';

class HelpCenterScreen extends StatelessWidget {
  const HelpCenterScreen({super.key});

  @override
  Widget build(BuildContext context) {
    
    return Scaffold(
      appBar: AppBar(
        title: const Text('Trung tâm trợ giúp'),
        backgroundColor: const Color(0xFF667eea),
      ),
      body: UserGradientBackground(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: ListView(
            children: const [
              Text(
                'Trung tâm trợ giúp',
                style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Color(0xFF667eea)),
              ),
              SizedBox(height: 16),
              Text(
                'Nếu bạn cần hỗ trợ về tài khoản, đặt phòng, thanh toán hoặc các vấn đề khác, vui lòng liên hệ:\n'
                '• Email: support@homestaybooking.vn\n'
                '• Hotline: 1900-xxxxxx\n'
                '• Fanpage Facebook: facebook.com/homestaybooking\n\n'
                'Bạn cũng có thể xem các câu hỏi thường gặp hoặc gửi phản hồi trực tiếp tại đây.',
                style: TextStyle(fontSize: 16),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
