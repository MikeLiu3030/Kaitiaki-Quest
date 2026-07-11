using KaitiakiQuest.API.DTOs;

namespace KaitiakiQuest.API.Services.Interfaces
{
    public interface ITeamService
    {
        // Get the current user's team.
        Task<ServiceResult<TeamDetailDto>> GetMyTeamAsync(string userId);

        // Get team detail by ID
        Task<ServiceResult<TeamDetailDto>> GetTeamByIdAsync(int teamId);

        // Create a team
        Task<ServiceResult<CreateTeamDto>> CreateTeamAsync(string userId, CreateTeamDto dto);

        // Join a team
        Task<ServiceResult<TeamDetailDto>> JoinTeamAsync(string userId, JoinTeamDto dto);

        // Leave a team
        Task<ServiceResult<bool>> LeaveTeamAsync(string userId);

        // Get a team Leaderboard
        Task<ServiceResult<List<TeamLeaderboardDto>>> GetTeamLeaderboardAsync();
    }
}
