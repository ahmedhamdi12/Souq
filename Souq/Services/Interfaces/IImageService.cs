namespace Souq.Services.Interfaces
{
    public interface IImageService
    {
        Task<string?> UploadAsync(IFormFile file, string folder);
        Task DeleteAsync(string publicId);
    }
}
