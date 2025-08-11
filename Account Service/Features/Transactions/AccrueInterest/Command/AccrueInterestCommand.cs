using MediatR;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Features.Transactions.AccrueInterest.Command
{
    public class AccrueInterestCommand : IRequest<MbResult<bool>>
    {
        public Guid? AccountId { get; set; } // Если null, начислять для всех счетов типа Deposit
    }
}
