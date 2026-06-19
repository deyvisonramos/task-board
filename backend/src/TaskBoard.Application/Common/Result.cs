namespace TaskBoard.Application.Common;

public class Result
{
    protected Result(bool isSuccess, Error? error, IReadOnlyList<ValidationError> validationErrors)
    {
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public static Result Success()
    {
        return new Result(true, null, []);
    }

    public static Result Failure(string code, string message)
    {
        return new Result(false, new Error(code, message), []);
    }

    public static Result ValidationFailure(IReadOnlyList<ValidationError> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        return new Result(false, null, validationErrors);
    }
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, null, [])
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, error, [])
    {
    }

    private Result(IReadOnlyList<ValidationError> validationErrors)
        : base(false, null, validationErrors)
    {
    }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value)
    {
        return new Result<T>(value);
    }

    public new static Result<T> Failure(string code, string message)
    {
        return new Result<T>(new Error(code, message));
    }

    public new static Result<T> ValidationFailure(IReadOnlyList<ValidationError> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(validationErrors);

        return new Result<T>(validationErrors);
    }
}
