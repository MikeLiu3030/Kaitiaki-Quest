using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.DTOs
{

    public class BadgeResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int UnlockXP { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserBadgeResponseDto { 
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int BadgeId { get; set; }
        public string BadgeName { get; set; } = string.Empty;
        public DateTime AwardedDate { get; set; }
        public BadgeResponseDto Badge { get; set; } = new();
    }

    public class BadgeProgressDto
    {
        public bool HasNextBadge { get; set; }
        public string? Name { get; set; }
        public int UnlockXP { get; set; }
        public int CurrentXP { get; set; }
        public double Progress { get; set; }
    }

}
