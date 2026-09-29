using System.Net;
using System.Text.Json;
using InternshipManagementApi.Common.Exceptions;
using InternshipManagementApi.Common.Models;

namespace InternshipManagementApi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            string message = "An internal server error occurred.";
            object? errors = null;

            switch (exception)
            {
                case ApiException apiEx:
                    statusCode = apiEx.StatusCode;
                    message = apiEx.Message;
                    errors = apiEx.Errors;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = "Unauthorized access.";
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = exception.Message;
                    break;

                case ArgumentException or BadHttpRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                default:
                    // In production, keep internal message generic for security
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred on the server.";
                    errors = exception.Message;
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse.Fail(message, errors);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }
    }
}
