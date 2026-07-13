import type { AuthResponse, LoginRequest, RegisterRequest, User } from "../types/auth";
import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import apiClient from '../api/client';
import type { ApiResponse } from "../types/api";




interface AuthState {
    user: User | null;
    token: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    // actions
    login: (credentials: LoginRequest) => Promise<void>;
    register: (data: RegisterRequest) => Promise<void>;
    logout: () => void;
    fetchUser: () => Promise<void>;
    setLoading: (loading: boolean) => void;
}

export const useAuthStore = create<AuthState>()(
    persist(
        (set, get) => ({
            user:null,
            token: null,
            isAuthenticated: false,
            isLoading: false,

            // login
            login: async (credentials: LoginRequest) => { 
                set({ isLoading: true });
                try {
                    const response = await apiClient.post<ApiResponse<AuthResponse>>('/api/auth/login', credentials);
                    const apiResponse = response.data;
                    if (apiResponse.success && apiResponse.data) { 
                        const data = apiResponse.data;
                        // save token and user information to local storage
                        set({
                            user: {
                                id: '',
                                email: data.email,
                                userName: data.userName,
                                totalXP: data.totalXP,
                                level: data.level,
                                currentStreak: 0,
                                roles: data.roles,
                            },
                            token: data.token,
                            isAuthenticated: true,   
                            isLoading: false,                         
                        });
                    } else {
                        throw new Error(apiResponse.message || 'Login failed');
                    }
                } catch (error) {
                    set({ isLoading: false });
                    throw error;
                }
            },

            // register
            register: async (data: RegisterRequest) => {
                set({ isLoading: true });
                try {
                    const response = await apiClient.post<ApiResponse<AuthResponse>>('/api/auth/register', data);
                    const authData = response.data.data;
                    if (authData) {

                        set({
                            user: {
                                id: '',
                                email: authData.email,
                                userName: authData.userName,
                                totalXP: authData.totalXP,
                                level: authData.level,
                                currentStreak: 0,
                                roles: authData.roles,
                            },
                            token: authData.token,
                            isAuthenticated: true,
                            isLoading: false,
                        });

                    }

                } catch (error) {
                    set({ isLoading: false });
                    throw error; 
                }
            },

            // logout
            logout: () => {
                set({
                    user: null,
                    token: null,
                    isAuthenticated: false,
                    isLoading: false,
                });
            },

            // fetch user data
            fetchUser: async () => {
                try {
                    const response = await apiClient.get<ApiResponse<User>>('/api/auth/me');
                    const userData = response.data.data;
                    
                    if (userData) {
                        set({
                            user: userData,
                            isAuthenticated: true,
                        });
                    }                    
                } catch (error) {
                    console.error("Failed to fetch user.", error);
                    get().logout();
                }
            },

            // set loading state
            setLoading: (loading: boolean) => {
                set({ isLoading: loading });
            },
        }),
        {
            name: 'auth-storage', // name of the item in the storage 
            storage: createJSONStorage(() => localStorage),
            partialize: (state) => ({
                user: state.user,
                token: state.token,
                isAuthenticated: state.isAuthenticated,
            }),
        }
    )
);



