using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RecipeService.Controllers;
using RecipeService.Models.Categories;
using RecipeService.Models.Categories.DTOs;
using RecipeService.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RecipeService.Tests
{
    public class CategoryControllerTests
    {
        private readonly Mock<ICategoryRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly CategoryController _controller;

        public CategoryControllerTests()
        {
            _mockRepo = new Mock<ICategoryRepository>();
            _mockMapper = new Mock<IMapper>();
            _controller = new CategoryController(_mockRepo.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsOkResult_WithListOfCategories()
        {
            // Arrange
            var categoriesFromDb = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Cat1" },
                new Category { Id = Guid.NewGuid(), Name = "Cat2" }
            };
                var categoriesDto = new List<CategoryDTO>
            {
                new CategoryDTO { Id = categoriesFromDb[0].Id, Name = "Cat1" },
                new CategoryDTO { Id = categoriesFromDb[1].Id, Name = "Cat2" }
            };

            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(categoriesFromDb);
            _mockMapper.Setup(m => m.Map<List<CategoryDTO>>(categoriesFromDb)).Returns(categoriesDto);

            // Act
            var result = await _controller.GetCategoriesAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnCategories = Assert.IsType<List<CategoryDTO>>(okResult.Value);
            Assert.Equal(2, returnCategories.Count);
            Assert.Equal("Cat1", returnCategories[0].Name);
        }

        [Fact]
        public async Task GetAsync_WithValidId_ReturnsCategory()
        {
            // Arrange
            var id = Guid.NewGuid();
            var category = new Category { Id = id, Name = "Cat1" };
            var categoryDto = new CategoryDTO { Id = id, Name = "Cat1" };

            _mockRepo
                    .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Category, bool>>>(),
                                           It.IsAny<bool>(),
                                           It.IsAny<bool>(),
                                           It.IsAny<bool>()))
                    .ReturnsAsync(category);

            _mockMapper.Setup(m => m.Map<CategoryDTO>(category)).Returns(categoryDto);

            // Act
            var result = await _controller.GetAsync(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnCategory = Assert.IsType<CategoryDTO>(okResult.Value);
            Assert.Equal(id, returnCategory.Id);
        }

        [Fact]
        public async Task CreateAsync_WithNullDto_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CreateAsync(null);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task CreateAsync_WhenNameExists_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CategoryCreateDTO { Name = "Existing" };

            _mockRepo.Setup(r => r.GetAsync(u => u.Name.ToLower() == createDto.Name.ToLower(), false, false, false)).ReturnsAsync(new Category { Name = "Existing" });

            // Act
            var result = await _controller.CreateAsync(createDto);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsNoContent()
        {
            // Arrange
            var createDto = new CategoryCreateDTO { Name = "NewCategory" };
            _mockRepo.Setup(r => r.GetAsync(null, false, false, false))
                .ReturnsAsync((Category)null);

            _mockMapper.Setup(m => m.Map<Category>(createDto)).Returns(new Category { Name = "NewCategory" });
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateAsync(createDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockRepo.Verify(r => r.CreateAsync(It.IsAny<Category>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithEmptyGuid_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteAsync(Guid.Empty);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteAsync_CategoryNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.GetAsync(null, false, false, false)).ReturnsAsync((Category)null);

            // Act
            var result = await _controller.DeleteAsync(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        //[Fact]
        //public async Task DeleteAsync_CategoryExists_ReturnsNoContent()
        //{
        //    // Arrange
        //    var id = Guid.NewGuid();
        //    var category = new Category { Id = id };
        //    _mockRepo.Setup(r => r.GetAsync(u => u.Id == category.Id, false, false, false)).ReturnsAsync(category);
        //    _mockRepo.Setup(r => r.RemoveAsync(category)).Returns(Task.CompletedTask);

        //    // Act
        //    var result = await _controller.DeleteAsync(id);

        //    // Assert
        //    Assert.IsType<NoContentResult>(result);
        //    _mockRepo.Verify(r => r.RemoveAsync(category), Times.Once);
        //}

        [Fact]
        public async Task UpdateCategoryAsync_WithInvalidDto_ReturnsBadRequest()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new CategoryUpdateDTO { Id = Guid.NewGuid() }; // id != dto.Id

            // Act
            var result = await _controller.UpdateCategoryAsync(id, dto);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        //[Fact]
        //public async Task UpdateCategoryAsync_ValidDto_ReturnsNoContent()
        //{
        //    // Arrange
        //    var id = Guid.NewGuid();
        //    var dto = new CategoryUpdateDTO { Id = id };
        //    var category = new Category { Id = id };

        //    _mockMapper.Setup(m => m.Map<Category>(dto)).Returns(category);
        //    _mockRepo.Setup(r => r.UpdateAsync(category)).Returns(Task.CompletedTask);

        //    // Act
        //    var result = await _controller.UpdateCategoryAsync(id, dto);

        //    // Assert
        //    Assert.IsType<NoContentResult>(result);
        //}

        //// Аналогічно можна писати тести для Patch, GetBaseCategoriesAsync, GetSubCategoriesAsync і т.д.
    }

}
