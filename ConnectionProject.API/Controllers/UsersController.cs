using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;
using ConnectionProject.API.Controllers.Base;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Handles user profile operations
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : BaseApiController
{
    private readonly IUserService _service;

    public UsersController(IUserService service, ILogger<UsersController> logger) : base(logger)
    {
        _service = service;
    }

    /// <summary>
    /// Upload a profile image for the current authenticated user
    /// </summary>
    /// <param name="file">Image file sent as multipart/form-data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>JSON object containing the public URL</returns>
    [HttpPost("me/upload-profile-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult> UploadProfileImage(IFormFile file, CancellationToken ct)
    {
        if (file is null)
        {
            return BadRequest(new { message = "No file was provided" });
        }

        try
        {
            var userId = GetCurrentUserId();
            var url = await _service.UploadProfileImageAsync(userId, file, ct);
            return Ok(new { url });
        }
        catch (FluentValidation.ValidationException ex)
        {
            Logger.LogWarning(ex, "Profile image validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized upload attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading profile image");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Get the current authenticated user's profile
    /// </summary>
    /// <remarks>
    /// Retrieves the profile information for the currently authenticated user.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/users/me
    ///     Authorization: Bearer {token}
    /// 
    /// Requires a valid JWT token in the Authorization header.
    /// </remarks>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User profile data</returns>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _service.GetMeAsync(userId, ct);
            return user is null ? NotFound(new { message = "User not found" }) : Ok(user);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }

    /// <summary>
    /// Update the current authenticated user's profile
    /// </summary>
    /// <remarks>
    /// Updates the profile information for the currently authenticated user.
    /// 
    /// Sample request:
    /// 
    ///     PUT /api/users/me
    ///     Authorization: Bearer {token}
    ///     {
    ///        "phone": "+1234567890",
    ///        "house": "Johnson Hall",
    ///        "profileImageUrl": "https://example.com/images/profile.jpg"
    ///     }
    /// 
    /// All fields are optional. Validation rules:
    /// - Phone: max 20 characters
    /// - House: max 50 characters
    /// - ProfileImageUrl: must be a valid absolute URL
    /// 
    /// Requires a valid JWT token in the Authorization header.
    /// </remarks>
    /// <param name="req">Update request with new profile data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated user profile data</returns>
    /// <response code="200">Profile successfully updated</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateMeRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();
            var user = await _service.UpdateMeAsync(userId, req, ct);
            return Ok(user);
        }
        catch (FluentValidation.ValidationException ex)
        {
            Logger.LogWarning("Validation failed for update profile: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            Logger.LogWarning("User not found for update: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }
}
