using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniShare.Data;

namespace UniShare.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly UniShareDbContext _db;
        private readonly IWebHostEnvironment _env;
        
        public AdminController(UniShareDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET /api/v1/admin/users?q=&isBanned=&house=
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? q, [FromQuery] bool? isBanned, [FromQuery] string? house)
        {
            var users = _db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                users = users.Where(u => u.FirstName.Contains(q) || u.LastName.Contains(q) || u.Email.Contains(q));
            if (isBanned.HasValue)
                users = users.Where(u => u.IsBanned == isBanned);
            var result = await users.Select(u => new
            {
                id = u.Id,
                firstName = u.FirstName,
                lastName = u.LastName,
                email = u.Email,
                isBanned = u.IsBanned,
                createdAt = u.CreatedAt
            }).ToListAsync();
            return Ok(result);
        }

        // PUT /api/v1/admin/users/{id}/ban
        [HttpPut("users/{id}/ban")]
        public async Task<IActionResult> BanUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsBanned = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PUT /api/v1/admin/users/{id}/unban
        [HttpPut("users/{id}/unban")]
        public async Task<IActionResult> UnbanUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsBanned = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/v1/admin/reset-database (Development only)
        [HttpPost("reset-database")]
        [AllowAnonymous] // Allow anonymous access for development
        public async Task<IActionResult> ResetDatabase()
        {
            if (!_env.IsDevelopment())
            {
                return BadRequest("This endpoint is only available in development environment.");
            }

            try
            {
                // Clear existing data
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM Messages");
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM ConversationParticipants");
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM Conversations");
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM ItemImages");
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM Items");
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM Users");
                
                // Reset identity seeds
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Users', RESEED, 0)");
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Items', RESEED, 0)");
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Conversations', RESEED, 0)");
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Messages', RESEED, 0)");
                
                // Re-seed the database
                await DevSeeder.SeedAsync(_db);
                
                return Ok(new { message = "Database reset and re-seeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to reset database", error = ex.Message });
            }
        }
    }
}
