using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KaitiakiQuest.API.Models
{
    public class UserMission
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int EcoMissionId { get; set; }

        public MissionStatus Status { get; set; } = MissionStatus.Pending;

        public DateTime? AcceptedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime? FailedDate { get; set; }

        public string? EvidenceImageUrl { get; set; } // user upload evidence image

        public int EarnedXP { get; set; } = 0; // The actual experience value

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(EcoMissionId))]
        public virtual EcoMission? EcoMission { get; set; }
    }

    public enum MissionStatus
    {
        Pending,    // Collected and pending completion
        Completed,  // Completed (reviewed)
        Failed,     // Failure (timeout or giving up)
        UnderReview // Evidence has been submitted and is under review
    }
}