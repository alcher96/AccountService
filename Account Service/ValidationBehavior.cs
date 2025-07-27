using FluentValidation;
using MediatR;

namespace Account_Service
{
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!validators.Any()) return await next(cancellationToken);
            var context = new ValidationContext<TRequest>(request);
            var failures = validators
                .Select(x =>x.Validate(context))
                .SelectMany(x => x.Errors)
                .Where(x =>x != null)
                .ToList();
            if (failures.Any())
                throw new ValidationException(failures);
            return await next(cancellationToken);
        }
    }
}
