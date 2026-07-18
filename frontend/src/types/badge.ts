// badge information
export interface Badge {
  id: number;
  name: string;
  description: string;
  iconUrl: string | null;
  unlockXP: number;
  isActive: boolean;
}

//The badges obtained by users 
export interface UserBadge {
  id: number;
  userId: string;
  badgeId: number;
  awardedDate: string;
  badge: Badge;
}


// user statistics information
export interface UserProfileStats {
  totalMissions: number;
  totalXP: number;
  currentStreak: number;
  weeklyMissions: number;
  level: number;
  nextLevelXP: number;
  xpProgress: number; // 0-100
}