import React, { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  Chip,
  Box,
  Divider,
  TextField,
  IconButton,
  Alert,
} from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import type { UserMission } from '../../types/mission';

interface MissionDetailDialogProps {
  open: boolean;
  mission: UserMission | null;
  onClose: () => void;
  onComplete: (missionId: number, evidence?: string) => void;
  onAbandon: (missionId: number) => void;
  isCompleting?: boolean;
  isAbandoning?: boolean;
}

export default function MissionDetailDialog ({
  open,
  mission,
  onClose,
  onComplete,
  onAbandon,
  isCompleting = false,
  isAbandoning = false,
}:MissionDetailDialogProps) {
  const [evidence, setEvidence] = useState('');

  if (!mission) return null;

  const isPending = mission.status === 'Pending';
  const isCompleted = mission.status === 'Completed';
  const isFailed = mission.status === 'Failed';

  const getStatusColor = () => {
    if (isCompleted) return 'success';
    if (isFailed) return 'error';
    return 'warning';
  };

  const getStatusLabel = () => {
    if (isCompleted) return '✅ Completed';
    if (isFailed) return '❌ Failed';
    return '⏳ Pending';
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">{mission.missionTitle}</Typography>
          <IconButton onClick={onClose}>
            <CloseIcon />
          </IconButton>
        </Box>
      </DialogTitle>

      <DialogContent dividers>
        {/* status label */}
        <Chip
          label={getStatusLabel()}
          color={getStatusColor()}
          sx={{ mb: 2 }}
        />

        {/* Description */}
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {mission.missionDescription}
        </Typography>

        <Divider sx={{ my: 2 }} />

        {/* points information */}
        <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
          <Typography variant="body2" color="text.secondary">
            XP Earned: <strong>{mission.earnedXP || 0}</strong>
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Status: <strong>{mission.status}</strong>
          </Typography>
        </Box>

        {/* Evidence pictures (if any) */}
        {mission.evidenceImageUrl && (
          <Box sx={{ mb: 2 }}>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
              Evidence:
            </Typography>
            <img
              src={mission.evidenceImageUrl}
              alt="Evidence"
              style={{
                maxWidth: '100%',
                maxHeight: 200,
                borderRadius: 8,
                objectFit: 'cover',
              }}
            />
          </Box>
        )}

        {/* mission completion form (only displayed in Pending status)） */}
        {isPending && (
          <>
            <Divider sx={{ my: 2 }} />
            <Typography variant="subtitle2" sx={{ mb: 1 }}>
              Submit Evidence (Optional)
            </Typography>
            <TextField
              fullWidth
              placeholder="Enter evidence description or image URL"
              value={evidence}
              onChange={(e) => setEvidence(e.target.value)}
              size="small"
              disabled={isCompleting}
              sx={{ mb: 1 }}
            />
            <Alert severity="info" sx={{ mt: 1 }}>
              💡 You can upload an image URL as evidence. AI recognition coming soon!
            </Alert>
          </>
        )}
      </DialogContent>

      <DialogActions>
        {isPending && (
          <>
            <Button
              onClick={() => onAbandon(mission.id)}
              color="error"
              variant="outlined"
              disabled={isAbandoning}
            >
              {isAbandoning ? 'Abandoning...' : 'Abandon Mission'}
            </Button>
            <Button
              onClick={() => onComplete(mission.id, evidence)}
              color="primary"
              variant="contained"
              disabled={isCompleting}
            >
              {isCompleting ? 'Completing...' : '✅ Complete Mission'}
            </Button>
          </>
        )}
        {!isPending && (
          <Button onClick={onClose} variant="contained">
            Close
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
};
