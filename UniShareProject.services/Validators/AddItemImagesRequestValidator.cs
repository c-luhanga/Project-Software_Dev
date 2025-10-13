using FluentValidation;
using UniShareProject.services.Models;

namespace UniShareProject.services.Validators;

/// <summary>
/// Validator for AddItemImagesRequest with business rules
/// </summary>
public class AddItemImagesRequestValidator : AbstractValidator<AddItemImagesRequest>
{
    public AddItemImagesRequestValidator()
    {
        RuleFor(x => x.ImageUrls)
            .NotNull()
            .WithMessage("ImageUrls is required")
            .NotEmpty()
            .WithMessage("At least one image URL is required")
            .Must(urls => urls.Count >= 1 && urls.Count <= 4)
            .WithMessage("Must contain between 1 and 4 image URLs");

        RuleForEach(x => x.ImageUrls)
            .NotEmpty()
            .WithMessage("Image URL cannot be empty")
            .Must(BeAValidAbsoluteUrl)
            .WithMessage("Each image URL must be a valid absolute URL");
    }

    private static bool BeAValidAbsoluteUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}