import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { enqueueSnackbar } from 'notistack';
import apiClient from '../client';

// ============================================================
// 1. Mock external dependencies
// ============================================================
vi.mock('notistack', () => ({
  enqueueSnackbar: vi.fn(),
}));

// ============================================================
// 2. setup localStorage mock
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

// ============================================================
// 3. Mock window.location
// ============================================================
const originalLocation = window.location;

beforeEach(() => {
  // @ts-ignore -  Delete the read-only attribute for mock
  delete window.location;
  window.location = { href: '' } as Location;
});

afterEach(() => {
  window.location = originalLocation;
});

// ============================================================
// 4. test suits
// ============================================================
describe('apiClient', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorageMock.clear();
    window.location.href = '';
  });

  // ============================================================
  // Basic configuration test
  // ============================================================
  describe('Basic', () => {
    it('he baseURL in the environment variable should be used', () => {
      expect(apiClient.defaults.baseURL).toBe(import.meta.env.VITE_API_BASE_URL || 'https://localhost:7225');
    });

    it('Should setup Content-Type 为 application/json', () => {
      expect(apiClient.defaults.headers['Content-Type']).toBe('application/json');
    });

    it('he timeout period should be set to 30000ms', () => {
      expect(apiClient.defaults.timeout).toBe(30000);
    });
  });

  // ============================================================
  // request interceptor test
  // ============================================================
  describe('request', () => {
    it('When there are tokens in localStorage, the Authorization Header should be automatically added', async () => {
      const mockState = {
        state: {
          token: 'mock-jwt-token',
        },
      };
      localStorageMock.setItem('auth-storage', JSON.stringify(mockState));

      const config = await apiClient.interceptors.request.handlers[0].fulfilled({
        headers: {},
      } as any);

      expect(config.headers.Authorization).toBe('Bearer mock-jwt-token');
    });

    it('When there is no token in localStorage, the Authorization Header should not be added', async () => {
      localStorageMock.setItem('auth-storage', '');

      const config = await apiClient.interceptors.request.handlers[0].fulfilled({
        headers: {},
      } as any);

      expect(config.headers.Authorization).toBeUndefined();
    });

    it('When auth-storage does not exist in localStorage, the Authorization Header should not be added', async () => {
      const config = await apiClient.interceptors.request.handlers[0].fulfilled({
        headers: {},
      } as any);

      expect(config.headers.Authorization).toBeUndefined();
    });

    it('When the auth-storage format is invalid, an error notification should be displayed', async () => {
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      localStorageMock.setItem('auth-storage', 'invalid-json');

      await apiClient.interceptors.request.handlers[0].fulfilled({
        headers: {},
      } as any);

      expect(consoleErrorSpy).toHaveBeenCalledWith(
        'Failed to parse auth storage',
        expect.any(Error)
      );
      expect(enqueueSnackbar).toHaveBeenCalledWith(
        'Failed to parse auth storage',
        { variant: 'error' }
      );

      consoleErrorSpy.mockRestore();
    });

    it('When a request error occurs, the error should be passed correctly ', async () => {
      const error = new Error('Request error');

      await expect(apiClient.interceptors.request.handlers[0].rejected(error)).rejects.toThrow('Request error');
    });
  });

  // ============================================================
  // response interceptor test
  // ============================================================
  describe('response interceptor', () => {
    it('When the response is successful, it should be returned directly', async () => {
      const response = { data: { success: true } };
      const result = await apiClient.interceptors.response.handlers[0].fulfilled(response);
      expect(result).toBe(response);
    });

    // ============================================================
    // 401 handle error
    // ============================================================
    describe('401 error', () => {
      it('401 error:', async () => { 
        const error = {
          response: {
            status: 401,
            data: { message: 'Unauthorized' },
          },
          config: { url: '/api/some-endpoint' },
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch{
            //An error is expected to be thrown
        }

        expect(window.location.href).toBe('/login');
        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Your session has expired. Please log in again.',
          { variant: 'error' }
        );
      });

      it('When a 401 is received and it is a login request, only an error notification should be displayed without redirection', async () => {
        const error = {
          response: {
            status: 401,
            data: { message: 'Invalid credentials' },
          },
          config: { url: '/api/auth/login' },
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown
        }

        expect(window.location.href).toBe('');
        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Invalid credentials',
          { variant: 'error' }
        );
      });

      it('When a 401 is received and the URL contains "login" (case-insensitive), only error notifications should be displayed', async () => {
        const error = {
          response: {
            status: 401,
            data: { message: 'Login failed' },
          },
          config: { url: '/api/LOGIN' },
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown
        }

        expect(window.location.href).toBe('');
        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Login failed',
          { variant: 'error' }
        );
      });

      it('401  rejected Promise', async () => {
        const error = {
          response: {
            status: 401,
            data: { message: 'Unauthorized' },
          },
          config: { url: '/api/some-endpoint' },
        };

        await expect(
          apiClient.interceptors.response.handlers[0].rejected(error)
        ).rejects.toEqual(error);
      });
    });

    // ============================================================
    // Non-401 error handling
    // ============================================================
    describe('Non-401 error handling', () => {
      it('When silent is false, an error notification should be displayed', async () => {
        const error = {
          response: {
            status: 400,
            data: { message: 'Bad request' },
          },
          config: { silent: false },
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown
        }

        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Bad request',
          { variant: 'error' }
        );
      });

      it('When silent is true, error notifications should not be displayed', async () => {
        const error = {
          response: {
            status: 400,
            data: { message: 'Bad request' },
          },
          config: { silent: true },
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown
        }

        expect(enqueueSnackbar).not.toHaveBeenCalled();
      });

      it('When the error, the response data. The message does not exist, the default error message should be displayed', async () => {
        const error = {
          response: {
            status: 500,
            data: {},
          },
          config: { silent: false },
          message: 'Network Error',
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown


        }

        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Network Error',
          { variant: 'error' }
        );
      });

      it('When error.response does not exist, error.message should be used', async () => {
        const error = {
          config: { silent: false },
          message: 'Network Error',
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown


        }

        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Network Error',
          { variant: 'error' }
        );
      });

      it('When there is a 400 error, the rejected Promise should be returned', async () => {
        const error = {
          response: {
            status: 400,
            data: { message: 'Bad request' },
          },
          config: { silent: false },
        };

        await expect(
          apiClient.interceptors.response.handlers[0].rejected(error)
        ).rejects.toEqual(error);
      });

      it('When a 500 error occurs, an error notification should be displayed and the rejected Promise should be returned', async () => {
        const error = {
          response: {
            status: 500,
            data: { message: 'Internal server error' },
          },
          config: { silent: false },
        };

        await expect(
          apiClient.interceptors.response.handlers[0].rejected(error)
        ).rejects.toEqual(error);

        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Internal server error',
          { variant: 'error' }
        );
      });

      it('An error notification should be displayed when a 404 error occurs', async () => {
        const error = {
          response: {
            status: 404,
            data: { message: 'Not found' },
          },
          config: { silent: false },
        };

        try {
          await apiClient.interceptors.response.handlers[0].rejected(error);
        } catch {
          // An error is expected to be thrown
        }

        expect(enqueueSnackbar).toHaveBeenCalledWith(
          'Not found',
          { variant: 'error' }
        );
      });
    });
  });
});