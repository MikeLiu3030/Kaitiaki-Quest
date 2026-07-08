using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KaitiakiQuest.API.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string InviteCode { get; set; } = string.Empty; // An invitation code for joining the team

        public int TotalTeamXP { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property：Team creator
        public string? CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        // Navigation：Team mermbers(a team can have multiple user)
        public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
    }
}
