using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using TipBuddyApi.Controllers;
using TipBuddyApi.Contracts;
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
        private readonly Mock<IDemoDataSeeder> _demoDataSeederMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
            _configMock = new Mock<IConfiguration>();
            _mapperMock = new Mock<IMapper>();
            _demoDataSeederMock = new Mock<IDemoDataSeeder>();

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.EnvironmentName).Returns("Development");

            // Default JWT config
            _configMock.Setup(c => c["Jwt:Key"]).Returns("supersecretkey12345678901234567890");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("audience");

            _controller = new AuthController(_userManagerMock.Object, _configMock.Object, _mapperMock.Object, _envMock.Object, _demoDataSeederMock.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private static string CreateJwt(string userId, DateTime utcNow, TimeSpan lifetime, string issuer, string audience, string key)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId)
            };
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: utcNow,
                expires: utcNow.Add(lifetime),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetUserPrincipal(params Claim[] claims)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = user
            };
        }

        private void SetRequestCookie(string name, string value)
        {
            var ctx = _controller.ControllerContext.HttpContext;
            ctx.Request.Headers["Cookie"] = $"{name}={value}";
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
        public async Task Login_ReturnsOkWithMessage_IfSuccess()
        {
            var dto = new LoginDto { UserName = "user", Password = "pass" };
            var user = new User { Id = "id", Email = "e@e.com", UserName = "user" };
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);

            var result = await _controller.Login(dto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            Assert.Contains("Login successful", ok.Value.ToString());
        }

        [Fact]
        public async Task DemoLogin_ReturnsOkWithMessage_IfDemoUserExists()
        {
            var demoUser = new User { Id = "demo-id", Email = "demo@example.com", UserName = "demouser" };
            _userManagerMock.Setup(m => m.FindByNameAsync("demouser")).ReturnsAsync(demoUser);

            var result = await _controller.DemoLogin();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            Assert.Contains("Demo login successful", ok.Value.ToString());
        }

        [Fact]
        public async Task DemoLogin_SeedsAndReturnsOkWithMessage_IfDemoUserNotFound()
        {
            var demoUser = new User { Id = "demo-id", Email = "demo@example.com", UserName = "demouser" };

            // First call returns null, second call (after seeding) returns the user
            _userManagerMock.SetupSequence(m => m.FindByNameAsync("demouser"))
                .ReturnsAsync((User)null)
                .ReturnsAsync(demoUser);

            _demoDataSeederMock.Setup(s => s.SeedDemoDataAsync()).Returns(Task.CompletedTask);

            var result = await _controller.DemoLogin();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            Assert.Contains("Demo login successful", ok.Value.ToString());

            // Verify that seeding was called
            _demoDataSeederMock.Verify(s => s.SeedDemoDataAsync(), Times.Once);
        }

        [Fact]
        public async Task DemoLogin_ReturnsBadRequest_IfSeedingFails()
        {
            // Both calls return null (seeding failed to create user)
            _userManagerMock.Setup(m => m.FindByNameAsync("demouser")).ReturnsAsync((User)null);
            _demoDataSeederMock.Setup(s => s.SeedDemoDataAsync()).Returns(Task.CompletedTask);

            var result = await _controller.DemoLogin();
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Failed to create demo user", badRequest.Value.ToString());

            // Verify that seeding was called
            _demoDataSeederMock.Verify(s => s.SeedDemoDataAsync(), Times.Once);
        }

        // New tests for Me endpoint

        [Fact]
        public async Task Me_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            SetUserPrincipal(); // No claims
            var result = await _controller.Me();
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Me_ReturnsUnauthorized_WhenUserNotFound()
        {
            SetUserPrincipal(new Claim(ClaimTypes.NameIdentifier, "missing"));
            _userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User)null);

            var result = await _controller.Me();
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Me_ReturnsUserInfo_WithNameIdentifierClaim()
        {
            var user = new User { Id = "u1", UserName = "alice", Email = "a@a.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            // Set principal claim and cookie token
            SetUserPrincipal(new Claim(ClaimTypes.NameIdentifier, "u1"));
            var now = DateTime.UtcNow;
            var token = CreateJwt("u1", now, TimeSpan.FromMinutes(10), _configMock.Object["Jwt:Issuer"], _configMock.Object["Jwt:Audience"], _configMock.Object["Jwt:Key"]);
            SetRequestCookie("access_token", token);

            var result = await _controller.Me();
            var ok = Assert.IsType<OkObjectResult>(result);

            // Serialize anonymous object to JSON to assert fields
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("u1", root.GetProperty("id").GetString());
            Assert.Equal("alice", root.GetProperty("userName").GetString());
            Assert.Equal("a@a.com", root.GetProperty("email").GetString());
            Assert.True(root.TryGetProperty("issuedAt", out _));
            Assert.True(root.TryGetProperty("expiresAt", out _));
            Assert.False(root.GetProperty("isDemo").GetBoolean());
        }

        [Fact]
        public async Task Me_ReturnsUserInfo_WhenOnlySubClaimPresent()
        {
            var user = new User { Id = "u2", UserName = "bob", Email = "b@b.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u2")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            // Only sub claim
            SetUserPrincipal(new Claim(JwtRegisteredClaimNames.Sub, "u2"));
            var token = CreateJwt("u2", DateTime.UtcNow, TimeSpan.FromMinutes(10), _configMock.Object["Jwt:Issuer"], _configMock.Object["Jwt:Audience"], _configMock.Object["Jwt:Key"]);
            SetRequestCookie("access_token", token);

            var result = await _controller.Me();
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("u2", root.GetProperty("id").GetString());
        }

        [Fact]
        public async Task Me_SlidingRefresh_ExtendsExpiry_WhenLessThanFiveMinutesRemaining()
        {
            var user = new User { Id = "u3", UserName = "charlie", Email = "c@c.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("u3")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            SetUserPrincipal(new Claim(ClaimTypes.NameIdentifier, "u3"));
            var now = DateTime.UtcNow;
            var token = CreateJwt("u3", now, TimeSpan.FromMinutes(1), _configMock.Object["Jwt:Issuer"], _configMock.Object["Jwt:Audience"], _configMock.Object["Jwt:Key"]);
            SetRequestCookie("access_token", token);

            var result = await _controller.Me();
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var expiresAt = root.GetProperty("expiresAt").GetDateTime();
            Assert.True(expiresAt.ToUniversalTime() >= now.AddMinutes(10));
        }

        [Fact]
        public async Task Me_SetsIsDemoTrue_ForDemoUser()
        {
            var user = new User { Id = "demo-id", UserName = "demouser", Email = "demo@example.com" };
            _userManagerMock.Setup(m => m.FindByIdAsync("demo-id")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            SetUserPrincipal(new Claim(ClaimTypes.NameIdentifier, "demo-id"));
            var token = CreateJwt("demo-id", DateTime.UtcNow, TimeSpan.FromMinutes(10), _configMock.Object["Jwt:Issuer"], _configMock.Object["Jwt:Audience"], _configMock.Object["Jwt:Key"]);
            SetRequestCookie("access_token", token);

            var result = await _controller.Me();
            var ok = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(ok.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.True(root.GetProperty("isDemo").GetBoolean());
        }

        // New tests for Logout endpoint
        [Fact]
        public void Logout_SetsExpiredCookie_AndReturnsOk()
        {
            // Arrange
            SetUserPrincipal(new Claim(ClaimTypes.NameIdentifier, "u1"));

            // Act
            var result = _controller.Logout();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var headers = _controller.ControllerContext.HttpContext.Response.Headers;
            Assert.True(headers.TryGetValue("Set-Cookie", out var values));
            var cookieHeader = values.FirstOrDefault(v => v.Contains("access_token="));
            Assert.False(string.IsNullOrEmpty(cookieHeader));
            Assert.Contains("expires=", cookieHeader!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Thu, 01 Jan 1970", cookieHeader!); // Unix epoch
            Assert.Contains("path=/", cookieHeader!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", cookieHeader!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cookieHeader!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", cookieHeader!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("domain=", cookieHeader!, StringComparison.OrdinalIgnoreCase); // not set in Development
        }

        [Fact]
        public void Logout_SetsDomain_InProduction()
        {
            // Arrange: new controller with Production env and CookieDomain
            var envProd = new Mock<IWebHostEnvironment>();
            envProd.Setup(e => e.EnvironmentName).Returns("Production");

            var config = new Mock<IConfiguration>();
            config.Setup(c => c["CookieDomain"]).Returns("example.com");
            // JWT keys not required for logout

            var controller = new AuthController(_userManagerMock.Object, config.Object, _mapperMock.Object, envProd.Object, _demoDataSeederMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "u1") }, "TestAuth"));

            // Act
            var result = controller.Logout();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var headers = controller.ControllerContext.HttpContext.Response.Headers;
            Assert.True(headers.TryGetValue("Set-Cookie", out var values));
            var cookieHeader = values.FirstOrDefault(v => v.Contains("access_token="));
            Assert.False(string.IsNullOrEmpty(cookieHeader));
            Assert.Contains("domain=example.com", cookieHeader!, StringComparison.OrdinalIgnoreCase);
        }
    }
}
