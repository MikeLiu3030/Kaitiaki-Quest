namespace KaitiakiQuest.API.DTOs
{
    // Claim a task
    public class AcceptMissionDto
    {
        public int EcoMissionId { get; set; }
    }

    // submit
    public class CompleteMissionDto
    {
        public string? EvidenceImageUrl { get; set; }
    }

    // Return the user task record
    public class UserMissionResponseDto
    {
        public int Id { get; set; }
        public int EcoMissionId { get; set; }
        public string MissionTitle { get; set; } = string.Empty;
        public string MissionDescription { get; set; } = string.Empty;
        public int EarnedXP { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? AcceptedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? EvidenceImageUrl { get; set; }
    }
}
