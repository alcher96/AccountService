using System.Text.Json;
using AccountService.Data;
using AccountService.Messaging.Events;
using AccountService.Messaging.Events.Client;
using AccountService.Repositories;
using MassTransit;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleMultipleEnumeration
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Messaging;

/// <summary>
/// Кастомный паблишер для реализации паттерна OutboxMessage
/// </summary>

public class CustomOutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CustomOutboxPublisherService> _logger;
    private bool _isPublishingEnabled = true;
    private bool _isInitialized;

    public CustomOutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        ILogger<CustomOutboxPublisherService> logger,
        bool isInitialized = true)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _isInitialized = isInitialized;
    }

    public void DisablePublishing() => _isPublishingEnabled = false;

    public void EnablePublishing() => _isPublishingEnabled = true;

    public void SetInitialized()
    {
        _isInitialized = true;
        _logger.LogInformation("CustomOutboxPublisherService initialized via SetInitialized.");
    }

    public async Task ProcessOutboxAsync(CancellationToken stoppingToken = default)
    {
        if (!_isInitialized)
        {
            _logger.LogInformation("Service not initialized, skipping outbox processing.");
            return;
        }
        if (!_isPublishingEnabled)
        {
            _logger.LogInformation("Publishing is disabled, skipping outbox processing.");
            return;
        }
        using var scope = _scopeFactory.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        var unsent = await outboxRepo.GetUnsentAsync();
        _logger.LogInformation("Found {Count} unsent messages.", unsent.Count());
        foreach (var msg in unsent)
        {
            try
            {
                _logger.LogInformation("Processing message: Id={Id}, EventType={EventType}", msg.Id, msg.EventType);
                (Type eventType, string routingKey) = msg.EventType switch
                {
                    "AccountOpened" => (typeof(AccountOpenedEvent), "account.opened"),
                    "MoneyCredited" => (typeof(MoneyCreditedEvent), "money.credited"),
                    "MoneyDebited" => (typeof(MoneyDebitedEvent), "money.debited"),
                    "InterestAccrued" => (typeof(InterestAccruedEvent), "money.interest_accrued"),
                    "ClientBlocked" => (typeof(ClientBlockedEvent), "client.blocked"),
                    "ClientUnblocked" => (typeof(ClientUnblockedEvent), "client.unblocked"),
                    "TransferCompleted" => (typeof(TransferCompletedEvent), "transfer.completed"), 
                    _ => throw new InvalidOperationException($"Unknown event type: {msg.EventType}")
                };
                var @event = JsonSerializer.Deserialize(msg.Payload, eventType, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (@event == null)
                {
                    _logger.LogWarning("Failed to deserialize event: {EventType}, Id: {Id}, Payload: {Payload}", msg.EventType, msg.Id, msg.Payload);
                    continue;
                }
                var correlationId = Guid.NewGuid().ToString();
                await publishEndpoint.Publish(@event, ctx =>
                {
                    ctx.SetRoutingKey(routingKey);
                    ctx.CorrelationId = Guid.Parse(correlationId);
                    ctx.Headers.Set("X-Correlation-Id", correlationId);
                    ctx.Headers.Set("X-Causation-Id", Guid.NewGuid().ToString());
                    return Task.CompletedTask;
                }, stoppingToken);
                await outboxRepo.MarkAsSentAsync(msg.Id);
                _logger.LogInformation(
                    "Published event: EventId: {EventId}, Type: {Type}, CorrelationId: {CorrelationId}, Retry: {Retry}, Latency: {Latency}ms",
                    GetEventId(@event),
                    msg.EventType,
                    correlationId,
                    msg.RetryCount,
                    (DateTime.UtcNow - msg.CreatedAt).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {EventType} with Id: {Id}", msg.EventType, msg.Id);
                await outboxRepo.IncrementRetryAsync(msg.Id);
                if (msg.RetryCount >= 5)
                {
                    dbContext.InboxDeadLetters.Add(new InboxDeadLetter
                    {
                        MessageId = msg.Id,
                        EventType = msg.EventType,
                        Payload = msg.Payload,
                        FailedAt = DateTime.UtcNow,
                        Reason = "Max retry count exceeded"
                    });
                    dbContext.OutboxMessages.Remove(msg);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Moved to DLQ: EventType: {EventType}, Id: {Id}", msg.EventType, msg.Id);
                }
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
                await Task.Delay(5000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ExecuteAsync canceled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteAsync");
                await Task.Delay(1000, stoppingToken); // Небольшая задержка перед повтором
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _isInitialized = false;
        _isPublishingEnabled = false;
        _logger.LogInformation("CustomOutboxPublisherService stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("CustomOutboxPublisherService stopped.");
    }

    private Guid GetEventId(object @event) =>
        @event switch
        {
            AccountOpenedEvent e => e.EventId,
            MoneyCreditedEvent e => e.EventId,
            MoneyDebitedEvent e => e.EventId,
            InterestAccruedEvent e => e.EventId,
            ClientBlockedEvent e => e.EventId,
            ClientUnblockedEvent e => e.EventId,
            TransferCompletedEvent e => e.EventId, 
            _ => throw new InvalidOperationException("Unknown event type")
        };
}



