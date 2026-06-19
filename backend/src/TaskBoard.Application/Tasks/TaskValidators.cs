using FluentValidation;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.Application.Tasks;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .WithErrorCode("Task.TitleRequired")
            .WithMessage("Title is required.")
            .MaximumLength(TaskItem.TitleMaxLength)
            .WithErrorCode("Task.TitleTooLong")
            .WithMessage("Title must be 100 characters or fewer.");

        RuleFor(request => request.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength)
            .WithErrorCode("Task.DescriptionTooLong")
            .WithMessage("Description must be 1000 characters or fewer.");

        RuleFor(request => request.DueDate)
            .NotNull()
            .WithErrorCode("Task.DueDateRequired")
            .WithMessage("Due date is required.");

        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(status))
            .WithErrorCode("Task.StatusInvalid")
            .WithMessage("Status is invalid.");
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .WithErrorCode("Task.TitleRequired")
            .WithMessage("Title is required.")
            .MaximumLength(TaskItem.TitleMaxLength)
            .WithErrorCode("Task.TitleTooLong")
            .WithMessage("Title must be 100 characters or fewer.");

        RuleFor(request => request.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength)
            .WithErrorCode("Task.DescriptionTooLong")
            .WithMessage("Description must be 1000 characters or fewer.");

        RuleFor(request => request.DueDate)
            .NotNull()
            .WithErrorCode("Task.DueDateRequired")
            .WithMessage("Due date is required.");

        RuleFor(request => request.Status)
            .Must(status => Enum.IsDefined(status))
            .WithErrorCode("Task.StatusInvalid")
            .WithMessage("Status is invalid.");
    }
}
