using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Fruitables.Constants;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;

namespace Fruitables.Services;

/// <summary>
/// Service quản lý cấu hình website với caching
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private const string CacheKeyPrefix = "Setting_";
    private const string AllSettingsCacheKey = "AllSettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    
    // File upload constants
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".ico", ".svg" };
    private const string UploadsFolder = "uploads/settings";

    public SettingsService(IUnitOfWork unitOfWork, IMemoryCache cache, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _webHostEnvironment = webHostEnvironment;
    }

    /// <inheritdoc/>
    public async Task<string?> GetSettingAsync(string key, string? defaultValue = null)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedValue))
        {
            return cachedValue ?? defaultValue;
        }

        var setting = await _unitOfWork.Settings.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key);

        var value = setting?.Value;
        
        _cache.Set(cacheKey, value, CacheDuration);
        
        return value ?? defaultValue;
    }

    /// <inheritdoc/>
    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default)
    {
        var stringValue = await GetSettingAsync(key);
        
        if (string.IsNullOrEmpty(stringValue))
        {
            return defaultValue;
        }

        try
        {
            var targetType = typeof(T);
            
            if (targetType == typeof(string))
            {
                return (T)(object)stringValue;
            }

            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(stringValue, out var intValue))
                {
                    return (T)(object)intValue;
                }
            }
            
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (bool.TryParse(stringValue, out var boolValue))
                {
                    return (T)(object)boolValue;
                }
            }
            
            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                if (decimal.TryParse(stringValue, out var decimalValue))
                {
                    return (T)(object)decimalValue;
                }
            }
            
            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string?>> GetSettingsByGroupAsync(string group)
    {
        var settings = await _unitOfWork.Settings.Query()
            .AsNoTracking()
            .Where(s => s.Group == group)
            .ToListAsync();

        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    /// <inheritdoc/>
    public async Task<List<Setting>> GetAllSettingsAsync()
    {
        if (_cache.TryGetValue(AllSettingsCacheKey, out List<Setting>? cachedSettings) && cachedSettings != null)
        {
            return cachedSettings;
        }

        var settings = await _unitOfWork.Settings.Query()
            .AsNoTracking()
            .ToListAsync();

        _cache.Set(AllSettingsCacheKey, settings, CacheDuration);

        return settings;
    }

    /// <inheritdoc/>
    public async Task<SettingResult> SaveSettingAsync(string key, string? value, string? group = null)
    {
        try
        {
            // Validate email format for contact_email key
            if (key == SettingKeys.ContactEmail && !string.IsNullOrEmpty(value))
            {
                if (!ValidateEmail(value))
                {
                    return SettingResult.Error("Email không đúng định dạng");
                }
            }

            // Validate URL format for social settings (allow empty)
            if (IsSocialUrlKey(key) && !string.IsNullOrEmpty(value))
            {
                if (!ValidateUrl(value))
                {
                    return SettingResult.Error("URL không đúng định dạng");
                }
            }

            var setting = await _unitOfWork.Settings.Query().FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new Setting
                {
                    Key = key,
                    Value = value,
                    Group = group
                };
                await _unitOfWork.Settings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                if (group != null)
                {
                    setting.Group = group;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            
            // Invalidate cache after save
            InvalidateCache(key);

            return SettingResult.Ok(value);
        }
        catch (Exception ex)
        {
            return SettingResult.Error($"Lỗi khi lưu setting: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<SettingResult> SaveSettingsAsync(Dictionary<string, string?> settings, string? group = null)
    {
        try
        {
            foreach (var kvp in settings)
            {
                var setting = await _unitOfWork.Settings.Query().FirstOrDefaultAsync(s => s.Key == kvp.Key);

                if (setting == null)
                {
                    setting = new Setting
                    {
                        Key = kvp.Key,
                        Value = kvp.Value,
                        Group = group
                    };
                    await _unitOfWork.Settings.AddAsync(setting);
                }
                else
                {
                    setting.Value = kvp.Value;
                    if (group != null)
                    {
                        setting.Group = group;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();
            
            // Invalidate cache for each key that was updated
            foreach (var key in settings.Keys)
            {
                InvalidateCache(key);
            }

            return SettingResult.Ok();
        }
        catch (Exception ex)
        {
            return SettingResult.Error($"Lỗi khi lưu settings: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<SettingResult> SaveFileSettingAsync(string key, IFormFile file, string? group = null)
    {
        try
        {
            // Validate file is not null or empty
            if (file == null || file.Length == 0)
            {
                return SettingResult.Error("File không được để trống");
            }

            // Validate file size (max 2MB)
            if (file.Length > MaxFileSizeBytes)
            {
                return SettingResult.Error("File vượt quá 2MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ValidateFileExtension(extension))
            {
                return SettingResult.Error("Định dạng file không hợp lệ. Chỉ chấp nhận: jpg, png, gif, ico, svg");
            }

            // Create uploads directory if not exists
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, UploadsFolder);
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var uniqueFileName = $"{key}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Save file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save relative path to database
            var relativePath = $"/{UploadsFolder}/{uniqueFileName}";
            var result = await SaveSettingAsync(key, relativePath, group);

            if (!result.Success)
            {
                // Cleanup file if database save failed
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return result;
            }

            return SettingResult.Ok(relativePath);
        }
        catch (Exception ex)
        {
            return SettingResult.Error($"Lỗi khi upload file: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void InvalidateCache()
    {
        // Remove all settings cache - using a pattern approach
        _cache.Remove(AllSettingsCacheKey);
        
        // Note: IMemoryCache doesn't support pattern-based removal
        // Individual keys will be invalidated on next access or via InvalidateCache(key)
    }

    /// <inheritdoc/>
    public void InvalidateCache(string key)
    {
        var cacheKey = $"{CacheKeyPrefix}{key}";
        _cache.Remove(cacheKey);
        _cache.Remove(AllSettingsCacheKey);
    }

    #region Validation Methods

    /// <summary>
    /// Validates email format
    /// </summary>
    /// <param name="email">Email string to validate</param>
    /// <returns>True if valid email format, false otherwise</returns>
    public static bool ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        // RFC 5322 compliant email regex pattern
        const string emailPattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";
        
        try
        {
            return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates URL format
    /// </summary>
    /// <param name="url">URL string to validate</param>
    /// <returns>True if valid URL format, false otherwise</returns>
    public static bool ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Try to parse as absolute URI with http or https scheme
        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
        {
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }

        return false;
    }

    /// <summary>
    /// Checks if the key is a social URL setting key
    /// </summary>
    private static bool IsSocialUrlKey(string key)
    {
        return key == SettingKeys.SocialFacebook ||
               key == SettingKeys.SocialTwitter ||
               key == SettingKeys.SocialInstagram ||
               key == SettingKeys.SocialYoutube ||
               key == SettingKeys.SocialLinkedIn;
    }

    /// <summary>
    /// Validates file extension for image uploads
    /// </summary>
    /// <param name="extension">File extension including dot (e.g., ".jpg")</param>
    /// <returns>True if valid image extension, false otherwise</returns>
    public static bool ValidateFileExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return AllowedImageExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Validates file size for uploads
    /// </summary>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>True if file size is within limit, false otherwise</returns>
    public static bool ValidateFileSize(long fileSize)
    {
        return fileSize > 0 && fileSize <= MaxFileSizeBytes;
    }

    #endregion
}
