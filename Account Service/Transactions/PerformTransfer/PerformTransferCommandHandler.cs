using Account_Service.Repositories;
using Account_Service.Transactions.PerformTransfer.Command;
using AutoMapper;
using FluentValidation;
using MediatR;
#pragma warning disable CS1591 // Избыточный xml комментарий

namespace Account_Service.Transactions.PerformTransfer
{
    // ReSharper disable once UnusedMember.Global
    public class PerformTransferCommandHandler : IRequestHandler<PerformTransferCommand, MbResult<TransactionDto[]>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<PerformTransferCommand> _validator;

        public PerformTransferCommandHandler(IAccountRepository accountRepository, IMapper mapper,
            IValidator<PerformTransferCommand> validator)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<MbResult<TransactionDto[]>> Handle(PerformTransferCommand request,
            CancellationToken cancellationToken)
        {
            // Валидация команды
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return MbResult<TransactionDto[]>.Failure(errors);
            }

            var fromAccount = await _accountRepository.GetByIdAsync(request.FromAccountId);
            var toAccount = await _accountRepository.GetByIdAsync(request.ToAccountId);


            if (fromAccount.Balance < request.Amount)
            {
                return MbResult<TransactionDto[]>.Failure("Недостаточно средств на счёте отправителя");
            }


            var debitTransaction =
                _mapper.Map<Transaction>(request, opt => opt.Items["TransactionType"] = TransactionType.Debit);
            var creditTransaction =
                _mapper.Map<Transaction>(request, opt => opt.Items["TransactionType"] = TransactionType.Credit);

 
            fromAccount.Balance -= request.Amount;
            fromAccount.Transactions.Add(creditTransaction); // Credit = списание
            toAccount.Balance += request.Amount;
            toAccount.Transactions.Add(debitTransaction); // Debit = зачисление

            await _accountRepository.UpdateAsync(fromAccount);
            await _accountRepository.UpdateAsync(toAccount);

            var result = new[]
            {
                _mapper.Map<TransactionDto>(creditTransaction),
                _mapper.Map<TransactionDto>(debitTransaction)
            };
            return MbResult<TransactionDto[]>.Success(result);
        }
    }
}
