using TaskBoard.Application.Common;

namespace TaskBoard.Api.Responses;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    IReadOnlyList<ValidationErrorResponse> Validation);

public sealed record ValidationErrorResponse(string Code, string Message)
{
    public static ValidationErrorResponse FromValidationError(ValidationError error)
    {
        return new ValidationErrorResponse(error.Code, error.Message);
    }
}
