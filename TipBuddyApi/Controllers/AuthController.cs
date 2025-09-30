using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TipBuddyApi.Contracts;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Auth;

namespace TipBuddyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<User> userManager, IConfiguration configuration, IMapper mapper, IWebHostEnvironment env, IDemoDataSeeder demoDataSeeder) : ControllerBase
    {
        private const string DemoUserName = "demouser";
        private const int AccessTokenMinutes = 15;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            var user = mapper.Map<User>(model);
            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
                return Ok();

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await userManager.FindByNameAsync(model.UserName);
            if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized();
            }

            var accessToken = GenerateJwtToken(user);

            var cookieOptions = GetAccessTokenCookieOptions(DateTimeOffset.UtcNow.AddMinutes(AccessTokenMinutes));
            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            // TODO: Implement refresh token support in the future for better session management

            return Ok(new { message = "Login successful" });
        }

        [HttpPost("demo")]
        public async Task<IActionResult> DemoLogin()
        {
            var demoUser = await userManager.FindByNameAsync(DemoUserName);
            if (demoUser == null)
            {
                // Seed demo data if demo user doesn't exist
                await demoDataSeeder.SeedDemoDataAsync();
                
                // Try to find the demo user again after seeding
                demoUser = await userManager.FindByNameAsync(DemoUserName);
                if (demoUser == null)
                {
                    return BadRequest(new { message = "Failed to create demo user. Please try again." });
                }
            }

            var accessToken = GenerateJwtToken(demoUser);

            var cookieOptions = GetAccessTokenCookieOptions(DateTimeOffset.UtcNow.AddMinutes(AccessTokenMinutes));
            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            return Ok(new { message = "Demo login successful" });
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Remove the JWT cookie by setting its expiration to the past
            var cookieOptions = GetAccessTokenCookieOptions(DateTimeOffset.UnixEpoch);
            Response.Cookies.Append("access_token", "", cookieOptions);

            return Ok(new { message = "Logout successful" });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            string? token = Request.Cookies["access_token"];
            DateTimeOffset? issuedAt = null;
            DateTimeOffset? expiresAt = null;

            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                issuedAt = jwt.ValidFrom;
                expiresAt = jwt.ValidTo;

                // Optional sliding refresh: extend token if <5 min left
                if (expiresAt.HasValue && expiresAt.Value.UtcDateTime - DateTime.UtcNow < TimeSpan.FromMinutes(5))
                {
                    var refreshed = GenerateJwtToken(user);
                    Response.Cookies.Append("access_token", refreshed, GetAccessTokenCookieOptions(DateTimeOffset.UtcNow.AddMinutes(AccessTokenMinutes)));

                    var newJwt = handler.ReadJwtToken(refreshed);
                    issuedAt = newJwt.ValidFrom;
                    expiresAt = newJwt.ValidTo;
                }
            }

            var roles = await userManager.GetRolesAsync(user);
            var isDemo = string.Equals(user.UserName, DemoUserName, StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email,
                roles,
                issuedAt,
                expiresAt,
                isDemo
            });
        }

        private CookieOptions GetAccessTokenCookieOptions(DateTimeOffset expires)
        {
            var options = new CookieOptions
            {
                HttpOnly = true, // Set access token as HttpOnly cookie for HTTPS frontend
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = expires
            };

            // Browsers reject Domain=localhost; only set in non-development
            if (!env.IsDevelopment())
            {
                options.Domain = configuration["CookieDomain"];
            }

            return options;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}