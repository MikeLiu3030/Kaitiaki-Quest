import axios from 'axios';
import { enqueueSnackbar } from 'notistack';


// Create Axios instance
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:7225',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

// Request interceptor: Automatically add Token
apiClient.interceptors.request.use(
  (config) => {
    // add token to headers 
    const authStorageStr = localStorage.getItem('autho-storage');
    if (authStorageStr) {
      try {
        const { state } = JSON.parse(authStorageStr);
        if (state && state.token){
          config.headers.Authorization = `Bearer ${state.token}`;
        }
      } catch (error) {
        // handle error
        console.error("Failed to parse auth storage", error);
        enqueueSnackbar("Failed to parse auth storage", { variant: 'error' });
      } 
      
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
      const isLoginRequest = error.config.url?.toLowerCase().includes('login');
      if (isLoginRequest) {
        enqueueSnackbar(message, { variant: 'error' });
      } else { 
          window.location.href = '/login';
          enqueueSnackbar('Your session has expired. Please log in again.', { variant: 'error' });
      }  


    } else {
      // Other error display notifications(400, 500)
      if(!error.config?.silent) {
        enqueueSnackbar(message, { variant: 'error' });
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;