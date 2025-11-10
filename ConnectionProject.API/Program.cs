using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UniShareProject.Repository.Repositories;
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
using ConnectionProject.API.Hubs;
using ConnectionProject.API.Services;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using UniShareProject.API.Authorization; // Ensure this namespace exists for your custom handler

// Ensure wwwroot directory exists (required by Microsoft.NET.Sdk.Web even for pure APIs)
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

// Ensure uploads directory exists for static file serving
var uploadsPath = Path.Combine(wwwrootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UniShareProject.services.Settings.JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Configure file upload options
builder.Services.Configure<UniShareProject.services.Interfaces.FileUploadOptions>(options =>
{
    options.MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    options.SupportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
    options.SupportedMimeTypes = new[] { 
        "image/jpeg", 
        "image/png", 
        "image/gif", 
        "image/webp", 
        "image/bmp" 
    };
    options.StorageProvider = "Local"; // Can be changed to "S3", "Azure", etc.
});

// Bind Database settings (expects section: ConnectionStrings:DefaultConnection)
builder.Services.Configure<DatabaseSettings>(options =>
{
    options.DefaultConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("UniSharePolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",     // React development server
                "http://localhost:3001",     // Alternative React port
                "http://localhost:4200",     // Angular development server
                "http://localhost:5173",     // Vite development server
                "http://localhost:8080",     // Vue.js development server
                "https://localhost:3000",    // HTTPS versions
                "https://localhost:3001",
                "https://localhost:4200",
                "https://localhost:5173",
                "https://localhost:8080"
            )
            .AllowAnyMethod()                    // Allow GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()                    // Allow any request headers
            .AllowCredentials()                  // Allow cookies/credentials - Required for SignalR
            .WithExposedHeaders("Authorization", "Content-Disposition"); // Expose specific headers to client
    });

    // Alternative: More permissive policy for development (use carefully)
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();

// Add SignalR for real-time messaging
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add HttpContextAccessor for authorization handlers
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddScoped<IItemImageRepository, ItemImageRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Register new abstractions interfaces
builder.Services.AddScoped<UniShareProject.Repository.Abstractions.IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<UniShareProject.Repository.Abstractions.IMessageRepository, MessageRepository>();

// Services - Updated to use the new IAuthService interface and ItemService from Implementations
builder.Services.AddScoped<IAuthService, UniShareProject.services.Implementations.AuthService>();
builder.Services.AddScoped<IItemService, UniShareProject.services.Implementations.ItemService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();

// Real-time notification service
builder.Services.AddScoped<UniShareProject.services.Interfaces.IRealTimeNotificationService, SignalRNotificationService>();

// File Upload Services - Register based on configuration
builder.Services.AddScoped<IFileUploadService, UniShareProject.services.Implementations.LocalFileUploadService>();

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
        RoleClaimType = ClaimTypes.Role // <-- Map role claim to ClaimTypes.Role
    };

    // Detailed event logging for bearer auth
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("JwtBearerEvents");
            var authHeader = context.Request.Headers["Authorization"].ToString();
            
            // Support SignalR token authentication via query parameter
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
                logger.LogInformation("[Auth] SignalR token received via query parameter for {Path}", path);
            }
            else if (string.IsNullOrEmpty(authHeader))
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
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // "AdminOnly" policy: RequireRole("admin")
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));

    // "PrincipiaEmail" policy: custom assertion for @principia.edu
    options.AddPolicy("PrincipiaEmail", policy =>
        policy.RequireAssertion(context =>
        {
            var email = context.User.Identity?.Name
                ?? context.User.FindFirst("email")?.Value
                ?? context.User.FindFirst(ClaimTypes.Email)?.Value;

            return !string.IsNullOrEmpty(email) && email.EndsWith("@principia.edu", StringComparison.OrdinalIgnoreCase);
        }));

    // "ItemOwnerOrAdmin" policy: empty requirement, handled by custom handler
    options.AddPolicy("ItemOwnerOrAdmin", policy =>
        policy.Requirements.Add(new ItemOwnerOrAdminRequirement()));
});

