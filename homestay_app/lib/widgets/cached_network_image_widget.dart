import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../utils/image_helper.dart';

/// Widget to display cached network images with fallback
class CachedNetworkImageWidget extends StatelessWidget {
  final String? imageUrl;
  final double? width;
  final double? height;
  final BoxFit fit;
  final String? placeholder;
  final BorderRadius? borderRadius;

  const CachedNetworkImageWidget({
    Key? key,
    required this.imageUrl,
    this.width,
    this.height,
    this.fit = BoxFit.cover,
    this.placeholder,
    this.borderRadius,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    final fullUrl = ImageHelper.getFullImageUrl(imageUrl);
    final placeholderUrl = placeholder ?? ImageHelper.homestayPlaceholder;

    Widget imageWidget = CachedNetworkImage(
      imageUrl: fullUrl,
      width: width,
      height: height,
      fit: fit,
      placeholder: (context, url) => Container(
        width: width,
        height: height,
        color: Colors.grey[200],
        child: const Center(
          child: CircularProgressIndicator(
            strokeWidth: 2,
          ),
        ),
      ),
      errorWidget: (context, url, error) {
        // Try to load placeholder instead
        return CachedNetworkImage(
          imageUrl: placeholderUrl,
          width: width,
          height: height,
          fit: fit,
          errorWidget: (context, url, error) {
            // If placeholder also fails, show icon
            return Container(
              width: width,
              height: height,
              color: Colors.grey[300],
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.home_outlined,
                    size: 50,
                    color: Colors.grey[600],
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Không thể tải ảnh',
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );

    if (borderRadius != null) {
      return ClipRRect(
        borderRadius: borderRadius!,
        child: imageWidget,
      );
    }

    return imageWidget;
  }
}

/// Widget for circular avatar images
class AvatarImage extends StatelessWidget {
  final String? imageUrl;
  final double radius;

  const AvatarImage({
    Key? key,
    required this.imageUrl,
    this.radius = 30,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    final fullUrl = ImageHelper.getFullImageUrl(imageUrl);

    return CircleAvatar(
      radius: radius,
      backgroundColor: Colors.grey[300],
      backgroundImage: CachedNetworkImageProvider(fullUrl),
      onBackgroundImageError: (exception, stackTrace) {
        // Fallback already handled by CachedNetworkImageProvider
      },
      child: imageUrl == null || imageUrl!.isEmpty
          ? Icon(
              Icons.person,
              size: radius,
              color: Colors.grey[600],
            )
          : null,
    );
  }
}

/// Widget for homestay image gallery
class HomestayImageGallery extends StatelessWidget {
  final List<String> imageUrls;
  final double height;

  const HomestayImageGallery({
    Key? key,
    required this.imageUrls,
    this.height = 250,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    if (imageUrls.isEmpty) {
      return CachedNetworkImageWidget(
        imageUrl: null,
        height: height,
        width: double.infinity,
      );
    }

    if (imageUrls.length == 1) {
      return CachedNetworkImageWidget(
        imageUrl: imageUrls.first,
        height: height,
        width: double.infinity,
      );
    }

    // Show image carousel for multiple images
    return SizedBox(
      height: height,
      child: PageView.builder(
        itemCount: imageUrls.length,
        itemBuilder: (context, index) {
          return CachedNetworkImageWidget(
            imageUrl: imageUrls[index],
            height: height,
            width: double.infinity,
          );
        },
      ),
    );
  }
}
