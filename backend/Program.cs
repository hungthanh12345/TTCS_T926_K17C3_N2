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

var builder = WebApplication.CreateBuilder(args);

// Cloud Environment: Bind dynamic port (e.g., Render, Railway)
var dynamicPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(dynamicPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{dynamicPort}");
}

// 1. Configure Database Connection (MySQL via Pomelo EF Core)
var connectionString = ResolveConnectionString(builder.Configuration);

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
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMentorService, MentorService>();
builder.Services.AddScoped<IStudentService, StudentService>();

// 3. Configure JWT Authentication & Authorization
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? "InternshipManagementSystem_SuperSecretSecureKey_2026_JWT_Production_Key!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "InternshipManagementApi";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "InternshipManagementClient";

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

var allowedOrigins = new List<string>
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://localhost:3000",
    "http://127.0.0.1:5173",
    "http://127.0.0.1:5174"
};

if (!string.IsNullOrWhiteSpace(allowedOriginsEnv))
{
    var parsedOrigins = allowedOriginsEnv
        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    allowedOrigins.AddRange(parsedOrigins);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins.Distinct().ToArray())
              .SetIsOriginAllowed(origin =>
              {
                  if (string.IsNullOrWhiteSpace(origin)) return false;
                  if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;

                  if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                  {
                      // Allow any *.vercel.app domain for Vercel preview & production deployments
                      if (uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                          return true;

                      // Allow localhost on any port for local development
                      if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                          return true;
                  }
                  return false;
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "InternshipManagementApi",
    timestamp = DateTime.UtcNow
}));

app.MapControllers();

app.Run();

// Helper: Connection String Resolver supporting ADO.NET and URI formats
static string ResolveConnectionString(IConfiguration configuration)
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
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 3306;
        var database = uri.AbsolutePath.TrimStart('/');

        return $"Server={host};Port={port};Database={database};User={username};Password={password};CharSet=utf8mb4;SslMode=Preferred;AllowPublicKeyRetrieval=True;";
    }

    // Append cloud-friendly MySQL flags if missing
    var connBuilder = new StringBuilder(rawConnection.TrimEnd(';'));
    if (!rawConnection.Contains("CharSet=", StringComparison.OrdinalIgnoreCase))
    {
        connBuilder.Append(";CharSet=utf8mb4");
    }
    if (!rawConnection.Contains("AllowPublicKeyRetrieval=", StringComparison.OrdinalIgnoreCase))
    {
        connBuilder.Append(";AllowPublicKeyRetrieval=True");
    }
    if (!rawConnection.Contains("SslMode=", StringComparison.OrdinalIgnoreCase))
    {
        connBuilder.Append(";SslMode=Preferred");
    }

    return connBuilder.ToString();
}
