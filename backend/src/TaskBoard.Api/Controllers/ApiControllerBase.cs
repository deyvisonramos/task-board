using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Responses;
using TaskBoard.Application.Common;

namespace TaskBoard.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var subject = User.FindFirst("sub")?.Value;

        return Guid.TryParse(subject, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated user is missing a valid subject claim.");
    }

    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        return FromFailure(result);
    }

    protected IActionResult FromResult(Result result, Func<IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return FromFailure(result);
    }

    private IActionResult FromFailure(Result result)
    {
        if (result.ValidationErrors.Count > 0)
        {
            return BadRequest(new ApiErrorResponse(
                "Validation.Failed",
                "Validation failed.",
                result.ValidationErrors.Select(ValidationErrorResponse.FromValidationError).ToArray()));
        }

        var error = result.Error
            ?? new Error("Unknown.Error", "An unexpected error occurred.");

        var response = new ApiErrorResponse(error.Code, error.Message, []);

        return error.Code switch
        {
            "Task.NotFound" => NotFound(response),
            _ => BadRequest(response)
        };
    }
}
