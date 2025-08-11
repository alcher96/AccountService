using System.Data;
using AccountService.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using AccountService.Features.Transactions.AccrueInterest.Command;
using AccountService.Features.Accounts;
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Features.Transactions.AccrueInterest;

public class AccrueInterestCommandHandler(
    AccountDbContext context,
    IValidator<AccrueInterestCommand> validator)
    : IRequestHandler<AccrueInterestCommand, MbResult<bool>>
{
    public async Task<MbResult<bool>> Handle(AccrueInterestCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[AccrueInterestCommandHandler] Starting interest accrual for AccountId={request.AccountId?.ToString() ?? "all accounts"}");

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            Console.WriteLine($"[AccrueInterestCommandHandler] Validation failed: {string.Join(", ", errors.SelectMany(e => e.Value))}");
            return MbResult<bool>.Failure(errors);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (request.AccountId.HasValue)
            {
                var account = await context.Accounts
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

                Console.WriteLine($"[AccrueInterestCommandHandler] Calling accrue_interest for AccountId={request.AccountId.Value}");
                await context.Database.ExecuteSqlRawAsync(
                    "CALL accrue_interest({0})",
                    new object[] { request.AccountId.Value },
                    cancellationToken
                );
                await EnsureBalancePrecision(request.AccountId.Value, cancellationToken);
            }
            else
            {
                var depositAccounts = await context.Accounts
                    .Where(a => a.AccountType == AccountType.Deposit)
                    .Select(a => a.AccountId)
                    .ToListAsync(cancellationToken);

                Console.WriteLine($"[AccrueInterestCommandHandler] Found {depositAccounts.Count} deposit accounts");
                foreach (var accountId in depositAccounts)
                {
                    Console.WriteLine($"[AccrueInterestCommandHandler] Calling accrue_interest for AccountId={accountId}");
                    await context.Database.ExecuteSqlRawAsync(
                        "CALL accrue_interest({0})",
                        new object[] { accountId },
                        cancellationToken
                    );
                    await EnsureBalancePrecision(accountId, cancellationToken);
                }
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
        var account = await context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
        if (account != null)
        {
            var oldBalance = account.Balance;
            account.Balance = Math.Round(account.Balance, 2, MidpointRounding.AwayFromZero);
            Console.WriteLine($"[AccrueInterestCommandHandler] Rounded balance for AccountId={accountId} from {oldBalance} to {account.Balance}");
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}