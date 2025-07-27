using Account_Service.Accounts.GetAccount.Query;
using FluentValidation;

namespace Account_Service.Accounts.Validators
{
    public class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
    {
        public GetAccountsQueryValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum()
                .When(x => x.Type.HasValue)
                .WithMessage("Недопустимый тип счета");
        }
    }
}
