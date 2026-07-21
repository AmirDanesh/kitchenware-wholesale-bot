using FluentValidation;
using KitchenwareBot.Application.DTOs;

namespace KitchenwareBot.Application.Validation;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
    }
}
