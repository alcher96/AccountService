using Account_Service.Repositories;
using Account_Service.Transactions.GetAccountTransactions.Query;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.GetAccountTransactions
{
    // ReSharper disable once UnusedMember.Global
    public class
        GetAccountTransactionsQueryHandler(
            IAccountRepository accountRepository,
            IMapper mapper,
            IValidator<GetAccountTransactionsQuery> validator)
        : IRequestHandler<GetAccountTransactionsQuery,
            MbResult<List<TransactionDto>>>
    {
        public async Task<MbResult<List<TransactionDto>?>> Handle(GetAccountTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            // Валидация запроса
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<List<TransactionDto>>.Failure(errors)!;
            }

            var account = await accountRepository.GetByIdAsync(request.AccountId);

            var transactions = account.Transactions.AsQueryable();
            if (request.StartDate.HasValue)
            {
                transactions = transactions.Where(t => t.DateTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                transactions = transactions.Where(t => t.DateTime <= request.EndDate.Value);
            }

            var transactionDto = mapper.Map<List<TransactionDto>>(transactions.ToList());
            return MbResult<List<TransactionDto>>.Success(transactionDto)!;
        }
    }
}
