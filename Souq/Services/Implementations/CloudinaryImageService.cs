using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Souq.Services.Interfaces;

namespace Souq.Services.Implementations
{
    public class CloudinaryImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryImageService> _logger;

        public CloudinaryImageService(IConfiguration config,
            ILogger<CloudinaryImageService> logger)
        {
            
            _logger = logger;

            var account = new Account
                (
                    config["Cloudinary:CloudName"],
                    config["Cloudinary:ApiKey"],
                    config["Cloudinary:ApiSecret"]
                );

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
        public async Task DeleteAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            if (result.Error != null)
            {
                _logger.LogError(
                    "Cloudinary delete failed: {Error}",
                    result.Error.Message);
            }
        }

        public async Task<string?> UploadAsync(IFormFile file, string folder)
        {
            if (file.Length == 0) return null;
            var allowedTypes = new[]
            {
                "image/jpeg", "image/jpg",
                "image/png", "image/webp"
            };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                _logger.LogWarning(
                    "Invalid file type attempted: {ContentType}",
                    file.ContentType);
                return null;
            }

            /*
                Max file size 5MB
            */
            if (file.Length > 5 * 1024 * 1024)
            {
                _logger.LogWarning(
                    "File too large: {Size} bytes", file.Length);
                return null;
            }

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(
                    file.FileName, stream),
                Folder = $"Souq/{folder}",
                /*
                    Auto-optimize:
                    - Resize to max 800px width
                    - Convert to WebP format
                    - Auto quality compression
                    This reduces file size by ~70% automatically.
                */
                Transformation = new Transformation()
                    .Width(800).Crop("limit")
                    .FetchFormat("auto")
                    .Quality("auto"),
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if(result.Error != null)
            {
                _logger.LogError(
                    "Cloudinary upload failed: {Error}",
                    result.Error.Message);
                return null;
            }

            _logger.LogInformation(
                "Image Uploded Successfully : {Url}",
                result.SecureUrl);
            return result.SecureUrl.ToString();


        }
    }
}
