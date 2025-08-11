using AccountService.Repositories;
using FluentValidation;
using AccountService.Features.Accounts.DeleteAccount.Command;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
    {
        public DeleteAccountCommandValidator(IAccountRepository accountRepository)
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
