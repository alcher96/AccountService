using AccountService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

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
        private readonly IConnectionFactory _rabbitMqConnectionFactory;
        private const int BacklogWarningThreshold = 100;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HealthController"/>.
        /// </summary>
        /// <param name="dbContext">Контекст базы данных для проверки Outbox.</param>
        /// <param name="rabbitMqConnectionFactory">Фабрика подключения к RabbitMQ для проверки доступности.</param>
        public HealthController(AccountDbContext dbContext, IConnectionFactory rabbitMqConnectionFactory)
        {
            _dbContext = dbContext;
            _rabbitMqConnectionFactory = rabbitMqConnectionFactory;
        }

        /// <summary>
        /// Проверяет, работает ли приложение (liveliness probe).
        /// </summary>
        /// <returns>JSON-ответ с полем "status" равным "Healthy".</returns>
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new { status = "Healthy" });
        }

        /// <summary>
        /// Проверяет готовность приложения, включая подключение к базе данных, RabbitMQ и отставание Outbox.
        /// </summary>
        /// <returns>JSON-ответ с общим статусом и деталями проверок.</returns>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            var checks = new[]
            {
                await CheckDatabase(),
                CheckRabbitMq(),
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

            // Определяем общий статус
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
        private CheckResult CheckRabbitMq()
        {
            try
            {
                using var connection = _rabbitMqConnectionFactory.CreateConnectionAsync();
                return new CheckResult("RabbitMQ", "Healthy", "RabbitMQ is reachable");
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
    }
}
