using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaskBoard.Api.Responses;

namespace TaskBoard.Api.Validation;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public FluentValidationActionFilter(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var validationErrors = new List<ValidationErrorResponse>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            validationErrors.AddRange(result.Errors.Select(error =>
                new ValidationErrorResponse(error.ErrorCode, error.ErrorMessage)));
        }

        if (validationErrors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new ApiErrorResponse(
                "Validation.Failed",
                "Validation failed.",
                validationErrors));
            return;
        }

        await next();
    }
}
