using Account_Service.Accounts.GetAccount.Query;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace Account_Service.Accounts.GetAccount
{
    // ReSharper disable once UnusedMember.Global
    public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, MbResult<AccountDto>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<GetAccountByIdQuery> _validator;

        public GetAccountByIdQueryHandler(IAccountRepository accountRepository, IMapper mapper,
            IValidator<GetAccountByIdQuery> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            // Валидация запроса
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<AccountDto>.Failure(errors);
            }
            var account = await _accountRepository.GetByIdAsync(request.Id);
            var accountDto = _mapper.Map<AccountDto>(account);
            return MbResult<AccountDto>.Success(accountDto);
        }
    }
}
