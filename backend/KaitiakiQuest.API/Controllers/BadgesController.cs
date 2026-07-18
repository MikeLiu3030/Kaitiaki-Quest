using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KaitiakiQuest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BadgesController : ControllerBase
    {
        private readonly IBadgeService _badgeService;
        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? throw new UnauthorizedAccessException("User not authenticated");
        }
        public BadgesController(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserBadgeResponseDto>>>> GetMyBadges()
        {
            var result = await _badgeService.GetUserBadgesAsync(GetUserId());
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<List<UserBadgeResponseDto>>.Fail(result.Message));
            }
            return Ok(ApiResponse<List<UserBadgeResponseDto>>.Ok(result.Data!, result.Message));
        }
    }
}
