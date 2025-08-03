using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
    {
        public GetAccountByIdQueryValidator(IAccountRepository accountRepository)
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Id обязателен")
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract решарпер предлагает неправильную логику
                .Must(id => accountRepository.GetByIdAsync(id).Result != null)
                .WithMessage("Счет не найден");
        }
    }
}
