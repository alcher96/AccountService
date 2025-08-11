using AccountService.Features.Accounts.GetAccount.Query;
using FluentValidation;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Accounts.Validators
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
    {
        public GetAccountsQueryValidator()
        {
            RuleFor(x => x.Type)
                .Cascade(CascadeMode.Stop)
                .IsInEnum()
                .When(x => x.Type.HasValue)
                .WithMessage("Недопустимый тип счета");
        }
    }
}
