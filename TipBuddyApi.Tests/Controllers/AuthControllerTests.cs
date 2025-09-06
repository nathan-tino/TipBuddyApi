using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using TipBuddyApi.Controllers;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Auth;

namespace TipBuddyApi.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _configMock = new Mock<IConfiguration>();
            _mapperMock = new Mock<IMapper>();

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.EnvironmentName).Returns("Development");

            _controller = new AuthController(_userManagerMock.Object, _configMock.Object, _mapperMock.Object, _envMock.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenSucceeded()
        {
            var dto = new RegisterDto { FirstName = "A", LastName = "B", UserName = "user", Email = "e@e.com", Password = "pass" };
            var user = new User();
            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _userManagerMock.Setup(m => m.CreateAsync(user, dto.Password)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Register(dto);
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenFailed()
        {
            var dto = new RegisterDto { FirstName = "A", LastName = "B", UserName = "user", Email = "e@e.com", Password = "pass" };
            var user = new User();
            var errors = new List<IdentityError> { new IdentityError { Description = "fail" } };
            var failResult = IdentityResult.Failed(errors.ToArray());
            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _userManagerMock.Setup(m => m.CreateAsync(user, dto.Password)).ReturnsAsync(failResult);

            var result = await _controller.Register(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errors, badRequest.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_IfUserNotFound()
        {
            var dto = new LoginDto { UserName = "user", Password = "pass" };
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((User)null);

            var result = await _controller.Login(dto);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_IfPasswordIncorrect()
        {
            var dto = new LoginDto { UserName = "user", Password = "pass" };
            var user = new User();
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(false);

            var result = await _controller.Login(dto);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsOkWithToken_IfSuccess()
        {
            var dto = new LoginDto { UserName = "user", Password = "pass" };
            var user = new User { Id = "id", Email = "e@e.com", UserName = "user" };
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _configMock.Setup(c => c["Jwt:Key"]).Returns("supersecretkey12345678901234567890");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("audience");

            var result = await _controller.Login(dto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            Assert.Contains("message", ok.Value.ToString());
        }
    }
}