// Register custom authorization handler for "ItemOwnerOrAdmin"
builder.Services.AddScoped<IAuthorizationHandler, ItemOwnerOrAdminHandler>();

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

A RESTful API for the UniShare project, enabling students at Principia College to buy, sell, trade, and communicate within their campus community.

## Features
- **User Authentication**: JWT-based authentication with @principia.edu email validation
- **Item Management**: Create, search, and manage marketplace listings
- **Purchase System**: Request-to-purchase workflow with status tracking
- **Messaging System**: Real-time communication between buyers and sellers
- **Category & Condition Filtering**: Organized browsing by type and condition
- **Pagination**: Efficient browsing of large item collections
- **Admin Dashboard**: Administrative tools for platform management

## Getting Started
1. **Register**: Create an account with your @principia.edu email
2. **Login**: Authenticate to receive a JWT token
3. **Authorize**: Click the ?? button below and enter your token
4. **Start Trading**: Create listings, browse items, and communicate with other users

## API Endpoints Overview

### ?? Authentication (`/api/v1/auth`)
- **POST /register** - Create new user account
- **POST /login** - Authenticate and receive JWT token
- **POST /refresh** - Refresh expired tokens

### ?? Item Management (`/api/v1/items`)
- **GET /search** - Search and filter items with pagination
- **POST /** - Create new item listing
- **GET /{id}** - Get item details
- **PUT /{id}/status** - Update item status
- **POST /{id}/images** - Add images to item
- **DELETE /{id}** - Delete item (admin/owner only)

### ?? Messaging (`/api/v1/messages`)
- **POST /conversations** - Start new conversation with another user
- **POST /send** - Send message in existing conversation
- **GET /conversations/{id}** - Get messages from conversation (paginated)
- **GET /inbox** - Get user's conversation list (paginated)

### ?? User Management (`/api/v1/users`)
- **GET /me** - Get current user profile
- **PUT /me** - Update user profile
- **POST /ban** - Ban user (admin only)
- **POST /unban** - Unban user (admin only)

### ?? Admin Dashboard (`/api/v1/admin`)
- **GET /dashboard** - Get platform statistics and metrics

## Data Reference

### Categories
- **1**: Books & Textbooks
- **2**: Electronics
- **3**: Furniture
- **4**: Clothing
- **5**: Other

### Item Conditions  
- **1**: Like New - Excellent condition, minimal wear
- **2**: Good - Minor signs of use, fully functional
- **3**: Fair - Noticeable wear but still usable
- **4**: Poor - Significant wear, may need repairs

### Item Status
- **1**: Active - Available for purchase
- **2**: Pending - Purchase request submitted
- **3**: Sold - Transaction completed
- **4**: Withdrawn - Removed from sale

### Messaging Features
- **Conversation Management**: Automatic conversation creation/retrieval
- **Real-time Messaging**: Send and receive messages instantly
- **Unread Tracking**: Keep track of unread messages per conversation
- **Item Context**: Link conversations to specific marketplace items
- **Pagination**: Efficient loading of message history
- **Participant Validation**: Secure access control for conversations

## Response Formats

All API responses follow a consistent JSON format:
- **Success**: Direct data or `{ data: {...} }` wrapper
- **Error**: `{ error: ""message"" }` or `{ errors: [...] }` for validation
- **Pagination**: Includes `total`, `page`, `pageSize`, `totalPages`, `hasNextPage`, `hasPreviousPage`

## Rate Limiting & Security
- JWT tokens expire after 24 hours
- All endpoints (except auth) require valid authentication
- Role-based access control for admin functions
- Input validation on all request bodies
- SQL injection protection via parameterized queries
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
1. Use the `/api/v1/auth/register` endpoint to create an account
2. Use the `/api/v1/auth/login` endpoint to authenticate  
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

    // Add custom operation processor for better endpoint organization
    c.TagActionsBy(api =>
    {
        // Check if this is a controller-based action
        if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName))
        {
            return controllerName switch
            {
                "Auth" => new[] { "?? Authentication" },
                "Items" => new[] { "?? Item Management" },
                "Messages" => new[] { "?? Messaging System" },
                "Users" => new[] { "?? User Management" },
                "Admin" => new[] { "?? Admin Dashboard" },
                _ => new[] { controllerName ?? "Other" }
            };
        }
        
        // Handle minimal API endpoints
        return new[] { "?? API Information" };
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

    // Custom ordering: Auth first, then functional endpoints, then admin
    c.OrderActionsBy(apiDesc =>
    {
        // Check if this is a controller-based action
        if (apiDesc.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) &&
            apiDesc.ActionDescriptor.RouteValues.TryGetValue("action", out var action))
        {
            var method = apiDesc.HttpMethod;
            
            return controller switch
            {
                "Auth" => $"1_{controller}_{action}_{method}",
                "Items" => $"2_{controller}_{action}_{method}",
                "Messages" => $"3_{controller}_{action}_{method}",
                "Users" => $"4_{controller}_{action}_{method}",
                "Admin" => $"5_{controller}_{action}_{method}",
                _ => $"9_{controller}_{action}_{method}"
            };
        }
        
        // Handle minimal API endpoints
        var path = apiDesc.RelativePath ?? "/";
        var methodName = apiDesc.HttpMethod;
        return $"0_MinimalAPI_{path.Replace("/", "_")}_{methodName}";
    });
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
        c.DocumentTitle = "UniShare API Documentation - Principia College Marketplace";
        
        // Enhanced UI configuration
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableValidator();
        c.ShowExtensions();
        
        // Show the Authorize button prominently
        c.EnablePersistAuthorization();
        
        // Configure default expansion
        c.DefaultModelsExpandDepth(1);
        c.DefaultModelExpandDepth(1);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();

// Configure static file serving for uploaded images
app.UseStaticFiles(); // Default wwwroot serving
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Add security headers for image files
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // Set cache headers for better performance
        var cache = TimeSpan.FromDays(30);
        ctx.Context.Response.Headers["Cache-Control"] = $"public, max-age={cache.TotalSeconds}";
        ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.Add(cache).ToString("R");
    }
});

// Add exception handling middleware early in the pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Enable CORS - Must be placed after UseRouting() if using it, but before UseAuthentication()
app.UseCors(app.Environment.IsDevelopment() ? "DevelopmentPolicy" : "UniSharePolicy");

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

// Map SignalR hub for real-time messaging
app.MapHub<MessagingHub>("/hubs/messaging");

// Add a simple root endpoint to show API information
app.MapGet("/", () => new
{
    Title = "UniShare API",
    Version = "v1.0",
    Description = "API for the UniShare project - a platform for sharing items and communication within the Principia College community.",
    Documentation = "/swagger",
    Status = "Online",
    Endpoints = new
    {
        Authentication = "/api/v1/auth",
        Items = "/api/v1/items",
        Messages = "/api/v1/messages",
        Users = "/api/v1/users", 
        Admin = "/api/v1/admin",
        Documentation = "/swagger"
    },
    Features = new[]
    {
        "User Authentication & Authorization",
        "Item Marketplace with Search & Filtering", 
        "Real-time Messaging System",
        "Purchase Request Workflow",
        "Admin Dashboard & Management",
        "Comprehensive API Documentation"
    },
    SupportedFormats = new[] { "application/json" },
    Authentication = "JWT Bearer Token",
    SampleEndpoints = new
    {
        Register = "POST /api/v1/auth/register",
        Login = "POST /api/v1/auth/login", 
        SearchItems = "GET /api/v1/items/search?q=textbook&page=1",
        StartConversation = "POST /api/v1/messages/conversations",
        SendMessage = "POST /api/v1/messages/send",
        GetInbox = "GET /api/v1/messages/inbox?page=1&pageSize=20"
    }
}).AllowAnonymous().WithTags("API Information").WithSummary("Get API information and available endpoints");

app.Run();
