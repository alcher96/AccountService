using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Repositories;
using Account_Service.Utility;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
    {

        public UpdateAccountCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Id обязателен")
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract решарпер предлагает неправильную логику
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");

            RuleFor(x => x.Request!.OwnerId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("OwnerId обязателен");

            RuleFor(x => x.Request!.Currency)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => currency != null && Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x.Request!.Type)
                .Cascade(CascadeMode.Stop)
                .IsInEnum()
                .WithMessage("Недопустимый тип счета");

            RuleFor(x => x.Request!.InterestRate)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .When(x => x.Request!.Type is AccountType.Deposit or AccountType.Credit)
                .WithMessage("Процентная ставка обязательна для депозитов и кредитов")
                .Null()
                .When(x => x.Request!.Type == AccountType.Checking)
                .WithMessage("Процентная ставка не должна указываться для текущих счетов");
        }
    }
}
