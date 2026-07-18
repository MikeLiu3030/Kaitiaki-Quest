import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  TextField,
  Button,
  Grid,
  Avatar,
  Chip,
  CircularProgress,
  Alert,
  Divider,
  Paper,
  IconButton,
  Tooltip,
  Stack,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
} from '@mui/material';
import {
  ContentCopy as CopyIcon,
  GroupAdd as GroupAddIcon,
  ExitToApp as LeaveIcon,
  PersonAdd as PersonAddIcon,
} from '@mui/icons-material';
import { enqueueSnackbar } from 'notistack';
import { teamApi } from '../../api/teamApi';
import type { JoinTeamRequest, LeaveTeamRequest, TeamDetail, TeamMember } from '../../types/team';
import { signalRService } from '../../services/signalRService';
import getApiErrorMsg from '../../utils/handleApiErrorMsg';



// ----------------------------------------------------------------------
// subcomponent: create team form
// ----------------------------------------------------------------------
function CreateTeamForm({ onTeamUpdate }: { onTeamUpdate: (team: TeamDetail) => void }) {
  const [teamName, setTeamName] = useState('');
  const [teamDescription, setTeamDescription] = useState('');
  const [isCreating, setIsCreating] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); 
    if (!teamName.trim()) {
      enqueueSnackbar('Please enter a team name', { variant: 'warning' });
      return;
    }
    
    setIsCreating(true);
    try {
      const res = await teamApi.createTeam({
        name: teamName.trim(),
        description: teamDescription.trim() || undefined,
        connectionId: signalRService.connectionId || undefined,
      });
      if (res.data) {
        enqueueSnackbar(`🎉 Team "${res.data.name}" created successfully!`, { variant: 'success' });
        setTeamName('');
        setTeamDescription('');
        onTeamUpdate(res.data);
      } else {
        enqueueSnackbar('Unexpected error: No team data received from server.', { variant: 'error' });
        
      }
    } catch (err: unknown) {
      // enqueueSnackbar(getApiErrorMsg(err), { variant: 'error' });
      console.log(getApiErrorMsg(err));
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <Card component="form" onSubmit={handleSubmit}>
      <CardContent>
        <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <GroupAddIcon color="primary" /> Create a Team
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Start your own team and invite friends to join.
        </Typography>

        <TextField
          fullWidth
          label="Team Name"
          value={teamName}
          onChange={(e) => setTeamName(e.target.value)}
          sx={{ mb: 2 }}
          disabled={isCreating}
        />
        <TextField
          fullWidth
          label="Description (optional)"
          value={teamDescription}
          onChange={(e) => setTeamDescription(e.target.value)}
          multiline
          rows={2}
          sx={{ mb: 2 }}
          disabled={isCreating}
        />
        <Button
          type="submit"
          fullWidth
          variant="contained"
          size="large"
          disabled={isCreating}
          sx={{ fontWeight: 600 }}
        >
          {isCreating ? <CircularProgress size={24} color="inherit" /> : '🌱 Create Team'}
        </Button>
      </CardContent>
    </Card>
  );
}

// ----------------------------------------------------------------------
// subcomponent: join team form
// ----------------------------------------------------------------------
function JoinTeamForm({ onTeamUpdate }: { onTeamUpdate: (team: TeamDetail) => void }) {
  const [inviteCode, setInviteCode] = useState('');
  const [isJoining, setIsJoining] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inviteCode.trim()) {
      enqueueSnackbar('Please enter an invite code', { variant: 'warning' });
      return;
    }

    setIsJoining(true);
    try {
      
      const joinTeamData: JoinTeamRequest = {
        inviteCode: inviteCode.trim().toUpperCase(),
        connectionId: signalRService.connectionId || undefined,
      };
  
      const res = await teamApi.joinTeam(joinTeamData);
      if (res.data) {
        enqueueSnackbar(`🎉 Joined "${res.data.name}" successfully!`, { variant: 'success' });
        setInviteCode('');
        onTeamUpdate(res.data);
      } else {
        enqueueSnackbar('Unexpected error: No team data received from server.', { variant: 'error' });
      }
    } catch (err: unknown) {
      // enqueueSnackbar(getApiErrorMsg(err), { variant: 'error' });
      console.log(getApiErrorMsg(err));
    } finally {
      setIsJoining(false);
    }
  };

  return (
    <Card component="form" onSubmit={handleSubmit}>
      <CardContent>
        <Typography variant="h6" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <PersonAddIcon color="secondary" /> Join a Team
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Enter an invite code to join an existing team.
        </Typography>

        <TextField
          fullWidth
          label="Invite Code"
          value={inviteCode}
          onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
          placeholder="e.g. ABC12345"
          sx={{ 
            mb: 2,'& input': { 
            textTransform: 'uppercase'} 
          }}
          disabled={isJoining}
        />
        <Button
          type="submit"
          fullWidth
          variant="outlined"
          size="large"
          disabled={isJoining}
          sx={{ fontWeight: 600 }}
        >
          {isJoining ? <CircularProgress size={24} color="inherit" /> : '🔑 Join Team'}
        </Button>
      </CardContent>
    </Card>
  );
}

