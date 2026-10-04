using System.Security.Claims;
using System.Text;
using System.Text.Json;
using InternshipManagementApi.Common.Models;
using InternshipManagementApi.Data;
using InternshipManagementApi.Middleware;
using InternshipManagementApi.Repositories;
using InternshipManagementApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Cloud Environment: Bind dynamic port (e.g., Render, Railway)
var dynamicPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(dynamicPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{dynamicPort}");
}

// 1. Configure Database Connection (MySQL via Pomelo EF Core)
var connectionString = ResolveConnectionString(builder.Configuration, builder.Environment.IsProduction());

builder.Services.AddDbContext<AppDbContext>(options =>
{
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    catch
    {
        // Fallback for cloud cold-starts or strict network policies
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
    }
});

// 2. Configure Dependency Injection (Repositories & Services)
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMentorRepository, MentorRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentRegistrationService, StudentRegistrationService>();
builder.Services.AddScoped<IMentorTaskService, MentorTaskService>();
builder.Services.AddScoped<IStudentScheduleService, StudentScheduleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMentorService, MentorService>();
builder.Services.AddScoped<IStudentService, StudentService>();

// 3. Configure JWT Authentication & Authorization
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
    jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "A JWT signing key must be configured with JWT_SECRET_KEY or Jwt:Key.");
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("The JWT signing key must be at least 32 UTF-8 bytes.");

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "InternshipManagementApi";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "InternshipManagementClient";

// Ensure token creation uses exactly the same resolved values as validation.
builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var payload = ApiResponse.Fail("Unauthorized: Missing, invalid, or expired Bearer token.");
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var payload = ApiResponse.Fail("Forbidden: You do not possess the required role permissions to perform this action.");
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        }
    };
});

builder.Services.AddAuthorization();

// 4. Configure Controllers & Model Validation
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                );

            var response = ApiResponse.Fail("Validation failed. Please verify request inputs.", errors);
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
        };
    });

// 5. Configure CORS (Dynamic production origins from CORS_ALLOWED_ORIGINS)
var allowedOriginsEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?? builder.Configuration["CORS_ALLOWED_ORIGINS"];
var isDevelopmentEnvironment = builder.Environment.IsDevelopment();

var allowedOrigins = isDevelopmentEnvironment
    ? new List<string>
    {
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:3000",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:5174"
    }
    : new List<string>();

if (!string.IsNullOrWhiteSpace(allowedOriginsEnv))
{
    var parsedOrigins = allowedOriginsEnv
        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var origin in parsedOrigins)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            (!isDevelopmentEnvironment && uri.Scheme != Uri.UriSchemeHttps) ||
            origin.Contains('*', StringComparison.Ordinal) || !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException($"Invalid CORS origin '{origin}'. Configure a full origin without a path or wildcard.");
        }

        allowedOrigins.Add(uri.GetLeftPart(UriPartial.Authority));
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (allowedOrigins.Count == 0)
            return;

        policy.WithOrigins(allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 6. Configure Swagger/OpenAPI with JWT Bearer Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Internship Management System API",
        Version = "v1",
        Description = "Production-ready RESTful API for managing internships, mentors, students, and user access control."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter token directly or with 'Bearer ' prefix (e.g., 'eyJhbGciOi...').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
});

var app = builder.Build();

// 7. Configure Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Always enable Swagger UI for interactive testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internship Management API v1");
    c.RoutePrefix = "swagger"; // Serve Swagger UI at /swagger
});

// Also redirect root "/" to "/swagger" so accessing either / or /swagger opens Swagger Docs
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoint for Render / Railway / Docker
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        var databaseIsAvailable = await db.Database.CanConnectAsync(cancellationToken);
        var status = databaseIsAvailable ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return Results.Json(new
        {
            status = databaseIsAvailable ? "Healthy" : "Unhealthy",
            service = "InternshipManagementApi",
            database = databaseIsAvailable ? "Available" : "Unavailable",
            timestamp = DateTime.UtcNow
        }, statusCode: status);
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Database readiness check failed.");
        return Results.Json(new
        {
            status = "Unhealthy",
            service = "InternshipManagementApi",
            database = "Unavailable",
            timestamp = DateTime.UtcNow
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();

app.Run();

// Helper: Connection String Resolver supporting ADO.NET and URI formats
static string ResolveConnectionString(IConfiguration configuration, bool requireTls)
{
    var rawConnection = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? Environment.GetEnvironmentVariable("MYSQL_URL")
        ?? configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(rawConnection))
    {
        throw new InvalidOperationException("Database connection string is not configured. Set 'ConnectionStrings:DefaultConnection' or 'DATABASE_URL'.");
    }

    // Handle URI format: mysql://user:pass@host:port/database
    if (rawConnection.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
        rawConnection.StartsWith("mysqls://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(rawConnection);
        var uriRequiresTls = requireTls || string.Equals(uri.Scheme, "mysqls", StringComparison.OrdinalIgnoreCase);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 3306;
        var database = uri.AbsolutePath.TrimStart('/');

        var uriBuilder = new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = (uint)port,
            Database = database,
            UserID = username,
            Password = password,
            CharacterSet = "utf8mb4",
            SslMode = uriRequiresTls ? MySqlSslMode.Required : MySqlSslMode.Preferred,
            AllowPublicKeyRetrieval = true
        };
        return uriBuilder.ConnectionString;
    }

    // Normalize MySQL options and require encrypted transport in production.
    var connBuilder = new MySqlConnectionStringBuilder(rawConnection);
    if (!connBuilder.ContainsKey("Character Set") && !connBuilder.ContainsKey("CharSet"))
    {
        connBuilder.CharacterSet = "utf8mb4";
    }
    if (!connBuilder.ContainsKey("AllowPublicKeyRetrieval"))
    {
        connBuilder.AllowPublicKeyRetrieval = true;
    }
    if (requireTls)
    {
        connBuilder.SslMode = MySqlSslMode.Required;
    }
    else if (!connBuilder.ContainsKey("SslMode"))
    {
        connBuilder.SslMode = MySqlSslMode.Preferred;
    }

    return connBuilder.ConnectionString;
}
