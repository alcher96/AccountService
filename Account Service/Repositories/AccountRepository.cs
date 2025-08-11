using System.Data;
using AccountService.Data;
using Microsoft.EntityFrameworkCore;
using AccountService.Features.Transactions;
using AccountService.Features.Accounts;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AccountDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AccountRepository(AccountDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Account account)
    {
        _context.Accounts.Attach(account);
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
            .Include(a => a.Transactions) // Явно загружаем транзакции
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
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var fromAccount = await _context.Accounts.FindAsync(fromAccountId);
            var toAccount = await _context.Accounts.FindAsync(toAccountId);
            

            fromAccount!.Balance -= amount;
            toAccount!.Balance += amount;
            var debit = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                AccountId = fromAccountId,
                CounterpartyAccountId = toAccountId,
                Amount = amount,
                Type = TransactionType.Debit,
                DateTime = DateTime.UtcNow,
                Currency = currency ?? throw new ArgumentNullException(nameof(currency), "Currency cannot be null"),
                Description = description
            };
            var credit = new Transaction
            {
                TransactionId = Guid.NewGuid(),
                AccountId = toAccountId,
                CounterpartyAccountId = fromAccountId,
                Amount = amount,
                Type = TransactionType.Credit,
                DateTime = DateTime.UtcNow,
                Currency = currency ?? throw new ArgumentNullException(nameof(currency), "Currency cannot be null"),
                Description = description
            };
            _context.Transactions.Add(debit);
            _context.Transactions.Add(credit);
            await _context.SaveChangesAsync();
            await _context.Transactions
                .Where(t => t.TransactionId == debit.TransactionId || t.TransactionId == credit.TransactionId)
                .ToListAsync();

            await transaction.CommitAsync();
            return (debit, credit);
        }
        
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error in TransferAsync: FromAccountId={fromAccountId}, ToAccountId={toAccountId}, Message={ex.Message}, StackTrace={ex.StackTrace}");
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<Transaction?> AddTransactionAsync(Guid accountId, Transaction transaction)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Загружаем счёт
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
            if (account == null)
            {
                return null;
            }

            var initialBalance = account.Balance;
            var balanceChange = transaction.Type == TransactionType.Debit ? transaction.Amount : -transaction.Amount;
            account.Balance = initialBalance + balanceChange;

            _context.Accounts.Attach(account);
            _context.Entry(account).Property(a => a.Balance).IsModified = true;
            await _context.SaveChangesAsync();
           

            // Добавляем транзакцию
            transaction.TransactionId = Guid.NewGuid();
            transaction.DateTime = DateTime.UtcNow;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();


            // Проверяем баланс после обновления
            var updatedAccount = await _context.Accounts.FindAsync(accountId);
            if (updatedAccount!.Balance != initialBalance + balanceChange)
            {
                await dbTransaction.RollbackAsync();
                return null;
            }

            await dbTransaction.CommitAsync();
            return transaction;
        }
        catch (DbUpdateConcurrencyException)
        {
            //пробрасываем concurrency conflict дальше
            throw;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            return null;
        }
    }
}