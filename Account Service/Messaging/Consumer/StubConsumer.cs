using AccountService.Data;
using AccountService.Messaging.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Messaging.Consumer;


/// <summary>
/// кастомный consumer для событий
/// </summary>
public class StubConsumer :
    IConsumer<AccountOpenedEvent>,
    IConsumer<MoneyCreditedEvent>,
    IConsumer<MoneyDebitedEvent>
{
    private readonly ILogger<StubConsumer> _logger;
    private readonly AccountDbContext _dbContext;

    // ReSharper disable once ConvertToPrimaryConstructor
    public StubConsumer(ILogger<StubConsumer> logger, AccountDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    private async Task ConsumeEvent<T>(ConsumeContext<T> context) where T : class
    {
        var @event = context.Message;
        Guid eventId = GetEventId(@event);
        DateTime occurredAt = GetOccurredAt(@event);
        Guid? operationId = GetOperationId(@event);

        var existing = await _dbContext.InboxConsumed
            .AnyAsync(x => x.MessageId == eventId, context.CancellationToken);
        if (existing)
        {
            _logger.LogInformation("Message already consumed: EventId: {EventId}", eventId);
            return;
        }

        string eventType = typeof(T).Name switch
        {
            nameof(AccountOpenedEvent) => "AccountOpened",
            nameof(MoneyCreditedEvent) => "MoneyCredited",
            nameof(MoneyDebitedEvent) => "MoneyDebited",
            nameof(InterestAccruedEvent) => "InterestAccrued",
            _ => "Unknown"
        };

        _logger.LogInformation(
            "Consumed event: EventId: {EventId}, Type: {Type}, CorrelationId: {CorrelationId}, OperationId: {OperationId}, Retry: {Retry}, Latency: {Latency}ms",
            eventId,
            eventType,
            context.CorrelationId,
            operationId,
            context.Headers.Get<int>("MT-Redelivery-Count") ?? 0,
            (DateTime.UtcNow - occurredAt).TotalMilliseconds);

        _dbContext.InboxConsumed.Add(new InboxConsumed
        {
            MessageId = eventId,
            ConsumedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private Guid GetEventId<T>(T @event) =>
        @event switch
        {
            AccountOpenedEvent e => e.EventId,
            MoneyCreditedEvent e => e.EventId,
            MoneyDebitedEvent e => e.EventId,
            InterestAccruedEvent e => e.EventId,
            _ => throw new InvalidOperationException($"Unknown event type: {typeof(T).Name}")
        };

    private DateTime GetOccurredAt<T>(T @event) =>
        @event switch
        {
            AccountOpenedEvent e => e.OccurredAt,
            MoneyCreditedEvent e => e.OccurredAt,
            MoneyDebitedEvent e => e.OccurredAt,
            InterestAccruedEvent e => e.OccurredAt,
            _ => throw new InvalidOperationException($"Unknown event type: {typeof(T).Name}")
        };

    private Guid? GetOperationId<T>(T @event) =>
        @event switch
        {
            MoneyCreditedEvent e => e.OperationId,
            MoneyDebitedEvent e => e.OperationId,
            _ => null // InterestAccruedEvent не имеет OperationId
        };

    public Task Consume(ConsumeContext<AccountOpenedEvent> context) => ConsumeEvent(context);
    public Task Consume(ConsumeContext<MoneyCreditedEvent> context) => ConsumeEvent(context);
    public Task Consume(ConsumeContext<MoneyDebitedEvent> context) => ConsumeEvent(context);
    public Task Consume(ConsumeContext<InterestAccruedEvent> context) => ConsumeEvent(context);
}
