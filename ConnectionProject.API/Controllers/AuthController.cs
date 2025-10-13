using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;
using ConnectionProject.API.Controllers.Base;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Handles user authentication operations
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous] // Explicitly allow anonymous access to auth endpoints
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger) : base(logger)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <remarks>
    /// Register a new user with Principia College email domain.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///        "firstName": "John",
    ///        "lastName": "Doe",
    ///        "email": "john.doe@principia.edu",
    ///        "password": "SecurePassword123!"
    ///     }
    /// 
    /// Only @principia.edu email addresses are allowed for registration.
    /// Password should be at least 8 characters with mixed case, numbers, and symbols.
    /// </remarks>
    /// <param name="request">User registration details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User ID of the newly created account</returns>
    /// <response code="200">User successfully registered</response>
    /// <response code="400">Invalid request data or email domain not allowed</response>
    /// <response code="409">User with this email already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 409)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
            return validationResult;

        try
        {
            var userId = await _authService.RegisterAsync(request, ct);
            return Ok(new { userId });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            Logger.LogWarning("Registration failed - duplicate email: {Email}", request.Email);
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("@principia.edu"))
        {
            Logger.LogWarning("Registration failed - invalid domain: {Email}", request.Email);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Registration failed for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error occurred during registration" });
        }
    }

    /// <summary>
    /// Authenticate user and receive JWT token
    /// </summary>
    /// <remarks>
    /// Authenticate with email and password to receive a JWT token for accessing protected endpoints.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///        "email": "john.doe@principia.edu",
    ///        "password": "SecurePassword123!"
    ///     }
    /// 
    /// The returned JWT token should be used in the Authorization header as "Bearer {token}" 
    /// for all subsequent requests to protected endpoints.
    /// 
    /// Token expires after 24 hours by default.
    /// </remarks>
    /// <param name="request">User login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>JWT token and user information</returns>
    /// <response code="200">Login successful, returns JWT token</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
            return validationResult;

        try
        {
            var loginResponse = await _authService.LoginAsync(request, ct);
            return Ok(loginResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning("Login failed for email: {Email} - {Message}", request.Email, ex.Message);
            return Unauthorized(new { message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Login failed for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error occurred during login" });
        }
    }
}