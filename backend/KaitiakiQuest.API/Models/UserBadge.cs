using System.ComponentModel.DataAnnotations.Schema;

namespace KaitiakiQuest.API.Models
{
    public class UserBadge
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int BadgeId { get; set; }

        public DateTime AwardedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(BadgeId))]
        public virtual Badge? Badge { get; set; }
    }
}
