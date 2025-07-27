using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using FluentValidation;

namespace Account_Service.Accounts.Validators
{
    public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
    {
        public GetAccountByIdQueryValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Id обязателен")
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");
        }
    }
}
