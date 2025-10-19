using UniShareProject.services.DTOs;

namespace UniShareProject.services.Interfaces;

/// <summary>
/// Service interface for user-related operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get the current user's profile information
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User profile data or null if not found</returns>
    Task<UserDto?> GetMeAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Update the current user's profile information
    /// </summary>
    /// <param name="userId">The ID of the user to update</param>
    /// <param name="req">Update request with new profile data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated user profile data</returns>
    Task<UserDto> UpdateMeAsync(int userId, UpdateMeRequest req, CancellationToken ct);

    /// <summary>
    /// Ban a user (Admin operation)
    /// </summary>
    /// <param name="userId">The ID of the user to ban</param>
    /// <param name="adminId">The ID of the admin performing the action</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if user was banned successfully</returns>
    Task<bool> BanUserAsync(int userId, int adminId, CancellationToken ct);

    /// <summary>
    /// Unban a user (Admin operation)
    /// </summary>
    /// <param name="userId">The ID of the user to unban</param>
    /// <param name="adminId">The ID of the admin performing the action</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if user was unbanned successfully</returns>
    Task<bool> UnbanUserAsync(int userId, int adminId, CancellationToken ct);
}
