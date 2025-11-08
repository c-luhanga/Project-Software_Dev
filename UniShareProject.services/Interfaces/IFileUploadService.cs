using Microsoft.AspNetCore.Http;

namespace UniShareProject.services.Interfaces;

/// <summary>
/// File upload service contract for image storage abstraction
/// Follows Dependency Inversion Principle (DIP) and Interface Segregation Principle (ISP)
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Upload a single image file and return the public URL
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="itemId">The item ID for organizing uploads</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL where the image can be accessed</returns>
    /// <exception cref="ArgumentException">Thrown when file is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when upload fails</exception>
    Task<string> UploadImageAsync(IFormFile file, int itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload multiple image files and return their public URLs
    /// </summary>
    /// <param name="files">Collection of image files to upload</param>
    /// <param name="itemId">The item ID for organizing uploads</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of public URLs where the images can be accessed</returns>
    /// <exception cref="ArgumentException">Thrown when any file is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when upload fails</exception>
    Task<IReadOnlyList<string>> UploadImagesAsync(IEnumerable<IFormFile> files, int itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an image from storage
    /// </summary>
    /// <param name="imageUrl">The URL of the image to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful, false if image was not found</returns>
    Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if the provided file is a valid image
    /// </summary>
    /// <param name="file">File to validate</param>
    /// <returns>True if file is a valid image format</returns>
    bool IsValidImageFile(IFormFile file);

    /// <summary>
    /// Get the maximum allowed file size in bytes
    /// </summary>
    /// <returns>Maximum file size in bytes</returns>
    long GetMaxFileSizeBytes();

    /// <summary>
    /// Get supported image file extensions
    /// </summary>
    /// <returns>Array of supported file extensions (e.g., [".jpg", ".png", ".gif"])</returns>
    string[] GetSupportedExtensions();
}

/// <summary>
/// Configuration options for file upload service
/// </summary>
public class FileUploadOptions
{
    /// <summary>
    /// Maximum file size in bytes (default: 5MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB

    /// <summary>
    /// Supported image file extensions
    /// </summary>
    public string[] SupportedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    /// <summary>
    /// Supported MIME types for images
    /// </summary>
    public string[] SupportedMimeTypes { get; set; } = { 
        "image/jpeg", 
        "image/png", 
        "image/gif", 
        "image/webp", 
        "image/bmp" 
    };

    /// <summary>
    /// Storage provider type (Local, S3, Azure, etc.)
    /// </summary>
    public string StorageProvider { get; set; } = "Local";
}

/// <summary>
/// Result model for file upload operations
/// </summary>
public record FileUploadResult
{
    /// <summary>
    /// Public URL where the uploaded file can be accessed
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string OriginalFileName { get; init; } = string.Empty;

    /// <summary>
    /// Generated filename on storage
    /// </summary>
    public string StorageFileName { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Content type of the uploaded file
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}