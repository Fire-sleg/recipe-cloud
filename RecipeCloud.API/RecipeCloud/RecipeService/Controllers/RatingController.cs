using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecipeService.Models.Rating.DTOs;
using RecipeService.Models.Recipes.DTOs;
using RecipeService.Repository;
using RecipeService.Services;
using System.Security.Claims;

namespace RecipeService.Controllers
{
    [Route("api/rating")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IRecipeRatingRepository _dbRating;
        public RatingController(IRecipeRatingRepository dbRating)
        {
            _dbRating = dbRating;
        }

        [Authorize]
        [HttpPost("rate")]
        public async Task<IActionResult> RateRecipe([FromBody] RecipeRatingDTO ratingDto)
        {
            try
            {
                if (ratingDto.Rating < 1 || ratingDto.Rating > 5)
                {
                    return BadRequest("Rating must be between 1 and 5");
                }

                // Отримуємо UserId з токена
                var userId = GetCurrentUserId();

                var result = await _dbRating.RateRecipeAsync(userId, ratingDto.RecipeId, ratingDto.Rating);

                if (result)
                {
                    return Ok( new { message = "Rating submitted successfully" });
                }

                return BadRequest("Failed to submit rating");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }
        [Authorize]
        [HttpGet("get-rating/{recipeId:guid}")]
        public async Task<IActionResult> GetRating(Guid recipeId)
        {
            try
            {
                // Отримуємо UserId з токена
                var userId = GetCurrentUserId();

                var rating = await _dbRating.GetRecipeRating(recipeId, userId);
                if (rating != null)
                {
                    return Ok(rating);
                }

                return BadRequest("Failed to get rating");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [Authorize]
        [HttpGet("get-user-ratings")]
        public async Task<IActionResult> GetUserRatings()
        {
            try
            {
                // Отримуємо UserId з токена
                var userId = GetCurrentUserId();

                var ratings = await _dbRating.GetUserRatingAsync(userId);
                if (ratings != null)
                {
                    return Ok(ratings);
                }

                return BadRequest("Failed to get rating");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }


        private Guid GetCurrentUserId()
        {
            // Припускаю, що UserId зберігається в claims
            var userId = Guid.Parse(User.FindFirst("ident")?.Value);
            return userId;
        }
    }
}
