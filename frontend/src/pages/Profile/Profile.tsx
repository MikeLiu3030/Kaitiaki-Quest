import React, { useState, useEffect, useMemo, useCallback } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  Typography,
  Avatar,
  Chip,
  LinearProgress,
  Button,
  Divider,
  Alert,
  Tooltip,
  useTheme,
} from '@mui/material';
import {
  EmojiEvents as EmojiEventsIcon,
  LocalFireDepartment as LocalFireDepartmentIcon,
  CheckCircle as CheckCircleIcon,
  TrendingUp as TrendingUpIcon,
} from '@mui/icons-material';
import { useAuthStore } from '../../store/useAuthStore';
import { missionApi } from '../../api/missionApi';
import { badgeApi } from '../../api/badgeApi';
import type { UserProfileStats } from '../../types/badge';
import { getBadgeIcon } from '../../utils/badgeIcons';

// ==================== 类型定义 ====================
interface Badge {
  id: string | number;
  name: string;
  description: string;
  iconUrl: string;
  unlocked: boolean;
  unlockXP: number;
}

interface ProfileData {
  stats: UserProfileStats & { nextLevelXP: number; xpProgress: number };
  badges: Badge[];
}

interface BadgeItemProps {
  name: string;
  description: string;
  icon: string;
  unlocked: boolean;
  unlockXP: number;
  currentXP: number;
}

// ==================== Custom Hook：load data ====================
const useProfileData = () => {
  const [data, setData] = useState<ProfileData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

const loadData = useCallback(async () => {
  try {
    setLoading(true);
    setError(null);

    // 1. Get profile stats, user badges, and all badges
    const [statsRes, userBadgesRes, allBadgesRes] = await Promise.allSettled([
      missionApi.getMyStats(),
      badgeApi.getUserBadges(),
      badgeApi.getAllBadges().catch(() => null),
    ]);

    // 2. check if core interface is successful
    if (statsRes.status === 'rejected') {
      throw new Error(statsRes.reason?.response?.data?.message || 'Failed to load profile stats');
    }

    // 3. Deconstructing data
    const rawStats = statsRes.value.data;
    const userBadges = userBadgesRes.status === 'fulfilled' ? userBadgesRes.value.data : [];
    const allBadges = allBadgesRes.status === 'fulfilled' && allBadgesRes.value ? allBadgesRes.value.data : null;

    // 4. Calculate level progress
    const currentLevel = rawStats?.level || 1;
    const nextLevelXP = currentLevel * 100;
    const totalXP = rawStats?.totalXP || 0;
    const xpProgress = nextLevelXP > 0 ? Math.min(100, (totalXP / nextLevelXP) * 100) : 0;

    const stats: UserProfileStats & { nextLevelXP: number; xpProgress: number } = {
        ...rawStats,
        totalMissions: rawStats?.totalMissions || 0,
        totalXP: totalXP,
        currentStreak: rawStats?.currentStreak || 0,
        weeklyMissions: rawStats?.weeklyMissions || 0,
        level: currentLevel,
        nextLevelXP,
        xpProgress,
    };

    // 5. Initialize an empty array to store the final processed badge data 
    // that conforms to the format of the front-end BadgeItem component
    let badges: Badge[] = [];
    if (allBadges) {
      // store the badge IDs of the unlocked badges 
      const unlockedIds = new Set((userBadges ?? []).map((b) => b.badgeId));
      
      // map all badges to the BadgeItem format
      badges = allBadges.map((badge) => ({
        id: badge.id,
        name: badge.name || 'Badge',
        description: badge.description || '',
        iconUrl: getBadgeIcon(badge.name),
        unlocked: unlockedIds.has(badge.id),
        unlockXP: badge.unlockXP || 0,
      }));
    } 

    setData({ stats, badges });
  } catch (err: unknown) {
    setError(err instanceof Error ? err.message : 'Unknown error occurred');
  } finally {
    setLoading(false);
  }
}, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  return { data, loading, error, retry: loadData };
};

// ==================== subcomponent: Badge Item==================
const BadgeItem: React.FC<BadgeItemProps> = React.memo(
  ({ name, description, icon, unlocked, unlockXP, currentXP }) => {
    const progress = useMemo(
      () => (unlockXP > 0 ? Math.min(100, (currentXP / unlockXP) * 100) : 0),
      [currentXP, unlockXP]
    );

    return (
      <Tooltip
        title={
          <Box>
            <Typography variant="body2">{description}</Typography>
            {!unlocked && (
              <Typography variant="caption" color="text.secondary">
                Requires {unlockXP} XP (Current: {currentXP})
              </Typography>
            )}
          </Box>
        }
        placement="top"
        arrow
      >
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            p: 2,
            borderRadius: 2,
            bgcolor: unlocked ? 'primary.light' : 'action.hover',
            opacity: unlocked ? 1 : 0.5,
            transition: 'all 0.3s ease',
            cursor: 'default',
            width: '100%',            
            height: '100%',
            minHeight: 120,
            '&:hover': { transform: 'scale(1.05)' },
          }}
          role="img"
          aria-label={`${name} badge, ${unlocked ? 'unlocked' : 'locked'}`}
        >
          <Typography variant="h3" sx={{ fontSize: 40 }} aria-hidden="true">
            {icon}
          </Typography>
          <Typography
            variant="caption"
            sx={{ 
                fontWeight: 600, 
                mt: 0.5, 
                textAlign: 'center',
                wordBreak: 'break-word', 
                maxWidth: '100%',
                fontSize: {
                    xs: '0.65rem',
                    sm: '0.75rem',
                }
            }}
          >
            {name}
          </Typography>
          {!unlocked && (
            <Box sx={{ width: '100%', mt: 0.5 }}>
              <LinearProgress
                variant="determinate"
                value={progress}
                aria-valuenow={progress}
                sx={{ height: 4, borderRadius: 2 }}
              />
            </Box>
          )}
          {unlocked && (
            <Chip label="✅ Unlocked" size="small" color="success" sx={{ mt: 0.5 }} />
          )}
        </Box>
      </Tooltip>
    );
  }
);
BadgeItem.displayName = 'BadgeItem';



