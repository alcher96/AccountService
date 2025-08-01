using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.GetAccount
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, MbResult<List<AccountDto>>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<GetAccountsQuery> _validator;

        public GetAccountsQueryHandler(IAccountRepository accountRepository, IMapper mapper, IValidator<GetAccountsQuery> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<List<AccountDto>>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
        {

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<List<AccountDto>>.Failure(errors);
            }

            var accounts = await _accountRepository.GetAllAsync(request.OwnerId, request.Type);
            var accountDto = _mapper.Map<List<AccountDto>>(accounts);
            return MbResult<List<AccountDto>>.Success(accountDto);
        }
    }
}
