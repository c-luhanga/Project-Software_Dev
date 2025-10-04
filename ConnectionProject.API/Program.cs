using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UniShareProject.Repository.Repositories;
using UniShareProject.services.Services;
using FluentValidation.AspNetCore;
using FluentValidation;
using Microsoft.Extensions.Options;
using UniShareProject.API.Settings;
using UniShareProject.services.Settings;
using Microsoft.Data.SqlClient;
using UniShareProject.Repository.Implementations;
using UniShareProject.Repository.Data.Interfaces;
using UniShareProject.Repository.Data.Implementation;
using UniShareProject.services.Interfaces;
using UniShareProject.services.Implementations;
using UniShareProject.services.Mapping;
using UniShareProject.services.Validators;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ConnectionProject.API.Middleware;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UniShareProject.services.Settings.JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Bind Database settings (expects section: ConnectionStrings:DefaultConnection)
builder.Services.Configure<DatabaseSettings>(options =>
{
    options.DefaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
});

// Add services to the container.
builder.Services.AddControllers();

// FluentValidation - Register validators from both API and Services assemblies
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblyContaining<Program>() // API assembly
                .AddValidatorsFromAssemblyContaining<CreateItemRequestValidator>(); // Services assembly

// AutoMapper - Add profiles from services assembly
builder.Services.AddAutoMapper(typeof(UniShareProfile), typeof(Program));

// Direct IDbConnection registration (if ever needed for Dapper without UoW)
builder.Services.AddScoped<SqlConnection>(sp =>
{
    var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    return new SqlConnection(dbSettings.DefaultConnection);
});

// Database and Data Access (UnitOfWork pattern still using factory)
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
{
    var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    return new SqlConnectionFactory(dbSettings.DefaultConnection);
});
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Services - Updated to use the new IAuthService interface and ItemService from Implementations
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IItemService, UniShareProject.services.Implementations.ItemService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<UniShareProject.services.Settings.JwtSettings>()!;
var key = Encoding.ASCII.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.IncludeErrorDetails = true; // helps diagnose token issues
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        // Add these to help with claim mapping
        NameClaimType = "sub",
        RoleClaimType = "role"
    };

    // Detailed event logging for bearer auth
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtBearerEvents");
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                logger.LogInformation("[Auth] No Authorization header found for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                var preview = authHeader.Length > 20 ? authHeader[..20] + "..." : authHeader;
                logger.LogInformation("[Auth] Authorization header received: {Preview} for {Method} {Path}", preview, context.Request.Method, context.Request.Path);
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtBearerEvents");
            var sub = context.Principal?.FindFirst("sub")?.Value
                      ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation("[Auth] Token validated. UserId(sub): {Sub}", sub ?? "<null>");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtBearerEvents");
            logger.LogWarning(context.Exception, "[Auth] Authentication failed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtBearerEvents");
            logger.LogWarning("[Auth] Challenge issued. Error: {Error}, Description: {Description}", context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// Authorization with default policy requiring authentication
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "UniShare API", 
        Version = "v1.0",
        Description = @"
**UniShare API - Principia College Marketplace**

A RESTful API for the UniShare project, enabling students at Principia College to buy, sell, and trade items within their campus community.

## Features
- **User Authentication**: JWT-based authentication with @principia.edu email validation
- **Item Management**: Create, search, and manage marketplace listings
- **Purchase System**: Request-to-purchase workflow with status tracking
- **Category & Condition Filtering**: Organized browsing by type and condition
- **Pagination**: Efficient browsing of large item collections

## Getting Started
1. **Register**: Create an account with your @principia.edu email
2. **Login**: Authenticate to receive a JWT token
3. **Authorize**: Click the ?? button below and enter your token
4. **Start Trading**: Create listings, browse items, and request purchases

## Categories
- **1**: Books & Textbooks
- **2**: Electronics
- **3**: Furniture
- **4**: Clothing
- **5**: Other

## Item Conditions  
- **1**: Like New
- **2**: Good
- **3**: Fair
- **4**: Poor

## Item Status
- **1**: Active (available for purchase)
- **2**: Pending (purchase requested)
- **3**: Sold (transaction completed)
- **4**: Withdrawn (removed from sale)
",
        Contact = new OpenApiContact
        {
            Name = "UniShare Development Team",
            Email = "unishare@principia.edu",
            Url = new Uri("https://github.com/c-luhanga/Project-Software_Dev")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add JWT Authentication to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = @"Enter your JWT token in the text input below.

**Example:** `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`

To get a token:
1. Use the `/api/auth/register` endpoint to create an account
2. Use the `/api/auth/login` endpoint to authenticate  
3. Copy the `token` value from the response
4. Paste it in the field above (without 'Bearer ' prefix)
5. Click **Authorize** to apply to all requests"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Enable XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Include XML comments from services assembly if available
    var servicesXmlFile = "UniShareProject.services.xml";
    var servicesXmlPath = Path.Combine(AppContext.BaseDirectory, servicesXmlFile);
    if (File.Exists(servicesXmlPath))
    {
        c.IncludeXmlComments(servicesXmlPath);
    }

    // Order controllers and operations alphabetically
    c.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniShare API V1.0");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "UniShare API Documentation";
        
        // Enhanced UI configuration
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableValidator();
        
        // Show the Authorize button prominently
        c.EnablePersistAuthorization();
    });
}

// Enable static files for serving custom CSS and other assets
app.UseStaticFiles();

app.UseHttpsRedirection();

// Add exception handling middleware early in the pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();

// Custom lightweight request/response logging (masks Authorization)
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestResponseLogger");

    var authHeader = context.Request.Headers["Authorization"].ToString();
    string maskedAuth = string.Empty;
    if (!string.IsNullOrEmpty(authHeader))
    {
        var display = authHeader.Length > 20 ? authHeader[..20] + "..." : authHeader;
        maskedAuth = display;
    }

    var userId = context.User?.FindFirst("sub")?.Value
              ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    logger.LogInformation("[HTTP] {Method} {Path}{Query} | Auth: {AuthPresent} {AuthPreview} | UserId: {UserId}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
        string.IsNullOrEmpty(authHeader) ? "none" : "present",
        string.IsNullOrEmpty(maskedAuth) ? string.Empty : maskedAuth,
        userId ?? "<anon>");

    await next();

    logger.LogInformation("[HTTP] Response {StatusCode} for {Method} {Path}", context.Response.StatusCode, context.Request.Method, context.Request.Path);
});

app.UseAuthorization();

app.MapControllers();

// Add a simple root endpoint to show API information
app.MapGet("/", () => new
{
    Title = "UniShare API",
    Version = "v1.0",
    Description = "API for the UniShare project - a platform for sharing items within the Principia College community.",
    Documentation = "/swagger",
    Status = "Online",
    Endpoints = new
    {
        Authentication = "/api/auth",
        Items = "/api/items",
        Documentation = "/swagger"
    },
    SupportedFormats = new[] { "application/json" },
    Authentication = "JWT Bearer Token"
}).AllowAnonymous().WithTags("API Information").WithSummary("Get API information and available endpoints");

app.Run();
