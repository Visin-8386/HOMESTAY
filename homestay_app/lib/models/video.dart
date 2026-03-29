class Video {
  final String videoId;
  final String title;
  final String? description;
  final String? thumbnail;
  final String? channelTitle;
  final String? publishedAt;
  final String? category;

  Video({
    required this.videoId,
    required this.title,
    this.description,
    this.thumbnail,
    this.channelTitle,
    this.publishedAt,
    this.category,
  });

  factory Video.fromJson(Map<String, dynamic> json) {
    return Video(
      videoId: json['videoId']?.toString() ?? '',
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString(),
      thumbnail: json['thumbnail']?.toString() ?? json['thumbnailUrl']?.toString(),
      channelTitle: json['channelTitle']?.toString(),
      publishedAt: json['publishedAt']?.toString(),
      category: json['category']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'videoId': videoId,
      'title': title,
      'description': description,
      'thumbnail': thumbnail,
      'channelTitle': channelTitle,
      'publishedAt': publishedAt,
      'category': category,
    };
  }
}