// ----------------------------------------------------------------------
// main component
// ----------------------------------------------------------------------
export default function Teams() {
  const [team, setTeam] = useState<TeamDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Dialog status
  const [leaveDialogOpen, setLeaveDialogOpen] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);

  // useRef is used to record the currently added room to ensure that 
  // the component exits correctly when uninstalled and prevent memory leaks
  const currentRoomId = useRef<string | null>(null);


  // 1. The core function for gett team data
const loadTeam = useCallback(async (silent = false) => {
  if (!silent) setIsLoading(true);
  setError(null);
  try {
    const res = await teamApi.getMyTeam();
    if (!res.data) {
      // No data is regarded as no team
      setTeam(null);
      currentRoomId.current = null;
      return;
    }
    
    const teamData = res.data;
    setTeam(teamData);    
    currentRoomId.current = teamData.inviteCode;


  } catch (err: unknown) {
    const apiErr = err as { response?: { status: number } };
    if (apiErr.response?.status === 404) {
      setTeam(null);
      setError(null); // clean residual errors.
      currentRoomId.current = null;

    } else {
      setError(getApiErrorMsg(err));
    }
  } finally {
    if (!silent) setIsLoading(false);
  }
}, []);

  // Cleaning during initialization and component uninstallation
  useEffect(() => {
    Promise.resolve().then(() => {
      loadTeam();
    });
    
    // Cleanup function: Exit SignalR room when leaving page
    return () => {
      currentRoomId.current = null;
    };
  }, [loadTeam]);

  // monitoring team XP updates event of SignalR
