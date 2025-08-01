using Account_Service.Repositories;
using Account_Service.Transactions.PerformTransfer.Command;
using Account_Service.Utility;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class PerformTransferCommandValidator : AbstractValidator<PerformTransferCommand>
    {

        public PerformTransferCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.FromAccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("FromAccountId обязателен")
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract решарпер предлагает неправильную логику
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет отправителя не найден");

            RuleFor(x => x.ToAccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("ToAccountId обязателен")
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract решарпер предлагает неправильную логику
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет получателя не найден")
                .NotEqual(x => x.FromAccountId)
                .WithMessage("Счета отправителя и получателя не должны совпадать");

            RuleFor(x => x.Amount)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage("Сумма перевода должна быть больше нуля");

            RuleFor(x => x.Currency)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => Currencies.SupportedCurrencies.Contains(currency!))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x)
                .Cascade(CascadeMode.Stop)
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
                .Cascade(CascadeMode.Stop)
                .Must(cmd =>
                {
                    var fromAccount = accountRepository.GetByIdAsync(cmd.FromAccountId).Result;
                    return fromAccount.Balance >= cmd.Amount;
                })
                .WithMessage("Недостаточно средств на счете отправителя");

            RuleFor(x => x.Description)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Описание обязательно");
        }
    }
}
