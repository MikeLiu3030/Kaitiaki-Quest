import React, { useState, useEffect, useMemo, useCallback } from 'react';
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
  IconButton,
  CircularProgress,
  Alert,
  useMediaQuery,
  useTheme,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  Button,
} from '@mui/material';
import {
  CheckCircle as CheckCircleIcon,
  Pending as PendingIcon,
  Error as ErrorIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material';
import { missionApi } from '../../api/missionApi';
import type { CompleteMissionRequest, UserMission } from '../../types/mission';
import MissionDetailDialog from '../../components/missions/MissionDetailDialog';
import { enqueueSnackbar } from 'notistack';
import axios from 'axios'; 
import { useAuthStore } from '../../store/useAuthStore';
import { signalRService } from '../../services/signalRService';

// types define
type MissionStatus = 'Pending' | 'Completed' | 'Failed';


// Static configuration item
const STATUS_CONFIG: Record<MissionStatus, { icon: React.ReactNode; color: 'warning' | 'success' | 'error' }> = {
  Pending: { icon: <PendingIcon />, color: 'warning' },
  Completed: { icon: <CheckCircleIcon />, color: 'success' },
  Failed: { icon: <ErrorIcon />, color: 'error' },
};

const TAB_STATUS_MAP: MissionStatus[] = ['Pending', 'Completed', 'Failed'];


interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div role="tabpanel" hidden={value !== index} id={`mission-tabpanel-${index}`}>
    {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
  </div>
);


export default function MyMissions() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  
  const [missions, setMissions] = useState<UserMission[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tabValue, setTabValue] = useState(0);
  
  const [selectedMission, setSelectedMission] = useState<UserMission | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [isCompleting, setIsCompleting] = useState(false);
  const [isAbandoning, setIsAbandoning] = useState(false);

  const [confirmAbandonOpen, setConfirmAbandonOpen] = useState(false);
  const [missionToAbandon, setMissionToAbandon] = useState<number | null>(null);
  
  // get user information
  const fetchUser  = useAuthStore((state) => state.fetchUser);
  
  // Load missions
  const loadMyMissions = useCallback(async () => {    
    try {
      const res = await missionApi.getMyMissions();
    
      setMissions(res.data ?? []);
      setError(null); 
      
    } catch (err: unknown) {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.message || 'Failed to load missions');
      } else {
        setError('An unexpected error occurred');
      }
    } 
  }, []);

  useEffect(() => {
    const initFetch = async () => {
        await loadMyMissions();
        setIsLoading(false);
    }
    initFetch();
  }, [loadMyMissions]);


  // Performance optimization: Use useMemo cache to filter results
  const filteredMissions = useMemo(() => {
    const currentStatus = TAB_STATUS_MAP[tabValue];
    return missions.filter((mission) => mission.status === currentStatus);
  }, [missions, tabValue]);

  // Performance optimization: Cache the quantity of each state
  const counts = useMemo(() => ({
    Pending: missions.filter((m) => m.status === 'Pending').length,
    Completed: missions.filter((m) => m.status === 'Completed').length,
    Failed: missions.filter((m) => m.status === 'Failed').length,
  }), [missions]);

  const handleOpenDetail = useCallback((mission: UserMission) => {
    setSelectedMission(mission);
    setDialogOpen(true);
  }, []);

  const handleCompleteMission = async (missionId: number, evidence?: string) => {
    setIsCompleting(true);
    try {
      const requestData: CompleteMissionRequest = {
        evidenceImageUrl: evidence,
        connectionId: signalRService.connectionId || undefined,
      }
      await missionApi.completeMission(missionId, requestData);
      enqueueSnackbar('🎉 Mission completed! XP earned!', { variant: 'success' });
      await loadMyMissions();
      await fetchUser();
      setDialogOpen(false);
    } catch (err: unknown) {
      const errMsg = axios.isAxiosError(err) ? err.response?.data?.message : 'Failed to complete mission';
      enqueueSnackbar(errMsg, { variant: 'error' });
    } finally {
      setIsCompleting(false);
    }
  };

  const handleAbandonClick = (missionId: number) => {
    setMissionToAbandon(missionId);
    setConfirmAbandonOpen(true);
  }

  const handleCloseConfirm = () => { 
    setConfirmAbandonOpen(false);
    setMissionToAbandon(null);
  }
  const executeAbandonMission = async () => {
    if (!missionToAbandon) return;
    
    setIsAbandoning(true);
    try {
      await missionApi.abandonMission(missionToAbandon);
      enqueueSnackbar('Mission abandoned', { variant: 'info' });
      await loadMyMissions();
      setDialogOpen(false); // close mission detail dialog
    } catch (err: unknown) {
      const errMsg = axios.isAxiosError(err) ? err.response?.data?.message : 'Failed to abandon mission';
      enqueueSnackbar(errMsg, { variant: 'error' });
    } finally {
      setIsAbandoning(false);
      handleCloseConfirm();
    }
  };

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      {/* head area */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          📋 My Missions
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Track your progress and complete missions to earn XP.
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Tabs */}
      <Tabs
        value={tabValue}
        onChange={(_, newValue) => setTabValue(newValue)}
        sx={{ mb: 2 }}
        variant={isMobile ? 'fullWidth' : 'standard'}
      >
        <Tab label={`Pending (${counts.Pending})`} />
        <Tab label={`Completed (${counts.Completed})`} />
        <Tab label={`Failed (${counts.Failed})`} />
      </Tabs>

      {/* List rendering area */}
      <TabPanel value={tabValue} index={tabValue}>
        <MissionList 
          missions={filteredMissions} 
          status={TAB_STATUS_MAP[tabValue]} 
          onView={handleOpenDetail} 
        />
      </TabPanel>

      {/* pop-up windows  */}
      <MissionDetailDialog
        open={dialogOpen}
        mission={selectedMission}
        onClose={() => setDialogOpen(false)}
        onComplete={handleCompleteMission}
        onAbandon={handleAbandonClick}
        isCompleting={isCompleting}
        isAbandoning={isAbandoning}
      />

      {/* A pop-up window for a second confirmation of giving up the task */}
      <Dialog
        open={confirmAbandonOpen}
        onClose={handleCloseConfirm}
        aria-labelledby="abandon-dialog-title"
        aria-describedby="abandon-dialog-description"
      >
        <DialogTitle id="abandon-dialog-title">
          Abandon Mission?
        </DialogTitle>
        <DialogContent>
          <DialogContentText id="abandon-dialog-description">
            Are you sure you want to abandon this mission? You will lose any progress made, and you won't earn XP for it.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseConfirm} disabled={isAbandoning}>
            Cancel
          </Button>
          <Button 
            onClick={executeAbandonMission} 
            color="error" 
            variant="contained"
            disabled={isAbandoning}
          >
            {isAbandoning ? 'Abandoning...' : 'Yes, Abandon'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

// MissionList
interface MissionListProps {
  missions: UserMission[];
  status: MissionStatus;
  onView: (mission: UserMission) => void;
}

export function MissionList ({ missions, status, onView }: MissionListProps) {
  if (missions.length === 0) {
    const emptyMessages: Record<MissionStatus, string> = {
      Pending: 'No pending missions. Go to Dashboard and accept some! 🌱',
      Completed: 'No completed missions yet. Start your journey today! 🚀',
      Failed: 'No failed missions. Keep up the good work! 💪',
    };

    return (
      <Box sx={{ textAlign: 'center', py: 8 }}>
        <Typography variant="body1" color="text.secondary">
          {emptyMessages[status]}
        </Typography>
      </Box>
    );
  }

  return (
    <Card>
      <List sx={{ p: 0 }}>
        {missions.map((mission, index) => {

          const config = STATUS_CONFIG[mission.status as MissionStatus] || STATUS_CONFIG.Pending;

          return (
            <React.Fragment key={mission.id}>
              {index > 0 && <Divider />}
              <ListItem
                sx={{ py: 2, '&:hover': { bgcolor: 'action.hover' } }}
                secondaryAction={
                  <IconButton edge="end" onClick={() => onView(mission)} aria-label="view details">
                    <VisibilityIcon />
                  </IconButton>
                }
              >
                <ListItemAvatar>
                  <Avatar sx={{ bgcolor: `${config.color}.light`, color: `${config.color}.main` }}>
                    {config.icon}
                  </Avatar>
                </ListItemAvatar>
                <ListItemText
                  disableTypography // disabled MUI's <p> tag wrapper
                  primary={
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                        {mission.missionTitle}
                      </Typography>
                      <Chip label={mission.status} size="small" color={config.color} />
                    </Box>
                  }
                  secondary={
                    <Box sx={{ mt: 0.5 }}>
                      <Typography variant="body2" color="text.secondary">
                        XP Earned: {mission.earnedXP || 0}
                      </Typography>
                      {mission.completedDate && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        Completed: {new Date(mission.completedDate).toLocaleDateString()}
                        </Typography>
                      )}
                      {mission.acceptedDate && !mission.completedDate && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          Accepted: {new Date(mission.acceptedDate).toLocaleDateString()}
                        </Typography>
                      )}
                    </Box>
                  }
                />
              </ListItem>
            </React.Fragment>
          );
        })}
      </List>
    </Card>
  );
};


