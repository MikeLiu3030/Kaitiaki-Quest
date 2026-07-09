using KaitiakiQuest.API.DTOs;

namespace KaitiakiQuest.API.Services.Interfaces
{
    public interface IUserMissionService
    {
        Task<ServiceResult<List<UserMissionResponseDto>>> GetMyMissionsAsync(string userId, string? status);
        Task<ServiceResult<object>> GetMyStatsAsync(string userId);
        Task<ServiceResult<UserMissionResponseDto>> AcceptMissionAsync(string userId, AcceptMissionDto dto);
        Task<ServiceResult<UserMissionResponseDto>> CompleteMissionAsync(string userId, int missionId, CompleteMissionDto? dto);
        Task<ServiceResult<bool>> AbandonMissionAsync(string userId, int missionId);
        Task<ServiceResult<List<object>>> GetLeaderboardAsync();
    }
}
