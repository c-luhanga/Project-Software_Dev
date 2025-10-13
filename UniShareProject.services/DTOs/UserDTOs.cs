namespace UniShareProject.services.DTOs;

/// <summary>
/// Data transfer object for user profile information
/// </summary>
/// <param name="UserId">User's unique identifier</param>
/// <param name="FirstName">User's first name</param>
/// <param name="LastName">User's last name</param>
/// <param name="Email">User's email address</param>
/// <param name="Phone">User's phone number (optional)</param>
/// <param name="House">User's house name (optional)</param>
/// <param name="ProfileImageUrl">URL to user's profile image (optional)</param>
/// <param name="IsAdmin">Whether the user has admin privileges</param>
/// <param name="IsBanned">Whether the user is banned</param>
/// <param name="CreatedAt">When the user account was created</param>
/// <param name="LastSeen">Last time the user was active (optional)</param>
public record UserDto(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? House,
    string? ProfileImageUrl,
    bool IsAdmin,
    bool IsBanned,
    DateTime CreatedAt,
    DateTime? LastSeen
);

/// <summary>
/// Request model for updating user profile information
/// </summary>
/// <param name="Phone">User's phone number (optional, max 20 characters)</param>
/// <param name="House">User's house name (optional, max 50 characters)</param>
/// <param name="ProfileImageUrl">URL to user's profile image (optional, must be valid absolute URL)</param>
public record UpdateMeRequest(
    string? Phone,
    string? House,
    string? ProfileImageUrl
);
