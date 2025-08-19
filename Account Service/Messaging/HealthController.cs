using AccountService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// ReSharper disable NotAccessedPositionalProperty.Local
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace AccountService.Messaging
{
    /// <summary>
    /// Контроллер для проверки работоспособности и готовности сервиса.
    /// </summary>
    [Route("health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly AccountDbContext _dbContext;
        private readonly IBus _bus;
        private const int BacklogWarningThreshold = 100;


        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HealthController"/>.
        /// </summary>
        /// <param name="dbContext">Контекст базы данных для проверки Outbox.</param>
        public HealthController(AccountDbContext dbContext, IBus bus)
        {
            _dbContext = dbContext;
            _bus = bus;
        }

        /// <summary>
        /// Проверяет, работает ли приложение (liveliness probe).
        /// </summary>
        /// <returns>JSON-ответ с полем "status" равным "Healthy".</returns>
        [HttpGet("live")]
        [AllowAnonymous]
        public IActionResult Live()
        {
            return Ok(new { status = "Healthy" });
        }

        /// <summary>
        /// Проверяет готовность приложения, включая подключение к базе данных, RabbitMQ и отставание Outbox.
        /// </summary>
        /// <returns>JSON-ответ с общим статусом и деталями проверок.</returns>
        [HttpGet("ready")]
        [AllowAnonymous]
        public async Task<IActionResult> Ready()
        {
            var checks = new[]
            {
                await CheckDatabase(),
                await CheckRabbitMq(),
                await CheckOutbox()
            };
            var result = new
            {
                status = "Healthy",
                checks = checks.Select(c => new
                {
                    name = c.Name,
                    status = c.Status,
                    description = c.Description
                }).ToArray()
            };
            if (checks.Any(c => c.Status == "Unhealthy"))
            {
                result = result with { status = "Unhealthy" };
            }
            else if (checks.Any(c => c.Status == "Degraded"))
            {
                result = result with { status = "Degraded" };
            }
            return Ok(result);
        }

        /// <summary>
        /// Результат проверки состояния сервиса.
        /// </summary>
        private record CheckResult(string Name, string Status, string Description);

        /// <summary>
        /// Проверяет доступность базы данных.
        /// </summary>
        /// <returns>Результат проверки базы данных.</returns>
        private async Task<CheckResult> CheckDatabase()
        {
            try
            {
                await _dbContext.Database.CanConnectAsync();
                return new CheckResult("Database", "Healthy", "Database is accessible");
            }
            catch (Exception ex)
            {
                return new CheckResult("Database", "Unhealthy", $"Database connection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет подключение к RabbitMQ.
        /// </summary>
        /// <returns>Результат проверки RabbitMQ.</returns>
        private async Task<CheckResult> CheckRabbitMq()
        {
            try
            {
                // Публикация тестового сообщения для проверки подключения
                await _bus.Publish(new TestMessage(Id: Guid.NewGuid()));
                return new CheckResult("RabbitMQ", "Healthy", "RabbitMQ is reachable via MassTransit");
            }
            catch (Exception ex)
            {
                return new CheckResult("RabbitMQ", "Unhealthy", $"RabbitMQ connection failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет количество непубликованных сообщений в OutboxMessages.
        /// </summary>
        /// <returns>Результат проверки Outbox.</returns>
        private async Task<CheckResult> CheckOutbox()
        {
            try
            {
                var unpublishedCount = await _dbContext.OutboxMessages
                    .CountAsync(m => m.SentAt == null);
                if (unpublishedCount > BacklogWarningThreshold)
                {
                    return new CheckResult(
                        "Outbox",
                        "Degraded",
                        $"Outbox backlog warning: {unpublishedCount} unpublished messages (threshold: {BacklogWarningThreshold})");
                }
                return new CheckResult(
                    "Outbox",
                    "Healthy",
                    $"Outbox messages: {unpublishedCount} unpublished");
            }
            catch (Exception ex)
            {
                return new CheckResult("Outbox", "Unhealthy", $"Outbox check failed: {ex.Message}");
            }
        }
        // Тестовое сообщение для проверки RabbitMQ
        private record TestMessage(Guid Id);
    }
}
