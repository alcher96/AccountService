using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Data;

public class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountDbContext>();
        const string connectionString = "Host=localhost;Port=5432;Database=account_service;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);

        return new AccountDbContext(optionsBuilder.Options);
    }
}