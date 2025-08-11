using Hangfire.Dashboard;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Utility;

/// <summary>
/// мокаем авторизацию для hangfire
/// </summary>
public class AllowAnonymousAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}