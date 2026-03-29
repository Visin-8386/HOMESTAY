import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../config/app_colors.dart';
import '../../widgets/user_gradient_background.dart';

class AboutScreen extends StatefulWidget {
  const AboutScreen({super.key});

  @override
  State<AboutScreen> createState() => _AboutScreenState();
}

class _AboutScreenState extends State<AboutScreen> {
  bool _showHero = false;
  bool _showStories = false;
  bool _showValues = false;
  bool _showTeam = false;
  bool _showCta = false;

  @override
  void initState() {
    super.initState();
    // Staggered reveal
    Future.delayed(const Duration(milliseconds: 120), () => setState(() => _showHero = true));
    Future.delayed(const Duration(milliseconds: 320), () => setState(() => _showStories = true));
    Future.delayed(const Duration(milliseconds: 620), () => setState(() => _showValues = true));
    Future.delayed(const Duration(milliseconds: 920), () => setState(() => _showTeam = true));
    Future.delayed(const Duration(milliseconds: 1220), () => setState(() => _showCta = true));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: AppBar(
        title: const Text('Về chúng tôi'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
      ),
      body: UserGradientBackground(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              // Hero
              AnimatedOpacity(
                duration: const Duration(milliseconds: 400),
                opacity: _showHero ? 1 : 0,
                child: AnimatedSlide(
                  duration: const Duration(milliseconds: 400),
                  offset: _showHero ? Offset.zero : const Offset(0, 0.03),
                  child: Container(
                    padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 14),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withAlpha(15),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Column(
                      children: <Widget>[
                        const Text(
                          '✨ Đom Đóm Dream ✨',
                          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: AppColors.primary),
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 8),
                        const Text(
                          'Nơi mỗi chuyến đi trở thành kỷ niệm đẹp,\nnơi mỗi ngôi nhà trở thành mái ấm thứ hai.',
                          style: TextStyle(fontSize: 14, color: Colors.black87),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                ),
              ),

              const SizedBox(height: 20),

              // Story header
              const Text(
                '📖 Câu Chuyện Của Chúng Tôi',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppColors.primary),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 12),

              // Stories
              AnimatedOpacity(
                duration: const Duration(milliseconds: 400),
                opacity: _showStories ? 1 : 0,
                child: AnimatedSlide(
                  duration: const Duration(milliseconds: 400),
                  offset: _showStories ? Offset.zero : const Offset(0, 0.02),
                  child: Column(
                    children: <Widget>[
                      _buildStoryCard(
                        imageUrl:
                            'https://images.unsplash.com/photo-1571896349842-33c89424de2d?auto=format&fit=crop&w=800&q=80',
                        title: '🌟 Khởi Nguồn Đom Đóm',
                        content:
                            'Trong một đêm hè đầy sao, ý tưởng về Đom Đóm Dream được sinh ra. Chúng tôi tin rằng mỗi chuyến đi đều có thể mang lại ánh sáng nhỏ bé nhưng ý nghĩa.',
                        badge: '2020',
                      ),
                      const SizedBox(height: 12),
                      _buildStoryCard(
                        imageUrl:
                            'https://images.unsplash.com/photo-1521747116042-5a810fda9664?auto=format&fit=crop&w=800&q=80',
                        title: '❤️ Xây Dựng Cộng Đồng',
                        content:
                            'Chúng tôi xây dựng một cộng đồng ấm áp nơi mỗi homestay là một câu chuyện, mỗi chủ nhà là một người bạn.',
                      ),
                      const SizedBox(height: 12),
                      _buildStoryCard(
                        imageUrl:
                            'https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=800&q=80',
                        title: '✨ Trải Nghiệm Độc Đáo',
                        content:
                            'Từ những ngôi nhà nhỏ ấm cúng đến những villa sang trọng, mỗi nơi đều chứa đựng câu chuyện riêng.',
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 20),

              // Values + Stats
              AnimatedOpacity(
                duration: const Duration(milliseconds: 400),
                opacity: _showValues ? 1 : 0,
                child: AnimatedSlide(
                  duration: const Duration(milliseconds: 400),
                  offset: _showValues ? Offset.zero : const Offset(0, 0.02),
                  child: Column(
                    children: <Widget>[
                      const Text('💎 Giá Trị Cốt Lõi',
                          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppColors.primary)),
                      const SizedBox(height: 12),
                      Row(
                        children: <Widget>[
                          Expanded(
                            child: _buildValueCard(
                              emoji: '💝',
                              title: 'Tận Tâm',
                              content:
                                  'Chúng tôi đặt trái tim vào mỗi dịch vụ để mang lại trải nghiệm hoàn hảo cho khách hàng.',
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: _buildValueCard(
                              emoji: '🤝',
                              title: 'Tin Cậy',
                              content:
                                  'Minh bạch và cam kết bảo vệ quyền lợi của khách hàng và chủ nhà.',
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: _buildValueCard(
                              emoji: '🚀',
                              title: 'Đổi Mới',
                              content:
                                  'Không ngừng cải tiến và áp dụng công nghệ mới để tạo ra giải pháp sáng tạo.',
                            ),
                          ),
                        ],
                      ),

                      const SizedBox(height: 16),

                      Row(
                        children: <Widget>[
                          _buildAnimatedStat(10000, 'Homestay'),
                          const SizedBox(width: 8),
                          _buildAnimatedStat(50000, 'Khách hàng'),
                          const SizedBox(width: 8),
                          _buildAnimatedStat(100, 'Địa điểm'),
                        ],
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 20),

              // Team
              AnimatedOpacity(
                duration: const Duration(milliseconds: 400),
                opacity: _showTeam ? 1 : 0,
                child: AnimatedSlide(
                  duration: const Duration(milliseconds: 400),
                  offset: _showTeam ? Offset.zero : const Offset(0, 0.02),
                  child: Column(
                    children: <Widget>[
                      const Text('👥 Đội Ngũ Đom Đóm',
                          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppColors.primary)),
                      const SizedBox(height: 12),
                      Wrap(
                        spacing: 12,
                        runSpacing: 12,
                        children: <Widget>[
                          _buildTeamCard(avatarEmoji: '👨‍💼', name: 'Lê Hoàng', role: 'CEO & Founder', quote: 'Tôi tin rằng du lịch không chỉ là đi từ nơi này đến nơi khác, mà là hành trình khám phá chính mình.'),
                          _buildTeamCard(avatarEmoji: '👩‍💻', name: 'Phạm Hoàng Minh', role: 'CTO', quote: 'Công nghệ là cầu nối giúp chúng ta kết nối những trái tim yêu thích khám phá thế giới.'),
                          _buildTeamCard(avatarEmoji: '👨‍🎨', name: 'Thạch Hoàng Thành', role: 'Head of Design', quote: 'Mỗi thiết kế đều mang trong mình một câu chuyện, một cảm xúc mà chúng tôi muốn truyền tải.'),
                          _buildTeamCard(avatarEmoji: '👩‍🏢', name: 'Nguyễn Hoàng Thiên Ân', role: 'Customer Success', quote: 'Hạnh phúc của khách hàng chính là động lực để chúng tôi không ngừng cải thiện dịch vụ.'),
                          _buildTeamCard(avatarEmoji: '👩‍🎓', name: 'Lê Thị Mỹ Duyên', role: 'Community & Content', quote: 'Đóng góp nội dung và hỗ trợ cộng đồng để chia sẻ nhiều hơn những trải nghiệm địa phương.'),
                        ],
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 20),

              // CTA
              AnimatedOpacity(
                duration: const Duration(milliseconds: 400),
                opacity: _showCta ? 1 : 0,
                child: AnimatedSlide(
                  duration: const Duration(milliseconds: 400),
                  offset: _showCta ? Offset.zero : const Offset(0, 0.02),
                  child: Card(
                    elevation: 2,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        children: <Widget>[
                          const Text('🌟 Cùng Chúng Tôi Thắp Sáng Ước Mơ', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.primary)),
                          const SizedBox(height: 8),
                          const Text('Hãy trở thành một phần của cộng đồng Đom Đóm Dream. Chia sẻ ngôi nhà của bạn hoặc khám phá những trải nghiệm mới.', textAlign: TextAlign.center),
                          const SizedBox(height: 12),
                          // Use a Wrap here so buttons flow to the next line on narrow screens
                          Wrap(
                            alignment: WrapAlignment.center,
                            spacing: 12,
                            runSpacing: 8,
                            children: <Widget>[
                              ElevatedButton.icon(
                                onPressed: () {},
                                icon: const Icon(Icons.person_add),
                                label: const Text('Trở Thành Host'),
                                style: ElevatedButton.styleFrom(backgroundColor: AppColors.primary),
                              ),
                              OutlinedButton.icon(
                                onPressed: () {},
                                icon: const Icon(Icons.search),
                                label: const Text('Khám Phá Homestay'),
                              ),
                            ],
                          )
                        ],
                      ),
                    ),
                  ),
                ),
              ),

              const SizedBox(height: 20),

              // Contact row (uses the launch helpers so they are referenced)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Center(
                  child: Wrap(
                    alignment: WrapAlignment.center,
                    spacing: 12,
                    runSpacing: 8,
                    children: <Widget>[
                      ElevatedButton.icon(
                        onPressed: _launchEmail,
                        icon: const Icon(Icons.email),
                        label: const Text('Email'),
                        style: ElevatedButton.styleFrom(backgroundColor: AppColors.primary),
                      ),
                      ElevatedButton.icon(
                        onPressed: _launchPhone,
                        icon: const Icon(Icons.phone),
                        label: const Text('Gọi'),
                      ),
                      OutlinedButton.icon(
                        onPressed: () => _launchSocial('facebook'),
                        icon: const Icon(Icons.facebook),
                        label: const Text('Facebook'),
                      ),
                    ],
                  ),
                ),
              ),

              // Footer
              const Text('Phiên bản 1.0.0', style: TextStyle(color: Color(0xFF9E9E9E), fontSize: 14), textAlign: TextAlign.center),
              const SizedBox(height: 8),
              const Text('© 2024 Đom Đóm Dream. Tất cả quyền được bảo lưu.', style: TextStyle(color: Color(0xFF9E9E9E), fontSize: 12), textAlign: TextAlign.center),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }

  // Helpers
  Widget _buildStoryCard({required String imageUrl, required String title, required String content, String? badge}) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ClipRRect(borderRadius: const BorderRadius.vertical(top: Radius.circular(12)), child: Image.network(imageUrl, height: 140, fit: BoxFit.cover)),
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(children: [Expanded(child: Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold))), if (badge != null) Container(padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4), decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(8)), child: Text(badge, style: const TextStyle(color: Colors.white, fontSize: 12)))]),
              const SizedBox(height: 8),
              Text(content, style: const TextStyle(color: Color(0xFF616161), height: 1.4)),
            ]),
          ),
        ],
      ),
    );
  }

  Widget _buildValueCard({required String emoji, required String title, required String content}) {
    return Card(
      elevation: 1,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      child: Padding(padding: const EdgeInsets.all(12), child: Column(children: [Text(emoji, style: const TextStyle(fontSize: 28)), const SizedBox(height: 8), Text(title, style: const TextStyle(fontWeight: FontWeight.bold), textAlign: TextAlign.center), const SizedBox(height: 6), Text(content, style: TextStyle(color: Colors.grey[700], fontSize: 12), textAlign: TextAlign.center)])),
    );
  }

  Widget _buildAnimatedStat(int targetValue, String label) {
    return Expanded(
      child: Card(
        elevation: 1,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: TweenAnimationBuilder<double>(
            tween: Tween(begin: 0.0, end: targetValue.toDouble()),
            duration: const Duration(seconds: 2),
            builder: (context, val, child) {
              final int v = val.toInt();
              final String display = targetValue >= 1000 ? '${(v / 1000).toStringAsFixed(0)}K+' : v.toString();
              return Column(
                children: [
                  Text(
                    display,
                    style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: AppColors.primary),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    label,
                    style: const TextStyle(color: Color(0xFF757575), fontSize: 12),
                    textAlign: TextAlign.center,
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _buildTeamCard({required String avatarEmoji, required String name, required String role, required String quote}) {
    return SizedBox(
      width: 170,
      child: Card(
        elevation: 1,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            children: [
              CircleAvatar(radius: 28, backgroundColor: AppColors.primary.withAlpha(25), child: Text(avatarEmoji, style: const TextStyle(fontSize: 20))),
              const SizedBox(height: 8),
              Text(name, style: const TextStyle(fontWeight: FontWeight.w600), textAlign: TextAlign.center),
              const SizedBox(height: 4),
              Text(role, style: const TextStyle(color: AppColors.primary, fontSize: 12), textAlign: TextAlign.center),
              const SizedBox(height: 8),
              Text(quote, style: const TextStyle(color: Color(0xFF757575), fontSize: 12), textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    );
  }

  // Contact / social helpers
  Future<void> _launchEmail() async {
    final Uri emailUri = Uri(scheme: 'mailto', path: 'support@homestaybooking.vn', queryParameters: {'subject': 'Hỗ trợ Homestay Booking'});
    if (await canLaunchUrl(emailUri)) await launchUrl(emailUri);
  }

  Future<void> _launchPhone() async {
    final Uri phoneUri = Uri(scheme: 'tel', path: '1900XXXXXX');
    if (await canLaunchUrl(phoneUri)) await launchUrl(phoneUri);
  }

  Future<void> _launchSocial(String platform) async {
    String url = '';
    if (platform == 'facebook') url = 'https://www.facebook.com/homestaybooking';
    if (platform == 'instagram') url = 'https://www.instagram.com/homestaybooking';
    if (platform == 'youtube') url = 'https://www.youtube.com/@homestaybooking';
    if (url.isNotEmpty) {
      final Uri uri = Uri.parse(url);
      if (await canLaunchUrl(uri)) await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }
}
