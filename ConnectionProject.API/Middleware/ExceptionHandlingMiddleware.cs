using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace ConnectionProject.API.Middleware;

/// <summary>
/// Middleware to handle exceptions and convert them to appropriate HTTP responses with ProblemDetails
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);
        
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Detail = exception.Message
        };

        switch (exception)
        {
            case InvalidOperationException:
                problemDetails.Title = "Invalid Operation";
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                break;

            case ArgumentException:
                problemDetails.Title = "Invalid Argument";
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                break;

            case UnauthorizedAccessException:
                problemDetails.Title = "Unauthorized";
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                problemDetails.Detail = "Access denied";
                break;

            case KeyNotFoundException:
                problemDetails.Title = "Not Found";
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                break;

            default:
                // Check for specific message patterns to determine status codes
                if (exception.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    problemDetails.Title = "Not Found";
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                }
                else if (exception.Message.Contains("not available", StringComparison.OrdinalIgnoreCase) ||
                         exception.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
                         exception.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
                {
                    problemDetails.Title = "Conflict";
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                }
                else if (exception.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
                         exception.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase))
                {
                    problemDetails.Title = "Forbidden";
                    problemDetails.Status = (int)HttpStatusCode.Forbidden;
                    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                }
                else
                {
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                    problemDetails.Detail = "An unexpected error occurred";
                }
                break;
        }

        return problemDetails;
    }
}