using AccountService.Data;
using AccountService.Messaging;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Repositories;

/// <summary>
/// репозиторий для OutboxMessage
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AccountDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OutboxRepository(AccountDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnsentAsync(int maxCount = 100)
    {
        return await _context.OutboxMessages
            .Where(m => m.SentAt == null)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task MarkAsSentAsync(Guid id)
    {
        var message = await _context.OutboxMessages.FindAsync(id);
        if (message != null)
        {
            message.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementRetryAsync(Guid id)
    {
        var message = await _context.OutboxMessages.FindAsync(id);
        if (message != null)
        {
            message.RetryCount++;
            await _context.SaveChangesAsync();
        }
    }
}