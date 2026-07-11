using KaitiakiQuest.API.Models;

namespace KaitiakiQuest.API.DTOs
{
    // Create a team request
    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // Join a team request
    public class JoinTeamDto
    {
        public string InviteCode { get; set; } = string.Empty;
    }

    // team member information
    public class TeamMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalXP { get; set; }
        public int Level { get; set; }
        public bool IsTeamLeader { get; set; } // Is he the captain?
    }

    // Team detail response
    public class TeamDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string InviteCode { get; set; } = string.Empty;
        public int TotalTeamXP { get; set; }
        public int MemberCount { get; set; }
        public string? TeamLeaderName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TeamMemberDto> Members { get; set; } = new List<TeamMemberDto>();
    }

    // Team ranking list entry
    public class TeamLeaderboardDto
    {
        public int Rank { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int TotalTeamXP { get; set; }
        public int MemberCount { get; set; }
        public string? TeamLeaderName { get; set; }
    }
}
