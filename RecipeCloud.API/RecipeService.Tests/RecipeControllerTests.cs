using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using RecipeService.Controllers;
using RecipeService.Models;
using RecipeService.Models.Filter;
using RecipeService.Models.Pagination;
using RecipeService.Models.Recipes;
using RecipeService.Models.Recipes.DTOs;
using RecipeService.Repository;
using RecipeService.Services;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace RecipeService.Tests
{
    public class RecipeControllerTests
    {
        private readonly Mock<IRecipeRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMinIOService> _mockMinIO;
        private readonly Mock<IValidator<(RecipeCreateDTO, IFormFile)>> _mockCreateValidator;
        private readonly Mock<IValidator<(RecipeUpdateDTO, IFormFile)>> _mockUpdateValidator;
        private readonly RecipeController _controller;

        public RecipeControllerTests()
        {
            _mockRepo = new Mock<IRecipeRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockMinIO = new Mock<IMinIOService>();
            _mockCreateValidator = new Mock<IValidator<(RecipeCreateDTO, IFormFile)>>();
            _mockUpdateValidator = new Mock<IValidator<(RecipeUpdateDTO, IFormFile)>>();

            _controller = new RecipeController(
                _mockRepo.Object,
                _mockMapper.Object,
                _mockMinIO.Object,
                _mockCreateValidator.Object,
                _mockUpdateValidator.Object
            );
        }

        [Fact]
        public async Task GetRecipesCountAsync_ReturnsOkWithCount()
        {
            // Arrange
            _mockRepo.Setup(r => r.CountAsync(null)).ReturnsAsync(5);

            // Act
            var result = await _controller.GetRecipesCountAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(5, okResult.Value);
        }

        [Fact]
        public async Task GetRecipesAsync_ReturnsPagedResponse()
        {
            // Arrange
            var pagination = new PaginationParams();
            var recipeList = new List<Recipe> { new Recipe { Id = Guid.NewGuid() } };
            var recipeDTOs = new List<RecipeDTO> { new RecipeDTO { Id = recipeList[0].Id } };

            _mockRepo.Setup(r => r.GetAllAsync(null, pagination)).ReturnsAsync(recipeList);
            _mockRepo.Setup(r => r.CountAsync(null)).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<List<RecipeDTO>>(recipeList)).Returns(recipeDTOs);

            // Act
            var result = await _controller.GetRecipesAsync(pagination);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<APIResponse>(okResult.Value);
            Assert.True(response.IsSuccess);
        }

        //[Fact]
        //public async Task GetAsync_WithValidId_ReturnsRecipe()
        //{
        //    // Arrange
        //    var id = Guid.NewGuid();
        //    var recipe = new Recipe { Id = id };
        //    var recipeDTO = new RecipeDTO { Id = id };

        //    _mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Recipe, bool>>>(), false))
        //        .ReturnsAsync(recipe);

        //    _mockMapper.Setup(m => m.Map<RecipeDTO>(recipe)).Returns(recipeDTO);

        //    // Act
        //    var result = await _controller.GetRecipeByIdAsync(id);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result.Result);
        //    var response = Assert.IsType<APIResponse>(okResult.Value);
        //    Assert.True(response.IsSuccess);
        //    Assert.Equal(recipeDTO.Id, ((RecipeDTO)response.Result).Id);
        //}

        [Fact]
        public async Task GetAsync_WithInvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetRecipeByIdAsync(Guid.Empty);
            
            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<APIResponse>(badResult.Value);
            Assert.False(response.IsSuccess);
        }

        [Fact]
        public async Task CreateRecipeAsync_ReturnsCreatedAtRoute()
        {
            // Arrange
            var createDto = new RecipeCreateDTO();
            var recipe = new Recipe { Id = Guid.NewGuid() };
            var recipeDto = new RecipeDTO { Id = recipe.Id };

            _mockMapper.Setup(m => m.Map<Recipe>(createDto)).Returns(recipe);
            _mockRepo.Setup(r => r.CreateAsync(recipe)).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<RecipeDTO>(recipe)).Returns(recipeDto);

            // Act
            var result = await _controller.CreateRecipeAsync(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
            var response = Assert.IsType<APIResponse>(createdResult.Value);
            Assert.True(response.IsSuccess);
            Assert.Equal(recipeDto.Id, ((RecipeDTO)response.Result).Id);
        }

        [Fact]
        public async Task IncrementViewCount_ValidId_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.IncrementViewCountAsync(id)).ReturnsAsync(true);

            var result = await _controller.IncrementViewCount(id);

            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal("View count incremented successfully", okResult.Value);
        }

        [Fact]
        public async Task IncrementViewCount_InvalidId_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.IncrementViewCountAsync(id)).ReturnsAsync(false);

            var result = await _controller.IncrementViewCount(id);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(id.ToString(), notFound.Value.ToString());
        }

        [Fact]
        public async Task FilterRecipesAsync_ValidFilter_ReturnsPagedResponse()
        {
            var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };
            var filterDto = new RecipeFilterDTO();
            var sortOrder = "asc";
            var filters = "key=value";

            var recipes = new List<Recipe> { new Recipe() };
            var recipesDto = new List<RecipeDTO> { new RecipeDTO() };

            _mockRepo.Setup(r => r.FilterRecipesAsync(It.IsAny<RecipeFilterDTO>(), paginationParams, sortOrder))
                .ReturnsAsync(recipes);
            _mockRepo.Setup(r => r.GetFilterExpression(It.IsAny<RecipeFilterDTO>()))
                .Returns(r => true);
            _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Recipe, bool>>>()))
                .ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<List<RecipeDTO>>(recipes)).Returns(recipesDto);

            var result = await _controller.FilterRecipesAsync(Uri.EscapeDataString(filters), paginationParams, sortOrder);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PagedResponse<RecipeDTO>>(okResult.Value);
            Assert.Single(response.Data);
        }

        [Fact]
        public async Task CheckboxFilterAsync_ValidCategoryId_ReturnsFilter()
        {
            var categoryId = Guid.NewGuid();
            var recipe = new Recipe
            {
                Diets = new List<string> { "Vegan" },
                Allergens = new List<string> { "Peanuts" },
                Cuisine = "Italian",
                Tags = new List<string> { "Quick" }
            };
            var dto = new RecipeDTO
            {
                Diets = recipe.Diets,
                Allergens = recipe.Allergens,
                Cuisine = recipe.Cuisine,
                Tags = recipe.Tags
            };

            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Recipe, bool>>>(), null))
                .ReturnsAsync(new List<Recipe> { recipe });
            _mockMapper.Setup(m => m.Map<List<RecipeDTO>>(It.IsAny<List<Recipe>>()))
                .Returns(new List<RecipeDTO> { dto });

            var result = await _controller.CheckboxFilterAsync(categoryId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var filter = Assert.IsType<CheckboxFilter>(okResult.Value);
            Assert.Contains("Vegan", filter.Diets);
            Assert.Contains("Peanuts", filter.Allergens);
            Assert.Contains("Italian", filter.Cuisines);
            Assert.Contains("Quick", filter.Tags);
        }

        [Fact]
        public async Task GetRecipesWithSimilarCategoryAsync_ValidCategoryId_ReturnsRecipes()
        {
            var categoryId = Guid.NewGuid();
            var recipes = new List<Recipe> { new Recipe { Id = Guid.NewGuid() } };
            var dtos = new List<RecipeDTO> { new RecipeDTO { Id = recipes[0].Id } };

            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Recipe, bool>>>(), null))
                .ReturnsAsync(recipes);
            _mockMapper.Setup(m => m.Map<List<RecipeDTO>>(recipes)).Returns(dtos);

            var result = await _controller.GetRecipesWithSimilarCategoryAsync(categoryId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<List<RecipeDTO>>(okResult.Value);
            Assert.Single(response);
            Assert.Equal(dtos[0].Id, response[0].Id);
        }

    }
}