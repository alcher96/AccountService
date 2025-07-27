using Account_Service.Accounts.DeleteAccount.Command;
using Account_Service.Repositories;
using FluentValidation;

namespace Account_Service.Accounts.Validators
{
    public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
    {
        public DeleteAccountCommandValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Id обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");
        }
    }
}
