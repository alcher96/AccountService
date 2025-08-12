using System.Data;
using AccountService.Data;
using Microsoft.EntityFrameworkCore;
using AccountService.Features.Transactions;
using AccountService.Features.Accounts;
using AccountService.Features.Transactions.PerformTransfer.Command;
using AutoMapper;
// ReSharper disable ConvertToPrimaryConstructor
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

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();
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

            fromAccount!.Balance -= amount;
            toAccount!.Balance += amount;

            var command = new PerformTransferCommand
            {
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Amount = amount,
                Currency = currency,
                Description = description
            };

            var debit = _mapper.Map<Transaction>(command, opts => opts.Items["TransactionType"] = TransactionType.Debit);
            var credit = _mapper.Map<Transaction>(command, opts => opts.Items["TransactionType"] = TransactionType.Credit);

            _context.Transactions.Add(debit);
            _context.Transactions.Add(credit);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return (debit, credit);
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
            var initialBalance = account.Balance;
            var balanceChange = transaction.Type == TransactionType.Debit ? transaction.Amount : -transaction.Amount;
            account.Balance = initialBalance + balanceChange;
            _context.Accounts.Attach(account);
            _context.Entry(account).Property(a => a.Balance).IsModified = true;
            await _context.SaveChangesAsync();

            transaction.TransactionId = Guid.NewGuid();
            transaction.DateTime = DateTime.UtcNow;
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
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception)
        {
            await dbTransaction.RollbackAsync();
            return null;
        }
    }
}