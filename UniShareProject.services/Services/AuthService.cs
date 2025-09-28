using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UniShareProject.Repository.Data;
using UniShareProject.Repository.Models;
using UniShareProject.Repository.Repositories;
using UniShareProject.services.DTOs;
using System.Globalization;

namespace UniShareProject.services.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IDbConnectionFactory connectionFactory, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _connectionFactory = connectionFactory;
        _configuration = configuration;
    }

    public async Task<int> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        // Enforce Principia domain
        if (!req.Email.EndsWith("@principia.edu", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only @principia.edu email addresses are allowed");
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(req.Email, ct))
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
        await using var unitOfWork = new UnitOfWork(_connectionFactory);
        try
        {
            var userId = await _userRepository.InsertAsync(user, ct);
            await unitOfWork.CommitAsync();
            return userId;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        // Fetch user by email
        var user = await _userRepository.GetByEmailAsync(req.Email, ct);
        
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Ensure user is not banned or deleted
        if (user.IsBanned)
        {
            throw new UnauthorizedAccessException("Account has been banned");
        }

        if (user.IsDeleted)
        {
            throw new UnauthorizedAccessException("Account has been deleted");
        }

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
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiresMinutes = int.Parse(jwtSettings["ExpiresMinutes"]!);

        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Create claims
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.IsAdmin ? "admin" : "user"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, 
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = issuer,
            Audience = audience
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}