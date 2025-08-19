using Microsoft.EntityFrameworkCore;
using Npgsql;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Extensions
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
            catch (DbUpdateConcurrencyException ex) when (ex.InnerException is PostgresException { SqlState: "40001" })
            {
                _logger.LogWarning(ex, "Concurrent update conflict detected");
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    isSuccess = false,
                    value = (object?)null,
                    mbError = "Concurrency conflict",
                    validationErrors = (object?)null
                });
            }
            catch (InvalidOperationException ex) when (ex.Message == "Account is frozen")
            {
                _logger.LogWarning(ex, "Account is frozen");
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    isSuccess = false,
                    value = (object?)null,
                    mbError = "Account is frozen",
                    validationErrors = (object?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    isSuccess = false,
                    value = (object?)null,
                    mbError = "Internal server error",
                    validationErrors = (object?)null
                });
            }
        }
    }
}
