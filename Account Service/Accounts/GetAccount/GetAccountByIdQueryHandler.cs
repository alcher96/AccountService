using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Избыточный xml комментарий
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace Account_Service.Accounts.GetAccount
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountByIdQueryHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<GetAccountByIdQuery> validator)
        : IRequestHandler<GetAccountByIdQuery, MbResult<AccountDto>>
    {
        public async Task<MbResult<AccountDto?>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            // Валидация запроса
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<AccountDto>.Failure(errors)!;
            }
            var account = await accountRepository.GetByIdAsync(request.Id);
            var accountDto = mapper.Map<AccountDto>(account);
            return MbResult<AccountDto>.Success(accountDto)!;
        }
    }
}
