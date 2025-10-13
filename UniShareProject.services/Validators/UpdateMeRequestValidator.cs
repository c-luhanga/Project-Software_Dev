using FluentValidation;
using UniShareProject.services.DTOs;

namespace UniShareProject.services.Validators;

/// <summary>
/// Validator for UpdateMeRequest with business rules
/// </summary>
public class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeRequestValidator()
    {
        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone must not exceed 20 characters");

        RuleFor(x => x.House)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.House))
            .WithMessage("House must not exceed 50 characters");

        RuleFor(x => x.ProfileImageUrl)
            .Must(BeAValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.ProfileImageUrl))
            .WithMessage("ProfileImageUrl must be a valid absolute URL");
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.IsWellFormedUriString(url, UriKind.Absolute);
    }
}
