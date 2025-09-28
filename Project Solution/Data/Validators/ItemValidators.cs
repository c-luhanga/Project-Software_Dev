using FluentValidation;
using UniShare.Data.Dtos;

namespace UniShare.Data.Validators
{
    public class ItemCreateDtoValidator : AbstractValidator<ItemCreateDto>
    {
        public ItemCreateDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().Length(3, 80);
            RuleFor(x => x.Description)
                .MaximumLength(2000);
            RuleFor(x => x.Category)
                .NotEmpty().MaximumLength(100);
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0);
            RuleFor(x => x.Condition)
                .Must(c => new[] { "New", "LikeNew", "Good", "Fair" }.Contains(c))
                .WithMessage("Condition must be one of: New, LikeNew, Good, Fair");
        }
    }

    public class ItemUpdateDtoValidator : AbstractValidator<ItemUpdateDto>
    {
        public ItemUpdateDtoValidator()
        {
            RuleFor(x => x.Title)
                .MinimumLength(3).MaximumLength(80)
                .When(x => x.Title != null);
            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description != null);
            RuleFor(x => x.Category)
                .MaximumLength(100)
                .When(x => x.Category != null);
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Price.HasValue);
            RuleFor(x => x.Condition)
                .Must(c => new[] { "New", "LikeNew", "Good", "Fair" }.Contains(c!))
                .When(x => x.Condition != null)
                .WithMessage("Condition must be one of: New, LikeNew, Good, Fair");
        }
    }

    public class MessageCreateDtoValidator : AbstractValidator<MessageCreateDto>
    {
        public MessageCreateDtoValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().Length(1, 1000);
        }
    }
}
