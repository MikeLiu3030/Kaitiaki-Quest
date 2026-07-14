import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../../store/useAuthStore';
import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

export function ProtectedRoute({ children }:ProtectedRouteProps) {
  
    const {isAuthenticated, isLoading} = useAuthStore(); 

    if (isLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh' }}>
                <CircularProgress />
            </Box>
        )
    }
    
    // If the user is not authenticated, redirect to the login page
    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    return <>{children}</>;
};