
// Mission entity (Corresponding to the backend EcoMissionResponseDto)
export interface EcoMission {
    id: number;
    title: string;
    description: string;
    basePoints: number;
    category: string;
    imageUrl: string | null;
    isDaily: boolean;
    isActive: boolean;
    createdAt: string;
}

// user mission record (Corresponding to the backend UserMissionResponseDto)
export interface UserMission {
    id: number;
    ecoMissionId: number;
    missionTitle: string;
    missionDescription: string;
    earnedXP: number;
    status: 'Pending' | 'Completed' | 'Failed' | 'UnderReview';
    acceptedDate: string | null;
    completedDate: string | null;
    evidenceImageUrl: string | null;
}

// Create mission request 
export interface CreateMissionRequest {
  title: string;
  description: string;
  basePoints: number;
  category: string;
  imageUrl?: string;
  isDaily?: boolean;
  isActive?: boolean;
}

// update mission
export type  UpdateMissionRequest = CreateMissionRequest;

// accept mission request
export interface AcceptMissionRequest {
  ecoMissionId: number;
}

// complete mission request
export interface CompleteMissionRequest {
  evidenceImageUrl?: string;
  evidenceFile?: File;
  connectionId?: string;
}

//user statistics
export interface UserStats {
  totalMissions: number;
  totalXP: number;
  currentStreak: number;
  weeklyMissions: number;
  level: number;
}


