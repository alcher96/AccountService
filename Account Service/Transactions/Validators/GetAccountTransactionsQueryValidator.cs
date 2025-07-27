using Account_Service.Repositories;
using Account_Service.Transactions.GetAccountTransactions.Query;
using FluentValidation;

namespace Account_Service.Transactions.Validators
{
    public class GetAccountTransactionsQueryValidator : AbstractValidator<GetAccountTransactionsQuery>
    {
        public GetAccountTransactionsQueryValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("AccountId обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Дата окончания не может быть раньше даты начала");
        }
    }
}
