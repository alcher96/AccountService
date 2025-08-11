using AccountService.Features.Transactions.AccrueInterest.Command;
using Hangfire;
using MediatR;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Extensions
{
    /// <summary>
    /// настраиваем Hangfire
    /// </summary>
    public static class HangfireJobConfiguration
    {
        public static void ConfigureJobs(WebApplication app)
        {
            if (app.Environment.IsEnvironment("Test")) return;
            using var scope = app.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            RecurringJob.AddOrUpdate(
                "accrue-interest",
                () => mediator.Send(new AccrueInterestCommand(), CancellationToken.None),
                "0 0 * * *"); // Ежедневно в полночь
        }
    }
}
