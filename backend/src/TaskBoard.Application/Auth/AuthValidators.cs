using FluentValidation;

namespace TaskBoard.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithErrorCode("Auth.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("Auth.EmailInvalid")
            .WithMessage("Email must be a valid email address.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithErrorCode("Auth.PasswordRequired")
            .WithMessage("Password is required.");
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithErrorCode("Auth.EmailRequired")
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithErrorCode("Auth.EmailInvalid")
            .WithMessage("Email must be a valid email address.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithErrorCode("Auth.PasswordRequired")
            .WithMessage("Password is required.");
    }
}
