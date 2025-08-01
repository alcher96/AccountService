using Account_Service.Accounts.UpdateAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.UpdateAccount
{
    // ReSharper disable once UnusedMember.Global
    public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, MbResult<AccountDto>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateAccountCommand> _validator;

        public UpdateAccountCommandHandler(IAccountRepository accountRepository, IMapper mapper,
            IValidator<UpdateAccountCommand> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<AccountDto>> Handle(UpdateAccountCommand request,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<AccountDto>.Failure(errors);
            }

            var account = await _accountRepository.GetByIdAsync(request.Id);
            _mapper.Map(request.Request, account);
            await _accountRepository.UpdateAsync(account);
            var accountDto = _mapper.Map<AccountDto>(account);
            return MbResult<AccountDto>.Success(accountDto);
        }
    }
}
