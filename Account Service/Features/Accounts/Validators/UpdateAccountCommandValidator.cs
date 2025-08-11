using AccountService.Repositories;
using AccountService.Utility;
using FluentValidation;
using AccountService.Features.Accounts.UpdateAccount.Command;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
    {

        private readonly IAccountRepository _accountRepository;

        public UpdateAccountCommandValidator(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;

            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Id обязателен")
                .MustAsync(async (id, _) =>
                {
                    var account = await _accountRepository.GetByIdAsync(id);
                    Console.WriteLine($"[UpdateAccountCommandValidator] Checking account: Id={id}, Found={account != null}");
                    return account != null;
                })
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
