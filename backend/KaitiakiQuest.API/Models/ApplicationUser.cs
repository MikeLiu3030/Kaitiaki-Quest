using Microsoft.AspNetCore.Identity;
namespace KaitiakiQuest.API.Models;

public class ApplicationUser: IdentityUser
{
    // gamification extended fiels
    public int TotalXP { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int CurrentStreak { get; set; } = 0;
    public DateTime? LastMissionCompleteDate { get; set; }

    // Navigation property
    public ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

    // Team relationship (a user can only belong to one team)
    public int? TeamId { get; set; }
    public Team? Team { get; set; }
}
