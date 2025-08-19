using System.Data;
using AccountService.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AccountService.Features.Transactions.AccrueInterest.Command;
using AccountService.Features.Accounts;
using AccountService.Messaging.Events;
using AccountService.Messaging;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Features.Transactions.AccrueInterest;

public class AccrueInterestCommandHandler(
    AccountDbContext _context,
    IValidator<AccrueInterestCommand> _validator)
    : IRequestHandler<AccrueInterestCommand, MbResult<bool>>
{


    public async Task<MbResult<bool>> Handle(AccrueInterestCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[AccrueInterestCommandHandler] Starting interest accrual for AccountId={request.AccountId?.ToString() ?? "all accounts"}");
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            Console.WriteLine($"[AccrueInterestCommandHandler] Validation failed: {string.Join(", ", errors.SelectMany(e => e.Value))}");
            return MbResult<bool>.Failure(errors);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (request.AccountId.HasValue)
            {
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == request.AccountId.Value, cancellationToken);
                if (account == null)
                {
                    Console.WriteLine($"[AccrueInterestCommandHandler] Account {request.AccountId.Value} not found");
                    return MbResult<bool>.Failure($"Account {request.AccountId.Value} not found");
                }
                if (account.AccountType != AccountType.Deposit)
                {
                    Console.WriteLine($"[AccrueInterestCommandHandler] Account {request.AccountId.Value} is not a deposit");
                    return MbResult<bool>.Failure($"Account {request.AccountId.Value} is not a deposit");
                }

                var initialBalance = account.Balance;
                Console.WriteLine($"[AccrueInterestCommandHandler] Calling accrue_interest for AccountId={request.AccountId.Value}");
                await _context.Database.ExecuteSqlRawAsync(
                    "CALL accrue_interest({0})",
                    new object[] { request.AccountId.Value },
                    cancellationToken
                );
                await _context.Entry(account).ReloadAsync(cancellationToken); // Обновляем баланс
                var accruedAmount = account.Balance - initialBalance;

                var @event = new InterestAccruedEvent
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = DateTime.UtcNow,
                    AccountId = request.AccountId.Value,
                    Amount = accruedAmount,
                    PeriodFrom = DateTime.UtcNow.Date.AddDays(-1), // Начало предыдущего дня
                    PeriodTo = DateTime.UtcNow.Date, // Конец предыдущего дня
                    Meta = new MetaData { Version = "1.0" }
                };
                var payload = System.Text.Json.JsonSerializer.Serialize(@event);
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    EventType = "InterestAccrued",
                    Payload = payload,
                    RetryCount = 0,
                    SentAt = null
                });

                await EnsureBalancePrecision(request.AccountId.Value, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var depositAccounts = await _context.Accounts
                    .Where(a => a.AccountType == AccountType.Deposit)
                    .ToListAsync(cancellationToken);
                Console.WriteLine($"[AccrueInterestCommandHandler] Found {depositAccounts.Count} deposit accounts");
                foreach (var account in depositAccounts)
                {
                    var initialBalance = account.Balance;
                    Console.WriteLine($"[AccrueInterestCommandHandler] Calling accrue_interest for AccountId={account.AccountId}");
                    await _context.Database.ExecuteSqlRawAsync(
                        "CALL accrue_interest({0})",
                        new object[] { account.AccountId },
                        cancellationToken
                    );
                    await _context.Entry(account).ReloadAsync(cancellationToken); // Обновляем баланс
                    var accruedAmount = account.Balance - initialBalance;

                    var @event = new InterestAccruedEvent
                    {
                        EventId = Guid.NewGuid(),
                        OccurredAt = DateTime.UtcNow,
                        AccountId = account.AccountId,
                        Amount = accruedAmount,
                        PeriodFrom = DateTime.UtcNow.Date.AddDays(-1), // Начало предыдущего дня
                        PeriodTo = DateTime.UtcNow.Date, // Конец предыдущего дня
                        Meta = new MetaData { Version = "1.0" }
                    };
                    var payload = System.Text.Json.JsonSerializer.Serialize(@event);
                    _context.OutboxMessages.Add(new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        EventType = "InterestAccrued",
                        Payload = payload,
                        RetryCount = 0,
                        SentAt = null
                    });

                    await EnsureBalancePrecision(account.AccountId, cancellationToken);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            Console.WriteLine("[AccrueInterestCommandHandler] Interest accrual committed");
            return MbResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Console.WriteLine($"[AccrueInterestCommandHandler] Failed to accrue interest: {ex.Message}");
            return MbResult<bool>.Failure($"Failed to accrue interest: {ex.Message}");
        }
    }

    private async Task EnsureBalancePrecision(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
        if (account != null)
        {
            var oldBalance = account.Balance;
            account.Balance = Math.Round(account.Balance, 2, MidpointRounding.AwayFromZero);
            Console.WriteLine($"[AccrueInterestCommandHandler] Rounded balance for AccountId={accountId} from {oldBalance} to {account.Balance}");
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
