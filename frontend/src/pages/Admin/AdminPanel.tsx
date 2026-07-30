import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Button,
  TextField,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  IconButton,
  Chip,
  CircularProgress,
  Switch,
  FormControlLabel,
  MenuItem,
  Grid,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { useAuthStore } from '../../store/useAuthStore';
import { missionApi } from '../../api/missionApi';
import type { EcoMission, CreateMissionRequest, UpdateMissionRequest } from '../../types/mission';
import { enqueueSnackbar } from 'notistack';
import getApiErrorMsg from '../../utils/handleApiErrorMsg';

// category mission choices
const CATEGORIES = ['Recycling', 'Energy', 'Transport', 'Planting', 'Water', 'Education', 'Community'];

export default function AdminPanel()  {
  const { user } = useAuthStore();
  const [missions, setMissions] = useState<EcoMission[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState<number | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingMission, setEditingMission] = useState<EcoMission | null>(null);

  // Form state
  const [formData, setFormData] = useState<CreateMissionRequest>({
    title: '',
    description: '',
    basePoints: 10,
    category: 'Recycling',
    imageUrl: '',
    isDaily: false,
    isActive: false,
  });

  // Delete confirmation modal
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteTargetId, setDeleteTargetId] = useState<number | null>(null);


  // check if the user is Admin.
  const isAdmin = user?.roles?.includes('Admin') || false;

  const fetchMissions = useCallback(async (): Promise<EcoMission[]> =>{
    const res = await missionApi.getMissions();
    if (!res.data) return [];
    return res.data.filter((m: EcoMission) => m.isActive)
  }, [])

  const loadMissions =useCallback(async () => {
    setIsLoading(true);
    try {
      // Get all missions
      const data = await fetchMissions();
      setMissions(data);
    } catch (error:unknown) {
      getApiErrorMsg(error);
    } finally {
      setIsLoading(false);
    }
  }, [fetchMissions]);

    useEffect(() => {
      if (!isAdmin) return;
      let ignore = false;

      fetchMissions()
        .then((data) => {
            if (!ignore) setMissions(data);
        })
        .catch((error:unknown) => {
            if(!ignore) getApiErrorMsg(error);
        })
        .finally(() => {
            if (!ignore) setIsLoading(false);
        });
    return () => {
        ignore = true;
    }
  }, [isAdmin, fetchMissions]);

  const handleOpenCreateDialog = () => {
    setEditingMission(null);
    setFormData({
      title: '',
      description: '',
      basePoints: 10,
      category: 'Recycling',
      imageUrl: '',
      isDaily: false,
      isActive: false,
    });
    setDialogOpen(true);
  };

  const handleOpenEditDialog = (mission: EcoMission) => {
    setEditingMission(mission);
    setFormData({
      title: mission.title,
      description: mission.description,
      basePoints: mission.basePoints,
      category: mission.category,
      imageUrl: mission.imageUrl || '',
      isDaily: mission.isDaily,
      isActive: mission.isActive,
    });
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingMission(null);
  };

  const handleSaveMission = async () => {

    if (!formData.title.trim() || !formData.description.trim()) {
      enqueueSnackbar('Title and description are required', { variant: 'warning' });
      return;
    }

    setIsSaving(true);
    try {
      if (editingMission) {
        // update
        await missionApi.updateMission(editingMission.id, formData as UpdateMissionRequest);
        enqueueSnackbar('Mission updated successfully!', { variant: 'success' });
      } else {
        // create
        await missionApi.createMission(formData);
        enqueueSnackbar('Mission created successfully!', { variant: 'success' });
      }
      await loadMissions();
      handleCloseDialog();
    } catch (error: unknown) {
        getApiErrorMsg(error);
      
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeleteClick = (id: number) => {
    setDeleteTargetId(id);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (deleteTargetId === null) return;
    console.log("ID:",deleteTargetId);
    setIsDeleting(deleteTargetId);
    try {
      const res = await missionApi.deleteMission(deleteTargetId);
      if (!res.data) return
      enqueueSnackbar('Mission deleted successfully', { variant: 'success' });
      await loadMissions();
    } catch (error: unknown) {
      getApiErrorMsg(error);
    } finally {
      setIsDeleting(null);
      setDeleteDialogOpen(false);
      setDeleteTargetId(null);
    }
  };

  // Non-administrators are shown as having no access rights
  if (!isAdmin) {
    return (
      <Box sx={{ textAlign: 'center', py: 8 }}>
        <Typography variant="h5" color="error" sx={{ mb: 2 }}>
          ⛔ Access Denied
        </Typography>
        <Typography variant="body1" color="text.secondary">
          You don't have permission to access this page.
        </Typography>
      </Box>
    );
  }

  return (
    <Box>
      {/* page title */}
      <Box sx={{ mb: 4 }}>
        <Grid container sx={{ alignItems: 'center', justifyContent: 'space-between' }} spacing={2}>
          {/* lift: title */}
          <Grid  size={{ xs: 12, md: 6 }} >
            <Typography variant="h4" sx={{ fontWeight: 700 }}>
              🛠️ Admin Panel
            </Typography>
            <Typography variant="body1" color="text.secondary">
              Manage missions and oversee the Kaitiaki Quest ecosystem.
            </Typography>
          </Grid>

          {/* right: buttons */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Box sx={{ display: 'flex', gap: 1, justifyContent: { xs: 'flex-start', md: 'flex-end' } }}>
              <Button
                variant="outlined"
                onClick={loadMissions}
                startIcon={<RefreshIcon />}
                disabled={isLoading}
              >
                Refresh
              </Button>
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={handleOpenCreateDialog}
                sx={{ fontWeight: 600 }}
              >
                New Mission
              </Button>
            </Box>
          </Grid>
        </Grid>
      </Box>
      {/* mission list */}
      <Card>
        <CardContent>
          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : missions.length === 0 ? (
            <Typography variant="body1" color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
              No missions found. Create your first mission! 🚀
            </Typography>
          ) : (
            <List disablePadding>
              {missions.map((mission) => (
                <ListItem
                  key={mission.id}
                  divider
                  sx={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'stretch', 
                    py: 2,
                    px: 2,
                    opacity: mission.isActive ? 1 : 0.5,
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                >
                  {/* 1. top level：title + button */}
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 1, mb: 1 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 600, wordBreak: 'break-word' }}>
                      {mission.title}
                    </Typography>
                    
                    <Box sx={{ display: 'flex', gap: 0.5, flexShrink: 0 }}>
                      <IconButton
                        size="small"
                        onClick={() => handleOpenEditDialog(mission)}
                        disabled={isDeleting === mission.id}
                      >
                        <EditIcon fontSize="small" />
                      </IconButton>
                      <IconButton
                        size="small"
                        onClick={() => handleDeleteClick(mission.id)}
                        color="error"
                        disabled={isDeleting === mission.id}
                      >
                        {isDeleting === mission.id ? <CircularProgress size={18} /> : <DeleteIcon fontSize="small" />}
                      </IconButton>
                    </Box>
                  </Box>

                  {/* 2. middle level：label group */}
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.8, mb: 1.5 }}>
                    <Chip
                      label={mission.category}
                      size="small"
                      color="primary"
                    />
                    {!mission.isActive && (
                      <Chip label="Inactive" size="small" color="error" />
                    )}
                    {mission.isDaily && (
                      <Chip label="🔥 Daily" size="small" color="warning" />
                    )}
                  </Box>

                  {/* 3. button level：meta data  */}
                  <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                    <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 500 }}>
                      💚 {mission.basePoints} XP
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Created: {new Date(mission.createdAt).toLocaleDateString()}
                    </Typography>
                  </Box>
                </ListItem>
              ))}
            </List>
          )}
        </CardContent>
      </Card>

      {/*  create/edit modal */}
      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingMission ? '✏️ Edit Mission' : '✨ Create New Mission'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Title"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              fullWidth
              required
            />
            <TextField
              label="Description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
              required
            />
            <TextField
              label="Base Points (XP)"
              type="number"
              value={formData.basePoints}
              onChange={(e) => setFormData({ ...formData, basePoints: Number(e.target.value) })}
              fullWidth
              variant="outlined"
              slotProps={{ 
                input: {
                    inputProps: {min:1}
                }
            }}
            />
            <TextField
              label="Category"
              value={formData.category}
              onChange={(e) => setFormData({ ...formData, category: e.target.value })}
              fullWidth
              select
            >
              {CATEGORIES.map((cat) => (
                <MenuItem key={cat} value={cat}>
                  {cat}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Image URL (Optional)"
              value={formData.imageUrl}
              onChange={(e) => setFormData({ ...formData, imageUrl: e.target.value })}
              fullWidth
              placeholder="https://images.unsplash.com/..."
            />
            <FormControlLabel
              control={
                <Switch
                  checked={formData.isDaily}
                  onChange={(e) => setFormData({ ...formData, isDaily: e.target.checked })}
                />
              }
              label="🔥 Daily Mission"
            />
            <FormControlLabel
              control={
                <Switch
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                />
              }
              label="🔥 Daily Mission"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog} disabled={isSaving}>
            Cancel
          </Button>
          <Button
            onClick={handleSaveMission}
            variant="contained"
            disabled={isSaving}
            sx={{ fontWeight: 600 }}
          >
            {isSaving ? <CircularProgress size={24} /> : (editingMission ? 'Update' : 'Create')}
          </Button>
        </DialogActions>
      </Dialog>

      {/* confirm Modal for deletion*/}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>⚠️ Confirm Delete</DialogTitle>
        <DialogContent>
          <Typography variant="body1">
            Are you sure you want to delete this mission? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleDeleteConfirm} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};





