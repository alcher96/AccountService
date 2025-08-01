using Account_Service.Accounts.PatchAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.PatchAccount
{
    // ReSharper disable once UnusedMember.Global
    public class PatchAccountCommandHandler : IRequestHandler<PatchAccountCommand, MbResult<AccountDto>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<PatchAccountCommand> _validator;

        public PatchAccountCommandHandler(IAccountRepository accountRepository, IMapper mapper,
            IValidator<PatchAccountCommand> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<AccountDto>> Handle(PatchAccountCommand request, CancellationToken cancellationToken)
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

            var account = await _accountRepository.GetByIdAsync(request.Id);
            _mapper.Map(request.Request, account);
            await _accountRepository.UpdateAsync(account);
            var accountDto = _mapper.Map<AccountDto>(account);
            return MbResult<AccountDto>.Success(accountDto);
        }
    }
}
