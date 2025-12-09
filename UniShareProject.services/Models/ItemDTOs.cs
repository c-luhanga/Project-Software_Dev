using UniShareProject.Repository.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace UniShareProject.services.Models;

/// <summary>
/// Request model for creating a new item listing
/// </summary>
/// <param name="Title">Item title (3-255 characters)</param>
/// <param name="Description">Detailed item description (10-4000 characters)</param>
/// <param name="CategoryId">Category ID (optional): 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other</param>
/// <param name="Price">Item price in USD (optional, must be >= 0)</param>
/// <param name="ConditionId">Condition ID (required): 1=Like New, 2=Good, 3=Fair, 4=Poor</param>
/// <example>
/// {
///   "title": "Calculus Textbook",
///   "description": "Like new condition calculus textbook for Math 151. No highlighting or writing inside.",
///   "categoryId": 1,
///   "price": 75.00,
///   "conditionId": 1
/// }
/// </example>
public record CreateItemRequest(
    [property: Required, StringLength(255, MinimumLength = 3)]
    [property: Description("Item title (3-255 characters)")]
    string Title,

    [property: Required, StringLength(4000, MinimumLength = 10)]
    [property: Description("Detailed item description (10-4000 characters)")]
    string Description,

    [property: Description("Category ID (optional): 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other")]
    int? CategoryId,

    [property: Range(0, double.MaxValue)]
    [property: Description("Item price in USD (optional, must be >= 0)")]
    decimal? Price,

    [property: Required, Range(1, 4)]
    [property: Description("Condition ID (required): 1=Like New, 2=Good, 3=Fair, 4=Poor")]
    byte ConditionId
);

/// <summary>
/// Request model for updating an existing item
/// </summary>
/// <param name="Title">Updated item title (3-100 characters, optional)</param>
/// <param name="Description">Updated item description (10-4000 characters, optional)</param>
/// <param name="CategoryId">Updated category ID (optional): 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other</param>
/// <param name="Price">Updated item price in USD (optional, must be >= 0)</param>
/// <param name="ConditionId">Updated condition ID (optional): 1=Like New, 2=Good, 3=Fair, 4=Poor</param>
/// <example>
/// {
///   "title": "Updated Calculus Textbook",
///   "description": "Updated description with new condition details",
///   "categoryId": 1,
///   "price": 65.00,
///   "conditionId": 2
/// }
/// </example>
public record UpdateItemRequest(
    [property: StringLength(100, MinimumLength = 3)]
    [property: Description("Updated item title (3-100 characters, optional)")]
    string? Title = null,

    [property: StringLength(4000, MinimumLength = 10)]
    [property: Description("Updated detailed item description (10-4000 characters, optional)")]
    string? Description = null,

    [property: Description("Updated category ID (optional): 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other")]
    int? CategoryId = null,

    [property: Range(0, double.MaxValue)]
    [property: Description("Updated item price in USD (optional, must be >= 0)")]
    decimal? Price = null,

    [property: Range(1, 4)]
    [property: Description("Updated condition ID (optional): 1=Like New, 2=Good, 3=Fair, 4=Poor")]
    byte? ConditionId = null
);

/// <summary>
/// Request model for adding images to an item
/// </summary>
/// <param name="ImageUrls">List of image URLs to add (1-4 URLs required)</param>
/// <example>
/// {
///   "imageUrls": [
//     "https://example.com/image1.jpg",
//     "https://example.com/image2.jpg"
///   ]
/// }
/// </example>
public record AddItemImagesRequest(
    [property: Required]
    [property: Description("List of image URLs to add (1-4 URLs required)")]
    List<string> ImageUrls
);

/// <summary>
/// Request model for searching items with optional filters and pagination
/// </summary>
/// <param name="CategoryId">Filter by category ID (optional)</param>
/// <param name="StatusId">Filter by status ID (optional): 1=Active, 2=Pending, 3=Sold, 4=Withdrawn</param>
/// <param name="ConditionId">Filter by condition ID (optional): 1=Like New, 2=Good, 3=Fair, 4=Poor</param>
/// <param name="Q">Text search query for title and description (optional)</param>
/// <param name="Page">Page number (default: 1)</param>
/// <param name="PageSize">Items per page (default: 20, max: 100)</param>
public record SearchItemsRequest(
    [property: Description("Filter by category ID: 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other")]
    int? CategoryId = null,

    [property: Description("Filter by status ID: 1=Active, 2=Pending, 3=Sold, 4=Withdrawn")]
    byte? StatusId = null,

    [property: Description("Filter by condition ID: 1=Like New, 2=Good, 3=Fair, 4=Poor")]
    byte? ConditionId = null,

    [property: Description("Text search query for title and description")]
    string? Q = null,

    [property: Range(1, int.MaxValue)]
    [property: Description("Page number (minimum: 1)")]
    int Page = 1,

    [property: Range(1, 100)]
    [property: Description("Items per page (1-100)")]
    int PageSize = 20
)
{
    /// <summary>
    /// Converts SearchItemsRequest to PageSpec for repository layer
    /// </summary>
    public PageSpec ToPageSpec() => new(Page, PageSize);
}

