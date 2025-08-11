using AccountService.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using AccountService.Features.Accounts.AddAccount.Command;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace AccountService.Features.Accounts.AddAccount
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
