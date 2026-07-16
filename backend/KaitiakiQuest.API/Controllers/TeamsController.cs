using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KaitiakiQuest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }


        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        ///<summary>
        ///Retrieve the current user's team
        /// </summary>
        [HttpGet("my-team")]
        public async Task<ActionResult<ApiResponse<TeamDetailDto>>> GetMyTeam()
        {
            var result = await _teamService.GetMyTeamAsync(GetUserId());
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<TeamDetailDto>.Fail(result.Message));

            return Ok(ApiResponse<TeamDetailDto>.Ok(result.Data!, result.Message));
        }

        ///<summary>
        /// Retrieve the details of spicified team
        /// </summary>
        [HttpGet("{teamId}")]
        public async Task<ActionResult<ApiResponse<TeamDetailDto>>> GetTeamById(int teamId)
        {
            var result = await _teamService.GetTeamByIdAsync(teamId);
            if (!result.IsSuccess)
            {
                if (result.Message.Contains("Invalid"))
                    return BadRequest(ApiResponse<TeamDetailDto>.Fail(result.Message));
                return NotFound(ApiResponse<TeamDetailDto>.Fail(result.Message));
            }
                
            return Ok(ApiResponse<TeamDetailDto>.Ok(result.Data!, result.Message));
        }

        /// <summary>
        /// create a team
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TeamDetailDto>>> CreateTeam([FromBody] CreateTeamDto dto)
        {
            var result = await _teamService.CreateTeamAsync(GetUserId(), dto);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<TeamDetailDto>.Fail(result.Message, result.Errors));
            return CreatedAtAction(nameof(GetTeamById), new { teamId = result.Data?.Id },
                ApiResponse<TeamDetailDto>.Ok(result.Data!, result.Message));
        }

        /// <summary>
        /// Join the team using the invitation code
        /// </summary>
        [HttpPost("join")]
        public async Task<ActionResult<ApiResponse<TeamDetailDto>>> JoinTeam([FromBody] JoinTeamDto dto)
        {
            var result = await _teamService.JoinTeamAsync(GetUserId(), dto);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<TeamDetailDto>.Fail(result.Message, result.Errors));
            return Ok(ApiResponse<TeamDetailDto>.Ok(result.Data!, result.Message));
        }

        /// <summary>
        /// Leave the current team
        /// </summary>
        [HttpPost("leave")]
        public async Task<ActionResult<ApiResponse<object>>> LeaveTeam()
        {
            var result = await _teamService.LeaveTeamAsync(GetUserId());
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<object>.Fail(result.Message, result.Errors));
            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }

        /// <summary>
        /// retrieve the team leaderboard rank
        /// </summary>
        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<TeamLeaderboardDto>>>> GetTeamLeaderboard()
        {
            var result = await _teamService.GetTeamLeaderboardAsync();
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<List<TeamLeaderboardDto>>.Fail(result.Message));
            return Ok(ApiResponse<List<TeamLeaderboardDto>>.Ok(result.Data!, result.Message));
        }

    }
}
