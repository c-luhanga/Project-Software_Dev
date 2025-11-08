using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using UniShareProject.services.Interfaces;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace UniShareProject.services.Implementations;

/// <summary>
/// Local file system implementation of file upload service
/// Stores files in wwwroot/uploads directory for development use
/// </summary>
public class LocalFileUploadService : IFileUploadService
{
    private readonly FileUploadOptions _options;
    private readonly ILogger<LocalFileUploadService> _logger;
    private readonly string _uploadsPath;
    private readonly string _baseUrl;

    public LocalFileUploadService(
        IOptions<FileUploadOptions> options,
        ILogger<LocalFileUploadService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _options = options.Value;
        _logger = logger;

        // Create uploads directory path
        _uploadsPath = Path.Combine("wwwroot", "uploads", "items");
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
            _logger.LogInformation("Created uploads directory: {Path}", _uploadsPath);
        }

        // Build base URL for serving files
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null)
        {
            _baseUrl = $"{request.Scheme}://{request.Host}/uploads/items";
        }
        else
        {
            _baseUrl = "/uploads/items"; // Fallback for non-HTTP contexts
        }
    }

    /// <inheritdoc />
    public async Task<string> UploadImageAsync(IFormFile file, int itemId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting image upload for item {ItemId}", itemId);

        // Validate file
        ValidateFile(file);

        // Generate unique filename
        var fileName = GenerateFileName(file, itemId);
        var filePath = Path.Combine(_uploadsPath, fileName);

        try
        {
            // Save file to disk
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream, cancellationToken);

            var publicUrl = $"{_baseUrl}/{fileName}";
            
            _logger.LogInformation(
                "Successfully uploaded image {FileName} for item {ItemId}. Size: {FileSize} bytes. URL: {Url}", 
                file.FileName, itemId, file.Length, publicUrl);

            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image {FileName} for item {ItemId}", file.FileName, itemId);
            
            // Clean up partial file if it exists
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete partial file {FilePath}", filePath);
                }
            }

            throw new InvalidOperationException($"Failed to upload image: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> UploadImagesAsync(IEnumerable<IFormFile> files, int itemId, CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        _logger.LogInformation("Starting batch upload of {Count} images for item {ItemId}", fileList.Count, itemId);

        if (fileList.Count == 0)
        {
            throw new ArgumentException("At least one file is required", nameof(files));
        }

        if (fileList.Count > 4)
        {
            throw new ArgumentException("Maximum 4 files allowed per upload", nameof(files));
        }

        var uploadedUrls = new List<string>();
        var uploadedFiles = new List<string>(); // Track for cleanup on failure

        try
        {
            // Upload each file
            foreach (var file in fileList)
            {
                var url = await UploadImageAsync(file, itemId, cancellationToken);
                uploadedUrls.Add(url);
                
                // Track filename for potential cleanup
                var fileName = url.Split('/').Last();
                uploadedFiles.Add(fileName);
            }

            _logger.LogInformation("Successfully uploaded {Count} images for item {ItemId}", uploadedUrls.Count, itemId);
            return uploadedUrls.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch upload failed for item {ItemId}, cleaning up {Count} uploaded files", itemId, uploadedFiles.Count);

            // Clean up any successfully uploaded files
            foreach (var fileName in uploadedFiles)
            {
                try
                {
                    var filePath = Path.Combine(_uploadsPath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Cleaned up file {FileName} after batch upload failure", fileName);
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to cleanup file {FileName}", fileName);
                }
            }

            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract filename from URL
            var fileName = ExtractFileNameFromUrl(imageUrl);
            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Could not extract filename from URL: {Url}", imageUrl);
                return Task.FromResult(false);
            }

            var filePath = Path.Combine(_uploadsPath, fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogInformation("File not found for deletion: {FilePath}", filePath);
                return Task.FromResult(false);
            }

            File.Delete(filePath);
            _logger.LogInformation("Successfully deleted image file: {FilePath}", filePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image: {Url}", imageUrl);
            throw new InvalidOperationException($"Failed to delete image: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return false;
        }

        // Check file size
        if (file.Length > _options.MaxFileSizeBytes)
        {
            return false;
        }

        // Check content type
        if (!_options.SupportedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_options.SupportedExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public long GetMaxFileSizeBytes() => _options.MaxFileSizeBytes;

    /// <inheritdoc />
    public string[] GetSupportedExtensions() => _options.SupportedExtensions;

    /// <summary>
    /// Validate uploaded file according to business rules
    /// </summary>
    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required and cannot be empty");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            var maxSizeMB = _options.MaxFileSizeBytes / (1024.0 * 1024.0);
            throw new ArgumentException($"File size ({file.Length / (1024.0 * 1024.0):F2}MB) exceeds maximum allowed size ({maxSizeMB:F1}MB)");
        }

        if (!_options.SupportedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unsupported file type: {file.ContentType}. Supported types: {string.Join(", ", _options.SupportedMimeTypes)}");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_options.SupportedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Unsupported file extension: {extension}. Supported extensions: {string.Join(", ", _options.SupportedExtensions)}");
        }

        // Additional safety check - verify file has actual content
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new ArgumentException("File must have a valid filename");
        }
    }

    /// <summary>
    /// Generate a unique filename for storage
    /// </summary>
    private static string GenerateFileName(IFormFile file, int itemId)
    {
        var extension = Path.GetExtension(file.FileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8]; // First 8 characters
        
        return $"item_{itemId}_{timestamp}_{uniqueId}{extension}";
    }

    /// <summary>
    /// Extract filename from a public URL
    /// </summary>
    private string ExtractFileNameFromUrl(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl, UriKind.RelativeOrAbsolute);
            return Path.GetFileName(uri.LocalPath);
        }
        catch
        {
            // If URL parsing fails, try simple string manipulation
            return imageUrl.Split('/', '\\').LastOrDefault() ?? string.Empty;
        }
    }
}