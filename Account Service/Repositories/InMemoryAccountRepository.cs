using Account_Service.Accounts;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Repositories
{
    //заглушка
    public class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts = [];
        public Task AddAsync(Account account)
        {
            _accounts.Add(account);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Account account)
        {
            var existing = _accounts.FirstOrDefault(a => a.AccountId == account.AccountId);
            if (existing != null) _accounts[_accounts.IndexOf(existing)] = account;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            _accounts.RemoveAll(a => a.AccountId == id);
            return Task.CompletedTask;
        }

        public Task<Account> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_accounts.FirstOrDefault(a => a.AccountId == id))!;
        }

        public Task<List<Account>> GetAllAsync(Guid? ownerId, AccountType? type)
        {
            var result = _accounts.Where(a => (!ownerId.HasValue || a.OwnerId == ownerId) &&
                                              (!type.HasValue || a.AccountType == type)).ToList();
            return Task.FromResult(result);
        }
    }
}
