import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import MyMissions from '../MyMissions';
import { useAuthStore } from '../../../store/useAuthStore';
import { missionApi } from '../../../api/missionApi';
import type { UserMission } from '../../../types/mission';

vi.mock('@mui/icons-material', () => ({
  CheckCircle: () => <span data-testid="icon">CheckCircle</span>,
  Pending: () => <span data-testid="icon">Pending</span>,
  Error: () => <span data-testid="icon">Error</span>,
  Visibility: () => <span data-testid="icon">Visibility</span>,
  Close: () => <span data-testid="icon">Close</span>,
}));

const mockFetchUser = vi.fn();
vi.mock('../../../store/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

vi.mock('../../../api/missionApi', () => ({
  missionApi: {
    getMyMissions: vi.fn(),
    completeMission: vi.fn(),
    abandonMission: vi.fn(),
  },
}));

vi.mock('../../../services/signalRService', () => ({
  signalRService: {
    connectionId: 'mock-connection-id',
  },
}));

vi.mock('notistack', () => ({
  enqueueSnackbar: vi.fn(),
}));

const mockMissions: UserMission[] = [
  {
    id: 1,
    ecoMissionId: 101,
    missionTitle: 'Recycle 10 Bottles',
    missionDescription: 'Collect and recycle 10 plastic bottles',
    earnedXP: 0,
    status: 'Pending',
    acceptedDate: '2024-01-01T00:00:00Z',
    completedDate: null,
    evidenceImageUrl: null,
  },
  {
    id: 2,
    ecoMissionId: 102,
    missionTitle: 'Walk 2 km',
    missionDescription: 'Walk 2 km instead of driving',
    earnedXP: 25,
    status: 'Completed',
    acceptedDate: '2024-01-02T00:00:00Z',
    completedDate: '2024-01-03T00:00:00Z',
    evidenceImageUrl: null,
  },
  {
    id: 3,
    ecoMissionId: 103,
    missionTitle: 'Failed Mission',
    missionDescription: 'This mission was abandoned',
    earnedXP: 0,
    status: 'Failed',
    acceptedDate: '2024-01-04T00:00:00Z',
    completedDate: null,
    evidenceImageUrl: null,
  },
];

describe('MyMissions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (useAuthStore as any).mockReturnValue({
      fetchUser: mockFetchUser,
    });
    (missionApi.getMyMissions as any).mockResolvedValue({ data: mockMissions });
    (missionApi.completeMission as any).mockResolvedValue({ data: {} });
    (missionApi.abandonMission as any).mockResolvedValue({ data: {} });
  });

  const renderMyMissions = () => {
    return render(
      <BrowserRouter>
        <MyMissions />
      </BrowserRouter>
    );
  };

  it('renders page title and tabs', async () => {
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('📋 My Missions')).toBeInTheDocument();
      expect(screen.getByText(/Pending \(1\)/)).toBeInTheDocument();
      expect(screen.getByText(/Completed \(1\)/)).toBeInTheDocument();
      expect(screen.getByText(/Failed \(1\)/)).toBeInTheDocument();
    });
  });

  it('shows loading spinner when loading', () => {
    (missionApi.getMyMissions as any).mockImplementation(() => new Promise(() => {}));
    renderMyMissions();
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('renders mission list for Pending tab', async () => {
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();

      const pendingElements = screen.getAllByText('Pending');
      expect(pendingElements.length).toBe(2);
    });
  });

  it('switches to Completed tab', async () => {
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /completed \(1\)/i }));

    await waitFor(() => {
      expect(screen.getByText('Walk 2 km')).toBeInTheDocument();
      expect(screen.queryByText('Recycle 10 Bottles')).not.toBeInTheDocument();
    });
  });

  it('switches to Failed tab', async () => {
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /failed \(1\)/i }));

    await waitFor(() => {
      expect(screen.getByText('Failed Mission')).toBeInTheDocument();
      expect(screen.queryByText('Recycle 10 Bottles')).not.toBeInTheDocument();
    });
  });

  it('shows empty message when no missions in tab', async () => {
    (missionApi.getMyMissions as any).mockResolvedValue({ data: [] });
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText(/No pending missions/i)).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /completed \(0\)/i }));
    await waitFor(() => {
      expect(screen.getByText(/No completed missions yet/i)).toBeInTheDocument();
    });
  });

  it('opens detail dialog when view button clicked', async () => {
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByRole('button', { name: /view details/i });
    await user.click(viewButtons[0]);

    await waitFor(() => {
      const dialogTitle = screen.getByRole('heading', { name: 'Recycle 10 Bottles' });
      expect(dialogTitle).toBeInTheDocument();
    });
  });

  it('completes mission from detail dialog', async () => {
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByRole('button', { name: /view details/i });
    await user.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Recycle 10 Bottles' })).toBeInTheDocument();
    });

    const completeButton = screen.getByRole('button', { name: /complete mission/i });
    await user.click(completeButton);

    await waitFor(() => {
      expect(missionApi.completeMission).toHaveBeenCalledWith(
        1,
        { evidenceImageUrl: "", connectionId: 'mock-connection-id' }
      );
    });
  });

  it('abandons mission from detail dialog', async () => {
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    const viewButtons = screen.getAllByRole('button', { name: /view details/i });
    await user.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: 'Recycle 10 Bottles' })).toBeInTheDocument();
    });

    const abandonButton = screen.getByRole('button', { name: /abandon mission/i });
    await user.click(abandonButton);

    await waitFor(() => {
      expect(screen.getByText(/abandon this mission/i)).toBeInTheDocument();
    });

    const confirmButton = screen.getByRole('button', { name: /yes, abandon/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(missionApi.abandonMission).toHaveBeenCalledWith(1);
    });
  });

  it('shows error message on API failure', async () => {
    const axiosError = {
      isAxiosError: true,
      response: {
        data: { message: 'Failed to load missions' },
        status: 500,
      },
      message: 'Failed to load missions',
    };
    (missionApi.getMyMissions as any).mockRejectedValueOnce(axiosError);
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText('Failed to load missions')).toBeInTheDocument();
    });
  });

  it('shows correct XP earned for completed mission', async () => {
    const user = userEvent.setup();
    renderMyMissions();
    await waitFor(() => {
      expect(screen.getByText(/Pending \(1\)/)).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /completed \(1\)/i }));

    await waitFor(() => {
      expect(screen.getByText('XP Earned: 25')).toBeInTheDocument();
    });
  });
});