using Account_Service.Accounts.AddAccount.Command;
using Account_Service.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace Account_Service.Accounts.AddAccount
{
    // ReSharper disable once UnusedMember.Global
    public class CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<CreateAccountCommand> validator)
        : IRequestHandler<CreateAccountCommand, MbResult<AccountDto>>
    {
        public async Task<MbResult<AccountDto>> Handle(CreateAccountCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<AccountDto>.Failure(errors);
            }

            var account = mapper.Map<Account>(request);
            await accountRepository.AddAsync(account);
            var accountDto = mapper.Map<AccountDto>(account);
            return MbResult<AccountDto>.Success(accountDto);
        }
    }
}
