using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace UniShareProject.services.DTOs;

/// <summary>
/// Request model for user registration
/// </summary>
/// <param name="FirstName">User's first name (required)</param>
/// <param name="LastName">User's last name (required)</param>
/// <param name="Email">Valid email address with @principia.edu domain (required)</param>
/// <param name="Password">Password with minimum 6 characters (required)</param>
/// <example>
/// {
///   "firstName": "John",
///   "lastName": "Doe", 
///   "email": "john.doe@principia.edu",
///   "password": "SecurePassword123!"
/// }
/// </example>
public record RegisterRequest(
    [property: Required, StringLength(50, MinimumLength = 1)]
    [property: Description("User's first name")]
    string FirstName,
    
    [property: Required, StringLength(50, MinimumLength = 1)]
    [property: Description("User's last name")]
    string LastName,
    
    [property: Required, EmailAddress]
    [property: Description("Valid email address with @principia.edu domain")]
    string Email,
    
    [property: Required, MinLength(6)]
    [property: Description("Password with minimum 6 characters")]
    string Password
);

/// <summary>
/// Request model for user authentication
/// </summary>
/// <param name="Email">Registered email address (required)</param>
/// <param name="Password">User password (required)</param>
/// <example>
/// {
///   "email": "john.doe@principia.edu",
///   "password": "SecurePassword123!"
/// }
/// </example>
public record LoginRequest(
    [property: Required, EmailAddress]
    [property: Description("Registered email address")]
    string Email,
    
    [property: Required]
    [property: Description("User password")]
    string Password
);

/// <summary>
/// Response model for successful authentication
/// </summary>
/// <param name="Token">JWT bearer token for API authentication</param>
/// <param name="UserId">Unique user identifier</param>
/// <param name="Email">User's email address</param>
/// <param name="Name">User's full name</param>
public record LoginResponse(
    [property: Description("JWT bearer token for API authentication")]
    string Token,
    
    [property: Description("Unique user identifier")]
    int UserId,
    
    [property: Description("User's email address")]
    string Email,
    
    [property: Description("User's full name")]
    string Name
);