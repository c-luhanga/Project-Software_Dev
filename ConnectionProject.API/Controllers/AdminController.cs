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
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        try
        {
            var adminId = GetCurrentUserId();
            
            Logger.LogInformation("Admin dashboard accessed by user {AdminId}", adminId);
            
            // This is a basic example - you would implement actual statistics gathering
            var dashboardData = new
            {
                TotalUsers = "Data not available - implement user counting service",
                TotalItems = "Data not available - implement item counting service",
                ActiveItems = "Data not available - implement active item counting",
                PendingItems = "Data not available - implement pending item counting",
                BannedUsers = "Data not available - implement banned user counting",
                Message = "Admin dashboard - implement actual statistics as needed",
                LastUpdated = DateTime.UtcNow
            };

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving admin dashboard");
            return StatusCode(500, new { message = "Internal server error occurred" });
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
            
            // Note: This would require implementing a BanUserAsync method in IUserService
            // For now, return a placeholder response
            return StatusCode(501, new { 
                message = "User banning not implemented yet",
                note = "Implement BanUserAsync method in IUserService and UserRepository",
                userId = id,
                adminId = adminId
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error banning user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error occurred" });
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
            
            // Note: This would require implementing an UnbanUserAsync method in IUserService
            // For now, return a placeholder response
            return StatusCode(501, new { 
                message = "User unbanning not implemented yet",
                note = "Implement UnbanUserAsync method in IUserService and UserRepository",
                userId = id,
                adminId = adminId
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unbanning user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }
}