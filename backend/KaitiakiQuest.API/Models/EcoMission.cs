using System.ComponentModel.DataAnnotations;

namespace KaitiakiQuest.API.Models
{
    public class EcoMission
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public int BasePoints { get; set; }

        public string Category { get; set; } = "Recycling"; // Recycling, Energy, Transport, Planting

        public string? ImageUrl { get; set; } // Task image

        public bool IsDaily { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<UserMission> UserMissions { get; set; } = new List<UserMission>();
    }
}