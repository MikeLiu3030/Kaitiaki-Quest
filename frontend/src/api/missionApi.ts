import apiClient from './client';
import type { ApiResponse } from '../types/api';
import type {
  EcoMission,
  UserMission,
  UserStats,
  AcceptMissionRequest,
  CompleteMissionRequest,
  CreateMissionRequest,
  UpdateMissionRequest,
} from '../types/mission';


export const missionApi = { 
    // Get all missions
    getMissions: async (params?: {category?: string; isDaily?: boolean}) => {
        const response = await apiClient.get<ApiResponse<EcoMission[]>>('/api/ecomissions', { params });
        return response.data;
    },

    //Get details of a mission
    getMissionById: async (id: number) => {
        const response = await apiClient.get<ApiResponse<EcoMission>>(`/api/ecomissions/${id}`);
        return response.data;
    },

    //Get the list of mission category
    getCategories: async () => {
    const response = await apiClient.get<ApiResponse<string[]>>('/api/ecomissions/categories');
    return response.data;
    },

    //Get My mission list
    getMyMissions: async (status?: string) => {
    const response = await apiClient.get<ApiResponse<UserMission[]>>('/api/usermissions/my-missions', {
      params: { status },
    });
    return response.data;
    },

    // Get my statistics data
    getMyStats: async () => {
    const response = await apiClient.get<ApiResponse<UserStats>>('/api/usermissions/stats');
    return response.data;
    },

    // Accept a mission
    acceptMission: async (data: AcceptMissionRequest) => {
    const response = await apiClient.post<ApiResponse<UserMission>>('/api/usermissions/accept', data);
    return response.data;
    },

    // Complete a mission
    completeMission: async (id: number, data: CompleteMissionRequest) => {
    const response = await apiClient.put<ApiResponse<UserMission>>(`/api/usermissions/${id}/complete`, data);
    return response.data;
    },

    // Cancel a mission
    abandonMission: async (id: number) => {
    const response = await apiClient.delete<ApiResponse<UserMission>>(`/api/usermissions/${id}`);
    return response.data;
    },

    // Get leaderboard
    getLeaderboard: async () => {
    const response = await apiClient.get<ApiResponse<Array<{ userName: string; totalXP: number; level: number; currentStreak: number }>>>(
      '/api/usermissions/leaderboard'
    );
    return response.data;
    },

    // Update mission
    updateMission: async (id: number, data: UpdateMissionRequest) => {
    const response = await apiClient.put<ApiResponse<EcoMission>>(`/api/ecomissions/${id}`, data);
    return response.data;
    },

    // Create mission
    createMission: async (data: CreateMissionRequest) => {
    const response = await apiClient.post<ApiResponse<EcoMission>>('/api/ecomissions', data);
    return response.data;
    },

    // Delete mission
    deleteMission: async (id: number) => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/api/ecomissions/${id}`);
    return response.data;
    },
};



