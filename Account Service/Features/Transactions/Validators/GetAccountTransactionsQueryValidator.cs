using AccountService.Features.Transactions.GetAccountTransactions.Query;
using AccountService.Repositories;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий
namespace AccountService.Features.Transactions.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
    {
        public GetAccountTransactionsQueryValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.AccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("AccountId обязателен")
                .MustAsync(async (id, _) =>
                {
                    var account = await accountRepository.GetByIdAsync(id);
                    Console.WriteLine($"[GetAccountTransactionsValidator] AccountId={id}, Found={account != null}");
                    return account != null;
                })
                .WithMessage("Счет не найден");

            RuleFor(x => x.EndDate)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Дата окончания не может быть раньше даты начала");
        }
    }
}
