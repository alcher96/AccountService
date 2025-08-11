using FluentValidation;
using MediatR;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace AccountService
{
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : MbResult<object>, new() // Ограничиваем TResponse типом MbResult
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var failures = validators
                    .Select(x => x.Validate(context))
                    .SelectMany(x => x.Errors)
                    .Where(x => x != null)
                    .ToList();

                if (failures.Any())
                {
                    var validationErrors = failures
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    return new TResponse
                    {
                        IsSuccess = false,
                        MbError = "Validation failed",
                        ValidationErrors = validationErrors
                    };
                }
            }

            return await next(cancellationToken);
        }
    }
}

