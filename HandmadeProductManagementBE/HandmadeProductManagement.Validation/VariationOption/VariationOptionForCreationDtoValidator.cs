﻿using FluentValidation;
using HandmadeProductManagement.ModelViews.VariationModelViews;
using HandmadeProductManagement.ModelViews.VariationOptionModelViews;

namespace HandmadeProductManagement.Validation.VariationOption
{
    public class VariationOptionForCreationDtoValidator : AbstractValidator<VariationOptionForCreationDto>
    {
        public VariationOptionForCreationDtoValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.")
                .MaximumLength(100).WithMessage("Value cannot exceed 100 characters.")
                .Matches(@"^[a-zA-Z0-9\s]+$").WithMessage("Value can only contain letters, numbers, and spaces.");

        }
    }
}
