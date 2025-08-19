using System.Data;
using System.Text.Json;
using AccountService.Data;
using Microsoft.EntityFrameworkCore;
using AccountService.Features.Transactions;
using AccountService.Features.Accounts;
using AccountService.Features.Transactions.PerformTransfer.Command;
using AutoMapper;
using AccountService.Messaging;
using AccountService.Messaging.Events;
// ReSharper disable ConvertToPrimaryConstructor
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AccountDbContext _context;
    private readonly IMapper _mapper;

    public AccountRepository(AccountDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task AddAsync(Account account, OutboxMessage outboxMessage)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Accounts.AddAsync(account);
            await _context.OutboxMessages.AddAsync(outboxMessage);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Attach(account);
        _context.Entry(account).Property(x => x.RowVersion).OriginalValue = account.RowVersion;
        _context.Entry(account).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account != null)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        return await _context.Accounts
            .Include(a => a.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == id);
    }

    public async Task<List<Account>> GetAllAsync(Guid? ownerId, AccountType? type)
    {
        var query = _context.Accounts.AsQueryable();
        if (ownerId.HasValue)
            query = query.Where(a => a.OwnerId == ownerId);
        if (type.HasValue)
            query = query.Where(a => a.AccountType == type);
        return await query.ToListAsync();
    }

    public async Task<(Transaction debit, Transaction credit)?> TransferAsync(Guid fromAccountId, Guid toAccountId,
     decimal amount, string currency, string description)
    {
        Console.WriteLine($"[AccountRepository] TransferAsync called: FromAccountId={fromAccountId}, ToAccountId={toAccountId}, Amount={amount}, Currency={currency}, Description={description}");
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var fromAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == fromAccountId);
            var toAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == toAccountId);

            if (fromAccount == null || toAccount == null)
            {
                Console.WriteLine("[AccountRepository] One or both accounts not found");
                await dbTransaction.RollbackAsync();
                return null;
            }

            if (fromAccount.IsFrozen || toAccount.IsFrozen)
            {
                Console.WriteLine("[AccountRepository] One or both accounts are frozen");
                await dbTransaction.RollbackAsync();
                throw new InvalidOperationException("One or both accounts are frozen");
            }

            if (fromAccount.Currency != currency || toAccount.Currency != currency)
            {
                Console.WriteLine("[AccountRepository] Currency mismatch");
                await dbTransaction.RollbackAsync();
                throw new InvalidOperationException("Currency mismatch");
            }

            if (fromAccount.Balance < amount)
            {
                Console.WriteLine("[AccountRepository] Insufficient funds in the source account");
                await dbTransaction.RollbackAsync();
                throw new InvalidOperationException("Insufficient funds in the source account");
            }

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            var command = new PerformTransferCommand
            {
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Amount = amount,
                Currency = currency,
                Description = description
            };

            var debit = _mapper.Map<Transaction>(command, opts => opts.Items["TransactionType"] = TransactionType.Debit);
            debit.TransactionId = Guid.NewGuid();
            debit.AccountId = fromAccountId;
            debit.CounterpartyAccountId = toAccountId;
            debit.DateTime = DateTime.UtcNow;

            var credit = _mapper.Map<Transaction>(command, opts => opts.Items["TransactionType"] = TransactionType.Credit);
            credit.TransactionId = Guid.NewGuid();
            credit.AccountId = toAccountId;
            credit.CounterpartyAccountId = fromAccountId;
            credit.DateTime = DateTime.UtcNow;

            _context.Transactions.Add(debit);
            _context.Transactions.Add(credit);

            // Создаём событие MoneyDebitedEvent
            var debitEvent = new MoneyDebitedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                AccountId = fromAccountId,
                Amount = amount,
                Currency = currency,
                OperationId = debit.TransactionId,
                Reason = description,
                Meta = new MetaData
                {
                    Version = "v1",
                    Source = "account-service",
                    CorrelationId = Guid.NewGuid(),
                    CausationId = Guid.NewGuid()
                }
            };
            var debitPayload = JsonSerializer.Serialize(debitEvent);
            var debitOutboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                EventType = "MoneyDebited",
                Payload = debitPayload,
                RetryCount = 0,
                SentAt = null
            };
            _context.OutboxMessages.Add(debitOutboxMessage);
            Console.WriteLine($"[AccountRepository] Added OutboxMessage: Id={debitOutboxMessage.Id}, EventType={debitOutboxMessage.EventType}, Payload={debitPayload}");

            // Создаём событие MoneyCreditedEvent
            var creditEvent = new MoneyCreditedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                AccountId = toAccountId,
                Amount = amount,
                Currency = currency,
                OperationId = credit.TransactionId,
                Meta = new MetaData
                {
                    Version = "v1",
                    Source = "account-service",
                    CorrelationId = Guid.NewGuid(),
                    CausationId = Guid.NewGuid()
                }
            };
            var creditPayload = JsonSerializer.Serialize(creditEvent);
            var creditOutboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                EventType = "MoneyCredited",
                Payload = creditPayload,
                RetryCount = 0,
                SentAt = null
            };
            _context.OutboxMessages.Add(creditOutboxMessage);
            Console.WriteLine($"[AccountRepository] Added OutboxMessage: Id={creditOutboxMessage.Id}, EventType={creditOutboxMessage.EventType}, Payload={creditPayload}");

            // Отладка: Проверяем содержимое OutboxMessages перед сохранением
            var outboxMessagesBeforeSave = await _context.OutboxMessages.ToListAsync();
            Console.WriteLine($"[AccountRepository] OutboxMessages before SaveChanges: {JsonSerializer.Serialize(outboxMessagesBeforeSave)}");

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            Console.WriteLine("[AccountRepository] Transfer completed successfully");
            return (debit, credit);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[AccountRepository] InvalidOperationException: {ex.Message}");
            await dbTransaction.RollbackAsync();
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine($"[AccountRepository] DbUpdateConcurrencyException: {ex.Message}");
            await dbTransaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AccountRepository] Exception: {ex.Message}");
            await dbTransaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Transaction?> AddTransactionAsync(Guid accountId, Transaction transaction)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null)
            {
                return null;
            }

            // Проверка, если счёт заморожен и операция дебетовая
            if (transaction.Type == TransactionType.Debit && account.IsFrozen)
            {
                throw new InvalidOperationException("Account is frozen"); // Будет обработано как 409 Conflict
            }

            var initialBalance = account.Balance;
            var balanceChange = transaction.Type == TransactionType.Debit ? -transaction.Amount : transaction.Amount;
            account.Balance = initialBalance + balanceChange;
            _context.Accounts.Attach(account);
            _context.Entry(account).Property(a => a.Balance).IsModified = true;

            // Создаём событие
            object @event = transaction.Type switch
            {
                TransactionType.Credit => new MoneyCreditedEvent
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = DateTime.UtcNow,
                    AccountId = accountId,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency!,
                    OperationId = Guid.NewGuid(),
                    Meta = new MetaData
                    {
                        Version = "v1",
                        Source = "account-service",
                        CorrelationId = Guid.NewGuid(),
                        CausationId = Guid.NewGuid()
                    }
                },
                TransactionType.Debit => new MoneyDebitedEvent
                {
                    EventId = Guid.NewGuid(),
                    OccurredAt = DateTime.UtcNow,
                    AccountId = accountId,
                    Amount = transaction.Amount,
                    Currency = transaction.Currency!,
                    OperationId = Guid.NewGuid(),
                    Reason = transaction.Description ?? "Debit transaction",
                    Meta = new MetaData
                    {
                        Version = "v1",
                        Source = "account-service",
                        CorrelationId = Guid.NewGuid(),
                        CausationId = Guid.NewGuid()
                    }
                },
                _ => throw new InvalidOperationException("Unknown transaction type")
            };

            var payload = JsonSerializer.Serialize(@event);
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                EventType = transaction.Type == TransactionType.Credit ? "MoneyCredited" : "MoneyDebited",
                Payload = payload,
                RetryCount = 0,
                SentAt = null
            };
            _context.OutboxMessages.Add(outboxMessage);
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();
            var updatedAccount = await _context.Accounts.FindAsync(accountId);
            if (updatedAccount!.Balance != initialBalance + balanceChange)
            {
                await dbTransaction.RollbackAsync();
                return null;
            }

            await dbTransaction.CommitAsync();
            return transaction;
        }
        catch (InvalidOperationException ex) when (ex.Message == "Account is frozen")
        {
            await dbTransaction.RollbackAsync();
            throw; // Пробрасываем для обработки в хендлере
        }
        catch (DbUpdateConcurrencyException)
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            return null;
        }
    }
}

