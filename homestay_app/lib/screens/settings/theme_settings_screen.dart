import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../providers/theme_provider.dart';
import '../../widgets/user_gradient_background.dart';

class ThemeSettingsScreen extends StatelessWidget {
  const ThemeSettingsScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Giao diện / Theme'),
        backgroundColor: AppColors.primary,
      ),
      body: UserGradientBackground(
        child: Consumer<ThemeProvider>(
          builder: (context, themeProvider, child) {
            return ListView(
              children: [
                const SizedBox(height: 16),
                RadioListTile<ThemeMode>(
                  title: const Row(
                    children: [
                      Icon(Icons.light_mode, color: Colors.orange, size: 24),
                      SizedBox(width: 12),
                      Text(
                        'Sáng / Light',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                  subtitle: const Padding(
                    padding: EdgeInsets.only(left: 36),
                    child: Text('Giao diện sáng'),
                  ),
                  value: ThemeMode.light,
                  groupValue: themeProvider.themeMode,
                  onChanged: (value) {
                    themeProvider.setThemeMode(ThemeMode.light);
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Đã chuyển sang giao diện sáng'),
                        duration: Duration(seconds: 1),
                      ),
                    );
                  },
                  activeColor: AppColors.primary,
                ),
                const Divider(height: 1),
                RadioListTile<ThemeMode>(
                  title: const Row(
                    children: [
                      Icon(Icons.dark_mode, color: Colors.indigo, size: 24),
                      SizedBox(width: 12),
                      Text(
                        'Tối / Dark',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                  subtitle: const Padding(
                    padding: EdgeInsets.only(left: 36),
                    child: Text('Giao diện tối'),
                  ),
                  value: ThemeMode.dark,
                  groupValue: themeProvider.themeMode,
                  onChanged: (value) {
                    themeProvider.setThemeMode(ThemeMode.dark);
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Đã chuyển sang giao diện tối'),
                        duration: Duration(seconds: 1),
                      ),
                    );
                  },
                  activeColor: AppColors.primary,
                ),
                const Divider(height: 1),
                RadioListTile<ThemeMode>(
                  title: const Row(
                    children: [
                      Icon(Icons.brightness_auto, color: Colors.blue, size: 24),
                      SizedBox(width: 12),
                      Text(
                        'Tự động / Auto',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                  subtitle: const Padding(
                    padding: EdgeInsets.only(left: 36),
                    child: Text('Theo hệ thống'),
                  ),
                  value: ThemeMode.system,
                  groupValue: themeProvider.themeMode,
                  onChanged: (value) {
                    themeProvider.setThemeMode(ThemeMode.system);
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Giao diện sẽ theo cài đặt hệ thống'),
                        duration: Duration(seconds: 2),
                      ),
                    );
                  },
                  activeColor: AppColors.primary,
                ),
                const Divider(height: 1),
                const SizedBox(height: 24),
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Card(
                    elevation: 2,
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Icon(
                                Icons.info_outline,
                                color: Colors.blue[700],
                                size: 20,
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'Thông tin',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.blue[700],
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          const Text(
                            '• Giao diện sáng: Phù hợp cho ban ngày\n\n'
                            '• Giao diện tối: Giảm căng thẳng mắt vào ban đêm\n\n'
                            '• Tự động: Thay đổi theo cài đặt hệ thống của bạn\n\n'
                            'Cài đặt sẽ được lưu và áp dụng cho toàn bộ ứng dụng.',
                            style: TextStyle(
                              fontSize: 14,
                              height: 1.5,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Card(
                    color: themeProvider.isDarkMode 
                        ? const Color(0xFF2C2C2C) 
                        : Colors.white,
                    elevation: 2,
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Xem trước',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 12),
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppColors.primary.withOpacity(0.1),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: const Row(
                              children: [
                                Icon(Icons.home, color: AppColors.primary),
                                SizedBox(width: 12),
                                Text('Đây là giao diện hiện tại'),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}
