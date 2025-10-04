using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.services.DTOs;
using UniShareProject.services.Interfaces;
using UniShareProject.services.Settings;

namespace UniShareProject.services.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly JwtSettings _jwt;

    public AuthService(IUserRepository userRepository, IUnitOfWorkFactory unitOfWorkFactory, IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _unitOfWorkFactory = unitOfWorkFactory;
        _jwt = jwtOptions.Value;
    }

    public async Task<int> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        // Enforce Principia domain
        if (!req.Email.EndsWith("@principia.edu", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only @principia.edu email addresses are allowed");
        }

        await using var uow = _unitOfWorkFactory.Create();

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(req.Email, uow, ct))
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Hash password with BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        // Create user object
        var user = new User
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsBanned = false,
            IsAdmin = false,
            IsDeleted = false
        };

        // Insert user and get new UserId
        try
        {
            var userId = await _userRepository.InsertAsync(user, uow, ct);
            await uow.CommitAsync();
            return userId;
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        await using var uow = _unitOfWorkFactory.Create();

        // Fetch user by email
        var user = await _userRepository.GetByEmailAsync(req.Email, uow, ct);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        // Verify password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        // Ensure user is not banned or deleted
        if (user.IsBanned)
            throw new UnauthorizedAccessException("Account has been banned");

        if (user.IsDeleted)
            throw new UnauthorizedAccessException("Account has been deleted");

        // Generate JWT token
        var token = GenerateJwtToken(user);

        // Return login response
        return new LoginResponse(
            Token: token,
            UserId: user.UserId,
            Email: user.Email,
            Name: $"{user.FirstName} {user.LastName}".Trim()
        );
    }

    private string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_jwt.Key);

        // Create claims - Adding ClaimTypes.NameIdentifier for compatibility
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Add this for better compatibility
            new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()), // Add this for compatibility
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email), // Add this for compatibility
            new("role", user.IsAdmin ? "admin" : "user"),
            new(ClaimTypes.Role, user.IsAdmin ? "admin" : "user"), // Add this for compatibility
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }
}