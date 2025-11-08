using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using UniShareProject.services.Interfaces;
// Note: This implementation requires AWS SDK for .NET
// Install: dotnet add package AWSSDK.S3
// using Amazon.S3;
// using Amazon.S3.Model;
using System.ComponentModel.DataAnnotations;

namespace UniShareProject.services.Implementations;

/// <summary>
/// AWS S3 implementation of file upload service for production use
/// 
/// This is a template implementation that requires:
/// 1. Install AWS SDK: dotnet add package AWSSDK.S3
/// 2. Uncomment AWS using statements above
/// 3. Configure AWS credentials and IAM roles
/// 4. Update Program.cs to register AWS services
/// 
/// For immediate use, the LocalFileUploadService is configured and working.
/// Use this S3 implementation when ready to deploy to production.
/// </summary>
public class S3FileUploadService : IFileUploadService
{
    private readonly FileUploadOptions _options;
    private readonly ILogger<S3FileUploadService> _logger;

    public S3FileUploadService(
        IOptions<FileUploadOptions> options,
        ILogger<S3FileUploadService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // This implementation is not yet ready - install AWS SDK first
        throw new NotImplementedException(
            "S3FileUploadService requires AWS SDK for .NET. " +
            "Install 'AWSSDK.S3' package and configure AWS credentials. " +
            "For development, use LocalFileUploadService instead.");
    }

    public Task<string> UploadImageAsync(IFormFile file, int itemId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Install AWS SDK and configure S3 settings first.");
    }

    public Task<IReadOnlyList<string>> UploadImagesAsync(IEnumerable<IFormFile> files, int itemId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Install AWS SDK and configure S3 settings first.");
    }

    public Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Install AWS SDK and configure S3 settings first.");
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0) return false;
        if (file.Length > _options.MaxFileSizeBytes) return false;
        if (!_options.SupportedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return false;
        
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && _options.SupportedExtensions.Contains(extension);
    }

    public long GetMaxFileSizeBytes() => _options.MaxFileSizeBytes;

    public string[] GetSupportedExtensions() => _options.SupportedExtensions;
}

/// <summary>
/// Configuration settings for S3 file upload service
/// Add this to appsettings.json when ready to use S3
/// </summary>
public class S3FileUploadSettings
{
    /// <summary>
    /// AWS S3 bucket name for storing uploaded files
    /// </summary>
    [Required]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS region where the S3 bucket is located
    /// </summary>
    [Required]
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// CloudFront distribution domain for CDN access (optional)
    /// Example: "d123456789.cloudfront.net"
    /// </summary>
    public string? CloudFrontDomain { get; set; }

    /// <summary>
    /// Custom domain for accessing files (optional)
    /// Example: "cdn.unishare.com"
    /// </summary>
    public string? CustomDomain { get; set; }

    /// <summary>
    /// Whether to use server-side encryption
    /// </summary>
    public bool UseServerSideEncryption { get; set; } = true;

    /// <summary>
    /// Default ACL for uploaded objects
    /// Options: "private", "public-read", etc.
    /// </summary>
    public string DefaultAcl { get; set; } = "private";
}