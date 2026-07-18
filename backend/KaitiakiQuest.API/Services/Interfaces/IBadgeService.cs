using KaitiakiQuest.API.DTOs;

namespace KaitiakiQuest.API.Services.Interfaces
{
    public interface IBadgeService
    {
        Task<ServiceResult<List<BadgeResponseDto>>> GetAllBadgesAsync();
        Task<ServiceResult<List<UserBadgeResponseDto>>> GetUserBadgesAsync(string userId);
    }
}
