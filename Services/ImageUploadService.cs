using Fruitables.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Fruitables.Services;

public class ImageUploadService : IImageUploadService
{
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] ValidImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] ValidImageContentTypes = 
    { 
        "image/jpeg", 
        "image/jpg", 
        "image/png", 
        "image/gif", 
        "image/webp" 
    };

    public ImageUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        if (!IsValidImageFile(file))
            throw new InvalidOperationException("File không phải định dạng ảnh hợp lệ");

        if (!IsValidFileSize(file))
            throw new InvalidOperationException("File vượt quá kích thước cho phép (5MB)");

        // Create upload directory if not exists
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative URL
        return $"/uploads/{folder}/{fileName}";
    }

    public Task DeleteImageAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Task.CompletedTask;

        // Convert URL to physical path
        var relativePath = imageUrl.TrimStart('/');
        var filePath = Path.Combine(_environment.WebRootPath, relativePath);

        // Delete file if exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ValidImageExtensions.Contains(extension))
            return false;

        // Check content type
        if (!ValidImageContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }

    public bool IsValidFileSize(IFormFile file, long maxSizeBytes = 5 * 1024 * 1024)
    {
        if (file == null)
            return false;

        return file.Length <= maxSizeBytes;
    }
}
