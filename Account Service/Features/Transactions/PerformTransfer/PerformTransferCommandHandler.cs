using AccountService.Features.Transactions.PerformTransfer.Command;
using AccountService.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions.PerformTransfer
{
    // ReSharper disable once UnusedMember.Global
    public class PerformTransferCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        IValidator<PerformTransferCommand> validator)
        : IRequestHandler<PerformTransferCommand, MbResult<TransactionDto[]>>
    {
        public async Task<MbResult<TransactionDto[]?>> Handle(PerformTransferCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<TransactionDto[]>.Failure(errors)!;
            }

            try
            {
                // Вызываем TransferAsync из репозитория
                var transactionResult = await accountRepository.TransferAsync(request.FromAccountId, request.ToAccountId, request.Amount, request.Currency!, request.Description!);

                if (transactionResult == null)
                {
                    return MbResult<TransactionDto[]>.Failure("Transfer failed")!;
                }

                // Маппим транзакции в DTO
                var debitDto = mapper.Map<TransactionDto>(transactionResult.Value.debit);
                var creditDto = mapper.Map<TransactionDto>(transactionResult.Value.credit);

                return MbResult<TransactionDto[]>.Success([debitDto, creditDto])!;
            }
            catch (DbUpdateConcurrencyException)
            {
                return MbResult<TransactionDto[]>.Failure("Concurrency conflict")!;
            }
            catch (Exception ex)
            {
                return MbResult<TransactionDto[]>.Failure(ex.Message)!;
            }
        }
    }
}
