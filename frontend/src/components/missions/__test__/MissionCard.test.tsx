import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import MissionCard, { MissionCardSkeleton } from '../MissionCard';
import type { EcoMission } from '../../../types/mission';

const mockMission: EcoMission = {
  id: 1,
  title: 'Recycle 10 Plastic Bottles',
  description: 'Collect and properly recycle 10 plastic bottles',
  basePoints: 30,
  category: 'Recycling',
  imageUrl: 'https://example.com/image.jpg',
  isDaily: true,
  isActive: true,
  createdAt: '2024-01-01T00:00:00Z',
};

const renderWithTheme = (component: React.ReactNode) => {
  return render(
    <ThemeProvider theme={createTheme()}>
      {component}
    </ThemeProvider>
  );
};

describe('MissionCard', () => {
  it('renders mission details correctly', () => {
    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={false}
      />
    );

    expect(screen.getByText('Recycle 10 Plastic Bottles')).toBeInTheDocument();
    expect(screen.getByText('Collect and properly recycle 10 plastic bottles')).toBeInTheDocument();
    expect(screen.getByText('Recycling')).toBeInTheDocument();
    expect(screen.getByText('🔥 Daily')).toBeInTheDocument();
    expect(screen.getByText('+30 XP')).toBeInTheDocument();
  });

  it('renders image when imageUrl is provided', () => {
    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={false}
      />
    );

    const image = screen.getByRole('img');
    expect(image).toHaveAttribute('src', 'https://example.com/image.jpg');
    expect(image).toHaveAttribute('alt', 'Recycle 10 Plastic Bottles');
  });

  it('renders placeholder when imageUrl is null', () => {
    const missionWithoutImage = { ...mockMission, imageUrl: null };
    renderWithTheme(
      <MissionCard
        mission={missionWithoutImage}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={false}
      />
    );

    expect(screen.getByText('🌿')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });

  it('does not show Daily chip when isDaily is false', () => {
    const missionNotDaily = { ...mockMission, isDaily: false };
    renderWithTheme(
      <MissionCard
        mission={missionNotDaily}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={false}
      />
    );

    expect(screen.queryByText('🔥 Daily')).not.toBeInTheDocument();
  });

  it('shows Accept button when not accepted', () => {
    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={false}
      />
    );

    expect(screen.getByRole('button', { name: /accept mission/i })).toBeInTheDocument();
    expect(screen.getByRole('button')).not.toBeDisabled();
  });

  it('shows Accepted status when isAccepted is true', () => {
    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={true}
      />
    );

    expect(screen.getByRole('button', { name: /accepted/i })).toBeInTheDocument();
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('shows Processing status when isAccepting is true', () => {
    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={vi.fn()}
        isAccepting={true}
        isAccepted={false}
      />
    );

    expect(screen.getByRole('button', { name: /processing/i })).toBeInTheDocument();
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('calls onAccept with mission id when button clicked', async () => {
    const handleAccept = vi.fn();
    const user = userEvent.setup();

    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={handleAccept}
        isAccepting={false}
        isAccepted={false}
      />
    );

    await user.click(screen.getByRole('button', { name: /accept mission/i }));
    expect(handleAccept).toHaveBeenCalledTimes(1);
    expect(handleAccept).toHaveBeenCalledWith(1);
  });

  it('applies correct category color', () => {
    renderWithTheme(
      <MissionCard
        mission={mockMission}
        onAccept={vi.fn()}
        isAccepting={false}
        isAccepted={false}
      />
    );

    const chip = screen.getByText('Recycling').closest('.MuiChip-root');
    expect(chip).toHaveStyle({ backgroundColor: '#4CAF50' });
  });
});

describe('MissionCardSkeleton', () => {
  it('renders skeleton loading state', () => {
    const { container } = renderWithTheme(<MissionCardSkeleton />);
    const skeletons = container.querySelectorAll('.MuiSkeleton-root');
    expect(skeletons).toHaveLength(7);
  });
});