/// <summary>
/// Data transfer object representing an item in API responses
/// </summary>
public class ItemDto
{
    /// <summary>
    /// Unique item identifier
    /// </summary>
    [Description("Unique item identifier")]
    public int Id { get; set; }

    /// <summary>
    /// Item title
    /// </summary>
    [Description("Item title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed item description
    /// </summary>
    [Description("Detailed item description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category ID: 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other
    /// </summary>
    [Description("Category ID: 1=Electronics, 2=Books, 3=Clothing, 4=Furniture, 5=Sports and Recreation, 6=Other")]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Category name (e.g., "Books", "Electronics")
    /// </summary>
    [Description("Category name (e.g., 'Books', 'Electronics')")]
    public string? CategoryName { get; set; }

    /// <summary>
    /// Item price in USD
    /// </summary>
    [Description("Item price in USD")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Condition ID: 1=Like New, 2=Good, 3=Fair, 4=Poor
    /// </summary>
    [Description("Condition ID: 1=Like New, 2=Good, 3=Fair, 4=Poor")]
    public byte ConditionId { get; set; }

    /// <summary>
    /// Status ID: 1=Active, 2=Pending, 3=Sold, 4=Withdrawn
    /// </summary>
    [Description("Status ID: 1=Active, 2=Pending, 3=Sold, 4=Withdrawn")]
    public byte StatusId { get; set; }

    /// <summary>
    /// Seller user ID
    /// </summary>
    [Description("Seller user ID")]
    public int SellerId { get; set; }

    /// <summary>
    /// Seller's full name (first + last name)
    /// </summary>
    [Description("Seller's full name (first + last name)")]
    public string? SellerName { get; set; }

    /// <summary>
    /// Seller's profile image URL
    /// </summary>
    [Description("Seller's profile image URL")]
    public string? SellerProfileImageUrl { get; set; }

    /// <summary>
    /// Seller's house/dormitory location
    /// </summary>
    [Description("Seller's house/dormitory location")]
    public string? SellerHouse { get; set; }

    /// <summary>
    /// Date and time when item was posted
    /// </summary>
    [Description("Date and time when item was posted")]
    public DateTime PostedDate { get; set; }

    /// <summary>
    /// Collection of image URLs for this item
    /// </summary>
    [Description("Collection of image URLs for this item")]
    public List<string> Images { get; set; } = new List<string>();

    /// <summary>
    /// First image URL to use as thumbnail (computed from Images)
    /// </summary>
    [Description("First image URL to use as thumbnail")]
    public string? ThumbnailUrl => Images?.FirstOrDefault();

    // Parameterless constructor for AutoMapper
    public ItemDto() { }

    /// <summary>
    /// Creates a new ItemDto instance
    /// </summary>
    /// <param name="id">Item ID</param>
    /// <param name="title">Item title</param>
    /// <param name="description">Item description</param>
    /// <param name="categoryId">Category ID</param>
    /// <param name="categoryName">Category name</param>
    /// <param name="price">Item price</param>
    /// <param name="conditionId">Condition ID</param>
    /// <param name="statusId">Status ID</param>
    /// <param name="sellerId">Seller ID</param>
    /// <param name="postedDate">Posted date</param>
    public ItemDto(int id, string title, string description, int? categoryId, string? categoryName,
                   decimal? price, byte conditionId, byte statusId, int sellerId, DateTime postedDate)
    {
        Id = id;
        Title = title;
        Description = description;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Price = price;
        ConditionId = conditionId;
        StatusId = statusId;
        SellerId = sellerId;
        PostedDate = postedDate;
    }
}

/// <summary>
/// Generic paginated result container for API responses
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
/// <param name="Items">Collection of items for current page</param>
/// <param name="Total">Total number of items across all pages</param>
/// <param name="Page">Current page number</param>
/// <param name="PageSize">Number of items per page</param>
/// <param name="TotalPages">Total number of pages</param>
/// <param name="HasNextPage">Whether there is a next page available</param>
/// <param name="HasPreviousPage">Whether there is a previous page available</param>
public record PagedResultDto<T>(
    [property: Description("Collection of items for current page")]
    IEnumerable<T> Items,

    [property: Description("Total number of items across all pages")]
    int Total,

    [property: Description("Current page number")]
    int Page,

    [property: Description("Number of items per page")]
    int PageSize,

    [property: Description("Total number of pages")]
    int TotalPages,

    [property: Description("Whether there is a next page available")]
    bool HasNextPage,

    [property: Description("Whether there is a previous page available")]
    bool HasPreviousPage
);

/// <summary>
/// Pagination specification for service layer (mirrors repository PageSpec)
/// </summary>
/// <param name="Page">Page number (default: 1)</param>
/// <param name="PageSize">Items per page (default: 20)</param>
public record PageSpecDto(
    [property: Description("Page number")]
    int Page = 1,

    [property: Description("Items per page")]
    int PageSize = 20
)
{
    /// <summary>
    /// Gets the number of items to skip for pagination (0-based offset)
    /// </summary>
    public int Offset => (Page - 1) * PageSize;
};