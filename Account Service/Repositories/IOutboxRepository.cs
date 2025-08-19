using AccountService.Messaging;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
    Task<IEnumerable<OutboxMessage>> GetUnsentAsync(int maxCount = 100);
    Task MarkAsSentAsync(Guid id);
    Task IncrementRetryAsync(Guid id);
}