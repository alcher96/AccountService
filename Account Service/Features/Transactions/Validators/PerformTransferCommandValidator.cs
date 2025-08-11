using AccountService.Features.Transactions.PerformTransfer.Command;
using AccountService.Repositories;
using AccountService.Utility;
using FluentValidation;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class PerformTransferCommandValidator : AbstractValidator<PerformTransferCommand>
    {

        private readonly IAccountRepository _accountRepository;

        public PerformTransferCommandValidator(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));

            RuleFor(x => x.FromAccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("FromAccountId обязателен")
                .MustAsync(async (id, _) => await _accountRepository.GetByIdAsync(id) != null)
                .WithMessage("Счет отправителя не найден");

            RuleFor(x => x.ToAccountId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("ToAccountId обязателен")
                .MustAsync(async (id, _) => await _accountRepository.GetByIdAsync(id) != null)
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
                .CustomAsync(async (cmd, context, _) =>
                {
                    var fromAccount = await _accountRepository.GetByIdAsync(cmd.FromAccountId);
                    var toAccount = await _accountRepository.GetByIdAsync(cmd.ToAccountId);

                    // Проверяем, что аккаунты существуют (хотя это уже проверено в MustAsync выше)
                    if (fromAccount == null)
                    {
                        context.AddFailure("FromAccountId", "Счет отправителя не найден");
                        return;
                    }

                    if (toAccount == null)
                    {
                        context.AddFailure("ToAccountId", "Счет получателя не найден");
                        return;
                    }

                    // Проверка валюты
                    if (fromAccount.Currency != cmd.Currency || toAccount.Currency != cmd.Currency)
                    {
                        context.AddFailure("Currency", "Валюта перевода не соответствует валюте счетов");
                        return;
                    }

                    // Проверка баланса
                    if (fromAccount.Balance < cmd.Amount)
                    {
                        context.AddFailure("Amount", "Недостаточно средств на счете отправителя");
                    }
                });

            RuleFor(x => x.Description)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Описание обязательно");
        }
    }
}
