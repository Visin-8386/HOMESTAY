import 'package:flutter/material.dart';
import '../../config/api_config.dart';
import '../../widgets/homestay_image_widget.dart';

/// Screen để test hiển thị ảnh từ server
class ImageTestScreen extends StatelessWidget {
  const ImageTestScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    // Test URLs
    final testImages = [
      '/uploads/homestays/4acd68d4-a8f7-418e-911b-df7b738d5193.JPG',
      '/uploads/homestays/27f8a070-6cc8-42fd-91d7-1f6b3acea811.jpg',
      '/uploads/homestays/3023f444-b3bb-4f03-bd52-b6c0bcb6b040.JPG',
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Test Hiển Thị Ảnh'),
        backgroundColor: Colors.teal,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Hiển thị base URL
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Backend URL:',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 8),
                  SelectableText(
                    ApiConfig.baseUrl,
                    style: const TextStyle(
                      fontSize: 14,
                      color: Colors.blue,
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          
          // Test các ảnh
          const Text(
            'Test Load Ảnh:',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 16),
          
          ...testImages.map((relativePath) {
            final fullUrl = ApiConfig.baseUrl + relativePath;
            
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(8),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Đường dẫn: $relativePath',
                          style: const TextStyle(fontSize: 12),
                        ),
                        const SizedBox(height: 4),
                        SelectableText(
                          'Full URL: $fullUrl',
                          style: const TextStyle(
                            fontSize: 10,
                            color: Colors.grey,
                          ),
                        ),
                        const SizedBox(height: 8),
                        
                        // Test với HomestayImageWidget
                        HomestayImageWidget(
                          imageUrl: fullUrl,
                          height: 200,
                          fit: BoxFit.cover,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        
                        const SizedBox(height: 8),
                        
                        // Nút copy URL
                        ElevatedButton.icon(
                          onPressed: () {
                            // Copy to clipboard
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text('Đã copy: $fullUrl'),
                                duration: const Duration(seconds: 2),
                              ),
                            );
                          },
                          icon: const Icon(Icons.copy, size: 16),
                          label: const Text('Copy URL'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.teal,
                            foregroundColor: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
              ],
            );
          }).toList(),
          
          // Test placeholder
          const Text(
            'Test Placeholder (không có ảnh):',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(8),
              child: NoImagePlaceholder(
                height: 200,
                borderRadius: BorderRadius.circular(8),
              ),
            ),
          ),
          
          const SizedBox(height: 16),
          
          // Hướng dẫn
          Card(
            color: Colors.blue[50],
            child: const Padding(
              padding: EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '💡 Hướng dẫn kiểm tra:',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  SizedBox(height: 8),
                  Text('1. Kiểm tra backend đang chạy'),
                  Text('2. Xem ảnh có hiển thị không'),
                  Text('3. Nếu lỗi, xem thông báo lỗi màu đỏ'),
                  Text('4. Copy URL và test trong browser'),
                  Text('5. Kiểm tra console Flutter có log gì'),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
