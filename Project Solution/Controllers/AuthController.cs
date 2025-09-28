using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using UniShare.Data;
using System.ComponentModel.DataAnnotations;

namespace UniShare.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UniShareDbContext _db;
        private readonly IConfiguration _config;
        public AuthController(UniShareDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // POST /api/v1/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                if (!dto.Email.EndsWith("@principia.edu", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Email must be a principia.edu address.");
                if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                    return Conflict("Email already registered.");
                var salt = RandomNumberGenerator.GetBytes(16);
                var hash = HashPassword(dto.Password, salt);
                var user = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PasswordHash = hash, // Set the password hash
                    CreatedAt = DateTime.UtcNow,
                    IsBanned = false,
                    Phone = null
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Registration failed: {ex.Message}");
            }
        }

        // POST /api/v1/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                // Log request details after successful model binding
                Console.WriteLine("[LOGIN] Request ContentType: " + Request.ContentType);
                Console.WriteLine("[LOGIN] Request ContentLength: " + Request.ContentLength);
                Console.WriteLine($"[LOGIN] DTO after binding: Email={dto?.Email}, Password={dto?.Password?.Length} chars (null? {dto == null})");
                
                if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                {
                    Console.WriteLine("[LOGIN] BadRequest: Email and password are required.");
                    return BadRequest("Email and password are required.");
                }
                
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null) {
                    Console.WriteLine($"[LOGIN] Unauthorized: No user found for email {dto.Email}");
                    return Unauthorized("Invalid email or password");
                }
                
                // Verify password - handle nullable PasswordHash
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    Console.WriteLine($"[LOGIN] Unauthorized: User {dto.Email} has no password hash stored");
                    return Unauthorized("Invalid email or password");
                }
                
                if (!VerifyPassword(dto.Password, user.PasswordHash))
                {
                    Console.WriteLine($"[LOGIN] Unauthorized: Invalid password for email {dto.Email}");
                    return Unauthorized("Invalid email or password");
                }

                var roles = new List<string>();
                if (user.IsAdmin)
                    roles.Add("admin");
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                };
                foreach (var role in roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));
                
                var jwtKey = GetJwtKey();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(8),
                    signingCredentials: creds
                );
                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                Console.WriteLine($"[LOGIN] Success: Token issued for user {user.Email}");
                return Ok(new { token = jwt });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] Exception: {ex}");
                return StatusCode(500, $"Login failed: {ex.Message}");
            }
        }

        // POST /api/v1/auth/refresh
        [HttpPost("refresh")]
        public IActionResult Refresh() => Ok(); // Stub

        // GET /api/v1/me
        [HttpGet("/api/v1/me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (userId == null) return Unauthorized();
            if (!int.TryParse(userId, out var intUserId)) return Unauthorized();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == intUserId);
            if (user == null) return Unauthorized();
            return Ok(new
            {
                id = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                isBanned = user.IsBanned,
                createdAt = user.CreatedAt
            });
        }

        public class RegisterDto
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        public class LoginDto
        {
            [Required]
            public string Email { get; set; } = string.Empty;
            
            [Required]
            public string Password { get; set; } = string.Empty;
        }

        private string GetJwtKey()
        {
            var key = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(key))
            {
                // Fallback key with minimum 32 characters (256 bits) required for HS256
                key = "dev_secret_key_please_change_32chars_minimum";
            }
            
            // Ensure the key is at least 32 bytes (256 bits) for HS256
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32)
            {
                // Pad the key to meet minimum requirements
                var paddedKey = key.PadRight(32, '0');
                Console.WriteLine($"[JWT] Warning: JWT key was too short ({keyBytes.Length} bytes), padded to 32 bytes");
                return paddedKey;
            }
            
            return key;
        }

        private static string HashPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 2)
                    return false;

                var salt = Convert.FromBase64String(parts[0]);
                var hash = Convert.FromBase64String(parts[1]);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
                var computedHash = pbkdf2.GetBytes(32);

                // Compare computed hash with stored hash
                return CryptographicOperations.FixedTimeEquals(hash, computedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
