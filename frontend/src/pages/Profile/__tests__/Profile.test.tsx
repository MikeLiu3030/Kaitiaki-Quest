import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import Profile from '../Profile';
import { useAuthStore } from '../../../store/useAuthStore';
import { missionApi } from '../../../api/missionApi';
import { badgeApi } from '../../../api/badgeApi';

vi.mock('@mui/icons-material', () => ({
  EmojiEvents: () => <span data-testid="icon">EmojiEvents</span>,
  LocalFireDepartment: () => <span data-testid="icon">LocalFireDepartment</span>,
  CheckCircle: () => <span data-testid="icon">CheckCircle</span>,
  TrendingUp: () => <span data-testid="icon">TrendingUp</span>,
}));

vi.mock('../../../store/useAuthStore', () => ({
  useAuthStore: vi.fn(),
}));

vi.mock('../../../api/missionApi', () => ({
  missionApi: {
    getMyStats: vi.fn(),
  },
}));

vi.mock('../../../api/badgeApi', () => ({
  badgeApi: {
    getUserBadges: vi.fn(),
    getAllBadges: vi.fn(),
  },
}));

vi.mock('../../../utils/badgeIcons', () => ({
  getBadgeIcon: vi.fn((name) => {
    const iconMap: Record<string, string> = {
      'Green Sprout': '🌱',
      'Eco Guardian': '🌿',
      'Recycling Master': '♻️',
      'Protector': '🌟',
      'Combo King': '🔥',
      'Eco Legend': '🌏',
    };
    return iconMap[name] || '🏅';
  }),
}));

const mockUser = {
  id: 'user-1',
  email: 'test@test.com',
  userName: 'testuser',
  totalXP: 150,
  level: 2,
  currentStreak: 3,
  roles: ['User'],
};

const mockStats = {
  totalMissions: 5,
  totalXP: 150,
  currentStreak: 3,
  weeklyMissions: 2,
  level: 2,
};

const mockUserBadges = [
  { id: 1, badgeId: 1, awardedDate: '2024-01-01', badge: { id: 1, name: 'Green Sprout', description: 'First badge', iconUrl: null, unlockXP: 10, isActive: true } },
  { id: 2, badgeId: 2, awardedDate: '2024-01-02', badge: { id: 2, name: 'Eco Guardian', description: 'Eco badge', iconUrl: null, unlockXP: 100, isActive: true } },
];

const mockAllBadges = [
  { id: 1, name: 'Green Sprout', description: 'First badge', iconUrl: null, unlockXP: 10, isActive: true },
  { id: 2, name: 'Eco Guardian', description: 'Eco badge', iconUrl: null, unlockXP: 100, isActive: true },
  { id: 3, name: 'Recycling Master', description: 'Recycling badge', iconUrl: null, unlockXP: 500, isActive: true },
  { id: 4, name: 'Protector', description: 'Protector badge', iconUrl: null, unlockXP: 1000, isActive: true },
  { id: 5, name: 'Combo King', description: 'Combo badge', iconUrl: null, unlockXP: 700, isActive: true },
  { id: 6, name: 'Eco Legend', description: 'Legend badge', iconUrl: null, unlockXP: 2000, isActive: true },
];

