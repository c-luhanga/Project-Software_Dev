using FluentValidation;
using UniShareProject.services.Models;

namespace UniShareProject.services.Validators;

/// <summary>
/// Validator for SearchItemsRequest with business rules
/// </summary>
public class SearchItemsRequestValidator : AbstractValidator<SearchItemsRequest>
{
    public SearchItemsRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .When(x => x.CategoryId.HasValue)
            .WithMessage("CategoryId must be greater than 0 when specified");

        RuleFor(x => x.StatusId)
            .InclusiveBetween((byte)0, (byte)4)
            .When(x => x.StatusId.HasValue)
            .WithMessage("StatusId must be between 0 and 4 when specified");

        RuleFor(x => x.ConditionId)
            .InclusiveBetween((byte)1, (byte)4)
            .When(x => x.ConditionId.HasValue)
            .WithMessage("ConditionId must be between 1 and 4 when specified");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.Q)
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Q))
            .WithMessage("Search query cannot exceed 255 characters");
    }
}