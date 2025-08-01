using Account_Service.Repositories;
using Account_Service.Transactions.GetAccountTransactions.Query;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.GetAccountTransactions
{
    // ReSharper disable once UnusedMember.Global
    public class
        GetAccountTransactionsQueryHandler : IRequestHandler<GetAccountTransactionsQuery,
        MbResult<List<TransactionDto>>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<GetAccountTransactionsQuery> _validator;

        public GetAccountTransactionsQueryHandler(IAccountRepository accountRepository, IMapper mapper,
            IValidator<GetAccountTransactionsQuery> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<List<TransactionDto>>> Handle(GetAccountTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            // Валидация запроса
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<List<TransactionDto>>.Failure(errors);
            }

            var account = await _accountRepository.GetByIdAsync(request.AccountId);

            var transactions = account.Transactions.AsQueryable();
            if (request.StartDate.HasValue)
            {
                transactions = transactions.Where(t => t.DateTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                transactions = transactions.Where(t => t.DateTime <= request.EndDate.Value);
            }

            var transactionDto = _mapper.Map<List<TransactionDto>>(transactions.ToList());
            return MbResult<List<TransactionDto>>.Success(transactionDto);
        }
    }
}
