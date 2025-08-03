using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.GetAccount
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountsQueryHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<GetAccountsQuery> validator)
        : IRequestHandler<GetAccountsQuery, MbResult<List<AccountDto>>>
    {
        public async Task<MbResult<List<AccountDto>?>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
        {

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<List<AccountDto>>.Failure(errors)!;
            }

            var accounts = await accountRepository.GetAllAsync(request.OwnerId, request.Type);
            var accountDto = mapper.Map<List<AccountDto>>(accounts);
            return MbResult<List<AccountDto>>.Success(accountDto)!;
        }
    }
}
