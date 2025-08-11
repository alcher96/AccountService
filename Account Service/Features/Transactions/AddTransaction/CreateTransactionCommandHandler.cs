using AccountService.Features.Transactions.AddTransaction.Command;
using AccountService.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions.AddTransaction
{
    // ReSharper disable once UnusedMember.Global
    public class CreateTransactionCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<CreateTransactionCommand> validator)
        : IRequestHandler<CreateTransactionCommand, MbResult<TransactionDto>>
    {
        public async Task<MbResult<TransactionDto?>> Handle(CreateTransactionCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                Console.WriteLine($"Validation errors for AccountId={request.AccountId}: {System.Text.Json.JsonSerializer.Serialize(errors)}");
                return MbResult<TransactionDto>.Failure(errors)!;
            }

            // Маппим команду в транзакцию
            var transaction = mapper.Map<Transaction>(request);
            if (transaction == null)
            {
                Console.WriteLine($"Mapping failed for CreateTransactionCommand: AccountId={request.AccountId}");
                return MbResult<TransactionDto>.Failure("Failed to map transaction")!;
            }

            // Добавляем транзакцию через репозиторий
            var result = await accountRepository.AddTransactionAsync(request.AccountId, transaction);
            if (result == null)
            {
                Console.WriteLine($"Failed to add transaction for AccountId={request.AccountId}");
                return MbResult<TransactionDto>.Failure("Concurrency conflict")!;
            }

            // Маппим в DTO
            var transactionDto = mapper.Map<TransactionDto>(result);
            Console.WriteLine($"Created transaction: TransactionId={transactionDto.TransactionId}, AccountId={transactionDto.AccountId}, Type={transactionDto.Type}, Amount={transactionDto.Amount}, Currency={transactionDto.Currency}");
            return MbResult<TransactionDto>.Success(transactionDto)!;
        }
    }
}
