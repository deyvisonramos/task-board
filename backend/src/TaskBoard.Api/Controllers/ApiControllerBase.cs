using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.Responses;
using TaskBoard.Application.Common;

namespace TaskBoard.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

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

        return BadRequest(response);
    }
}
