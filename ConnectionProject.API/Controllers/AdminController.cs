using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShareProject.services.Interfaces;
using UniShareProject.services.DTOs;
using ConnectionProject.API.Controllers.Base;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Handles administrative operations for UniShare platform
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class AdminController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IItemService _itemService;

    public AdminController(IUserService userService, IItemService itemService, ILogger<AdminController> logger) 
        : base(logger)
    {
        _userService = userService;
        _itemService = itemService;
    }

    /// <summary>
    /// Get administrative dashboard overview
    /// </summary>
    /// <remarks>
    /// Provides a summary of key platform metrics for administrators.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/admin/dashboard
    ///     Authorization: Bearer {admin_token}
    /// 
    /// Requires admin role for access.
    /// </remarks>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dashboard overview with platform statistics</returns>
    /// <response code="200">Dashboard data retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        try
        {
            var adminId = GetCurrentUserId();
            
            Logger.LogInformation("Admin dashboard accessed by user {AdminId}", adminId);
            
            var dashboardData = await _userService.GetDashboardAsync(ct);
            
            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving admin dashboard");
            return StatusCode(500, new { message = "Internal server error occurred while retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Get users for admin management
    /// </summary>
    /// <remarks>
    /// Retrieves a paginated list of users with filtering options for admin management.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/admin/users?page=1&amp;pageSize=50&amp;includeAdmins=true&amp;includeBanned=true
    ///     Authorization: Bearer {admin_token}
    /// 
    /// Query parameters:
    /// - page: Page number (1-based, default: 1)
    /// - pageSize: Items per page (default: 50, max: 100)
    /// - searchTerm: Search by name or email (optional)
    /// - includeAdmins: Include admin users (default: true)
    /// - includeBanned: Include banned users (default: true)
    /// 
    /// Requires admin role for access.
    /// </remarks>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Search term for name or email</param>
    /// <param name="includeAdmins">Whether to include admin users</param>
    /// <param name="includeBanned">Whether to include banned users</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated users list</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("users")]
    [ProducesResponseType(typeof(AdminUsersListDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool includeAdmins = true,
        [FromQuery] bool includeBanned = true,
        CancellationToken ct = default)
    {
        try
        {
            var adminId = GetCurrentUserId();
            
            // Validate parameters
            if (page < 1)
            {
                return BadRequest(new { message = "Page must be greater than 0" });
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "PageSize must be between 1 and 100" });
            }

            Logger.LogInformation("Admin {AdminId} requesting users list - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}", 
                adminId, page, pageSize, searchTerm ?? "None");
            
            var usersData = await _userService.GetUsersAsync(page, pageSize, searchTerm, includeAdmins, includeBanned, ct);
            
            Logger.LogInformation("Retrieved {UserCount} users out of {TotalUsers} for admin {AdminId}", 
                usersData.Users.Count, usersData.TotalUsers, adminId);
            
            return Ok(usersData);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving users list");
            return StatusCode(500, new { message = "Internal server error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Delete an item (Admin operation)
    /// </summary>
    /// <remarks>
    /// Permanently removes an item from the platform. This is an admin-only operation
    /// that bypasses normal ownership restrictions.
    /// 
    /// Sample request:
    /// 
    ///     DELETE /api/admin/items/123
    ///     Authorization: Bearer {admin_token}
    /// 
    /// This action is irreversible and should be used carefully.
    /// Consider updating item status to "Withdrawn" instead for non-permanent removal.
    /// </remarks>
    /// <param name="id">Item ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="200">Item deleted successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("items/{id:int}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
    {
        try
        {
            var adminId = GetCurrentUserId();
            
            Logger.LogWarning("Admin {AdminId} attempting to delete item {ItemId}", adminId, id);
            
            var success = await _itemService.AdminDeleteAsync(id, adminId, ct);
            
            if (success)
            {
                Logger.LogWarning("Admin {AdminId} successfully deleted item {ItemId}", adminId, id);
                return Ok(new { 
                    message = "Item deleted successfully",
                    itemId = id,
                    adminId = adminId,
                    deletedAt = DateTime.UtcNow
                });
            }
            else
            {
                Logger.LogWarning("Admin {AdminId} failed to delete item {ItemId} - no rows affected", adminId, id);
                return StatusCode(500, new { message = "Failed to delete item - no rows affected" });
            }
        }
        catch (KeyNotFoundException ex)
        {
            Logger.LogWarning("Admin deletion failed - item not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting item {ItemId}", id);
            return StatusCode(500, new { message = "Internal server error occurred during item deletion" });
        }
    }

    /// <summary>
    /// Ban a user (Admin operation)
    /// </summary>
    /// <remarks>
    /// Bans a user from the platform, preventing them from logging in or performing actions.
    /// 
    /// Sample request:
    /// 
    ///     PUT /api/admin/users/123/ban
    ///     Authorization: Bearer {admin_token}
    /// 
    /// Banned users cannot:
    /// - Log in to the platform
    /// - Create new items
    /// - Purchase items
    /// - Update their profile
    /// 
    /// Their existing items remain visible but cannot be modified.
    /// </remarks>
    /// <param name="id">User ID to ban</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of ban action</returns>
    /// <response code="200">User banned successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("users/{id:int}/ban")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> BanUser(int id, CancellationToken ct)
    {
        try
        {
            var adminId = GetCurrentUserId();
            
            Logger.LogWarning("Admin {AdminId} attempting to ban user {UserId}", adminId, id);
            
            var success = await _userService.BanUserAsync(id, adminId, ct);
            
            if (success)
            {
                Logger.LogWarning("Admin {AdminId} successfully banned user {UserId}", adminId, id);
                return Ok(new { 
                    message = "User banned successfully",
                    userId = id,
                    adminId = adminId,
                    bannedAt = DateTime.UtcNow
                });
            }
            else
            {
                Logger.LogWarning("Admin {AdminId} failed to ban user {UserId} - no rows affected", adminId, id);
                return StatusCode(500, new { message = "Failed to ban user - no rows affected" });
            }
        }
        catch (KeyNotFoundException ex)
        {
            Logger.LogWarning("Admin ban failed - user not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error banning user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error occurred during user ban" });
        }
    }

    /// <summary>
    /// Unban a user (Admin operation)
    /// </summary>
    /// <remarks>
    /// Removes a ban from a user, restoring their access to the platform.
    /// 
    /// Sample request:
    /// 
    ///     PUT /api/admin/users/123/unban
    ///     Authorization: Bearer {admin_token}
    /// 
    /// Unbanned users regain full platform access:
    /// - Can log in again
    /// - Can create and manage items
    /// - Can purchase items
    /// - Can update their profile
    /// </remarks>
    /// <param name="id">User ID to unban</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Confirmation of unban action</returns>
    /// <response code="200">User unbanned successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("users/{id:int}/unban")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> UnbanUser(int id, CancellationToken ct)
    {
        try
        {
            var adminId = GetCurrentUserId();
            
            Logger.LogInformation("Admin {AdminId} attempting to unban user {UserId}", adminId, id);
            
            var success = await _userService.UnbanUserAsync(id, adminId, ct);
            
            if (success)
            {
                Logger.LogInformation("Admin {AdminId} successfully unbanned user {UserId}", adminId, id);
                return Ok(new { 
                    message = "User unbanned successfully",
                    userId = id,
                    adminId = adminId,
                    unbannedAt = DateTime.UtcNow
                });
            }
            else
            {
                Logger.LogWarning("Admin {AdminId} failed to unban user {UserId} - no rows affected", adminId, id);
                return StatusCode(500, new { message = "Failed to unban user - no rows affected" });
            }
        }
        catch (KeyNotFoundException ex)
        {
            Logger.LogWarning("Admin unban failed - user not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unbanning user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error occurred during user unban" });
        }
    }
}