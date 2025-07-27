using Account_Service.Accounts.PatchAccount.Command;
using Account_Service.Utility;
using FluentValidation;

namespace Account_Service.Accounts.Validators
{
    public class PatchAccountCommandValidator : AbstractValidator<PatchAccountCommand>
    {

        public PatchAccountCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Id обязателен");
            RuleFor(x => x.Request.Currency)
                .Must(currency => currency == null || Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x.Request.Type)
                .IsInEnum()
                .When(x => x.Request.Type.HasValue)
                .WithMessage("Недопустимый тип счета");

            RuleFor(x => x.Request.InterestRate)
                .NotNull()
                .When(x => x.Request.Type == AccountType.Deposit || x.Request.Type == AccountType.Credit || (x.Request.Type == null && x.Request.InterestRate is not null))
                .WithMessage("Процентная ставка обязательна для депозитов и кредитов")
                .Null()
                .When(x => x.Request.Type == AccountType.Checking)
                .WithMessage("Процентная ставка не должна указываться для текущих счетов");

            RuleFor(x => x.Request.Balance)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Request.Balance.HasValue)
                .WithMessage("Баланс не может быть отрицательным");
        }
    }
}
