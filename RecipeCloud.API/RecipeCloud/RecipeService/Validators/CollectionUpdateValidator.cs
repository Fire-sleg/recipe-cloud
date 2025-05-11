using FluentValidation;
using RecipeService.Models.Collections.DTOs;

namespace RecipeService.Validators
{
    public class CollectionUpdateValidator : AbstractValidator<CollectionUpdateDTO>
    {
        public CollectionUpdateValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .Length(3, 100).WithMessage("Title must be between 3 and 100 characters");

            RuleFor(x => x.CreatedByUsername)
                .MaximumLength(50).WithMessage("Username cannot exceed 50 characters");

            RuleForEach(x => x.RecipeIds)
                .NotEmpty().WithMessage("Recipe ID cannot be empty");
        }
    }
}
