using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using AccountService.Messaging;

#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Repositories
{
    public interface IAccountRepository
    {
        Task AddAsync(Account account, OutboxMessage? outboxMessage = null);
        Task UpdateAsync(Account account);
        Task DeleteAsync(Guid id);
        Task<Account?> GetByIdAsync(Guid id);
        Task<List<Account>> GetAllAsync(Guid? ownerId, AccountType? type);

        Task<Transaction?> AddTransactionAsync(Guid accountId, Transaction transaction);
        Task<(Transaction debit, Transaction credit)?> TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount, string currency, string description);
    }
}
