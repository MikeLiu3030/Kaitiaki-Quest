import { createBrowserRouter, Navigate } from 'react-router-dom';
import Login from '../pages/Login/Login';
import Register from '../pages/Register/Register';
import Dashboard from '../pages/Dashboard/Dashboard'
import MyMissions from '../pages/MyMissions/MyMissions';
import Teams from '../pages/Teams/Teams';
import Leaderboard from '../pages/Leaderboard/Leaderboard'
import Profile from '../pages/Profile/Profile'
import MainLayout from '../components/layout/MainLayout';
import { ProtectedRoute } from '../components/common/ProtectedRoute';
import AdminPanel from '../pages/Admin/AdminPanel';

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <Login />,
  },
  {
    path: '/register',
    element: <Register />,
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <MainLayout />
      </ProtectedRoute>
    ),
    children: [
      {
        index: true,
        element: <Navigate to="/dashboard" replace />,
      },
      {
        path: 'dashboard',
        element: <Dashboard />,
      },
      {
        path: 'my-missions',
        element: <MyMissions />,
      },
      {
        path: 'teams',
        element: <Teams />,
      },
      {
        path: 'leaderboard',
        element: <Leaderboard />,
      },
      {
        path: 'profile',
        element: <Profile />,
      },
      {
        path: 'admin',
        element: <AdminPanel />
      }
    ],
  },
  {
    path: '*',
    element: <Navigate to="/dashboard" replace />,
  },
]);