using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Utility;
using FluentValidation;

namespace Account_Service.Accounts.Validators
{
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        public CreateAccountCommandValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("OwnerId обязателен");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x.AccountType)
                .IsInEnum()
                .WithMessage("Недопустимый тип счета");

            RuleFor(x => x.InterestRate)
                .NotNull()
                .When(x => x.AccountType == AccountType.Deposit || x.AccountType == AccountType.Credit)
                .WithMessage("Процентная ставка обязательна для депозитов и кредитов")
                .Null()
                .When(x => x.AccountType == AccountType.Checking)
                .WithMessage("Процентная ставка не должна указываться для текущих счетов");

            RuleFor(x => x.Balance)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Начальный баланс не может быть отрицательным");
        }
    }
}
