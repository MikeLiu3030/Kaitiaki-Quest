import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Tabs,
  Tab,
  Card,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar,
  Avatar,
  Chip,
  CircularProgress,
  Alert,
} from '@mui/material';
import { missionApi } from '../../api/missionApi';
import { teamApi } from '../../api/teamApi';
import type { TeamLeaderboardEntry } from '../../types/team';
import getApiErrorMsg from '../../utils/handleApiErrorMsg';


interface PersonalLeaderboardEntry {
  userId?: string | number; 
  userName: string;
  level: number;
  currentStreak: number;
  totalXP: number;
}


interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function CustomTabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`leaderboard-tabpanel-${index}`}
      aria-labelledby={`leaderboard-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ pt: 2 }}>{children}</Box>}
    </div>
  );
}

function a11yProps(index: number) {
  return {
    id: `leaderboard-tab-${index}`,
    'aria-controls': `leaderboard-tabpanel-${index}`,
  };
}


const getRankColor = (rank: number) => {
  switch (rank) {
    case 1:
      return '#FFD700'; 
    case 2:
      return '#C0C0C0'; 
    case 3:
      return '#CD7F32'; 
    default:
      return 'grey.300'; 
  }
};

export default function Leaderboard() {
  const [tabValue, setTabValue] = useState(0);
  const [personalLeaderboard, setPersonalLeaderboard] = useState<PersonalLeaderboardEntry[]>([]);
  const [teamLeaderboard, setTeamLeaderboard] = useState<TeamLeaderboardEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {

    const controller = new AbortController();

    const loadLeaderboards = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const [personalRes, teamRes] = await Promise.all([
          missionApi.getLeaderboard(),
          teamApi.getTeamLeaderboard(),
        ]);
        setPersonalLeaderboard(personalRes.data ?? []);
        setTeamLeaderboard(teamRes.data ?? []);
      } catch (err: unknown) {
        getApiErrorMsg(err);
      } finally {
        setIsLoading(false);
      }
    };

    loadLeaderboards();

    return () => {
      controller.abort(); // When a component is unloaded, unfinished requests are cancelled
    };
  }, []);

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
        🏆 Leaderboards
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        See who's making the biggest impact on Aotearoa.
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}>
        <Tabs 
          value={tabValue} 
          onChange={(_, v) => setTabValue(v)} 
          aria-label="leaderboard tabs"
        >
          <Tab label="Personal" {...a11yProps(0)} />
          <Tab label="Teams" {...a11yProps(1)} />
        </Tabs>
      </Box>

      {/* Personal Leaderboard */}
      <CustomTabPanel value={tabValue} index={0}>
        <Card>
          <List disablePadding>
            {personalLeaderboard.length === 0 ? (
              <ListItem>
                <ListItemText primary="No data yet. Start completing missions!" />
              </ListItem>
            ) : (
              personalLeaderboard.map((entry, index) => {
                const rank = index + 1;
                return (
                  <ListItem 
                    key={entry.userName}
                    divider={index !== personalLeaderboard.length - 1}
                  >
                    <ListItemAvatar>
                      <Avatar sx={{ bgcolor: getRankColor(rank), color: rank <= 3 ? 'white' : 'text.primary' }}>
                        {rank}
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary={entry.userName}
                      secondary={`Level ${entry.level} • Streak ${entry.currentStreak} days`}
                    />
                    <Chip label={`${entry.totalXP} XP`} color="primary" variant={rank <= 3 ? "filled" : "outlined"} />
                  </ListItem>
                );
              })
            )}
          </List>
        </Card>
      </CustomTabPanel>

      {/* Team Leaderboard */}
      <CustomTabPanel value={tabValue} index={1}>
        <Card>
          <List disablePadding>
            {teamLeaderboard.length === 0 ? (
              <ListItem>
                <ListItemText primary="No teams yet. Create one and start earning XP!" />
              </ListItem>
            ) : (
              teamLeaderboard.map((entry, index) => (
                <ListItem 
                  key={entry.teamId}
                  divider={index !== teamLeaderboard.length - 1}
                >
                  <ListItemAvatar>
                    <Avatar sx={{ bgcolor: getRankColor(entry.rank), color: entry.rank <= 3 ? 'white' : 'text.primary' }}>
                      {entry.rank}
                    </Avatar>
                  </ListItemAvatar>
                  <ListItemText
                    primary={entry.teamName}
                    secondary={`Leader: ${entry.teamLeaderName || 'Unknown'} • ${entry.memberCount} members`}
                  />
                  <Chip label={`${entry.totalTeamXP} XP`} color="secondary" variant={entry.rank <= 3 ? "filled" : "outlined"} />
                </ListItem>
              ))
            )}
          </List>
        </Card>
      </CustomTabPanel>
    </Box>
  );
}
