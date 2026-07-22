import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { BrowserRouter } from 'react-router-dom';
import Dashboard from '../Dashboard';
import { useAuthStore } from '../../../store/useAuthStore';
import { missionApi } from '../../../api/missionApi';
import type { EcoMission, UserStats } from '../../../types/mission';

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
    getCategories: vi.fn(),
    getMissions: vi.fn(),
    getMyMissions: vi.fn(),
    acceptMission: vi.fn(),
  },
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

const mockStats: UserStats = {
  totalMissions: 5,
  totalXP: 150,
  currentStreak: 3,
  weeklyMissions: 2,
  level: 2,
};

const mockMissions: EcoMission[] = [
  {
    id: 1,
    title: 'Recycle 10 Bottles',
    description: 'Collect and recycle 10 plastic bottles',
    basePoints: 30,
    category: 'Recycling',
    imageUrl: null,
    isDaily: true,
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 2,
    title: 'Walk 2 km',
    description: 'Walk 2 km instead of driving',
    basePoints: 25,
    category: 'Transport',
    imageUrl: null,
    isDaily: false,
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z',
  },
];

const mockCategories = ['Recycling', 'Transport', 'Energy'];

describe('Dashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (useAuthStore as any).mockReturnValue({ user: mockUser });
    (missionApi.getMyStats as any).mockResolvedValue({ data: mockStats });
    (missionApi.getCategories as any).mockResolvedValue({ data: mockCategories });
    (missionApi.getMissions as any).mockResolvedValue({ data: mockMissions });
    (missionApi.getMyMissions as any).mockResolvedValue({ data: [] });
    (missionApi.acceptMission as any).mockResolvedValue({ data: {} });
  });

  const renderDashboard = () => {
    return render(
      <BrowserRouter>
        <Dashboard />
      </BrowserRouter>
    );
  };

  it('renders welcome message with user name', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/welcome back, testuser/i)).toBeInTheDocument();
    });
  });

  it('renders stats cards', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('Total XP')).toBeInTheDocument();
      expect(screen.getByText('150')).toBeInTheDocument();
      expect(screen.getByText('Level')).toBeInTheDocument();
      expect(screen.getByText('2')).toBeInTheDocument();
    });
  });

  it('renders mission list', async () => {
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
      expect(screen.getByText('Walk 2 km')).toBeInTheDocument();
    });
  });

  it('filters missions by category', async () => {
    const user = userEvent.setup();
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    const [categoryChip] = screen.getAllByText('Transport');
    await user.click(categoryChip);

    await waitFor(() => {
      expect(missionApi.getMissions).toHaveBeenCalledWith({ category: 'Transport' });
    });
  });

  it('accepts mission when button clicked', async () => {
    const user = userEvent.setup();
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('Recycle 10 Bottles')).toBeInTheDocument();
    });

    const acceptButton = screen.getAllByRole('button', { name: /accept mission/i })[0];
    await user.click(acceptButton);

    await waitFor(() => {
      expect(missionApi.acceptMission).toHaveBeenCalledWith({ ecoMissionId: 1 });
    });
  });

  it('shows loading skeletons when loading', async () => {
    (missionApi.getMissions as any).mockImplementation(() => new Promise(() => {}));
    renderDashboard();
    await waitFor(() => {
      expect(document.querySelectorAll('.MuiSkeleton-root').length).toBeGreaterThan(0);
    });
  });

  it('shows empty message when no missions', async () => {
    (missionApi.getMissions as any).mockResolvedValue({ data: [] });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/no missions available at the moment/i)).toBeInTheDocument();
    });
  });
});