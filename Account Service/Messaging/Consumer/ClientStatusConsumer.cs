using AccountService.Data;
using AccountService.Messaging.Events.Client;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Diagnostics;
using System.Text.Json;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable DisposeOnUsingVariable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Messaging.Consumer
{
    /// <summary>
    /// Консьюмер для события block и unblock
    /// </summary>
    public class ClientStatusConsumer : IConsumer<ClientBlockedEvent>, IConsumer<ClientUnblockedEvent>
    {
        private readonly AccountDbContext _dbContext;
        private readonly ILogger<ClientStatusConsumer> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 100;

        public ClientStatusConsumer(AccountDbContext dbContext, ILogger<ClientStatusConsumer> logger, IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<ClientBlockedEvent> context)
        {
            var stopwatch = Stopwatch.StartNew();
            var @event = context.Message;
            var correlationId = context.Headers.Get<string>("X-Correlation-Id") ?? Guid.NewGuid().ToString();
            var messageId = @event.EventId;

            // Валидация версии
            if (@event.Meta.Version != "v1")
            {
                _logger.LogWarning("Invalid version {Version} for ClientBlocked event {EventId}, CorrelationId={CorrelationId}",
                    @event.Meta.Version, @event.EventId, correlationId);
                await _publishEndpoint.Publish(new QuarantineMessage
                {
                    EventId = @event.EventId,
                    Reason = $"Invalid version: {@event.Meta.Version}",
                    OriginalMessage = JsonSerializer.Serialize(@event)
                }, ctx => ctx.SetRoutingKey("quarantine"));
                return;
            }

            // Начало транзакции 
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(context.CancellationToken);

            try
            {
                // Обновление аккаунтов
                var accounts = await _dbContext.Accounts
                    .Where(a => a.OwnerId == @event.ClientId)
                    .ToListAsync(context.CancellationToken);

                if (accounts.Any())
                {
                    foreach (var account in accounts)
                    {
                        account.IsFrozen = true;
                    }
                }

                // Повторные попытки для SaveChangesAsync
                for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        await _dbContext.SaveChangesAsync(context.CancellationToken);
                        await transaction.CommitAsync(context.CancellationToken);
                        break; // Успех, выходим из цикла
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" } pgEx)
                    {
                        if (attempt == MaxRetryAttempts)
                        {
                            _logger.LogError(ex, "Failed to save changes after {Attempts} attempts for ClientBlockedEvent, EventId={EventId}, CorrelationId={CorrelationId}, MessageId={MessageId}, Detail={Detail}",
                                attempt, @event.EventId, correlationId, messageId, pgEx.Detail);
                            throw;
                        }
                        _logger.LogWarning("Serialization failure (attempt {Attempt}/{MaxAttempts}) for ClientBlockedEvent, EventId={EventId}, CorrelationId={CorrelationId}, MessageId={MessageId}, Detail={Detail}. Retrying...",
                            attempt, MaxRetryAttempts, @event.EventId, correlationId, messageId, pgEx.Detail);
                        await Task.Delay(RetryDelayMs, context.CancellationToken);
                        // Откатываем транзакцию и начинаем новую
                        await transaction.RollbackAsync(context.CancellationToken);
                        transaction.Dispose();
                        await _dbContext.Database.BeginTransactionAsync(context.CancellationToken);
                        // Повторно загружаем аккаунты
                        accounts = await _dbContext.Accounts
                            .Where(a => a.OwnerId == @event.ClientId)
                            .ToListAsync(context.CancellationToken);
                        if (accounts.Any())
                        {
                            foreach (var account in accounts)
                            {
                                account.IsFrozen = true;
                            }
                        }
                    }
                }

                _logger.LogInformation(
                    "Froze {AccountCount} accounts for ClientId={ClientId}, EventId={EventId}, CorrelationId={CorrelationId}, Latency={Latency}ms",
                    accounts.Count, @event.ClientId, @event.EventId, correlationId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(context.CancellationToken);
                _logger.LogError(ex, "Failed to process ClientBlockedEvent for ClientId={ClientId}, CorrelationId={CorrelationId}, MessageId={MessageId}",
                    @event.ClientId, correlationId, messageId);
                throw;
            }
        }

        public async Task Consume(ConsumeContext<ClientUnblockedEvent> context)
        {
            var stopwatch = Stopwatch.StartNew();
            var @event = context.Message;
            var correlationId = context.Headers.Get<string>("X-Correlation-Id") ?? Guid.NewGuid().ToString();

            // Валидация версии
            if (@event.Meta.Version != "v1")
            {
                _logger.LogWarning("Invalid or unsupported version {Version} for ClientUnblocked event {EventId}, CorrelationId={CorrelationId}",
                    @event.Meta.Version, @event.EventId, correlationId);
                await _publishEndpoint.Publish(new QuarantineMessage
                {
                    EventId = @event.EventId,
                    Reason = $"Invalid version: {@event.Meta.Version}",
                    OriginalMessage = JsonSerializer.Serialize(@event)
                }, ctx => ctx.SetRoutingKey("quarantine"));
                return;
            }

            // Начало транзакции (по умолчанию Read Committed)
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(context.CancellationToken);

            // Проверка идемпотентности внутри транзакции
            var messageId = @event.EventId;
            if (await _dbContext.InboxConsumed.AnyAsync(x => x.MessageId == messageId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Message already consumed: EventId={EventId}, Type=ClientUnblocked, CorrelationId={CorrelationId}, Retry={RetryCount}, Latency={Latency}ms, MessageId={MessageId}",
                    messageId, correlationId, context.GetRetryAttempt(), stopwatch.ElapsedMilliseconds, messageId);
                await transaction.RollbackAsync(context.CancellationToken);
                return;
            }

            _logger.LogInformation(
                "Processing ClientUnblocked event: EventId={EventId}, Type=ClientUnblocked, CorrelationId={CorrelationId}, Retry={RetryCount}, Latency={Latency}ms",
                @event.EventId, correlationId, context.GetRetryAttempt(), stopwatch.ElapsedMilliseconds);

            var accounts = await _dbContext.Accounts
                .Where(a => a.OwnerId == @event.ClientId)
                .ToListAsync(context.CancellationToken);

            if (!accounts.Any())
            {
                _logger.LogWarning("No accounts found for ClientId={ClientId}, CorrelationId={CorrelationId}",
                    @event.ClientId, correlationId);
                await transaction.RollbackAsync(context.CancellationToken);
                return;
            }

            foreach (var account in accounts)
            {
                account.IsFrozen = false;
            }

            _dbContext.InboxConsumed.Add(new InboxConsumed
            {
                MessageId = messageId,
                ConsumedAt = DateTime.UtcNow
            });

            try
            {
                // Повторные попытки для SaveChangesAsync
                for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        await _dbContext.SaveChangesAsync(context.CancellationToken);
                        await transaction.CommitAsync(context.CancellationToken);
                        break; // Успех, выходим из цикла
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "40001" } pgEx)
                    {
                        if (attempt == MaxRetryAttempts)
                        {
                            _logger.LogError(ex, "Failed to save changes after {Attempts} attempts for ClientUnblockedEvent, EventId={EventId}, CorrelationId={CorrelationId}, MessageId={MessageId}, Detail={Detail}",
                                attempt, @event.EventId, correlationId, messageId, pgEx.Detail);
                            throw;
                        }
                        _logger.LogWarning("Serialization failure (attempt {Attempt}/{MaxAttempts}) for ClientUnblockedEvent, EventId={EventId}, CorrelationId={CorrelationId}, MessageId={MessageId}, Detail={Detail}. Retrying...",
                            attempt, MaxRetryAttempts, @event.EventId, correlationId, messageId, pgEx.Detail);
                        await Task.Delay(RetryDelayMs, context.CancellationToken);
                        // Откатываем транзакцию и начинаем новую
                        await transaction.RollbackAsync(context.CancellationToken);
                        transaction.Dispose();
                        await _dbContext.Database.BeginTransactionAsync(context.CancellationToken);
                        // Повторно загружаем аккаунты
                        accounts = await _dbContext.Accounts
                            .Where(a => a.OwnerId == @event.ClientId)
                            .ToListAsync(context.CancellationToken);
                        if (accounts.Any())
                        {
                            foreach (var account in accounts)
                            {
                                account.IsFrozen = false;
                            }
                        }
                        _dbContext.InboxConsumed.Add(new InboxConsumed
                        {
                            MessageId = messageId,
                            ConsumedAt = DateTime.UtcNow
                        });
                    }
                }

                _logger.LogInformation(
                    "Unfrozen {AccountCount} accounts for ClientId={ClientId}, CorrelationId={CorrelationId}, Latency={Latency}ms",
                    accounts.Count, @event.ClientId, correlationId, stopwatch.ElapsedMilliseconds);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pgEx)
            {
                await transaction.RollbackAsync(context.CancellationToken);
                _logger.LogWarning(
                    "Duplicate inbox record detected, skipping EventId={EventId}, Type=ClientUnblocked, CorrelationId={CorrelationId}, MessageId={MessageId}, Detail={Detail}",
                    @event.EventId, correlationId, messageId, pgEx.Detail);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(context.CancellationToken);
                _logger.LogError(ex,
                    "Failed to process ClientUnblockedEvent for ClientId={ClientId}, CorrelationId={CorrelationId}, MessageId={MessageId}",
                    @event.ClientId, correlationId, messageId);
                throw;
            }
        }
    }
}
