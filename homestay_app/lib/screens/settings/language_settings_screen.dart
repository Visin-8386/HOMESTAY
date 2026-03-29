import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/app_colors.dart';
import '../../providers/locale_provider.dart';
import '../../widgets/user_gradient_background.dart';

class LanguageSettingsScreen extends StatelessWidget {
  const LanguageSettingsScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Ngôn ngữ / Language'),
        backgroundColor: AppColors.primary,
      ),
      body: UserGradientBackground(
        child: Consumer<LocaleProvider>(
          builder: (context, localeProvider, child) {
            return ListView(
              children: [
                RadioListTile<String>(
                  title: const Row(
                    children: [
                      Text('🇻🇳', style: TextStyle(fontSize: 24)),
                      SizedBox(width: 12),
                      Text(
                        'Tiếng Việt',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                  subtitle: const Padding(
                    padding: EdgeInsets.only(left: 36),
                    child: Text('Vietnamese'),
                  ),
                  value: 'vi',
                  groupValue: localeProvider.locale.languageCode,
                  onChanged: (value) {
                    localeProvider.setLocale(const Locale('vi'));
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Đã chuyển sang tiếng Việt'),
                        duration: Duration(seconds: 1),
                      ),
                    );
                  },
                  activeColor: AppColors.primary,
                ),
                const Divider(height: 1),
                RadioListTile<String>(
                  title: const Row(
                    children: [
                      Text('🇬🇧', style: TextStyle(fontSize: 24)),
                      SizedBox(width: 12),
                      Text(
                        'English',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                  subtitle: const Padding(
                    padding: EdgeInsets.only(left: 36),
                    child: Text('English'),
                  ),
                  value: 'en',
                  groupValue: localeProvider.locale.languageCode,
                  onChanged: (value) {
                    localeProvider.setLocale(const Locale('en'));
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Language changed to English'),
                        duration: Duration(seconds: 1),
                      ),
                    );
                  },
                  activeColor: AppColors.primary,
                ),
                const Divider(height: 1),
                const SizedBox(height: 24),
                const Padding(
                  padding: EdgeInsets.all(16),
                  child: Text(
                    'Thay đổi ngôn ngữ sẽ được áp dụng cho toàn bộ ứng dụng.\n\nLanguage changes will apply to the entire app.',
                    style: TextStyle(
                      fontSize: 14,
                      color: Colors.grey,
                      height: 1.5,
                    ),
                    textAlign: TextAlign.center,
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
