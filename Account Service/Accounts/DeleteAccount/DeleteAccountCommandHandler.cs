using Account_Service.Accounts.DeleteAccount.Command;
using Account_Service.Repositories;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Accounts.DeleteAccount
{
    // ReSharper disable once UnusedMember.Global
    public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, MbResult<Unit>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IValidator<DeleteAccountCommand> _validator;

        public DeleteAccountCommandHandler(IAccountRepository accountRepository,
            IValidator<DeleteAccountCommand> validator)
        {
            _accountRepository = accountRepository;
            _validator = validator;
        }

        public async Task<MbResult<Unit>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<Unit>.Failure(errors);
            }
            var account = await _accountRepository.GetByIdAsync(request.Id);
            await _accountRepository.DeleteAsync(request.Id);

            return MbResult<Unit>.Success(Unit.Value);
        }
    }
}
