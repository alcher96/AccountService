using FluentValidation;
using MediatR;
#pragma warning disable CS1591 //Избыточный xml комментарий

namespace Account_Service
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : MbResult<object>, new() // Ограничиваем TResponse типом MbResult
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var failures = _validators
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
                        Error = "Validation failed",
                        ValidationErrors = validationErrors
                    };
                }
            }

            return await next(cancellationToken);
        }
    }
}

