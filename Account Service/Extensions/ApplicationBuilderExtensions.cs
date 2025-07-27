using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Account_Service.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseFluentValidationExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
                    var exception = errorFeature?.Error;

                    if (exception is ValidationException validationException)
                    {
                        var errors = validationException.Errors.Select(err => new
                        {
                            Property = err.PropertyName,
                            Error = err.ErrorMessage,
                            ErrorCode = err.ErrorCode,
                            Severity = err.Severity.ToString()
                        });

                        var response = new
                        {
                            StatusCode = 400,
                            Message = "Validation failed",
                            Errors = errors,
                            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        };

                        var responseText = JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(responseText, Encoding.UTF8);
                    }
                    else
                    {
                        // Пропускаем другие исключения для обработки стандартным middleware
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/json";
                        var errorResponse = new
                        {
                            StatusCode = 500,
                            Message = "Internal server error",
                            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        };
                        var errorText = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        await context.Response.WriteAsync(errorText, Encoding.UTF8);
                    }
                });
            });
        }
    }
}
