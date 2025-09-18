using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecipeService.Models;
using RecipeService.Models.Rating.DTOs;
using RecipeService.Models.Recipes.DTOs;
using RecipeService.Repository;
using RecipeService.Services;
using System.Net;
using System.Security.Claims;

namespace RecipeService.Controllers
{
    [Route("api/rating")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IRecipeRatingRepository _ratingRepository;
        private readonly IValidator<RecipeRatingDTO> _ratingValidator;
        private readonly IMapper _mapper;
        private readonly ILogger<RatingController> _logger;

        public RatingController(
            IRecipeRatingRepository ratingRepository,
            IValidator<RecipeRatingDTO> ratingValidator,
            IMapper mapper,
            ILogger<RatingController> logger)
        {
            _ratingRepository = ratingRepository;
            _ratingValidator = ratingValidator;
            _mapper = mapper;
            _logger = logger;
        }

        #region Helpers
        private bool TryGetUserId(out Guid userId)
        {
            var userIdClaim = User.FindFirst("ident")?.Value;
            return Guid.TryParse(userIdClaim, out userId);
        }
        #endregion

        /// <summary>
        /// Submit or update a rating for a recipe.
        /// </summary>
        [Authorize]
        [HttpPost("rate")]
        public async Task<ActionResult<APIResponse<object>>> RateRecipeAsync(
            [FromBody] RecipeRatingDTO ratingDto,
            CancellationToken ct)
        {
            var validationResult = await _ratingValidator.ValidateAsync(ratingDto, ct);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for recipe rating.");
                return APIResponse<object>.Fail(
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                    HttpStatusCode.BadRequest);
            }

            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Unauthorized attempt to rate recipe.");
                return APIResponse<object>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            _logger.LogInformation("User {UserId} rates recipe {RecipeId} with {Rating}.", userId, ratingDto.RecipeId, ratingDto.Rating);

            var success = await _ratingRepository.RateRecipeAsync(userId, ratingDto.RecipeId, ratingDto.Rating);
            if (!success)
            {
                _logger.LogWarning("Failed to submit rating for recipe {RecipeId} by user {UserId}.", ratingDto.RecipeId, userId);
                return APIResponse<object>.Fail("Failed to submit rating.", HttpStatusCode.BadRequest);
            }

            return APIResponse<object>.Success(new { message = "Rating submitted successfully" }, HttpStatusCode.OK);
        }

        /// <summary>
        /// Get current user's rating for a specific recipe.
        /// </summary>
        [Authorize]
        [HttpGet("get-rating/{recipeId:guid}")]
        public async Task<ActionResult<APIResponse<RecipeRatingDTO>>> GetRatingAsync(Guid recipeId, CancellationToken ct)
        {
            if (recipeId == Guid.Empty)
            {
                return APIResponse<RecipeRatingDTO>.Fail("Invalid recipe ID", HttpStatusCode.BadRequest);
            }

            if (!TryGetUserId(out var userId))
            {
                return APIResponse<RecipeRatingDTO>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            _logger.LogInformation("Fetching rating for recipe {RecipeId} by user {UserId}.", recipeId, userId);

            var rating = await _ratingRepository.GetRecipeRatingAsync(recipeId, userId, ct);
            if (rating == null)
            {
                return APIResponse<RecipeRatingDTO>.Fail("No rating found", HttpStatusCode.NotFound);
            }

            return APIResponse<RecipeRatingDTO>.Success(rating, HttpStatusCode.OK);
        }

        /// <summary>
        /// Get all ratings of the current user.
        /// </summary>
        [Authorize]
        [HttpGet("get-user-ratings")]
        public async Task<ActionResult<APIResponse<List<RecipeRatingDTO>>>> GetUserRatingsAsync(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
            {
                return APIResponse<List<RecipeRatingDTO>>.Fail("Unauthorized", HttpStatusCode.Unauthorized);
            }

            _logger.LogInformation("Fetching all ratings for user {UserId}.", userId);

            var ratings = await _ratingRepository.GetUserRatingsAsync(userId, ct);
            var dtos = _mapper.Map<List<RecipeRatingDTO>>(ratings);
            return APIResponse<List<RecipeRatingDTO>>.Success(dtos, HttpStatusCode.OK);
        }
    }
}