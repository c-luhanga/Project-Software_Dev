using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConnectionProject.API.Controllers.Base;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Logger instance for the derived controller
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Initializes a new instance of the BaseApiController
    /// </summary>
    /// <param name="logger">Logger instance</param>
    protected BaseApiController(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims
    /// </summary>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">If user ID cannot be extracted or parsed</exception>
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("nameid")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            Logger.LogWarning("[Auth] Unable to extract valid user ID from JWT claims");
            throw new UnauthorizedAccessException("Invalid or missing user identification");
        }

        return userId;
    }

    /// <summary>
    /// Tries to get the current authenticated user's ID from JWT claims without throwing an exception
    /// </summary>
    /// <param name="userId">The extracted user ID if successful</param>
    /// <returns>True if user ID was successfully extracted and parsed; otherwise, false</returns>
    protected bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("nameid")?.Value;
        
        return int.TryParse(userIdClaim, out userId);
    }

    /// <summary>
    /// Gets the current authenticated user's email from JWT claims
    /// </summary>
    /// <returns>User email if found; otherwise, null</returns>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the current authenticated user's name from JWT claims
    /// </summary>
    /// <returns>User name if found; otherwise, null</returns>
    protected string? GetCurrentUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value 
            ?? User.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Checks if the current authenticated user has the admin role
    /// </summary>
    /// <returns>True if user has admin role; otherwise, false</returns>
    protected bool IsCurrentUserAdmin()
    {
        return User.IsInRole("admin");
    }

    /// <summary>
    /// Validates model state and returns BadRequest if invalid
    /// </summary>
    /// <returns>BadRequest result if model state is invalid; otherwise, null</returns>
    protected IActionResult? ValidateModelState()
    {
        return !ModelState.IsValid ? BadRequest(ModelState) : null;
    }
}
