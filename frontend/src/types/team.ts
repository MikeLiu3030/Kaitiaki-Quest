// team member information
export interface TeamMember {
  userId: string;
  userName: string;
  email: string;
  totalXP: number;
  level: number;
  isTeamLeader: boolean;
}

// team detail
export interface TeamDetail {
  id: number;
  name: string;
  description: string | null;
  inviteCode: string;
  totalTeamXP: number;
  memberCount: number;
  teamLeaderName: string | null;
  createdAt: string;
  members: TeamMember[];
}

// Create team request
export interface CreateTeamRequest {
  name: string;
  description?: string;
}

// Join team request
export interface JoinTeamRequest {
  inviteCode: string;
}


// Team leaderboard entry
export interface TeamLeaderboardEntry {
  rank: number;
  teamId: number;
  teamName: string;
  totalTeamXP: number;
  memberCount: number;
  teamLeaderName: string | null;
}