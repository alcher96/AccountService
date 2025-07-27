using Account_Service.Accounts;

namespace Account_Service.Repositories
{
    public interface IAccountRepository
    {
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
        Task DeleteAsync(Guid id);
        Task<Account> GetByIdAsync(Guid id);
        Task<List<Account>> GetAllAsync(Guid? ownerId, AccountType? type);
    }
}
