using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace Account_Service.Accounts.AddAccount
{
    // ReSharper disable once UnusedMember.Global
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, MbResult<AccountDto>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateAccountCommand> _validator;

        public CreateAccountCommandHandler(IAccountRepository accountRepository, IMapper mapper,
            IValidator<CreateAccountCommand> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<AccountDto>> Handle(CreateAccountCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<AccountDto>.Failure(errors);
            }

            var account = _mapper.Map<Account>(request);
            await _accountRepository.AddAsync(account);
            var accountDto = _mapper.Map<AccountDto>(account);
            return MbResult<AccountDto>.Success(accountDto);
        }
    }
}
