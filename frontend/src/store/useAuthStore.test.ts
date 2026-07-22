import { describe, it, expect, vi, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { useAuthStore } from './useAuthStore';
import apiClient from '../api/client';
import type { ApiResponse } from '../types/api';
import type { AuthResponse, User } from '../types/auth';

// ============================================================
// localStorage mock
// ============================================================
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: vi.fn((key: string) => {
      delete store[key];
    }),
    clear: vi.fn(() => {
      store = {};
    }),
  };
})();

Object.defineProperty(window, 'localStorage', { value: localStorageMock });


const getStore = () => useAuthStore.getState();

// ============================================================
// test suits
// ============================================================
describe('useAuthStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorageMock.clear();
    useAuthStore.setState({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
    });
  });

  // ============================================================
  // login test
  // ============================================================
  describe('login', () => {
    const mockCredentials = {
      email: 'test@test.com',
      password: 'Test123!',
    };

    const mockAuthResponse: ApiResponse<AuthResponse> = {
      success: true,
      message: 'Login successful',
      data: {
        token: 'mock-jwt-token',
        email: 'test@test.com',
        userName: 'testuser',
        totalXP: 0,
        level: 1,
        roles: ['User'],
      },
    };

    const mockUserResponse: ApiResponse<User> = {
      success: true,
      message: 'User fetched',
      data: {
        id: 'user-1',
        email: 'test@test.com',
        userName: 'testuser',
        totalXP: 0,
        level: 1,
        currentStreak: 0,
        roles: ['User'],
      },
    };

    it('should login successfully and store token', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({
        data: mockAuthResponse,
      });
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({
        data: mockUserResponse,
      });

      const { login } = getStore();

      await act(async () => {
        await login(mockCredentials);
      });

      const state = getStore();
      expect(state.isAuthenticated).toBe(true);
      expect(state.token).toBe('mock-jwt-token');
      expect(state.user?.userName).toBe('testuser');
      expect(postSpy).toHaveBeenCalledWith('/api/auth/login', mockCredentials, { silent: true });
      expect(getSpy).toHaveBeenCalledWith('/api/auth/me');

      postSpy.mockRestore();
      getSpy.mockRestore();
    });

    it('should handle login failure when API returns success=false', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({
        data: {
          success: false,
          message: 'Invalid credentials',
          data: null,
        },
      });

      const { login } = getStore();

      await expect(
        act(async () => {
          await login(mockCredentials);
        })
      ).rejects.toThrow('Invalid credentials');

      const state = getStore();
      expect(state.isAuthenticated).toBe(false);
      expect(state.token).toBeNull();
      expect(state.isLoading).toBe(false);

      postSpy.mockRestore();
    });

    it('should handle network error during login', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockRejectedValueOnce(new Error('Network error'));

      const { login } = getStore();

      await expect(
        act(async () => {
          await login(mockCredentials);
        })
      ).rejects.toThrow('Network error');

      const state = getStore();
      expect(state.isAuthenticated).toBe(false);
      expect(state.isLoading).toBe(false);

      postSpy.mockRestore();
    });
  });

  // ============================================================
  // register test
  // ============================================================
  describe('register', () => {
    const mockRegisterData = {
      userName: 'newuser',
      email: 'new@test.com',
      password: 'Test123!',
    };

    const mockAuthResponse: ApiResponse<AuthResponse> = {
      success: true,
      message: 'Registration successful',
      data: {
        token: 'mock-jwt-token',
        email: 'new@test.com',
        userName: 'newuser',
        totalXP: 0,
        level: 1,
        roles: ['User'],
      },
    };

    const mockUserResponse: ApiResponse<User> = {
      success: true,
      message: 'User fetched',
      data: {
        id: 'user-2',
        email: 'new@test.com',
        userName: 'newuser',
        totalXP: 0,
        level: 1,
        currentStreak: 0,
        roles: ['User'],
      },
    };

    it('should register successfully and store token', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockResolvedValueOnce({
        data: mockAuthResponse,
      });
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({
        data: mockUserResponse,
      });

      const { register } = getStore();

      await act(async () => {
        await register(mockRegisterData);
      });

      const state = getStore();
      expect(state.isAuthenticated).toBe(true);
      expect(state.token).toBe('mock-jwt-token');
      expect(state.user?.userName).toBe('newuser');
      expect(postSpy).toHaveBeenCalledWith('/api/auth/register', mockRegisterData, { silent: true });

      postSpy.mockRestore();
      getSpy.mockRestore();
    });

    it('should handle registration failure', async () => {
      const postSpy = vi.spyOn(apiClient, 'post').mockRejectedValueOnce({
        response: { data: { message: 'Email already exists' } },
      });

      const { register } = getStore();

      await expect(
        act(async () => {
          await register(mockRegisterData);
        })
      ).rejects.toThrow();

      const state = getStore();
      expect(state.isAuthenticated).toBe(false);
      expect(state.isLoading).toBe(false);

      postSpy.mockRestore();
    });
  });

  // ============================================================
  // logout test
  // ============================================================
  describe('logout', () => {
    it('should clear user state and token', () => {
      useAuthStore.setState({
        user: {
          id: '1',
          email: 'test@test.com',
          userName: 'testuser',
          totalXP: 100,
          level: 2,
          currentStreak: 5,
          roles: ['User'],
        },
        token: 'mock-token',
        isAuthenticated: true,
        isLoading: false,
      });

      const { logout } = getStore();

      act(() => {
        logout();
      });

      const state = getStore();
      expect(state.isAuthenticated).toBe(false);
      expect(state.token).toBeNull();
      expect(state.user).toBeNull();
    });
  });

  // ============================================================
  // fetchUser test
  // ============================================================
  describe('fetchUser', () => {
    const mockUserResponse: ApiResponse<User> = {
      success: true,
      message: 'User fetched',
      data: {
        id: 'user-1',
        email: 'test@test.com',
        userName: 'testuser',
        totalXP: 150,
        level: 2,
        currentStreak: 3,
        roles: ['User'],
      },
    };

    it('should fetch and update user data', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockResolvedValueOnce({
        data: mockUserResponse,
      });

      const { fetchUser } = getStore();

      await act(async () => {
        await fetchUser();
      });

      const state = getStore();
      expect(state.user?.userName).toBe('testuser');
      expect(state.user?.totalXP).toBe(150);
      expect(state.isAuthenticated).toBe(true);

      getSpy.mockRestore();
    });

    it('should logout on fetch failure (401)', async () => {
      const getSpy = vi.spyOn(apiClient, 'get').mockRejectedValueOnce({
        response: { status: 401 },
      });

      useAuthStore.setState({
        user: {
          id: '1',
          email: 'test@test.com',
          userName: 'testuser',
          totalXP: 100,
          level: 2,
          currentStreak: 5,
          roles: ['User'],
        },
        token: 'mock-token',
        isAuthenticated: true,
        isLoading: false,
      });

      const { fetchUser } = getStore();

      await act(async () => {
        await fetchUser();
      });

      const state = getStore();
      expect(state.isAuthenticated).toBe(false);
      expect(state.token).toBeNull();
      expect(state.user).toBeNull();

      getSpy.mockRestore();
    });
  });

  // ============================================================
  // setLoading test
  // ============================================================
  describe('setLoading', () => {
    it('should set loading state', () => {
      const { setLoading } = getStore();

      act(() => {
        setLoading(true);
      });
      let state = getStore();
      expect(state.isLoading).toBe(true);

      act(() => {
        setLoading(false);
      });
      state = getStore();
      expect(state.isLoading).toBe(false);
    });
  });
});