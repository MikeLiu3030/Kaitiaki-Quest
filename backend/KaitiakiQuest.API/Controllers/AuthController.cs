using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static KaitiakiQuest.API.DTOs.AuthDtos;

namespace KaitiakiQuest.API.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
    [ApiController]
    public class AuthController: ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtService _jwtService;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }

        ///<summary>
        /// User Register
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<OAuthTokenResponse>>> Register([FromBody] RegisterDto dto) 
        {
            // 1. Check whether the user already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<AuthResponseDto>.Fail("Email already registered"));
            }

            // 2. Create user
            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                TotalXP = 0,
                Level = 1,
                CurrentStreak = 0
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) 
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(ApiResponse<AuthResponseDto>.Fail("Registration failed", errors));
            }

            // 3. Assign the default user
            await _userManager.AddToRoleAsync(user, "User");

            // 4. Generate Token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            var response = new AuthResponseDto
            {
                Token = token,
                Email = user.Email ?? string.Empty,
                UserName = dto.Username ?? string.Empty,
                TotalXP = user.TotalXP,
                Level = user.Level,
                Roles = roles.ToList(),
            };

            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Registration successful!"));
               
        }

        ///<summary>
        /// user login
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
        {
            // 1. Find user
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) 
            {
                return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password"));
            }

            // 2. Check password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
            {
                return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Invalid email or password"));
            }

            // 3. Generate Token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            var response = new AuthResponseDto
            {
                Token = token,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                TotalXP = user.TotalXP,
                Level = user.Level,
                Roles = roles.ToList()
            };

            return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful!"));
        }

        ///<summary>
        /// Get the current user information (Required authentication)
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<object>.Fail("User not authenticated"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found"));

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id,
                user.Email,
                user.UserName,
                user.TotalXP,
                user.Level,
                user.CurrentStreak,
                Roles = roles
            }));
        }
    }

}
