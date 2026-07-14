import React from 'react';
import {
  Card,
  CardContent,
  CardMedia,
  Typography,
  Chip,
  Button,
  Box,
  CardActions,
  useTheme,
  Skeleton,
} from '@mui/material';
import type { EcoMission } from '../../types/mission';

interface MissionCardProps {
  mission: EcoMission;
  onAccept: (missionId: number) => void;
  isAccepting?: boolean;
  isAccepted?: boolean;
}

export default function Mission({
  mission,
  onAccept,
  isAccepting = false,
  isAccepted = false,
}: MissionCardProps)
{
  const theme = useTheme();

  //Color mapping of classification labels
  const categoryColors: Record<string, string> = {
    Recycling: '#4CAF50',
    Energy: '#FF9800',
    Transport: '#2196F3',
    Planting: '#8BC34A',
    Other: '#9E9E9E',
  };

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        transition: 'transform 0.2s, box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: theme.shadows[8],
        },
      }}
    >
      {/* Mission image */}
      {mission.imageUrl ? (
        <CardMedia
          component="img"
          height="180"
          image={mission.imageUrl}
          alt={mission.title}
          sx={{ objectFit: 'cover' }}
        />
      ) : (
        <Box
          sx={{
            height: 180,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: 'action.hover',
          }}
        >
          <Typography variant="h1" sx={{ fontSize: 64, opacity: 0.3 }}>
            🌿
          </Typography>
        </Box>
      )}

      <CardContent sx={{ flex: 1 }}>
        {/* lable row */}
        <Box sx={{ display: 'flex', gap: 1, mb: 1, flexWrap: 'wrap' }}>
          <Chip
            label={mission.category}
            size="small"
            sx={{
              bgcolor: categoryColors[mission.category] || '#9E9E9E',
              color: '#fff',
              fontWeight: 600,
            }}
          />
          {mission.isDaily && (
            <Chip label="🔥 Daily" size="small" color="warning" variant="outlined" />
          )}
        </Box>

        {/* Title */}
        <Typography variant="h6" sx={{ fontWeight: 600, mb: 1 }}>
          {mission.title}
        </Typography>

        {/* Description */}
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
            mb: 2,
          }}
        >
          {mission.description}
        </Typography>

        {/* Points */}
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography variant="body2" color="text.secondary">
            💚
          </Typography>
          <Typography variant="h6" sx={{ fontWeight: 700, color: 'primary.main' }}>
            +{mission.basePoints} XP
          </Typography>
        </Box>
      </CardContent>

      <CardActions sx={{ p: 2, pt: 0 }}>
        <Button
          fullWidth
          variant="contained"
          size="medium"
          onClick={() => onAccept(mission.id)}
          disabled={isAccepting || isAccepted}
          sx={{
            borderRadius: 2,
            fontWeight: 600,
          }}
        >
          {isAccepted
            ? '✅ Accepted'
            : isAccepting
            ? 'Processing...'
            : '🌱 Accept Mission'}
        </Button>
      </CardActions>
    </Card>
  );
};

// upload skeleton
export function MissionCardSkeleton() {
  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <Skeleton variant="rectangular" height={180} />
      <CardContent sx={{ flex: 1 }}>
        <Skeleton variant="text" width="40%" height={24} sx={{ mb: 1 }} />
        <Skeleton variant="text" width="80%" height={28} sx={{ mb: 1 }} />
        <Skeleton variant="text" width="100%" height={20} />
        <Skeleton variant="text" width="60%" height={20} sx={{ mb: 2 }} />
        <Skeleton variant="text" width="30%" height={32} />
      </CardContent>
      <CardActions sx={{ p: 2, pt: 0 }}>
        <Skeleton variant="rectangular" width="100%" height={36} />
      </CardActions>
    </Card>
  );
};
