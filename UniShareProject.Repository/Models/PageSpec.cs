namespace UniShareProject.Repository.Models;

/// <summary>
/// Represents pagination specifications for database queries
/// </summary>
public class PageSpec
{
    /// <summary>
    /// The page number (1-based). Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// The number of items per page. Defaults to 20.
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Gets the number of items to skip for pagination (0-based offset)
    /// </summary>
    public int Offset => (Page - 1) * PageSize;
    
    /// <summary>
    /// Constructor with page and page size
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    public PageSpec(int page = 1, int pageSize = 20)
    {
        Page = Math.Max(1, page); // Ensure page is at least 1
        PageSize = Math.Max(1, pageSize); // Ensure page size is at least 1
    }
}