using Microsoft.AspNetCore.Mvc;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Extensions
{
    /// <summary>
    /// выносим логику проверки и избыточные if из контроллера
    /// </summary>
    public static class ControllerExtensions
    {
        public static IActionResult ToActionResult<T>(
            this ControllerBase controller,
            MbResult<T> result,
            Func<T, IActionResult> onSuccess)
        {
            if (result.IsSuccess)
            {
                return onSuccess(result.Value!);
            }

            return result.MbError switch
            {
                "Concurrency conflict" => controller.Conflict(result),
                "Account is frozen" => controller.Conflict(result),
                _ => controller.BadRequest(result)
            };
        }
    }
}
