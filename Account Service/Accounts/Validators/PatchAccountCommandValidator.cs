using Account_Service.Accounts.PatchAccount.Command;
using Account_Service.Utility;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class PatchAccountCommandValidator : AbstractValidator<PatchAccountCommand>
    {

        public PatchAccountCommandValidator()
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Id обязателен");
            RuleFor(x => x.Request!.Currency)
                .Cascade(CascadeMode.Stop)
                .Must(currency => currency == null || Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x.Request!.Type)
                .Cascade(CascadeMode.Stop)
                .IsInEnum()
                // ReSharper disable once MergeIntoPattern
                .When(x => x.Request?.Type != null)
                .WithMessage("Недопустимый тип счета");

            RuleFor(x => x.Request!.InterestRate)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .When(x => x.Request != null && (x.Request?.Type == AccountType.Deposit || x.Request?.Type == AccountType.Credit || (x.Request?.Type == null && x.Request!.InterestRate is not null)))
                .WithMessage("Процентная ставка обязательна для депозитов и кредитов")
                .Null()
                .When(x => x.Request?.Type == AccountType.Checking)
                .WithMessage("Процентная ставка не должна указываться для текущих счетов");

            RuleFor(x => x.Request!.Balance)
                .Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(0)
                // ReSharper disable once MergeSequentialChecks 
                .When(x => x.Request is { Balance: not null })
                .WithMessage("Баланс не может быть отрицательным");
        }
    }
}
