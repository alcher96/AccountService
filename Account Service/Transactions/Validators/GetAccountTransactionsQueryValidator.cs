using Account_Service.Repositories;
using Account_Service.Transactions.GetAccountTransactions.Query;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий
namespace Account_Service.Transactions.Validators
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
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract решарпер предлагает неправильную логику
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");

            RuleFor(x => x.EndDate)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("Дата окончания не может быть раньше даты начала");
        }
    }
}
