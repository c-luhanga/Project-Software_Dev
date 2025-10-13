namespace UniShareProject.Repository.Sql;

/// <summary>
/// Contains all SQL queries for ItemImage repository operations
/// </summary>
public static class ItemImageQueries
{
    /// <summary>
    /// Inserts a new image URL for an item
    /// </summary>
    public const string Insert = @"
        INSERT INTO dbo.ItemImages (ItemID, ImageUrl)
        VALUES (@ItemId, @Url);
        SELECT @@ROWCOUNT";

    /// <summary>
    /// Counts the number of images for a specific item
    /// </summary>
    public const string CountByItem = @"
        SELECT COUNT(1) FROM dbo.ItemImages WHERE ItemID = @ItemId";

    /// <summary>
    /// Gets all image URLs for a specific item
    /// </summary>
    public const string GetUrlsByItem = @"
        SELECT ImageUrl FROM dbo.ItemImages 
        WHERE ItemID = @ItemId 
        ORDER BY ImageID";
}