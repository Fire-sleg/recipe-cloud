using AuthService.Controllers;
using AuthService.Data.Models.RequestModels;
using AuthService.Entities;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AuthService.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserStore<ApplicationUser>> _userStoreMock;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockUserService = new Mock<IUserService>();


            _userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new UserManager<ApplicationUser>(
                _userStoreMock.Object,
                null, // IOptions<IdentityOptions>
                new PasswordHasher<ApplicationUser>(),
                new IUserValidator<ApplicationUser>[0],
                new IPasswordValidator<ApplicationUser>[0],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null, // IServiceProvider
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );

            // Create a mock of UserManager that overrides virtual methods
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                _userStoreMock.Object,
                null, null, null, null, null, null, null, null);

            _controller = new AuthController(_mockUserService.Object, userManagerMock.Object);

            // Replace userManager for tests that need FindByIdAsync
            userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((string id) =>
            {
                if (id == "user123")
                    return new ApplicationUser { Id = "user123", FirstName = "John", Email = "john@example.com" };
                if (id == "userNull")
                    return new ApplicationUser { Id = "userNull", FirstName = null, Email = "null@example.com" };
                return null!;
            });
        }

        [Fact]
        public async Task Register_Success_ReturnsOkResult()
        {
            var registerRequest = new UserRegisterRequest
            {
                UserName = "Test",
                Email = "test@example.com",
                Password = "TestPassword123!"
            };

            var expectedResponse = new UserRegisterResponse
            {
                UserName = "Test",
                Email = "test@example.com",
            };

            _mockUserService.Setup(s => s.RegisterUser(It.IsAny<UserRegisterRequest>()))
                            .ReturnsAsync(expectedResponse);

            var result = await _controller.Register(registerRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<UserRegisterResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Email, response.Email);
            Assert.Equal(expectedResponse.UserName, response.UserName);
        }

        [Fact]
        public async Task Register_Exception_ReturnsInternalServerError()
        {
            var registerRequest = new UserRegisterRequest
            {
                Email = "test@example.com",
                Password = "TestPassword123!"
            };

            _mockUserService.Setup(s => s.RegisterUser(It.IsAny<UserRegisterRequest>()))
                            .ThrowsAsync(new Exception("Registration failed"));

            var result = await _controller.Register(registerRequest);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("Registration failed", statusResult.Value);
        }

        [Fact]
        public async Task Login_Success_ReturnsOkResult()
        {
            var loginRequest = new UserLoginRequest
            {
                Email = "test@example.com",
                Password = "TestPassword123!"
            };

            var token = new Token
            {
                AccessToken = "fake-access-token",
                AccessTokenExpireDate = DateTime.UtcNow.AddHours(1)
            };

            var expectedResponse = new UserLoginResponse("123", "testuser", "Basic", token);

            _mockUserService.Setup(s => s.CreateAdmin()).Returns(Task.CompletedTask);
            _mockUserService.Setup(s => s.LoginUser(It.IsAny<UserLoginRequest>())).ReturnsAsync(expectedResponse);

            var result = await _controller.Login(loginRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<UserLoginResponse>(okResult.Value);
            Assert.Equal("123", returnedValue.Id);
        }

        [Fact]
        public async Task Login_Exception_ReturnsInternalServerError()
        {
            var loginRequest = new UserLoginRequest
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _mockUserService.Setup(s => s.CreateAdmin()).Returns(Task.CompletedTask);
            _mockUserService.Setup(s => s.LoginUser(It.IsAny<UserLoginRequest>()))
                            .ThrowsAsync(new Exception("Invalid credentials"));

            var result = await _controller.Login(loginRequest);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("Invalid credentials", statusResult.Value);
        }

        [Fact]
        public async Task GetName_UserExists_ReturnsFirstName()
        {
            SetupUserContext(Roles.Basic);

            var result = await _controller.GetName("user123");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("John", okResult.Value);
        }

        [Fact]
        public async Task GetName_UserNotFound_ReturnsBadRequest()
        {
            SetupUserContext(Roles.Basic);

            var result = await _controller.GetName("nonexistent");

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task GetName_UserFirstNameIsNull_ReturnsBadRequest()
        {
            SetupUserContext(Roles.Basic);

            var result = await _controller.GetName("userNull");

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task GetUser_UserExists_ReturnsUser()
        {
            SetupUserContext(Roles.Standart);

            var result = await _controller.GetUser("user123");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<ApplicationUser>(okResult.Value);
            Assert.Equal("user123", returnedUser.Id);
        }

        [Fact]
        public async Task GetUser_UserNotFound_ReturnsBadRequest()
        {
            SetupUserContext(Roles.Standart);

            var result = await _controller.GetUser("nonexistent");

            Assert.IsType<BadRequestResult>(result);
        }

        private void SetupUserContext(string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, "testuser")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }

    public static class Roles
    {
        public const string Basic = "Basic";
        public const string Admin = "Admin";
        public const string Standart = "Standart";
    }
}
