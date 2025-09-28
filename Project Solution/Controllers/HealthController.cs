using Microsoft.AspNetCore.Mvc;
using UniShare.Data;
using System.Reflection;

namespace UniShare.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class HealthController : ControllerBase
    {
        private readonly UniShareDbContext _db;
        public HealthController(UniShareDbContext db)
        {
            _db = db;
        }

        // GET /api/v1/health
        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            string dbStatus = "ok";
            try
            {
                if (!await _db.Database.CanConnectAsync())
                    dbStatus = "down";
            }
            catch
            {
                dbStatus = "down";
            }
            return Ok(new { status = "ok", db = dbStatus });
        }

        // GET /api/v1/version
        [HttpGet("version")]
        public IActionResult Version()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            var gitSha = Environment.GetEnvironmentVariable("GIT_SHA") ?? "unknown";
            return Ok(new { version, gitSha });
        }
    }
}
