import 'package:flutter/material.dart';

import '../../config/app_colors.dart';
import '../../widgets/user_gradient_background.dart';

class TermsOfServiceScreen extends StatelessWidget {
  const TermsOfServiceScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Điều khoản sử dụng'),
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
                'Điều khoản và Điều kiện Sử dụng',
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
                '1. Chấp nhận Điều khoản',
                'Bằng việc truy cập và sử dụng ứng dụng Homestay Booking, bạn đồng ý tuân thủ '
                'các điều khoản và điều kiện được nêu trong tài liệu này. Nếu bạn không đồng ý với '
                'bất kỳ điều khoản nào, vui lòng không sử dụng dịch vụ của chúng tôi.',
              ),

              _buildSection(
                '2. Mô tả Dịch vụ',
                'Homestay Booking là nền tảng kết nối du khách với các homestay trên toàn quốc. '
                'Chúng tôi cung cấp:\n\n'
                '• Tìm kiếm và đặt phòng homestay\n'
                '• Xử lý thanh toán an toàn\n'
                '• Quản lý đặt phòng và đánh giá\n'
                '• Hỗ trợ khách hàng 24/7',
              ),

              _buildSection(
                '3. Điều kiện Sử dụng',
                'Để sử dụng dịch vụ, bạn phải:\n\n'
                '• Từ đủ 18 tuổi hoặc có sự đồng ý của phụ huynh\n'
                '• Cung cấp thông tin chính xác và cập nhật\n'
                '• Sử dụng dịch vụ cho mục đích hợp pháp\n'
                '• Tuân thủ các quy định của pháp luật Việt Nam\n'
                '• Không vi phạm quyền của bên thứ ba',
              ),

              _buildSection(
                '4. Tài khoản Người dùng',
                'Khi tạo tài khoản, bạn cam kết:\n\n'
                '• Cung cấp thông tin chính xác\n'
                '• Bảo mật thông tin đăng nhập\n'
                '• Thông báo ngay khi phát hiện lạm dụng\n'
                '• Chịu trách nhiệm về tất cả hoạt động trên tài khoản\n'
                '• Không chia sẻ tài khoản với người khác',
              ),

              _buildSection(
                '5. Chính sách Đặt phòng',
                'Việc đặt phòng qua Homestay Booking:\n\n'
                '• Đặt phòng sẽ được xác nhận sau khi thanh toán thành công\n'
                '• Giá phòng có thể thay đổi theo thời gian\n'
                '• Hủy phòng theo chính sách của từng homestay\n'
                '• Thay đổi đặt phòng phải được chủ homestay chấp thuận\n'
                '• Chúng tôi không chịu trách nhiệm về chất lượng dịch vụ thực tế',
              ),

              _buildSection(
                '6. Chính sách Hủy phòng',
                '• Hủy miễn phí 24h trước check-in\n'
                '• Phí hủy 50% từ 24h đến 12h trước check-in\n'
                '• Phí hủy 100% trong vòng 12h trước check-in\n'
                '• Chính sách có thể khác nhau theo từng homestay\n'
                '• Hoàn tiền trong vòng 7-14 ngày làm việc',
              ),

              _buildSection(
                '7. Thanh toán',
                'Chúng tôi chấp nhận:\n\n'
                '• Thẻ tín dụng/ghi nợ (Visa, Mastercard)\n'
                '• Ví điện tử (MoMo, ZaloPay)\n'
                '• Chuyển khoản ngân hàng\n'
                '• PayPal\n\n'
                'Tất cả giao dịch đều được mã hóa và bảo mật.',
              ),

              _buildSection(
                '8. Quyền và Nghĩa vụ',
                'Người dùng có quyền:\n\n'
                '• Sử dụng dịch vụ miễn phí để tìm kiếm\n'
                '• Đặt phòng và thanh toán an toàn\n'
                '• Gửi đánh giá và góp ý\n'
                '• Nhận hỗ trợ khách hàng\n\n'
                'Người dùng có nghĩa vụ:\n\n'
                '• Tuân thủ điều khoản sử dụng\n'
                '• Thanh toán đầy đủ cho đặt phòng\n'
                '• Tôn trọng chủ homestay và cộng đồng\n'
                '• Cung cấp thông tin chính xác',
              ),

              _buildSection(
                '9. Nội dung và Quyền Sở hữu Trí tuệ',
                '• Tất cả nội dung trên ứng dụng thuộc sở hữu của chúng tôi\n'
                '• Ảnh homestay thuộc sở hữu của chủ homestay\n'
                '• Không được sao chép hoặc sử dụng thương mại\n'
                '• Vi phạm sẽ bị xử lý theo pháp luật',
              ),

              _buildSection(
                '10. Từ chối Bảo đảm',
                'Chúng tôi không đảm bảo:\n\n'
                '• Dịch vụ luôn khả dụng 100%\n'
                '• Thông tin homestay luôn chính xác\n'
                '• Không có lỗi kỹ thuật\n'
                '• An toàn tuyệt đối của dữ liệu\n\n'
                'Dịch vụ được cung cấp "như hiện tại".',
              ),

              _buildSection(
                '11. Giới hạn Trách nhiệm',
                'Chúng tôi không chịu trách nhiệm về:\n\n'
                '• Thiệt hại gián tiếp hoặc đặc biệt\n'
                '• Mất mát dữ liệu hoặc lợi nhuận\n'
                '• Gián đoạn kinh doanh\n'
                '• Hành vi của chủ homestay\n'
                '• Sự cố bất khả kháng',
              ),

              _buildSection(
                '12. Chấm dứt Dịch vụ',
                'Chúng tôi có quyền:\n\n'
                '• Tạm ngừng hoặc chấm dứt tài khoản\n'
                '• Từ chối dịch vụ cho người dùng vi phạm\n'
                '• Thay đổi điều khoản với thông báo trước\n'
                '• Ngừng cung cấp dịch vụ với lý do chính đáng',
              ),

              _buildSection(
                '13. Luật áp dụng',
                '• Điều khoản này được điều chỉnh bởi pháp luật Việt Nam\n'
                '• Mọi tranh chấp sẽ được giải quyết tại tòa án Việt Nam\n'
                '• Ưu tiên giải quyết hòa bình trước khi khởi kiện',
              ),

              _buildSection(
                '14. Liên hệ',
                'Nếu bạn có câu hỏi về điều khoản sử dụng:\n\n'
                'Email: legal@homestaybooking.vn\n'
                'Hotline: 1900 XXX XXX\n'
                'Địa chỉ: Hà Nội, Việt Nam',
              ),

              const SizedBox(height: 32),

              Center(
                child: Text(
                  'Cảm ơn bạn đã sử dụng Homestay Booking!',
                  style: TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w500,
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
