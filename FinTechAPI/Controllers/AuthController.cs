// AuthController.cs
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FinTechAPI.Configuration;
using FinTechAPI.Data;
using FinTechAPI.DTOs;
using FinTechAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinTechAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly AuthSettings _authSettings;
        private readonly FinTechDbContext _context;
        
        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<AuthSettings> authSettings,
            FinTechDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _authSettings = authSettings.Value;
            _context = context;
        }
        
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerDto)
        {
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };
            
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            
            // Add default role
            await _userManager.AddToRoleAsync(user, "User");

            var account = new Account
            {
                Name = "Main",
                AccountType = AccountType.Checking,
                Balance = 0,
                Currency = Currency.USD,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            
            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
            
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
            
            var token = await GenerateJwtToken(user);
            
            Response.Cookies.Append("Authorization", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,  
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.Now.AddMinutes(_authSettings.ExpirationInMinutes)
            });
            
            return Ok(new AuthResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(_authSettings.ExpirationInMinutes),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                }
            });
        }
        
        private async Task<string> GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.SecretKey);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!)
            };
            
            // Add roles to claims
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_authSettings.ExpirationInMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _authSettings.Issuer,
                Audience = _authSettings.Audience
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}