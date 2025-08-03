using Account_Service.Accounts.DeleteAccount.Command;
using Account_Service.Repositories;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.DeleteAccount
{
    // ReSharper disable once UnusedMember.Global
    public class DeleteAccountCommandHandler(
        IAccountRepository accountRepository,
        IValidator<DeleteAccountCommand> validator)
        : IRequestHandler<DeleteAccountCommand, MbResult<Unit>>
    {
        public async Task<MbResult<Unit>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<Unit>.Failure(errors);
            }
            await accountRepository.GetByIdAsync(request.Id);
            await accountRepository.DeleteAsync(request.Id);

            return MbResult<Unit>.Success(Unit.Value);
        }
    }
}
