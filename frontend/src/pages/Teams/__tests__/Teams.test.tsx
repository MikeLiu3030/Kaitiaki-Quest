import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import Teams from '../Teams';
import { teamApi } from '../../../api/teamApi';
import { enqueueSnackbar } from 'notistack';
import type { TeamDetail } from '../../../types/team';

vi.mock('@mui/icons-material', () => ({
  ContentCopy: () => <span data-testid="icon">ContentCopy</span>,
  GroupAdd: () => <span data-testid="icon">GroupAdd</span>,
  ExitToApp: () => <span data-testid="icon">ExitToApp</span>,
  PersonAdd: () => <span data-testid="icon">PersonAdd</span>,
}));

vi.mock('../../../api/teamApi', () => ({
  teamApi: {
    getMyTeam: vi.fn(),
    createTeam: vi.fn(),
    joinTeam: vi.fn(),
    leaveTeam: vi.fn(),
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

vi.mock('../../../utils/handleApiErrorMsg', () => ({
  default: vi.fn((err) => err?.response?.data?.message || 'An error occurred'),
}));

const mockTeam: TeamDetail = {
  id: 1,
  name: 'Kiwi Guardians',
  description: 'Protecting Aotearoa',
  inviteCode: 'ABCD1234',
  totalTeamXP: 150,
  memberCount: 2,
  teamLeaderName: 'leaderuser',
  createdAt: '2024-01-01T00:00:00Z',
  members: [
    {
      userId: 'user-1',
      userName: 'leaderuser',
      email: 'leader@test.com',
      totalXP: 100,
      level: 2,
      isTeamLeader: true,
    },
    {
      userId: 'user-2',
      userName: 'memberuser',
      email: 'member@test.com',
      totalXP: 50,
      level: 1,
      isTeamLeader: false,
    },
  ],
};

describe('Teams', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    Object.defineProperty(window.navigator, 'clipboard', {
      value: { writeText: vi.fn().mockResolvedValue(undefined) },
      writable: true,
      configurable: true,
    });

    (teamApi.getMyTeam as any).mockResolvedValue({ data: mockTeam });
    (teamApi.createTeam as any).mockResolvedValue({ data: mockTeam });
    (teamApi.joinTeam as any).mockResolvedValue({ data: mockTeam });
    (teamApi.leaveTeam as any).mockResolvedValue({ data: {} });
  });

  const renderTeams = () => {
    return render(
      <BrowserRouter>
        <Teams />
      </BrowserRouter>
    );
  };

  it('shows loading spinner when loading', () => {
    (teamApi.getMyTeam as any).mockImplementation(() => new Promise(() => {}));
    renderTeams();
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('renders create and join forms when no team', async () => {
    (teamApi.getMyTeam as any).mockRejectedValueOnce({ response: { status: 404 } });
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText(/create a team/i)).toBeInTheDocument();
      expect(screen.getByText(/join a team/i)).toBeInTheDocument();
    });
  });

  it('renders team details when user has a team', async () => {
    renderTeams();
    await waitFor(
      () => {
        expect(screen.getByText('Kiwi Guardians')).toBeInTheDocument();
        expect(screen.getByText('Protecting Aotearoa')).toBeInTheDocument();
        expect(screen.getByText('Total XP: 150')).toBeInTheDocument();
        const leaderNames = screen.getAllByText('leaderuser');
        expect(leaderNames.length).toBeGreaterThan(0);
        expect(screen.getByText('memberuser')).toBeInTheDocument();
        expect(screen.getByText(/members \(2\)/i)).toBeInTheDocument();
      },
      { timeout: 3000 }
    );
  });

  it('shows team stats card', async () => {
    renderTeams();
    await waitFor(
      () => {
        expect(screen.getByText('Team Stats')).toBeInTheDocument();
        expect(screen.getByText('Total Members')).toBeInTheDocument();
        expect(screen.getByText('2')).toBeInTheDocument();
        expect(screen.getByText('Total Team XP')).toBeInTheDocument();
        expect(screen.getByText('150')).toBeInTheDocument();
        expect(screen.getByText('Team Leader')).toBeInTheDocument();
        const leaderNames = screen.getAllByText('leaderuser');
        expect(leaderNames.length).toBeGreaterThan(0);
      },
      { timeout: 3000 }
    );
  });

  it('displays invite code with copy button', async () => {
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText('ABCD1234')).toBeInTheDocument();
      const copyButton = screen.getByRole('button', { name: /copy invite code/i });
      expect(copyButton).toBeInTheDocument();
    });
  });

  it('copies invite code when copy button clicked', async () => {
    const user = userEvent.setup();
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText('ABCD1234')).toBeInTheDocument();
    });

    const copyButton = screen.getByRole('button', { name: /copy invite code/i });
    await user.click(copyButton);

    await waitFor(() => {
      expect(enqueueSnackbar).toHaveBeenCalledWith('Invite code copied!', { variant: 'success' });
    });
  });

  it('opens leave dialog when leave button clicked', async () => {
    const user = userEvent.setup();
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText('Kiwi Guardians')).toBeInTheDocument();
    });

    const leaveButton = screen.getByRole('button', { name: /leave team/i });
    await user.click(leaveButton);

    await waitFor(() => {
      expect(screen.getByText(/are you sure you want to leave/i)).toBeInTheDocument();
      const teamNames = screen.getAllByText('Kiwi Guardians');
      expect(teamNames.length).toBeGreaterThan(0);
    });
  });

  it('closes leave dialog when cancel clicked', async () => {
    const user = userEvent.setup();
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText('Kiwi Guardians')).toBeInTheDocument();
    });

    const leaveButton = screen.getByRole('button', { name: /leave team/i });
    await user.click(leaveButton);

    await waitFor(() => {
      expect(screen.getByText(/are you sure you want to leave/i)).toBeInTheDocument();
    });

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    await user.click(cancelButton);

    await waitFor(() => {
      expect(screen.queryByText(/are you sure you want to leave/i)).not.toBeInTheDocument();
    });
  });

  it('leaves team when confirm clicked', async () => {
    const user = userEvent.setup();
    (teamApi.getMyTeam as any)
      .mockResolvedValueOnce({ data: mockTeam })
      .mockResolvedValue({ data: null });

    renderTeams();

    await waitFor(() => {
      expect(screen.getByText('Kiwi Guardians')).toBeInTheDocument();
    });

    const leaveButton = screen.getByRole('button', { name: /leave team/i });
    await user.click(leaveButton);

    await waitFor(() => {
      expect(screen.getByText(/are you sure you want to leave/i)).toBeInTheDocument();
    });

    const confirmButton = screen.getByRole('button', { name: /leave/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(teamApi.leaveTeam).toHaveBeenCalledWith({ ConnectionId: 'mock-connection-id' });
      expect(screen.getByText(/create a team/i)).toBeInTheDocument();
      expect(screen.getByText(/join a team/i)).toBeInTheDocument();
      expect(screen.queryByText('Kiwi Guardians')).not.toBeInTheDocument();
    });
  });

  it('creates team when form submitted', async () => {
    const user = userEvent.setup();
    (teamApi.getMyTeam as any).mockRejectedValueOnce({ response: { status: 404 } });
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText(/create a team/i)).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/team name/i), 'New Team');
    await user.type(screen.getByLabelText(/description/i), 'Team Description');
    await user.click(screen.getByRole('button', { name: /create team/i }));

    await waitFor(() => {
      expect(teamApi.createTeam).toHaveBeenCalledWith({
        name: 'New Team',
        description: 'Team Description',
        connectionId: 'mock-connection-id',
      });
    });
  });

  it('joins team when form submitted', async () => {
    const user = userEvent.setup();
    (teamApi.getMyTeam as any).mockRejectedValueOnce({ response: { status: 404 } });
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText(/join a team/i)).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/invite code/i), 'test123');
    await user.click(screen.getByRole('button', { name: /join team/i }));

    await waitFor(() => {
      expect(teamApi.joinTeam).toHaveBeenCalledWith({
        inviteCode: 'TEST123',
        connectionId: 'mock-connection-id',
      });
    });
  });

  it('shows error message on API failure', async () => {
    (teamApi.getMyTeam as any).mockRejectedValueOnce({
      response: { data: { message: 'Failed to load team' } },
    });
    renderTeams();
    await waitFor(() => {
      expect(screen.getByText('Failed to load team')).toBeInTheDocument();
    });
  });
});