using System.Text.Json.Serialization;
using System.Text.Json;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member избыточный xml комментарий

namespace Account_Service.Extensions
{
    //кастомный мидлвейр для обработки 401 ошибки
    public class AuthenticationErrorMiddleware(RequestDelegate next)
    {
        // ReSharper disable once UnusedMember.Global
        public async Task InvokeAsync(HttpContext context)
        {
            await next(context);

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                context.Response.ContentType = "application/json";
                var response = MbResult<object>.Failure("Unauthorized: Invalid or missing token");
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                }));
            }
        }
    }
}
