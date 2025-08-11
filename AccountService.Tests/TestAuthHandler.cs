using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
// ReSharper disable ConvertToPrimaryConstructor

namespace AccountService.Tests
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Создаём тестового пользователя с claim 'sub', соответствующим OwnerId
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), // Совпадает с OwnerId первого счёта
                new Claim("sub", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                new Claim(ClaimTypes.Role, "user") // Добавляем роль, если требуется
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
