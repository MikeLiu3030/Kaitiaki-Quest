import apiClient from './client';
import type { ApiResponse } from '../types/api';
import type { TeamDetail, TeamLeaderboardEntry, CreateTeamRequest, JoinTeamRequest, LeaveTeamRequest } from '../types/team';

export const teamApi = {

  // Get the current user's team
  getMyTeam: async () => {
    const response = await apiClient.get<ApiResponse<TeamDetail>>('/api/teams/my-team');
    return response.data;
  },

  // Get detail of specified team
  getTeamById: async (teamId: number) => {
    const response = await apiClient.get<ApiResponse<TeamDetail>>(`/api/teams/${teamId}`);
    return response.data;
  },

  // create a team
  createTeam: async (data: CreateTeamRequest) => {
    const response = await apiClient.post<ApiResponse<TeamDetail>>('/api/teams', data);
    return response.data;
  },

  // join a team
  joinTeam: async (data: JoinTeamRequest) => {
    const response = await apiClient.post<ApiResponse<TeamDetail>>('/api/teams/join', data);
    return response.data;
  },

  // leave a team
  leaveTeam: async (data: LeaveTeamRequest) => {
    const response = await apiClient.post<ApiResponse<null>>('/api/teams/leave', data);
    return response.data;
  },

  // Get the team leaderboard
  getTeamLeaderboard: async () => {
    const response = await apiClient.get<ApiResponse<TeamLeaderboardEntry[]>>('/api/teams/leaderboard');
    return response.data;
  },
};