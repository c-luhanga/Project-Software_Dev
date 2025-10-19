using FluentValidation;
using UniShareProject.services.DTOs;

namespace UniShareProject.services.Validators;

/// <summary>
/// Validator for StartConversationRequest
/// </summary>
public class StartConversationRequestValidator : AbstractValidator<StartConversationRequest>
{
    public StartConversationRequestValidator()
    {
        RuleFor(x => x.OtherUserId)
            .GreaterThan(0)
            .WithMessage("Other user ID must be a positive integer.");

        RuleFor(x => x.ItemId)
            .GreaterThan(0)
            .When(x => x.ItemId.HasValue)
            .WithMessage("Item ID must be a positive integer when specified.");
    }
}