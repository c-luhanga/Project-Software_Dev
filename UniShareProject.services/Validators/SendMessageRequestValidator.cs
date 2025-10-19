using FluentValidation;
using UniShareProject.services.DTOs;

namespace UniShareProject.services.Validators;

/// <summary>
/// Validator for SendMessageRequest
/// </summary>
public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content is required.")
            .Length(1, 4000)
            .WithMessage("Message content must be between 1 and 4000 characters.");

        RuleFor(x => x.ConversationId)
            .GreaterThan(0)
            .WithMessage("Conversation ID must be a positive integer.");
    }
}