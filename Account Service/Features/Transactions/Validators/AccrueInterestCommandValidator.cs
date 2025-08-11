using AccountService.Features.Transactions.AccrueInterest.Command;
using FluentValidation;
// ReSharper disable UnusedMember.Global
// ReSharper disable EmptyConstructor Нестроая валидация
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AccountService.Features.Transactions.Validators
{
    public class AccrueInterestCommandValidator : AbstractValidator<AccrueInterestCommand>
    {
        public AccrueInterestCommandValidator()
        {
          
        }
    }
}
