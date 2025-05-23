using Xunit;
using Moq;
using FinTechAPI.Controllers;
using FinTechAPI.Models;
using FinTechAPI.DTOs;
using FinTechAPI.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using FinTechAPI.Data; // Для FinTechDbContext
using Microsoft.EntityFrameworkCore;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult; // Для DbContextOptionsBuilder

namespace FinTechAPI.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<IOptions<AuthSettings>> _mockAuthSettings;
        private readonly Mock<FinTechDbContext> _mockDbContext; // AuthController использует DbContext напрямую для создания счета
        private readonly AuthController _controller;
        private readonly AuthSettings _authSettings;

        public AuthControllerTests()
        {
            // Мокирование UserManager
            var store = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Мокирование SignInManager
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _mockSignInManager = new Mock<SignInManager<User>>(_mockUserManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

            // Мокирование AuthSettings
            _authSettings = new AuthSettings { SecretKey = "TestSecretKeySuperLongAndSecureEnough", Issuer = "TestIssuer", Audience = "TestAudience", ExpirationInMinutes = 60 };
            _mockAuthSettings = new Mock<IOptions<AuthSettings>>();
            _mockAuthSettings.Setup(o => o.Value).Returns(_authSettings);

            // Мокирование FinTechDbContext
             var options = new DbContextOptionsBuilder<FinTechDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Уникальное имя для каждой инстанции
                .Options;
            _mockDbContext = new Mock<FinTechDbContext>(options); // Передаем options
            // Настроим DbSet<Account>, чтобы Add и SaveChangesAsync работали ожидаемо для теста регистрации
            var mockAccountsDbSet = new Mock<DbSet<Account>>();
            _mockDbContext.Setup(db => db.Accounts).Returns(mockAccountsDbSet.Object);
            _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);


            _controller = new AuthController(_mockUserManager.Object, _mockSignInManager.Object, _mockAuthSettings.Object, _mockDbContext.Object);
            _controller.ControllerContext = new ControllerContext
            {
                // HttpContext нужен для Cookies
                HttpContext = ControllerTestHelpers.CreateHttpContext("any-user", "any@mail.com") 
            };
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenRegistrationSucceeds()
        {
            // Arrange
            var registerDto = new RegisterUserDto { Email = "newuser@example.com", Password = "Password123!", FirstName = "New", LastName = "User" };
            var user = new User { Id = "new-id", UserName = registerDto.Email, Email = registerDto.Email };

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), "User"))
                .ReturnsAsync(IdentityResult.Success);
            
            // Для создания счета внутри Register
            _mockDbContext.Setup(db => db.Accounts.Add(It.IsAny<Account>())); // Настроить мок для Add
            _mockDbContext.Setup(db => db.SaveChangesAsync(default)).ReturnsAsync(1); // Настроить мок для SaveChangesAsync

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(registerDto.Email, returnValue.Email);
            _mockDbContext.Verify(db => db.Accounts.Add(It.IsAny<Account>()), Times.Once); // Проверка, что счет добавляется
            _mockUserManager.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), "User"), Times.Once);
        }

        [Fact]
        public async Task Register_ShouldReturnBadRequest_WhenRegistrationFails()
        {
            // Arrange
            var registerDto = new RegisterUserDto { Email = "fail@example.com", Password = "Weak" };
            var errors = new List<IdentityError> { new IdentityError { Code = "TestError", Description = "Test error description" } };
            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsAssignableFrom<IEnumerable<IdentityError>>(badRequestResult.Value);
        }

        [Fact]
        public async Task Login_ShouldReturnOkWithToken_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123!" };
            var user = new User { Id = "test-id", UserName = loginDto.Email, Email = loginDto.Email, FirstName = "Test", LastName = "User" };

            _mockUserManager.Setup(um => um.FindByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockSignInManager.Setup(sm => sm.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(SignInResult.Success);
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" }); // Для генерации токена с ролями

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var authResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
            Assert.NotNull(authResponse.Token);
            Assert.Equal(user.Email, authResponse.User.Email);
            // Проверка установки Cookie (сложнее для юнит-теста, но можно проверить вызов Append)
            // _controller.Response.Cookies.Append был бы вызван, это можно проверить через мок HttpContext, если он настроен глубже.
            // В ControllerTestHelpers мы мокнули ResponseCookies, можно было бы верифицировать вызов Append на нем.
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "wrong@example.com", Password = "wrong" };
            _mockUserManager.Setup(um => um.FindByEmailAsync(loginDto.Email)).ReturnsAsync((User)null!); // Пользователь не найден

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorizedResult.Value);
        }
        
        // TODO: Добавить тесты для случаев, когда пользователь найден, но пароль неверный.
        // TODO: Если в GenerateJwtToken есть сложная логика, ее можно вынести в отдельный сервис и протестировать изолированно.
    }
}