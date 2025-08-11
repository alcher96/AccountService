using AccountService.Repositories;
using FluentValidation;
using AccountService.Features.Accounts.GetAccount.Query;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
    {
        private readonly IAccountRepository _accountRepository;

        public GetAccountByIdQueryValidator(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;

            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Id обязателен")
                .MustAsync(async (id, _) =>
                {
                    var account = await _accountRepository.GetByIdAsync(id);
                    Console.WriteLine($"[GetAccountByIdQueryValidator] Checking account: Id={id}, Found={account != null}");
                    return account != null;
                })
                .WithMessage("Счет не найден");
        }
    }
}
