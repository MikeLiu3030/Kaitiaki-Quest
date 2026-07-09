using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KaitiakiQuest.API.DTOs;
using KaitiakiQuest.API.Services;
using KaitiakiQuest.API.Services.Interfaces;

namespace KaitiakiQuest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class EcoMissionsController : ControllerBase
    {
        private readonly IEcoMissionService _service;

        public EcoMissionsController(IEcoMissionService service)
        {
            _service = service;
        }

        // ================================================================
        // Public Interface (no login required)
        // ================================================================


        ///<summary>
        ///Retrieve all valid EcoMission with category and daily task filters
        ///</summary>


        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<EcoMissionResponseDto>>>> GetMissions(
            [FromQuery] string? category,
            [FromQuery] bool? isDaily)
        {
            var result = await _service.GetAllMissionsAsync(category, isDaily);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<List<EcoMissionResponseDto>>.Fail(
                    result.Message,
                    result.Errors));
            }

            return Ok(ApiResponse<List<EcoMissionResponseDto>>.Ok(
                result.Data!,
                result.Message));
        }

        ///<summary>
        /// Get task details by ID
        ///</summary>


        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<EcoMissionResponseDto>>> GetMission(int id)
        {
            var result = await _service.GetMissionByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse<EcoMissionResponseDto>.Fail(result.Message));
            }

            return Ok(ApiResponse<EcoMissionResponseDto>.Ok(
                result.Data!,
                result.Message));
        }

        ///<summary>
        /// Get all available task categories
        ///</summary>


        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
        {
            var result = await _service.GetCategoriesAsync();

            return Ok(ApiResponse<List<string>>.Ok(
                result.Data!,
                result.Message));
        }

        // ================================================================
        // Admin-only endpoint (Requires Admin role)
        // ================================================================

        /// <summary>
        /// Create new EcoMission
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<EcoMissionResponseDto>>> CreateMission(
            [FromBody] CreateEcoMissionDto dto)
        {
            var result = await _service.CreateMissionAsync(dto);

            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<EcoMissionResponseDto>.Fail(
                    result.Message,
                    result.Errors));
            }

            // return 201 Created
            return CreatedAtAction(
                nameof(GetMission),
                new { id = result.Data?.Id },
                ApiResponse<EcoMissionResponseDto>.Ok(result.Data!, result.Message));
        }

        /// <summary>
        /// Update EcoMission (require Admin role)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<EcoMissionResponseDto>>> UpdateMission(
            int id,
            [FromBody] UpdateEcoMissionDto dto)
        {
            var result = await _service.UpdateMissionAsync(id, dto);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse<EcoMissionResponseDto>.Fail(result.Message));
            }

            return Ok(ApiResponse<EcoMissionResponseDto>.Ok(
                result.Data!,
                result.Message));
        }

        /// <summary>
        /// delete EcoMission
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteMission(int id)
        {
            var result = await _service.DeleteMissionAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(ApiResponse<object>.Fail(result.Message));
            }

            return Ok(ApiResponse<object>.Ok(null, result.Message));
        }
    }
}