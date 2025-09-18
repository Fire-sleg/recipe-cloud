using FluentValidation;
using Microsoft.AspNetCore.Http;
using RecipeService.Models.Recipes.DTOs;

namespace RecipeService.Validators.RecipeValidators
{
    public class RecipeCreateDTOValidator : AbstractValidator<RecipeCreateDTO>
    {
        public RecipeCreateDTOValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required");

            RuleFor(x => x.Ingredients)
                .NotEmpty().WithMessage("At least one ingredient is required.")
                .Must(ingredients => ingredients.All(i => !string.IsNullOrWhiteSpace(i)))
                .WithMessage("Ingredients cannot be empty or whitespace.");

            RuleFor(x => x.CookingTime)
                .GreaterThan(0).WithMessage("Cooking time must be greater than 0 minutes.");

            RuleFor(x => x.Difficulty)
                .NotEmpty().WithMessage("Difficulty is required.")
                .Must(difficulty => new[] { "easy", "medium", "hard" }.Contains(difficulty.ToLower()))
                .WithMessage("Difficulty must be 'easy', 'medium', or 'hard'.");

            RuleFor(x => x.Image)
                .NotNull().WithMessage("Image is required.")
                .Must(image => image.Length > 0).WithMessage("Image file cannot be empty.")
                .Must(image => image.Length <= 5 * 1024 * 1024).WithMessage("Image size must not exceed 5 MB.")
                .Must(image => new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(image.FileName).ToLower()))
                .WithMessage("Image must be in JPG or PNG format.");
        }
    }
}