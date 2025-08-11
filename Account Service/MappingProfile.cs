using AutoMapper;
using AccountService.Features.Transactions;
using AccountService.Features.Transactions.AddTransaction.Command;
using AccountService.Features.Transactions.PerformTransfer.Command;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.AddAccount.Command;
using AccountService.Features.Accounts.PatchAccount.Command;
using AccountService.Features.Accounts.UpdateAccount.Command;
// ReSharper disable UnusedParameter.Local
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Account <-> AccountDto
            CreateMap<Account, AccountDto>().ReverseMap();

            // CreateAccountCommand -> Account
            CreateMap<CreateAccountCommand, Account>()
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.OpeningDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(_ => Guid.NewGuid().ToByteArray())); // Добавляем RowVersion


            // UpdateAccountRequestDto -> Account
            CreateMap<UpdateAccountRequestDto, Account>()
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.Balance, opt => opt.Ignore())
                .ForMember(dest => dest.OpeningDate, opt => opt.Ignore());

            // Маппинг для PatchAccountRequestDto -> Account
            CreateMap<PatchAccountRequestDto, Account>()
                .ForMember(dest => dest.AccountId, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.OpeningDate, opt => opt.Ignore())
                .ForMember(dest => dest.Currency, opt => opt.Condition(src => src.Currency != null))
                .ForMember(dest => dest.AccountType, opt => opt.Condition(src => src.Type != null))
                .ForMember(dest => dest.InterestRate, opt => opt.Condition(src => src.InterestRate != null))
                .ForMember(dest => dest.Balance, opt => opt.Condition(src => src.Balance != null));

            // Transaction <-> TransactionDto
            CreateMap<Transaction, TransactionDto>().ReverseMap();

            // CreateTransactionCommand -> Transaction
            CreateMap<CreateTransactionCommand, Transaction>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.DateTime, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // PerformTransferCommand -> Transaction
            CreateMap<PerformTransferCommand, Transaction>()
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.AccountId, opt => opt.MapFrom((src, dest, member, ctx) =>
                    ctx.Items.TryGetValue("TransactionType", out var type) && type is TransactionType.Credit
                        ? src.ToAccountId
                        : src.FromAccountId))
                .ForMember(dest => dest.CounterpartyAccountId, opt => opt.MapFrom((src, dest, member, ctx) =>
                    ctx.Items.TryGetValue("TransactionType", out var type) && type is TransactionType.Credit
                        ? src.FromAccountId
                        : src.ToAccountId))
                .ForMember(dest => dest.Type, opt => opt.MapFrom((src, dest, member, ctx) =>
                    ctx.Items.TryGetValue("TransactionType", out var type) && type is TransactionType.Credit
                        ? TransactionType.Credit
                        : TransactionType.Debit))
                .ForMember(dest => dest.DateTime, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));
        }
    }
}