describe('Profile', () => {
  const mockLogout = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    (useAuthStore as any).mockReturnValue({
      user: mockUser,
      logout: mockLogout,
    });
    (missionApi.getMyStats as any).mockResolvedValue({ data: mockStats });
    (badgeApi.getUserBadges as any).mockResolvedValue({ data: mockUserBadges });
    (badgeApi.getAllBadges as any).mockResolvedValue({ data: mockAllBadges });
  });

  const renderProfile = () => {
    return render(
      <BrowserRouter>
        <Profile />
      </BrowserRouter>
    );
  };

  it('renders loading state', () => {
    (missionApi.getMyStats as any).mockImplementation(() => new Promise(() => {}));
    renderProfile();
    expect(screen.getByText('👤 Profile')).toBeInTheDocument();
    expect(screen.getByText('Your journey and achievements in Kaitiaki Quest.')).toBeInTheDocument();
  });

  it('renders user information', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
      expect(screen.getByText('test@test.com')).toBeInTheDocument();
      expect(screen.getByText('Level 2')).toBeInTheDocument();
    });
  });

  it('renders stats cards', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('Total XP')).toBeInTheDocument();
      expect(screen.getByText('150')).toBeInTheDocument();
      expect(screen.getByText('Completed')).toBeInTheDocument();
      expect(screen.getByText('5')).toBeInTheDocument();
      expect(screen.getByText('Streak')).toBeInTheDocument();
      expect(screen.getByText('3 🔥')).toBeInTheDocument();
      expect(screen.getByText('Level')).toBeInTheDocument();
      expect(screen.getByText('2')).toBeInTheDocument();
    });
  });

  it('renders XP progress bar', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText(/XP Progress/i)).toBeInTheDocument();
      expect(screen.getByText(/150 \/ 200 XP/i)).toBeInTheDocument();
      expect(screen.getByText(/50 XP to next level/i)).toBeInTheDocument();
    });
  });

  it('renders logout button', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /logout/i })).toBeInTheDocument();
    });
  });

  it('calls logout when logout button clicked', async () => {
    const user = userEvent.setup();
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
    });

    const logoutButton = screen.getByRole('button', { name: /logout/i });
    await user.click(logoutButton);

    expect(confirmSpy).toHaveBeenCalledWith('Are you sure you want to logout?');
    expect(mockLogout).toHaveBeenCalled();
    confirmSpy.mockRestore();
  });

  it('renders badges', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('🏅 Badges')).toBeInTheDocument();
      // 已解锁的徽章
      expect(screen.getByText('Green Sprout')).toBeInTheDocument();
      expect(screen.getByText('Eco Guardian')).toBeInTheDocument();
      // 未解锁的徽章（带进度条）
      expect(screen.getByText('Recycling Master')).toBeInTheDocument();
      expect(screen.getByText('Protector')).toBeInTheDocument();
      expect(screen.getByText('Combo King')).toBeInTheDocument();
      expect(screen.getByText('Eco Legend')).toBeInTheDocument();
    });
  });

  it('shows Unlocked chip for unlocked badges', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('Green Sprout')).toBeInTheDocument();
      const unlockedChips = screen.getAllByText('✅ Unlocked');
      expect(unlockedChips.length).toBe(2);
    });
  });

  it('shows progress bar for locked badges', async () => {
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('Recycling Master')).toBeInTheDocument();
      expect(document.querySelectorAll('.MuiLinearProgress-root').length).toBeGreaterThan(0);
    });
  });

  it('shows error message on API failure', async () => {
    (missionApi.getMyStats as any).mockRejectedValueOnce({
      response: { data: { message: 'Failed to load stats' } },
    });
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('Failed to load stats')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  it('retries loading when Retry button clicked', async () => {
    const getMyStats = missionApi.getMyStats as any;
    getMyStats.mockRejectedValueOnce({
      response: { data: { message: 'Failed to load stats' } },
    });
    getMyStats.mockResolvedValue({ data: mockStats });

    const user = userEvent.setup();
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('Failed to load stats')).toBeInTheDocument();
    });

    const retryButton = screen.getByRole('button', { name: /retry/i });
    await user.click(retryButton);

    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
      expect(screen.getByText('Total XP')).toBeInTheDocument();
    });
  });

  it('shows level progress correctly at max level', async () => {
    const maxStats = { ...mockStats, totalXP: 500, level: 5 };
    (missionApi.getMyStats as any).mockResolvedValue({ data: maxStats });
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText(/500 \/ 500 XP/i)).toBeInTheDocument();
      expect(screen.getByText(/Max level reached! 🎉/i)).toBeInTheDocument();
    });
  });

  it('shows no badges message when badges list is empty', async () => {
    (badgeApi.getAllBadges as any).mockResolvedValue({ data: [] });
    (badgeApi.getUserBadges as any).mockResolvedValue({ data: [] });
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText(/No badges yet/i)).toBeInTheDocument();
    });
  });

  it('shows default avatar letter when user name is empty', async () => {
    (useAuthStore as any).mockReturnValue({
      user: { ...mockUser, userName: '' },
      logout: mockLogout,
    });
    renderProfile();
    await waitFor(() => {
      expect(screen.getByText('U')).toBeInTheDocument();
    });
  });
});