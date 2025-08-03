using Account_Service.Repositories;
using Account_Service.Transactions.AddTransaction.Command;
using Account_Service.Utility;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
    {

        public CreateTransactionCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.AccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Идентификатор счёта обязателен")
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract решарпер предлагает неправильную логику
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счёт не найден");

            RuleFor(x => x.Currency)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => Currencies.SupportedCurrencies.Contains(currency!))
                .WithMessage("Валюта не поддерживается")
                .Must((cmd, currency) =>
                {
                    var account = accountRepository.GetByIdAsync(cmd.AccountId).Result;
                    return account.Currency == currency;
                })
                .WithMessage("Валюта транзакции должна совпадать с валютой счёта");

            RuleFor(x => x.Amount)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage("Сумма транзакции должна быть положительной");

            RuleFor(x => x.Type)
                .Cascade(CascadeMode.Stop)
                .IsInEnum()
                .WithMessage("Недопустимый тип транзакции");

            RuleFor(x => x)
                .Cascade(CascadeMode.Stop)
                .Must(cmd =>
                {
                    if (cmd.Type != TransactionType.Credit) return true;
                    var account = accountRepository.GetByIdAsync(cmd.AccountId).Result;
                    return account.Balance >= cmd.Amount;
                })
                .WithMessage("Недостаточно средств на счёте для кредитовой транзакции");
        }
    }
}
