using System.ComponentModel.DataAnnotations;

namespace KaitiakiQuest.API.Models
{
    public class Badge
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public string? IconUrl { get; set; }

        public int UnlockXP { get; set; } // Unlock the minimum XP required

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    }
}