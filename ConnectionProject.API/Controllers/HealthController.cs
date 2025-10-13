using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using UniShareProject.Repository.Data.Interfaces;

namespace ConnectionProject.API.Controllers;

/// <summary>
/// Provides health check and version information endpoints for monitoring
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IDbConnectionFactory connectionFactory, ILogger<HealthController> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <remarks>
    /// Returns the health status of the API and database connection.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/health
    /// 
    /// This endpoint is public and does not require authentication.
    /// Useful for load balancers, monitoring tools, and deployment health checks.
    /// 
    /// Response example:
    /// 
    ///     {
    ///       "status": "healthy",
    ///       "database": "connected",
    ///       "timestamp": "2025-01-13T10:30:00Z"
    ///     }
    /// 
    /// </remarks>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Health status information</returns>
    /// <response code="200">Service is healthy</response>
    /// <response code="503">Service is unhealthy (database connection failed)</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        string dbStatus = "connected";
        string overallStatus = "healthy";
        int statusCode = 200;

        try
        {
            // Test database connection
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);
            
            // Verify we can execute a simple query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(ct);
            
            _logger.LogInformation("[Health] Health check passed - database connected");
        }
        catch (Exception ex)
        {
            dbStatus = "disconnected";
            overallStatus = "unhealthy";
            statusCode = 503;
            
            _logger.LogError(ex, "[Health] Health check failed - database connection error");
        }

        var response = new
        {
            status = overallStatus,
            database = dbStatus,
            timestamp = DateTime.UtcNow
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Get API version information
    /// </summary>
    /// <remarks>
    /// Returns version information about the running API instance.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/health/version
    /// 
    /// This endpoint is public and does not require authentication.
    /// Useful for deployment verification and troubleshooting.
    /// 
    /// Response example:
    /// 
    ///     {
    ///       "version": "1.0.0.0",
    ///       "gitSha": "abc123def",
    ///       "buildDate": "2025-01-13T10:30:00Z",
    ///       "environment": "Development",
    ///       "dotnetVersion": "8.0.0"
    ///     }
    /// 
    /// </remarks>
    /// <returns>Version and build information</returns>
    /// <response code="200">Version information retrieved successfully</response>
    [HttpGet("version")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var gitSha = Environment.GetEnvironmentVariable("GIT_SHA") ?? "unknown";
        var buildDate = Environment.GetEnvironmentVariable("BUILD_DATE") ?? "unknown";
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown";
        var dotnetVersion = Environment.Version.ToString();

        _logger.LogInformation("[Health] Version info requested - v{Version}", version);

        var response = new
        {
            version,
            gitSha,
            buildDate,
            environment,
            dotnetVersion,
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Detailed health check with component status
    /// </summary>
    /// <remarks>
    /// Returns detailed health information including all system components.
    /// 
    /// Sample request:
    /// 
    ///     GET /api/health/detailed
    /// 
    /// This endpoint provides more granular health information for monitoring systems.
    /// 
    /// Response example:
    /// 
    ///     {
    ///       "status": "healthy",
    ///       "components": {
    ///         "database": {
    ///           "status": "healthy",
    ///           "responseTime": "15ms"
    ///         },
    ///         "api": {
    ///           "status": "healthy",
    ///           "uptime": "2d 5h 30m"
    ///         }
    ///       },
    ///       "timestamp": "2025-01-13T10:30:00Z"
    ///     }
    /// 
    /// </remarks>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed health status</returns>
    /// <response code="200">Detailed health information</response>
    /// <response code="503">One or more components are unhealthy</response>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 503)]
    public async Task<IActionResult> GetDetailedHealth(CancellationToken ct)
    {
        var components = new Dictionary<string, object>();
        string overallStatus = "healthy";
        int statusCode = 200;

        // Database health check with timing
        var dbStartTime = DateTime.UtcNow;
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(ct);
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(ct);
            
            var responseTime = (DateTime.UtcNow - dbStartTime).TotalMilliseconds;
            
            components["database"] = new
            {
                status = "healthy",
                responseTime = $"{responseTime:F0}ms"
            };
        }
        catch (Exception ex)
        {
            var responseTime = (DateTime.UtcNow - dbStartTime).TotalMilliseconds;
            components["database"] = new
            {
                status = "unhealthy",
                responseTime = $"{responseTime:F0}ms",
                error = ex.Message
            };
            overallStatus = "unhealthy";
            statusCode = 503;
            
            _logger.LogError(ex, "[Health] Detailed health check - database unhealthy");
        }

        // API health (always healthy if we can respond)
        components["api"] = new
        {
            status = "healthy",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"
        };

        var response = new
        {
            status = overallStatus,
            components,
            timestamp = DateTime.UtcNow
        };

        return StatusCode(statusCode, response);
    }
}