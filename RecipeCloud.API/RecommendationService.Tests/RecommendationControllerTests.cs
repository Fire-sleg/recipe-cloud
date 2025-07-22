using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecommendationService.Controllers;
using RecommendationService.Models;
using RecommendationService.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RecommendationService.Tests
{
    public class RecommendationControllerTests
    {
        private readonly Mock<IRecommendationService> _mockService;
        private readonly RecommendationController _controller;
        private readonly Guid _userId;

        public RecommendationControllerTests()
        {
            _userId = Guid.NewGuid();
            _mockService = new Mock<IRecommendationService>();

            _controller = new RecommendationController(_mockService.Object);

            // Set fake user with "ident" claim
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
        public async Task GetRecommendations_ReturnsOkWithRecommendations()
        {
            // Arrange
            var recommendations = new RecommendationResult
            {
                Recommendations = new List<RecipeDTO>
                {
                    new RecipeDTO { Id = Guid.NewGuid(), Title = "Recipe 1" },
                    new RecipeDTO { Id = Guid.NewGuid(), Title = "Recipe 2" }
                },
                Metrics = new Dictionary<string, double>
                {
                    { "score", 0.95 }
                }
            };

            _mockService.Setup(s => s.GetRecommendations(_userId, 6)).ReturnsAsync(recommendations);

            // Act
            var result = await _controller.GetRecommendations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedList = Assert.IsType<List<RecipeDTO>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);
        }

        //[Fact]
        //public async Task GetRecommendations_WithCustomLimit_ReturnsCorrectLimit()
        //{
        //    // Arrange
        //    int customLimit = 10;
        //    var recommendations = new 
        //    {
        //        Recommendations = new List<RecipeDTO>
        //        {
        //            new RecipeDTO { Id = Guid.NewGuid(), Title = "Recipe A" }
        //        },
        //        Metrics = null
        //        };

        //    _mockService.Setup(s => s.GetRecommendations(_userId, customLimit))
        //                .ReturnsAsync(recommendations);

        //    // Act
        //    var result = await _controller.GetRecommendations(customLimit);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result.Result);
        //    var returnedList = Assert.IsType<List<RecipeDTO>>(okResult.Value);
        //    Assert.Single(returnedList);
        //}

        //[Fact]
        //public async Task GetRecommendations_EmptyResult_ReturnsEmptyList()
        //{
        //    // Arrange
        //    var recommendations = new RecommendationResult
        //    {
        //        Recommendations = new List<RecipeDTO>(),
        //        Metrics = null
        //    };

        //    _mockService.Setup(s => s.GetRecommendations(_userId, 6))
        //                .ReturnsAsync(recommendations);

        //    // Act
        //    var result = await _controller.GetRecommendations();

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result.Result);
        //    var returnedList = Assert.IsType<List<RecipeDTO>>(okResult.Value);
        //    Assert.Empty(returnedList);
        //}
    }
}
