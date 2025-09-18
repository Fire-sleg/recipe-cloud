using FluentValidation;
using RecipeService.Models.Rating;
using RecipeService.Models.Rating.DTOs;

namespace RecipeService.Validators.RecipeRatingValidators
{
    public class RecipeRatingValidator : AbstractValidator<RecipeRatingDTO>
    {
        public RecipeRatingValidator()
        {
            RuleFor(r => r.RecipeId)
                .NotEmpty().WithMessage("RecipeId is required.");

            RuleFor(r => r.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5.");
        }
    }
}
