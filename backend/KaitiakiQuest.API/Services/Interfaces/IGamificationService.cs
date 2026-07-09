using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.Services.Interfaces
{
    public interface IGamificationService
    {
        Task<int> ProcessMissionCompletion(string userId, UserMission userMission);
        Task CheckAndAwardBadges(string userId);
        Task UpdateStreak(string userId);
    }
}
