using AccountService.Features.Accounts.AddAccount.Command;
using AccountService.Utility;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий
namespace AccountService.Features.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        public CreateAccountCommandValidator()
        {
            RuleFor(x => x.OwnerId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("OwnerId обязателен");

            RuleFor(x => x.Currency)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => currency != null && Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x.AccountType)
                .Cascade(CascadeMode.Stop)
                .IsInEnum()
                .WithMessage("Недопустимый тип счета");

            RuleFor(x => x.InterestRate)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .When(x =>
                {
                    if (x.AccountType == AccountType.Deposit) return true;
                    return x.AccountType == AccountType.Credit;
                })
                .WithMessage("Процентная ставка обязательна для депозитов и кредитов")
                .Null()
                .When(x => x.AccountType == AccountType.Checking)
                .WithMessage("Процентная ставка не должна указываться для текущих счетов");

            RuleFor(x => x.Balance)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Начальный баланс не может быть отрицательным");
        }
    }
}
