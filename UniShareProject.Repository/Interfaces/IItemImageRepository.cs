namespace UniShareProject.Repository.Repositories;

/// <summary>
/// Repository interface for managing item images
/// </summary>
public interface IItemImageRepository
{
    /// <summary>
    /// Insert a new image URL for an item
    /// </summary>
    /// <param name="itemId">The item ID</param>
    /// <param name="url">The image URL to insert</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The number of rows affected (should be 1 if successful)</returns>
    Task<int> InsertAsync(int itemId, string url, CancellationToken ct);

    /// <summary>
    /// Count the number of images for a specific item
    /// </summary>
    /// <param name="itemId">The item ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The number of images for the item</returns>
    Task<int> CountByItemAsync(int itemId, CancellationToken ct);

    /// <summary>
    /// Get all image URLs for a specific item
    /// </summary>
    /// <param name="itemId">The item ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A read-only list of image URLs</returns>
    Task<IReadOnlyList<string>> GetUrlsAsync(int itemId, CancellationToken ct);
}