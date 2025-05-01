using FluentValidation;
using Microsoft.AspNetCore.Http;
using RecipeService.Models.DTOs;

namespace RecipeService.Validators
{
    public class RecipeUpdateDTOValidator : AbstractValidator<(RecipeUpdateDTO DTO, IFormFile Image)>
    {
        public RecipeUpdateDTOValidator()
        {
            RuleFor(x => x.DTO.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(x => x.DTO.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(100).WithMessage("Title must not exceed 100 characters.")
                .When(x => x.DTO.Title != null);

            RuleFor(x => x.DTO.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
                .When(x => x.DTO.Description != null);

            RuleFor(x => x.DTO.Ingredients)
                .NotEmpty().WithMessage("At least one ingredient is required.")
                .Must(ingredients => ingredients.All(i => !string.IsNullOrWhiteSpace(i)))
                .WithMessage("Ingredients cannot be empty or whitespace.")
                .When(x => x.DTO.Ingredients != null);

            RuleFor(x => x.DTO.CookingTime)
                .GreaterThan(0).WithMessage("Cooking time must be greater than 0 minutes.")
                .When(x => x.DTO.CookingTime != 0);

            RuleFor(x => x.DTO.Difficulty)
                .NotEmpty().WithMessage("Difficulty is required.")
                .Must(difficulty => new[] { "easy", "medium", "hard" }.Contains(difficulty.ToLower()))
                .WithMessage("Difficulty Brasst be 'easy', 'medium', or 'hard'.")
                .When(x => x.DTO.Difficulty != null);

            RuleFor(x => x.Image)
                .Must(image => image.Length > 0).WithMessage("Image file cannot be empty.")
                .When(x => x.Image != null)
                .Must(image => image.Length <= 5 * 1024 * 1024).WithMessage("Image size must not exceed 5 MB.")
                .When(x => x.Image != null)
                .Must(image => new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(image.FileName).ToLower()))
                .WithMessage("Image must be in JPG or PNG format.")
                .When(x => x.Image != null);
        }
    }
}