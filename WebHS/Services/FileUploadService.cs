using Microsoft.AspNetCore.Http;

namespace WebHS.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName = "homestays");
        Task<List<string>> UploadImagesAsync(IEnumerable<IFormFile> files, string folderName = "homestays");
        Task<bool> DeleteImageAsync(string imageUrl);
        bool IsValidImageFile(IFormFile file);
        Task<string> UploadOptimizedImageAsync(IFormFile file, string folderName = "homestays", int maxWidth = 1200, int maxHeight = 800);
        Task<(bool IsValid, string ErrorMessage)> ValidateImageFileAsync(IFormFile file);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName = "homestays")
        {
            if (!IsValidImageFile(file))
                throw new ArgumentException("Invalid image file");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return relative URL
            return $"/uploads/{folderName}/{fileName}";
        }

        public async Task<List<string>> UploadImagesAsync(IEnumerable<IFormFile> files, string folderName = "homestays")
        {
            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                if (IsValidImageFile(file))
                {
                    try
                    {
                        var url = await UploadImageAsync(file, folderName);
                        uploadedUrls.Add(url);
                    }
                    catch
                    {
                        // Skip invalid files, continue with others
                        continue;
                    }
                }
            }

            return uploadedUrls;
        }

        public Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return Task.FromResult(false);

                // Remove leading slash and convert to physical path
                var relativePath = imageUrl.TrimStart('/');
                var physicalPath = Path.Combine(_environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch (Exception)
            {
                // Log error in production
                return Task.FromResult(false);
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size
            if (file.Length > _maxFileSize)
                return false;

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }

        // Enhanced method with image optimization
        public async Task<string> UploadOptimizedImageAsync(IFormFile file, string folderName = "homestays", int maxWidth = 1200, int maxHeight = 800)
        {
            if (!IsValidImageFile(file))
                throw new ArgumentException("Invalid image file");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // For now, just save the file as-is
            // In production, you would add image resizing here using libraries like ImageSharp
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // TODO: Add image optimization using ImageSharp
            // - Resize to maxWidth/maxHeight while maintaining aspect ratio
            // - Compress image quality
            // - Convert to WebP format for better compression

            // Return relative URL
            return $"/uploads/{folderName}/{fileName}";
        }

        // Enhanced validation with virus scanning placeholder
        public Task<(bool IsValid, string ErrorMessage)> ValidateImageFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Task.FromResult((false, "Không có file được chọn"));

            if (file.Length > _maxFileSize)
                return Task.FromResult((false, $"Kích thước file vượt quá {_maxFileSize / (1024 * 1024)}MB"));

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return Task.FromResult((false, "Định dạng file không được hỗ trợ"));

            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return Task.FromResult((false, "Loại file không được hỗ trợ"));

            // TODO: Add virus scanning
            // var virusScanResult = await _virusScanner.ScanFileAsync(file);
            // if (!virusScanResult.IsClean)
            //     return (false, "File không an toàn");

            return Task.FromResult((true, string.Empty));
        }
    }
}
