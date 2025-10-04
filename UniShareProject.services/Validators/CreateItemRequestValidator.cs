using FluentValidation;
using UniShareProject.services.Models;

namespace UniShareProject.services.Validators;

/// <summary>
/// Validator for CreateItemRequest with business rules
/// </summary>
public class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .Length(3, 255)
            .WithMessage("Title must be between 3 and 255 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .Length(10, 4000)
            .WithMessage("Description must be between 10 and 4000 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue)
            .WithMessage("Price must be greater than or equal to 0");

        RuleFor(x => x.ConditionId)
            .InclusiveBetween((byte)1, (byte)4)
            .WithMessage("ConditionId must be between 1 and 4");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("CategoryId must be greater than 0 when specified");
    }
}