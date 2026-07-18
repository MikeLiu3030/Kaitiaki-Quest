import apiClient from './client';
import type { ApiResponse } from '../types/api';
import type { Badge, UserBadge } from '../types/badge';

export const badgeApi = {
    getAllBadges: async () => {
        const response = await apiClient.get<ApiResponse<Badge[]>>('/api/badges');
        return response.data;
    } ,

    getUserBadges: async () => {
        const response = await apiClient.get<ApiResponse<UserBadge[]>>('/api/userbadges');
        return response.data;
    }
}