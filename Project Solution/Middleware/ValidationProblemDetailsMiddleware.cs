using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace UniShare.Middleware
{
    public class ValidationProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        public ValidationProblemDetailsMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            // Buffer the response
            var originalBody = context.Response.Body;
            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context);

            if (context.Response.StatusCode == 400 && context.Items.ContainsKey("ValidationProblemDetails"))
            {
                memStream.SetLength(0); // Clear any written content
                context.Response.ContentType = "application/problem+json";
                var problem = context.Items["ValidationProblemDetails"];
                await JsonSerializer.SerializeAsync(context.Response.Body, problem, problem.GetType());
            }
            else
            {
                memStream.Seek(0, SeekOrigin.Begin);
                await memStream.CopyToAsync(originalBody);
            }
            context.Response.Body = originalBody;
        }
    }

    public static class ValidationProblemDetailsMiddlewareExtensions
    {
        public static IApplicationBuilder UseValidationProblemDetails(this IApplicationBuilder builder)
            => builder.UseMiddleware<ValidationProblemDetailsMiddleware>();
    }
}
