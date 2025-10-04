namespace UniShareProject.Repository.Models;

/// <summary>
/// Represents a paginated result containing items and total count
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    
    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    public int Total { get; set; }
    
    /// <summary>
    /// The current page number (1-based)
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// The number of items per page
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// The total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
    
    /// <summary>
    /// Whether there are more pages after the current one
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
    
    /// <summary>
    /// Whether there are pages before the current one
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}