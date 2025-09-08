using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Auth;

namespace TipBuddyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(UserManager<User> userManager, IConfiguration configuration, IMapper mapper, IWebHostEnvironment env) : ControllerBase
    {
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

            var cookieOptions = GetAccessTokenCookieOptions(DateTimeOffset.Now.AddMinutes(15));
            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            // TODO: Implement refresh token support in the future for better session management

            return Ok(new { message = "Login successful" });
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

        private CookieOptions GetAccessTokenCookieOptions(DateTimeOffset expires)
        {
            return new CookieOptions
            {
                HttpOnly = true, // Set access token as HttpOnly cookie for HTTPS frontend
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Domain = env.IsDevelopment() ? "localhost" : configuration["CookieDomain"],
                Expires = expires
            };
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}