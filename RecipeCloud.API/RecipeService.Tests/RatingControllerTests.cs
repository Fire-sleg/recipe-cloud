using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeService.Controllers;
using RecipeService.Models.Rating;
using RecipeService.Models.Rating.DTOs;
using RecipeService.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RecipeService.Tests
{
    public class RatingControllerTests
    {
        private readonly Mock<IRecipeRatingRepository> _mockRepo;
        private readonly RatingController _controller;
        private readonly Guid _userId;

        public RatingControllerTests()
        {
            _mockRepo = new Mock<IRecipeRatingRepository>();
            _controller = new RatingController(_mockRepo.Object);
            _userId = Guid.NewGuid();

            // Set fake user with claim "ident"
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim("ident", _userId.ToString())
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task RateRecipe_ValidRating_ReturnsOk()
        {
            // Arrange
            var dto = new RecipeRatingDTO { RecipeId = Guid.NewGuid(), Rating = 4 };
            _mockRepo.Setup(r => r.RateRecipeAsync(_userId, dto.RecipeId, dto.Rating)).ReturnsAsync(true);

            // Act
            var result = await _controller.RateRecipe(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<OkObjectResult>(okResult);
        }

        [Fact]
        public async Task RateRecipe_InvalidRating_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RecipeRatingDTO { RecipeId = Guid.NewGuid(), Rating = 6 }; // invalid

            // Act
            var result = await _controller.RateRecipe(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Rating must be between 1 and 5", badRequest.Value);
        }

        [Fact]
        public async Task RateRecipe_RepositoryFails_ReturnsBadRequest()
        {
            // Arrange
            var dto = new RecipeRatingDTO { RecipeId = Guid.NewGuid(), Rating = 3 };
            _mockRepo.Setup(r => r.RateRecipeAsync(_userId, dto.RecipeId, dto.Rating)).ReturnsAsync(false);

            // Act
            var result = await _controller.RateRecipe(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to submit rating", badRequest.Value);
        }

        [Fact]
        public async Task GetRating_ValidRating_ReturnsOk()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            var expectedRating = new RecipeRatingDTO { RecipeId = recipeId };
            _mockRepo.Setup(r => r.GetRecipeRating(recipeId, _userId)).ReturnsAsync(expectedRating);

            // Act
            var result = await _controller.GetRating(recipeId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedRating, okResult.Value);
        }

        [Fact]
        public async Task GetRating_NotFound_ReturnsBadRequest()
        {
            // Arrange
            var recipeId = Guid.NewGuid();
            //var expectedRating = new RecipeRatingDTO();
            _mockRepo.Setup(r => r.GetRecipeRating(recipeId, _userId)).ReturnsAsync((RecipeRatingDTO)null);

            // Act
            var result = await _controller.GetRating(recipeId);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to get rating", badRequest.Value);
        }

        [Fact]
        public async Task GetUserRatings_ReturnsListOfRatings()
        {
            // Arrange
            var ratings = new List<RecipeRating>
            {
                new RecipeRating { RecipeId = Guid.NewGuid(), Rating = 5 },
                new RecipeRating { RecipeId = Guid.NewGuid(), Rating = 4 }
            };
            _mockRepo.Setup(r => r.GetUserRatingAsync(_userId)).ReturnsAsync(ratings);

            // Act
            var result = await _controller.GetUserRatings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRatings = Assert.IsAssignableFrom<List<RecipeRating>>(okResult.Value);
            Assert.Equal(2, returnedRatings.Count);
        }

        [Fact]
        public async Task GetUserRatings_RepositoryReturnsNull_ReturnsBadRequest()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserRatingAsync(_userId)).ReturnsAsync((List<RecipeRating>)null);

            // Act
            var result = await _controller.GetUserRatings();

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to get rating", badRequest.Value);
        }
    }
}
