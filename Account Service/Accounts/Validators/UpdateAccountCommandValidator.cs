using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Repositories;
using Account_Service.Utility;
using FluentValidation;

namespace Account_Service.Accounts.Validators
{
    public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
    {

        public UpdateAccountCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Id обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");

            RuleFor(x => x.Request.OwnerId)
                .NotEmpty()
                .WithMessage("OwnerId обязателен");

            RuleFor(x => x.Request.Currency)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x.Request.Type)
                .IsInEnum()
                .WithMessage("Недопустимый тип счета");

            RuleFor(x => x.Request.InterestRate)
                .NotNull()
                .When(x => x.Request.Type is AccountType.Deposit or AccountType.Credit)
                .WithMessage("Процентная ставка обязательна для депозитов и кредитов")
                .Null()
                .When(x => x.Request.Type == AccountType.Checking)
                .WithMessage("Процентная ставка не должна указываться для текущих счетов");
        }
    }
}
