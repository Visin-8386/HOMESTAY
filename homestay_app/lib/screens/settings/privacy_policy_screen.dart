import 'package:flutter/material.dart';

import '../../config/app_colors.dart';
import '../../widgets/user_gradient_background.dart';

class PrivacyPolicyScreen extends StatelessWidget {
  const PrivacyPolicyScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Chính sách bảo mật'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
      ),
      body: UserGradientBackground(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Chính sách Bảo mật Thông tin',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: AppColors.primary,
                ),
              ),

              const SizedBox(height: 16),

              const Text(
                'Cập nhật lần cuối: 17 tháng 10, 2024',
                style: TextStyle(
                  color: Colors.grey,
                  fontSize: 12,
                ),
              ),

              const SizedBox(height: 24),

              _buildSection(
                '1. Cam kết Bảo mật',
                'Homestay Booking cam kết bảo vệ thông tin cá nhân của người dùng. '
                'Chúng tôi thu thập, sử dụng và bảo vệ thông tin của bạn theo đúng '
                'quy định của pháp luật Việt Nam và các tiêu chuẩn quốc tế về bảo mật dữ liệu.',
              ),

              _buildSection(
                '2. Thông tin Chúng tôi Thu thập',
                'Chúng tôi có thể thu thập các loại thông tin sau:\n\n'
                '• Thông tin cá nhân: Họ tên, email, số điện thoại, địa chỉ\n'
                '• Thông tin đăng nhập: Tên đăng nhập, mật khẩu (đã mã hóa)\n'
                '• Thông tin thanh toán: Thông tin thẻ tín dụng (qua cổng thanh toán an toàn)\n'
                '• Thông tin sử dụng: Lịch sử đặt phòng, đánh giá, sở thích\n'
                '• Thông tin kỹ thuật: Địa chỉ IP, loại thiết bị, trình duyệt',
              ),

              _buildSection(
                '3. Mục đích Sử dụng Thông tin',
                'Thông tin của bạn được sử dụng để:\n\n'
                '• Cung cấp dịch vụ đặt phòng homestay\n'
                '• Xử lý thanh toán và xác nhận đặt phòng\n'
                '• Gửi thông báo về đặt phòng và khuyến mãi\n'
                '• Cải thiện trải nghiệm người dùng\n'
                '• Tuân thủ các quy định pháp luật\n'
                '• Ngăn chặn gian lận và lạm dụng',
              ),

              _buildSection(
                '4. Chia sẻ Thông tin',
                'Chúng tôi cam kết không bán, trao đổi hoặc cho thuê thông tin cá nhân của bạn cho bên thứ ba, trừ các trường hợp sau:\n\n'
                '• Với sự đồng ý của bạn\n'
                '• Để cung cấp dịch vụ (ví dụ: chia sẻ thông tin với chủ homestay)\n'
                '• Theo yêu cầu pháp luật\n'
                '• Để bảo vệ quyền lợi hợp pháp của chúng tôi\n'
                '• Trong trường hợp sáp nhập hoặc bán doanh nghiệp',
              ),

              _buildSection(
                '5. Bảo mật Thông tin',
                'Chúng tôi áp dụng các biện pháp bảo mật tiên tiến:\n\n'
                '• Mã hóa dữ liệu trong quá trình truyền tải (HTTPS)\n'
                '• Lưu trữ dữ liệu an toàn với mã hóa\n'
                '• Kiểm soát truy cập nghiêm ngặt\n'
                '• Giám sát và cập nhật hệ thống bảo mật thường xuyên\n'
                '• Đào tạo nhân viên về bảo mật thông tin',
              ),

              _buildSection(
                '6. Thời gian Lưu trữ',
                'Chúng tôi chỉ lưu trữ thông tin của bạn trong thời gian cần thiết:\n\n'
                '• Thông tin tài khoản: Trong suốt thời gian bạn sử dụng dịch vụ\n'
                '• Lịch sử đặt phòng: 5 năm sau khi đặt phòng hoàn thành\n'
                '• Thông tin thanh toán: Theo quy định của pháp luật về thanh toán\n'
                '• Dữ liệu phân tích: 2 năm sau khi thu thập',
              ),

              _buildSection(
                '7. Quyền của Người dùng',
                'Bạn có các quyền sau đối với thông tin cá nhân:\n\n'
                '• Truy cập và xem thông tin của mình\n'
                '• Sửa đổi thông tin cá nhân\n'
                '• Yêu cầu xóa thông tin\n'
                '• Từ chối nhận thông tin marketing\n'
                '• Khiếu nại về việc xử lý thông tin\n'
                '• Yêu cầu chuyển giao dữ liệu',
              ),

              _buildSection(
                '8. Cookie và Công nghệ Theo dõi',
                'Chúng tôi sử dụng cookie để:\n\n'
                '• Cải thiện trải nghiệm người dùng\n'
                '• Ghi nhớ thông tin đăng nhập\n'
                '• Phân tích lưu lượng truy cập\n'
                '• Cá nhân hóa nội dung và quảng cáo\n\n'
                'Bạn có thể quản lý cài đặt cookie trong trình duyệt của mình.',
              ),

              _buildSection(
                '9. Thông tin Thanh toán',
                'Thông tin thanh toán được xử lý qua các cổng thanh toán uy tín:\n\n'
                '• PayPal với tiêu chuẩn bảo mật PCI DSS\n'
                '• Các ngân hàng Việt Nam với bảo mật cao\n'
                '• Chúng tôi không lưu trữ thông tin thẻ tín dụng\n'
                '• Tất cả giao dịch đều được mã hóa SSL/TLS',
              ),

              _buildSection(
                '10. Chính sách Đối với Trẻ em',
                'Dịch vụ của chúng tôi dành cho người từ 18 tuổi trở lên. '
                'Chúng tôi không cố ý thu thập thông tin cá nhân từ trẻ em dưới 18 tuổi. '
                'Nếu phát hiện có thông tin của trẻ em, chúng tôi sẽ xóa ngay lập tức.',
              ),

              _buildSection(
                '11. Thay đổi Chính sách',
                'Chúng tôi có thể cập nhật chính sách bảo mật này. '
                'Khi có thay đổi quan trọng, chúng tôi sẽ:\n\n'
                '• Thông báo qua email\n'
                '• Đăng thông báo trên ứng dụng\n'
                '• Yêu cầu xác nhận đồng ý nếu cần thiết\n\n'
                'Việc tiếp tục sử dụng dịch vụ sau khi có thay đổi đồng nghĩa với việc chấp nhận chính sách mới.',
              ),

              _buildSection(
                '12. Liên hệ Về Bảo mật',
                'Nếu bạn có câu hỏi về chính sách bảo mật:\n\n'
                '• Email: privacy@homestaybooking.vn\n'
                '• Hotline: 1900 XXX XXX\n'
                '• Địa chỉ: Hà Nội, Việt Nam\n\n'
                'Chúng tôi cam kết phản hồi trong vòng 48 giờ.',
              ),

              _buildSection(
                '13. Tuân thủ Pháp luật',
                'Chính sách này tuân thủ:\n\n'
                '• Luật An toàn thông tin mạng Việt Nam\n'
                '• Nghị định về Bảo vệ dữ liệu cá nhân\n'
                '• GDPR (đối với người dùng châu Âu)\n'
                '• Các tiêu chuẩn quốc tế về bảo mật dữ liệu',
              ),

              const SizedBox(height: 32),

              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.blue[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.blue[200]!),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      '📞 Hỗ trợ Khách hàng',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Colors.blue,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Nếu bạn cần hỗ trợ về tài khoản, đặt phòng, hoặc có thắc mắc về bảo mật thông tin, đừng ngần ngại liên hệ với chúng tôi.',
                      style: TextStyle(
                        color: Colors.blue,
                        height: 1.5,
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 32),

              Center(
                child: Text(
                  'Cảm ơn bạn đã tin tưởng Homestay Booking!',
                  style: TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w500,
                    fontSize: 16,
                  ),
                  textAlign: TextAlign.center,
                ),
              ),

              const SizedBox(height: 16),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSection(String title, String content) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            color: AppColors.primary,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          content,
          style: TextStyle(
            fontSize: 14,
            color: Colors.grey[700],
            height: 1.6,
          ),
        ),
        const SizedBox(height: 24),
      ],
    );
  }
}
