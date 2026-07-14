import React from 'react';
import { Card, CardContent, Typography, Box, Skeleton } from '@mui/material';

interface StatsCardProps {
  icon: React.ReactNode;
  label: string;
  value: number | string;
  color?: string;
  isLoading?: boolean;
}

export default function StatsCard ({
  icon,
  label,
  value,
  color = 'primary.main',
  isLoading = false,
}: StatsCardProps)
{
  if (isLoading) {
    return (
      <Card>
        <CardContent>
          <Skeleton variant="text" width="60%" height={24} />
          <Skeleton variant="text" width="40%" height={40} />
        </CardContent>
      </Card>
    );
  }

  return (
    <Card
      sx={{
        height: '100%',
        transition: 'transform 0.2s',
        '&:hover': { transform: 'translateY(-2px)' },
      }}
    >
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          {icon}
          <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 500 }}>
            {label}
          </Typography>
        </Box>
        <Typography variant="h4" sx={{ fontWeight: 700, color }}>
          {value}
        </Typography>
      </CardContent>
    </Card>
  );
};