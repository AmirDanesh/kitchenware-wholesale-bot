using FluentValidation;
using KitchenwareBot.Application.DTOs;

namespace KitchenwareBot.Application.Validation;

public class TierInputDtoValidator : AbstractValidator<TierInputDto>
{
    public TierInputDtoValidator()
    {
        RuleFor(x => x.MinQuantity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x)
            .Must(x => x.MaxQuantity is null || x.MaxQuantity >= x.MinQuantity)
            .WithMessage("حداکثر تعداد نمی‌تواند کمتر از حداقل تعداد باشد.");
    }
}
