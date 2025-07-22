using AuthService.Controllers;
using AuthService.Models;
using AuthService.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests
{


    public class ViewHistoryControllerTests
    {
        private readonly Mock<IViewHistoryRepository> _repoMock;
        private readonly ViewHistoryController _controller;

        public ViewHistoryControllerTests()
        {
            _repoMock = new Mock<IViewHistoryRepository>();
            _controller = new ViewHistoryController(_repoMock.Object);

            // Мокаємо User з Claim "ident" для GetCurrentUserId()
            var userId = Guid.NewGuid().ToString();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim("ident", userId)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task Get_ReturnsOkWithViewHistory()
        {
            // Arrange
            var userId = Guid.Parse(_controller.User.FindFirst("ident")!.Value);
            var histories = new List<ViewHistory>
        {
            new ViewHistory { UserId = userId, RecipeId = Guid.NewGuid(), CollectionId = Guid.NewGuid() }
        };

            _repoMock.Setup(r => r.GetViewHistory(userId, 10))
                     .ReturnsAsync(histories);

            // Act
            var result = await _controller.Get(10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedHistories = Assert.IsAssignableFrom<IEnumerable<ViewHistory>>(okResult.Value);
            Assert.Single(returnedHistories);
        }

        [Fact]
        public async Task SendViewHistoryAsync_ReturnsOk_WhenRepositorySucceeds()
        {
            // Arrange
            var userId = Guid.Parse(_controller.User.FindFirst("ident")!.Value);
            var dto = new ViewHistoryDto { RecipeId = Guid.NewGuid() };

            _repoMock.Setup(r => r.SendViewHistoryAsync(dto.RecipeId, userId))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SendViewHistoryAsync(dto);

            // Assert
            Assert.IsType<OkResult>(result);
            _repoMock.Verify(r => r.SendViewHistoryAsync(dto.RecipeId, userId), Times.Once);
        }

        [Fact]
        public async Task SendViewHistoryAsync_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var userId = Guid.Parse(_controller.User.FindFirst("ident")!.Value);
            var dto = new ViewHistoryDto { RecipeId = Guid.NewGuid() };

            _repoMock.Setup(r => r.SendViewHistoryAsync(dto.RecipeId, userId))
                     .ThrowsAsync(new Exception("Some error"));

            // Act
            var result = await _controller.SendViewHistoryAsync(dto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("Internal server error", objectResult.Value);
        }
    }

}
