using AccountService.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using AccountService.Features.Accounts.AddAccount.Command;
using AccountService.Messaging.Events;
using AccountService.Messaging;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace AccountService.Features.Accounts.AddAccount
{
    // ReSharper disable once UnusedMember.Global
    public class CreateAccountCommandHandler(
        IAccountRepository accountRepository, 
        IMapper mapper,
        IValidator<CreateAccountCommand> validator,
        ILogger<CreateAccountCommandHandler> logger) // Добавляем логгер
        : IRequestHandler<CreateAccountCommand, MbResult<AccountDto>>
    {
        public async Task<MbResult<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            // Валидация
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<AccountDto>.Failure(errors);
            }

            try
            {
                var account = mapper.Map<Account>(request);

                // Создаем событие
                var @event = new AccountOpenedEvent
                {
                    AccountId = account.AccountId,
                    OwnerId = account.OwnerId,
                    Currency = account.Currency,
                    Type = account.AccountType.ToString()  // e.g., "Checking"
                };
                var payload = System.Text.Json.JsonSerializer.Serialize(@event);

                var outboxMessage = new OutboxMessage
                {
                    EventType = "AccountOpened",
                    Payload = payload
                };

                // Сохраняем в транзакции
                await accountRepository.AddAsync(account, outboxMessage);

                var accountDto = mapper.Map<AccountDto>(account);
                logger.LogInformation("Account created and event queued in outbox: AccountId: {AccountId}, EventId: {EventId}", account.AccountId, @event.EventId);

                return MbResult<AccountDto>.Success(accountDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create account for OwnerId: {OwnerId}", request.OwnerId);
                throw;
            }
        }
    }
}
