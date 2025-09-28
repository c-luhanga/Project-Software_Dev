using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using UniShare.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UniShare.Middleware;
using Serilog;
using Serilog.Events;
using UniShare.Options;

var builder = WebApplication.CreateBuilder(args);

// Serilog setup (optional, basic request logging)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddValidatorsFromAssemblyContaining<UniShare.Data.Validators.ItemCreateDtoValidator>();

// Register UniShareDbContext with SQL Server
builder.Services.AddDbContext<UniShareDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UniShareDb")));

// Bind options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

// Get JwtOptions for JWT setup
var jwtOptions = new JwtOptions();
builder.Configuration.GetSection("Jwt").Bind(jwtOptions);

// Get Jwt:Key from User Secrets or environment
var jwtKey = jwtOptions.Key;
if (string.IsNullOrEmpty(jwtKey))
{
    jwtKey = builder.Configuration["Jwt:Key"] ?? "dev_secret_key_please_change_32chars_minimum";
}

// Ensure the key is at least 32 bytes (256 bits) for HS256
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32)
{
    // Pad the key to meet minimum requirements
    jwtKey = jwtKey.PadRight(32, '0');
    Console.WriteLine($"[JWT Startup] Warning: JWT key was too short ({keyBytes.Length} bytes), padded to 32 bytes");
}

// JWT Auth configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// CORS policy from options
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "UniShare API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Suppress default model state invalid filter
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Use Serilog request logging
app.UseSerilogRequestLogging();

// Global exception handling middleware (RFC7807 Problem Details)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred!",
            Detail = exceptionHandlerPathFeature?.Error.Message,
            Instance = context.Request.Path
        };
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

// Custom validation middleware
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 400 && context.Items.ContainsKey("ValidationProblemDetails"))
    {
        // Already handled by ValidationProblemDetailsMiddleware
        return;
    }
    if (!context.Request.HasFormContentType && context.Request.Method == "POST" || context.Request.Method == "PUT")
    {
        if (context.Items.TryGetValue("ModelState", out var modelStateObj) && modelStateObj is ModelStateDictionary modelState && !modelState.IsValid)
        {
            var problemDetails = new ValidationProblemDetails(modelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
});

app.UseValidationProblemDetails();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Swagger with JWT example value
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<UniShareDbContext>();
        await DevSeeder.SeedAsync(db);
    }
}

app.Run();