useEffect(() => {

    let timeoutId: number;

    const handleTeamXPUpdate = (event: Event) => {

      const customEvent = event as CustomEvent<{ totalTeamXP?: number }>;

      if (customEvent.detail?.totalTeamXP) {

        setTeam(prev => prev ? { ...prev, totalTeamXP: customEvent.detail.totalTeamXP! } : null);

      } else {

        clearTimeout(timeoutId);

        timeoutId = setTimeout(() => {

          loadTeam(true);

        }, 1500);

      }

    };

    window.addEventListener('teamXPUpdated', handleTeamXPUpdate);

    return () => {

      window.removeEventListener('teamXPUpdated', handleTeamXPUpdate);

      clearTimeout(timeoutId);

    };

  }, [loadTeam]);

  // leave a team logic
  const handleLeaveTeam = async () => {
    
    if (!signalRService.connectionId) {  
      enqueueSnackbar('Failed to leave team. Please try again.', { variant: 'error' });
      return;
    };
    const requestData: LeaveTeamRequest = {
      ConnectionId: signalRService.connectionId,
    };

    setIsLeaving(true);
    try {      
      await teamApi.leaveTeam(requestData);
      await loadTeam();
      currentRoomId.current = null;

      enqueueSnackbar('You have left the team', { variant: 'info' });
      setLeaveDialogOpen(false);
    } catch (err: unknown) {
      enqueueSnackbar(getApiErrorMsg(err), { variant: 'error' });
    } finally {
      setIsLeaving(false);
    }
  };

  // Listen for events of memebers joining, leaving and xp updates 
  useEffect(() => {
    let timeoutId: ReturnType<typeof setTimeout>;

    const handleRosterChange = () => {
      clearTimeout(timeoutId);
      timeoutId = setTimeout(async () => {
        await loadTeam(true); 
      }, 800);
    };

    window.addEventListener('teamMemberJoined', handleRosterChange);
    window.addEventListener('teamMemberLeft', handleRosterChange);
    window.addEventListener('teamXPUpdated', handleRosterChange);

    return () => {
      window.removeEventListener('teamMemberJoined', handleRosterChange);
      window.removeEventListener('teamMemberLeft', handleRosterChange);
      window.addEventListener('teamXPUpdated', handleRosterChange);
      clearTimeout(timeoutId); 
    };
  }, [loadTeam]);
  

  // Copy the invitation code
  const copyInviteCode = async () => {
    if (team?.inviteCode) {
      try {
        await navigator.clipboard.writeText(team.inviteCode);
        enqueueSnackbar('Invite code copied!', { variant: 'success' });
      } catch (err:unknown) {
        console.log("Failed to copy: ", err);
        enqueueSnackbar('Failed to copy. Please copy manually.', { variant: 'error' });
      }
    }
  };

  // Rendering member list
  const renderMembers = (members: TeamMember[]) => (
    <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
      {members.map((member) => (
        <Chip
          key={member.userId}
          avatar={<Avatar>{member.userName.charAt(0).toUpperCase()}</Avatar>}
          label={
            <Box component="span" sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              {member.userName}
              {member.isTeamLeader && (
                <Chip label="Leader" size="small" color="primary" sx={{ ml: 0.5, height: 20 }} />
              )}
              <Typography variant="caption" color="text.secondary" sx={{ ml: 0.5 }}>
                (Lv.{member.level})
              </Typography>
            </Box>
          }
          variant="outlined"
          size="medium"
        />
      ))}
    </Stack>
  );

  // Loading render
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
        👥 Teams
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Collaborate with friends to earn more XP and protect Aotearoa together!
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {team ? (
        <Grid container spacing={3}>
          {/*left side: team core info and members */}
          <Grid size={{xs:12, md:8}}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <Box>
                    <Typography variant="h5" sx={{ fontWeight: 700 }}>
                      {team.name}
                    </Typography>
                    {team.description && (
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                        {team.description}
                      </Typography>
                    )}
                  </Box>
                  <Chip
                    label={`Total XP: ${team.totalTeamXP}`}
                    color="primary"
                    variant="filled"
                    sx={{ fontWeight: 600 }}
                  />
                </Box>

                <Divider sx={{ my: 2 }} />

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    Invite Code:
                  </Typography>
                  <Paper
                    elevation={0}
                    sx={{
                      px: 2,
                      py: 0.5,
                      bgcolor: 'action.hover',
                      borderRadius: 1,
                      fontFamily: 'monospace',
                      fontWeight: 600,
                      fontSize: '1.1rem',
                    }}
                  >
                    {team.inviteCode}
                  </Paper>
                  <Tooltip title="Copy invite code">
                    <IconButton size="small" onClick={copyInviteCode}>
                      <CopyIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Box>

                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  Members ({team.memberCount})
                </Typography>
                {renderMembers(team.members)}

                <Box sx={{ mt: 3 }}>
                  <Button
                    variant="outlined"
                    color="error"
                    startIcon={<LeaveIcon />}
                    onClick={() => setLeaveDialogOpen(true)}
                  >
                    Leave Team
                  </Button>
                </Box>
              </CardContent>
            </Card>
          </Grid>

          {/* right side: team stats */}
          <Grid size={{xs:12, md:4}}>
            <Card>
              <CardContent>
                <Typography variant="h6" sx={{ mb: 2 }}>
                  Team Stats
                </Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Total Members</Typography>
                    <Typography variant="h5">{team.memberCount}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Total Team XP</Typography>
                    <Typography variant="h5" color="primary.main">{team.totalTeamXP}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Team Leader</Typography>
                    <Typography variant="body1">{team.teamLeaderName || 'Unknown'}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">Created</Typography>
                    <Typography variant="body2">
                      {new Date(team.createdAt).toLocaleDateString()}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      ) : (

        // Display no team mode: two individual forms 
        <Grid container spacing={3}>
          <Grid size={{xs:12, md:6}} >
            <CreateTeamForm onTeamUpdate={() => loadTeam()} />
          </Grid>
          <Grid size={{xs:12, md:6}}>
            <JoinTeamForm onTeamUpdate={() => loadTeam()} />
          </Grid>
        </Grid>
      )}

      {/* A confirmation pop-up window for leaving the team */}
      <Dialog open={leaveDialogOpen} onClose={() => setLeaveDialogOpen(false)}>
        <DialogTitle>Leave Team?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to leave <b>{team?.name}</b>? You will need an invite code to join back later.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLeaveDialogOpen(false)} color="inherit" disabled={isLeaving}>
            Cancel
          </Button>
          <Button onClick={handleLeaveTeam} color="error" variant="contained" disabled={isLeaving}>
            {isLeaving ? <CircularProgress size={20} color="inherit" /> : 'Leave'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
