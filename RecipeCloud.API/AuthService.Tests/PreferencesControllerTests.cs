using AuthService.Controllers;
using AuthService.Models;
using AuthService.Models.DTOs;
using AuthService.Repository;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests
{


    public class PreferencesControllerTests
    {
        private readonly Mock<IUserPreferencesRepository> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly PreferencesController _controller;

        public PreferencesControllerTests()
        {
            _repoMock = new Mock<IUserPreferencesRepository>();
            _mapperMock = new Mock<IMapper>();
            var userManagerMock = new Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(
                Mock.Of<Microsoft.AspNetCore.Identity.IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            _controller = new PreferencesController(_repoMock.Object, userManagerMock.Object, _mapperMock.Object);
        }

        [Fact]
        public async Task Get_ReturnsOk_WithExistingPreferences()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var preferences = new UserPreferences
            {
                UserId = userId,
                DietaryPreferences = new List<string> { "Vegetarian" },
                Allergens = new List<string> { "Peanuts" },
                FavoriteCuisines = new List<string> { "Italian" }
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(preferences);

            // Act
            var result = await _controller.Get(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPrefs = Assert.IsType<UserPreferences>(okResult.Value);
            Assert.Equal(userId, returnedPrefs.UserId);
            Assert.Contains("Vegetarian", returnedPrefs.DietaryPreferences);
            Assert.Contains("Peanuts", returnedPrefs.Allergens);
            Assert.Contains("Italian", returnedPrefs.FavoriteCuisines);
        }

        [Fact]
        public async Task Get_ReturnsDefaultPreferences_IfNoneExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _repoMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserPreferences)null);

            // Act
            var result = await _controller.Get(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedPrefs = Assert.IsType<UserPreferences>(okResult.Value);
            Assert.Equal(userId, returnedPrefs.UserId);
            Assert.Empty(returnedPrefs.DietaryPreferences);
            Assert.Empty(returnedPrefs.Allergens);
            Assert.Empty(returnedPrefs.FavoriteCuisines);
        }

        [Fact]
        public async Task Get_ReturnsUnauthorized_WhenUserIdIsEmpty()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            var result = await _controller.Get(emptyId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Save_ReturnsBadRequest_WhenDtoIsNull()
        {
            // Act
            var result = await _controller.Save(null);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Save_ReturnsOk_WhenDtoIsValid()
        {
            // Arrange
            var dto = new UserPreferencesDTO
            {
                UserId = Guid.NewGuid(),
                DietaryPreferences = new List<string> { "Vegan" },
                Allergens = new List<string> { "Gluten" },
                FavoriteCuisines = new List<string> { "Mexican" }
            };

            var preferences = new UserPreferences
            {
                UserId = dto.UserId,
                DietaryPreferences = dto.DietaryPreferences,
                Allergens = dto.Allergens,
                FavoriteCuisines = dto.FavoriteCuisines
            };

            _mapperMock.Setup(m => m.Map<UserPreferences>(dto)).Returns(preferences);

            // Act
            var result = await _controller.Save(dto);

            // Assert
            _repoMock.Verify(r => r.SaveAsync(preferences), Times.Once);
            Assert.IsType<OkResult>(result);
        }
    }

}
