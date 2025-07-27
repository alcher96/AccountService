using Account_Service.Repositories;
using Account_Service.Transactions.PerformTransfer.Command;
using Account_Service.Utility;
using FluentValidation;

namespace Account_Service.Transactions.Validators
{
    public class PerformTransferCommandValidator : AbstractValidator<PerformTransferCommand>
    {

        public PerformTransferCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.FromAccountId)
                .NotEmpty()
                .WithMessage("FromAccountId обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет отправителя не найден");

            RuleFor(x => x.ToAccountId)
                .NotEmpty()
                .WithMessage("ToAccountId обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет получателя не найден")
                .NotEqual(x => x.FromAccountId)
                .WithMessage("Счета отправителя и получателя не должны совпадать");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Сумма перевода должна быть больше нуля");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x)
                .Custom((cmd, context) =>
                {
                    var fromAccount = accountRepository.GetByIdAsync(cmd.FromAccountId).Result;
                    var toAccount = accountRepository.GetByIdAsync(cmd.ToAccountId).Result;
                    if (fromAccount.Currency != cmd.Currency || toAccount.Currency != cmd.Currency)
                    {
                        context.AddFailure("Currency", "Валюта перевода не соответствует валюте счетов");
                    }
                });

            RuleFor(x => x)
                .Must(cmd =>
                {
                    var fromAccount = accountRepository.GetByIdAsync(cmd.FromAccountId).Result;
                    return fromAccount.Balance >= cmd.Amount;
                })
                .WithMessage("Недостаточно средств на счете отправителя");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Описание обязательно");
        }
    }
}
