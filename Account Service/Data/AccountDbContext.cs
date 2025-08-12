using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using Microsoft.EntityFrameworkCore;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Data
{
    public class AccountDbContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Создание индексов
            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(e => e.RowVersion)
                    .HasColumnName("xmin") // указываем, что это xmin
                    .HasColumnType("xid")  // тип xid в PostgreSQL
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();  // делаем токеном для оптимистической блокировки
                entity.HasIndex(e => e.OwnerId, "IX_Accounts_OwnerId").HasMethod("hash");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => new { e.AccountId, e.DateTime }, "IX_Transactions_AccountId_TransactionDate");
                entity.HasIndex(e => e.DateTime, "IX_Transactions_TransactionDate").HasMethod("gist");
            });
        }
            
        
    }
}
