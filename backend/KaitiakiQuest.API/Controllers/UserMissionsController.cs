using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KaitiakiQuest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserMissionsController : ControllerBase
    {
        private readonly IUserMissionService _service;

        public UserMissionsController(IUserMissionService service)
        {
            _service = service;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        [HttpGet("my-missions")]
        public async Task<ActionResult<ApiResponse<List<UserMissionResponseDto>>>> GetMyMissions([FromQuery] string? status)
        {
            var result = await _service.GetMyMissionsAsync(GetUserId(), status);
            return Ok(ApiResponse<List<UserMissionResponseDto>>.Ok(result.Data!, result.Message));
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<object>>> GetMyStats()
        {
            var result = await _service.GetMyStatsAsync(GetUserId());
            return Ok(ApiResponse<object>.Ok(result.Data!, result.Message));
        }

        [HttpPost("accept")]
        public async Task<ActionResult<ApiResponse<UserMissionResponseDto>>> AcceptMission(AcceptMissionDto dto)
        {
            var result = await _service.AcceptMissionAsync(GetUserId(), dto);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<UserMissionResponseDto>.Fail(result.Message, result.Errors));
            return Ok(ApiResponse<UserMissionResponseDto>.Ok(result.Data!, result.Message));
        }

        [HttpPut("{id}/complete")]
        public async Task<ActionResult<ApiResponse<UserMissionResponseDto>>> CompleteMission(
            int id, [FromBody] CompleteMissionDto? dto)
        {
            var result = await _service.CompleteMissionAsync(GetUserId(), id, dto);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<UserMissionResponseDto>.Fail(result.Message, result.Errors));
            return Ok(ApiResponse<UserMissionResponseDto>.Ok(result.Data!, result.Message));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> AbandonMission(int id)
        {
            var result = await _service.AbandonMissionAsync(GetUserId(), id);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Errors));
            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }

        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetLeaderboard()
        {
            var result = await _service.GetLeaderboardAsync();
            return Ok(ApiResponse<List<object>>.Ok(result.Data!, result.Message));
        }
    }
}