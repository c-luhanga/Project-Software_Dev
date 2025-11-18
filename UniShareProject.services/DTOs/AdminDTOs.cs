using System.ComponentModel;

namespace UniShareProject.services.DTOs;

/// <summary>
/// Admin dashboard overview data
/// </summary>
public record AdminDashboardDto
{
    /// <summary>
    /// Total number of registered users
    /// </summary>
    [Description("Total number of registered users")]
    public int TotalUsers { get; init; }

    /// <summary>
    /// Number of currently banned users
    /// </summary>
    [Description("Number of currently banned users")]
    public int BannedUsers { get; init; }

    /// <summary>
    /// Number of admin users
    /// </summary>
    [Description("Number of admin users")]
    public int AdminUsers { get; init; }

    /// <summary>
    /// Total number of items in the marketplace
    /// </summary>
    [Description("Total number of items in the marketplace")]
    public int TotalItems { get; init; }

    /// <summary>
    /// Number of active items available for purchase
    /// </summary>
    [Description("Number of active items available for purchase")]
    public int ActiveItems { get; init; }

    /// <summary>
    /// Number of items with pending purchase requests
    /// </summary>
    [Description("Number of items with pending purchase requests")]
    public int PendingItems { get; init; }

    /// <summary>
    /// Number of sold items
    /// </summary>
    [Description("Number of sold items")]
    public int SoldItems { get; init; }

    /// <summary>
    /// Number of withdrawn items
    /// </summary>
    [Description("Number of withdrawn items")]
    public int WithdrawnItems { get; init; }

    /// <summary>
    /// Timestamp when the dashboard data was generated
    /// </summary>
    [Description("Timestamp when the dashboard data was generated")]
    public DateTime LastUpdated { get; init; }

    /// <summary>
    /// Platform health status message
    /// </summary>
    [Description("Platform health status message")]
    public string Status { get; init; } = "Operational";
}

/// <summary>
/// User information for admin management
/// </summary>
public record AdminUserDto
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    [Description("User's unique identifier")]
    public int UserId { get; init; }

    /// <summary>
    /// User's first name
    /// </summary>
    [Description("User's first name")]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Description("User's last name")]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    [Description("User's email address")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's phone number (optional)
    /// </summary>
    [Description("User's phone number (optional)")]
    public string? Phone { get; init; }

    /// <summary>
    /// User's house/dormitory information (optional)
    /// </summary>
    [Description("User's house/dormitory information (optional)")]
    public string? House { get; init; }

    /// <summary>
    /// Whether the user is banned
    /// </summary>
    [Description("Whether the user is banned")]
    public bool IsBanned { get; init; }

    /// <summary>
    /// Whether the user has admin privileges
    /// </summary>
    [Description("Whether the user has admin privileges")]
    public bool IsAdmin { get; init; }

    /// <summary>
    /// When the user was created
    /// </summary>
    [Description("When the user was created")]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the user was last seen (optional)
    /// </summary>
    [Description("When the user was last seen (optional)")]
    public DateTime? LastSeen { get; init; }

    /// <summary>
    /// URL to user's profile image (optional)
    /// </summary>
    [Description("URL to user's profile image (optional)")]
    public string? ProfileImageUrl { get; init; }
}

/// <summary>
/// Paginated list of users for admin management
/// </summary>
public record AdminUsersListDto
{
    /// <summary>
    /// List of users for the current page
    /// </summary>
    [Description("List of users for the current page")]
    public List<AdminUserDto> Users { get; init; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    [Description("Current page number (1-based)")]
    public int CurrentPage { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Description("Number of items per page")]
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of users matching the filter criteria
    /// </summary>
    [Description("Total number of users matching the filter criteria")]
    public int TotalUsers { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    [Description("Total number of pages")]
    public int TotalPages { get; init; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    [Description("Whether there is a next page")]
    public bool HasNextPage { get; init; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    [Description("Whether there is a previous page")]
    public bool HasPreviousPage { get; init; }
}