// ==================== main component ====================
export default function Profile() {
  const theme = useTheme();
  const { user, logout } = useAuthStore();
  const { data, loading, error, retry } = useProfileData();

  const handleLogout = useCallback(() => {
    if (window.confirm('Are you sure you want to logout?')) {
      logout();
    }
  }, [logout]);


  if (loading) {
    return (
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
          👤 Profile
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
          Your journey and achievements in Kaitiaki Quest.
        </Typography>
      </Box>
    );
  }


  if (error || !data) {
    return (
      <Box>
        <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
          👤 Profile
        </Typography>
        <Alert severity="error" sx={{ mb: 2 }}>
          {error || 'Failed to load profile data'}
        </Alert>
        <Button variant="contained" onClick={retry}>
          Retry
        </Button>
      </Box>
    );
  }

  const { stats, badges } = data;

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
        👤 Profile
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Your journey and achievements in Kaitiaki Quest.
      </Typography>

      <Grid container spacing={3}>
        {/* lift side: user information */}
        <Grid size={{ xs: 12, md: 4}}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Avatar
                sx={{
                  width: 100,
                  height: 100,
                  margin: '0 auto',
                  bgcolor: 'secondary.main',
                  fontSize: 40,
                  mb: 2,
                }}
                alt={user?.userName || 'User avatar'}
              >
                {user?.userName?.charAt(0).toUpperCase() || 'U'}
              </Avatar>

              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {user?.userName || 'User'}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                {user?.email}
              </Typography>

              <Chip
                label={`Level ${stats.level}`}
                color="primary"
                sx={{ mb: 2, fontWeight: 600 }}
              />

              {/* level progress */}
              <Box sx={{ textAlign: 'left', mb: 2 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="caption" color="text.secondary">
                    XP Progress
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {stats.totalXP} / {stats.nextLevelXP} XP
                  </Typography>
                </Box>
                <LinearProgress
                  variant="determinate"
                  value={stats.xpProgress}
                  aria-valuenow={stats.xpProgress}
                  sx={{ height: 8, borderRadius: 4, bgcolor: 'action.hover' }}
                />
                <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                  {stats.nextLevelXP - stats.totalXP > 0
                    ? `${stats.nextLevelXP - stats.totalXP} XP to next level`
                    : 'Max level reached! 🎉'}
                </Typography>
              </Box>

              <Divider sx={{ my: 2 }} />

              <Button
                fullWidth
                variant="outlined"
                color="error"
                onClick={handleLogout}
                sx={{ fontWeight: 600 }}
              >
                Logout
              </Button>
            </CardContent>
          </Card>
        </Grid>

        {/* right side: stats + badges */}
        <Grid  size={{ xs: 12, md: 8 }}>
          {/* stats card */}
          <Grid container spacing={2} sx={{ mb: 3 }}>
            {[
              { label: 'Total XP', value: stats.totalXP, icon: <EmojiEventsIcon color="primary" fontSize="small" /> },
              { label: 'Completed', value: stats.totalMissions, icon: <CheckCircleIcon color="success" fontSize="small" /> },
              { label: 'Streak', value: `${stats.currentStreak} 🔥`, icon: <LocalFireDepartmentIcon sx={{ color: '#FF6B35' }} fontSize="small" />, valueColor: '#FF6B35' },
              { label: 'Level', value: stats.level, icon: <TrendingUpIcon color="info" fontSize="small" />, valueColor: theme.palette.info.main },
            ].map((stat) => (
              <Grid size={{ xs:6, sm:3}} key={stat.label}>
                <Card>
                  <CardContent>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                      {stat.icon}
                      <Typography variant="body2" color="text.secondary">
                        {stat.label}
                      </Typography>
                    </Box>
                    <Typography
                      variant="h5"
                      sx={{ fontWeight: 700, color: stat.valueColor || 'inherit' }}
                    >
                      {stat.value}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          {/* Badges wall */}
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ fontWeight: 600, mb: 2 }}>
                🏅 Badges
              </Typography>
              {badges.length > 0 ? (
                <Grid container spacing={1}>
                  {badges.map((badge) => (
                    <Grid key={badge.id} size={{xs:6, sm:3}}>
                      <BadgeItem
                        name={badge.name}
                        description={badge.description}
                        icon={getBadgeIcon(badge.name)}
                        unlocked={badge.unlocked}
                        unlockXP={badge.unlockXP}
                        currentXP={stats.totalXP}
                      />
                    </Grid>
                  ))}
                </Grid>
              ) : (
                <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
                  No badges yet. Complete missions to earn your first badge! 🌱
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};
