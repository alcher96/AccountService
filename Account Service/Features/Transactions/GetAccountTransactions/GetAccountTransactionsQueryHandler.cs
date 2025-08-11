using AccountService.Data;
using AccountService.Features.Transactions.GetAccountTransactions.Query;
using AccountService.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace AccountService.Features.Transactions.GetAccountTransactions
{
    // ReSharper disable once UnusedMember.Global
    public class
        GetAccountTransactionsQueryHandler(
            IAccountRepository accountRepository,
            IMapper mapper,
            AccountDbContext context,
            IValidator<GetAccountTransactionsQuery> validator)
        : IRequestHandler<GetAccountTransactionsQuery,
            MbResult<List<TransactionDto>>>
    {
        public async Task<MbResult<List<TransactionDto>>> Handle(GetAccountTransactionsQuery request,
        CancellationToken cancellationToken)
        {
            // Валидация запроса
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                Console.WriteLine($"Validation errors for AccountId={request.AccountId}: {System.Text.Json.JsonSerializer.Serialize(errors)}");
                return MbResult<List<TransactionDto>>.Failure(errors);
            }

            // Проверяем существование счёта
            var account = await accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
            {
                Console.WriteLine($"Account not found: AccountId={request.AccountId}");
                return MbResult<List<TransactionDto>>.Failure("Account not found");
            }

            // Логируем все AccountId из таблицы Transactions
            var allTransactionAccountIds = await context.Transactions
                .AsNoTracking()
                .Select(t => t.AccountId)
                .Distinct()
                .ToListAsync(cancellationToken);
            Console.WriteLine($"All AccountIds in Transactions table: {string.Join(", ", allTransactionAccountIds)}");

            // Загружаем транзакции напрямую из таблицы Transactions
            var transactionsQuery = context.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == request.AccountId);

            // Логируем SQL-запрос
            var sqlQuery = transactionsQuery.ToQueryString();
            Console.WriteLine($"SQL Query for AccountId={request.AccountId}: {sqlQuery}");

            // Применяем фильтры по датам
            if (request.StartDate.HasValue)
            {
                Console.WriteLine($"Filtering transactions after StartDate={request.StartDate.Value}");
                transactionsQuery = transactionsQuery.Where(t => t.DateTime >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                Console.WriteLine($"Filtering transactions before EndDate={request.EndDate.Value}");
                transactionsQuery = transactionsQuery.Where(t => t.DateTime <= request.EndDate.Value);
            }

            // Логируем количество транзакций
            var transactionList = await transactionsQuery.ToListAsync(cancellationToken);
            Console.WriteLine($"Loaded {transactionList.Count} transactions for AccountId={request.AccountId}");

            // Логируем детали транзакций
            foreach (var transaction in transactionList)
            {
                Console.WriteLine($"Transaction: TransactionId={transaction.TransactionId}, AccountId={transaction.AccountId}, CounterpartyAccountId={transaction.CounterpartyAccountId}, Type={transaction.Type}, DateTime={transaction.DateTime}, Amount={transaction.Amount}, Currency={transaction.Currency}, Description={transaction.Description}");
            }

            // Преобразуем в DTO
            var transactionDto = mapper.Map<List<TransactionDto>>(transactionList);
            Console.WriteLine($"Mapped {transactionDto.Count} transactions to DTO for AccountId={request.AccountId}");
            return MbResult<List<TransactionDto>>.Success(transactionDto);
        }
    }
}
