import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import Leaderboard from '../Leaderboard';
import { missionApi } from '../../../api/missionApi';
import { teamApi } from '../../../api/teamApi';
import type { TeamLeaderboardEntry } from '../../../types/team';

vi.mock('@mui/icons-material', () => ({
  EmojiEvents: () => <span data-testid="icon">EmojiEvents</span>,
}));

vi.mock('../../../api/missionApi', () => ({
  missionApi: {
    getLeaderboard: vi.fn(),
  },
}));

vi.mock('../../../api/teamApi', () => ({
  teamApi: {
    getTeamLeaderboard: vi.fn(),
  },
}));

vi.mock('../../../utils/handleApiErrorMsg', () => ({
  default: vi.fn((err) => err?.response?.data?.message || 'An error occurred'),
}));

const mockPersonalLeaderboard = [
  { userName: 'player1', totalXP: 1000, level: 10, currentStreak: 5 },
  { userName: 'player2', totalXP: 800, level: 8, currentStreak: 3 },
  { userName: 'player3', totalXP: 500, level: 5, currentStreak: 1 },
];

const mockTeamLeaderboard: TeamLeaderboardEntry[] = [
  { rank: 1, teamId: 1, teamName: 'Team Alpha', totalTeamXP: 2000, memberCount: 5, teamLeaderName: 'leader1' },
  { rank: 2, teamId: 2, teamName: 'Team Beta', totalTeamXP: 1500, memberCount: 4, teamLeaderName: 'leader2' },
  { rank: 3, teamId: 3, teamName: 'Team Gamma', totalTeamXP: 1000, memberCount: 3, teamLeaderName: 'leader3' },
];

describe('Leaderboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (missionApi.getLeaderboard as any).mockResolvedValue({ data: mockPersonalLeaderboard });
    (teamApi.getTeamLeaderboard as any).mockResolvedValue({ data: mockTeamLeaderboard });
  });

  const renderLeaderboard = () => {
    return render(
      <BrowserRouter>
        <Leaderboard />
      </BrowserRouter>
    );
  };

  it('renders page title and tabs', async () => {
    renderLeaderboard();
    await waitFor(() => {
      expect(screen.getByText('🏆 Leaderboards')).toBeInTheDocument();
      expect(screen.getByText("See who's making the biggest impact on Aotearoa.")).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /personal/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /teams/i })).toBeInTheDocument();
    });
  });

  it('shows loading spinner when loading', () => {
    (missionApi.getLeaderboard as any).mockImplementation(() => new Promise(() => {}));
    renderLeaderboard();
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('renders personal leaderboard data', async () => {
    renderLeaderboard();
    await waitFor(() => {
      expect(screen.getByText('player1')).toBeInTheDocument();
      expect(screen.getByText('Level 10 • Streak 5 days')).toBeInTheDocument();
      expect(screen.getByText('1000 XP')).toBeInTheDocument();
      expect(screen.getByText('player2')).toBeInTheDocument();
      expect(screen.getByText('Level 8 • Streak 3 days')).toBeInTheDocument();
      expect(screen.getByText('800 XP')).toBeInTheDocument();
      expect(screen.getByText('player3')).toBeInTheDocument();
      expect(screen.getByText('500 XP')).toBeInTheDocument();
    });
  });

  it('displays correct rank numbers for personal leaderboard', async () => {
    renderLeaderboard();
    await waitFor(() => {
      const rank1 = screen.getByText('1');
      const rank2 = screen.getByText('2');
      const rank3 = screen.getByText('3');
      expect(rank1).toBeInTheDocument();
      expect(rank2).toBeInTheDocument();
      expect(rank3).toBeInTheDocument();
    });
  });

  it('switches to Teams tab', async () => {
    const user = userEvent.setup();
    renderLeaderboard();

    await waitFor(() => {
      expect(screen.getByText('player1')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /teams/i }));

    await waitFor(() => {
      expect(screen.getByText('Team Alpha')).toBeInTheDocument();
      expect(screen.getByText('Leader: leader1 • 5 members')).toBeInTheDocument();
      expect(screen.getByText('2000 XP')).toBeInTheDocument();
      expect(screen.getByText('Team Beta')).toBeInTheDocument();
      expect(screen.getByText('Team Gamma')).toBeInTheDocument();
    });
  });

  it('shows empty message when personal leaderboard is empty', async () => {
    (missionApi.getLeaderboard as any).mockResolvedValue({ data: [] });
    renderLeaderboard();

    await waitFor(() => {
      expect(screen.getByText(/No data yet. Start completing missions!/i)).toBeInTheDocument();
    });
  });

  it('shows empty message when team leaderboard is empty', async () => {
    const user = userEvent.setup();
    (teamApi.getTeamLeaderboard as any).mockResolvedValue({ data: [] });
    renderLeaderboard();

    await waitFor(() => {
      expect(screen.getByText('player1')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('tab', { name: /teams/i }));

    await waitFor(() => {
      expect(screen.getByText(/No teams yet. Create one and start earning XP!/i)).toBeInTheDocument();
    });
  });

  it('shows error message on API failure', async () => {
    (missionApi.getLeaderboard as any).mockRejectedValueOnce({
      response: { data: { message: 'Failed to load leaderboard' } },
    });
    renderLeaderboard();

    await waitFor(() => {
      expect(screen.getByText('Failed to load leaderboard')).toBeInTheDocument();
    });
  });

  it('applies gold color for rank 1', async () => {
    renderLeaderboard();
    await waitFor(() => {
      const rank1Avatar = screen.getAllByRole('img')[0];
      expect(rank1Avatar).toHaveStyle({ backgroundColor: '#FFD700' });
    });
  });

  it('applies silver color for rank 2', async () => {
    renderLeaderboard();
    await waitFor(() => {
      const rank2Avatar = screen.getAllByRole('img')[1];
      expect(rank2Avatar).toHaveStyle({ backgroundColor: '#C0C0C0' });
    });
  });

  it('applies bronze color for rank 3', async () => {
    renderLeaderboard();
    await waitFor(() => {
      const rank3Avatar = screen.getAllByRole('img')[2];
      expect(rank3Avatar).toHaveStyle({ backgroundColor: '#CD7F32' });
    });
  });

  it('calls both APIs on mount', async () => {
    renderLeaderboard();
    await waitFor(() => {
      expect(missionApi.getLeaderboard).toHaveBeenCalledTimes(1);
      expect(teamApi.getTeamLeaderboard).toHaveBeenCalledTimes(1);
    });
  });
});