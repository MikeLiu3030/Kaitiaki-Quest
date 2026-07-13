import axios from 'axios';
import { enqueueSnackbar } from 'notistack';

// Get Token from localStorage
const getToken = () => localStorage.getItem('token');

// Create Axios instance
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7254',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Request interceptor: Automatically add Token
apiClient.interceptors.request.use(
  (config) => {
    const token = getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor: Unified error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;
    const message = error.response?.data?.message || error.message || 'Something went wrong';

    // 401: Token expiration or invalidity, redirect to log in
    if (status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      // If it is not the login page, jump to Login
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login';
        enqueueSnackbar('Session expired. Please log in again.', { variant: 'warning' });
      }
    } else {
      // Other error display notifications
      enqueueSnackbar(message, { variant: 'error' });
    }

    return Promise.reject(error);
  }
);

export default apiClient;