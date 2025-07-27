using Account_Service.Repositories;
using Account_Service.Transactions.AddTransaction.Command;
using Account_Service.Utility;
using FluentValidation;

namespace Account_Service.Transactions.Validators
{
    public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
    {

        public CreateTransactionCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("AccountId обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Сумма транзакции должна быть больше нуля");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Валюта обязательна")
                .Must(currency => Currencies.SupportedCurrencies.Contains(currency))
                .WithMessage("Валюта не поддерживается");

            RuleFor(x => x)
                .Custom((cmd, context) =>
                {
                    var account = accountRepository.GetByIdAsync(cmd.AccountId).Result;
                    if (account.Currency != cmd.Currency)
                    {
                        context.AddFailure("Currency", "Валюта транзакции не соответствует валюте счета");
                    }
                });

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Недопустимый тип транзакции");

            RuleFor(x => x)
                .Must(cmd =>
                {
                    if (cmd.Type != TransactionType.Debit) return true;
                    var account = accountRepository.GetByIdAsync(cmd.AccountId).Result;
                    return account.Balance >= cmd.Amount;
                })
                .WithMessage("Недостаточно средств для дебетовой транзакции");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Описание обязательно");

            RuleFor(x => x.DateTime)
                .NotEmpty()
                .WithMessage("Дата транзакции обязательна")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Дата транзакции не может быть в будущем");
        }
    }
}
