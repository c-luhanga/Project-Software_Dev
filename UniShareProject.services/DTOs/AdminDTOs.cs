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