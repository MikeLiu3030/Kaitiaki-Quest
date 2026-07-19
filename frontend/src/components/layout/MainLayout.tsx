import { Outlet, NavLink } from 'react-router-dom';
import {
  Box,
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Container,
  Button,
  useMediaQuery,
  useTheme,
  Menu,
  MenuItem,
  Avatar,
  Divider,
  ListItemIcon,
} from '@mui/material';
import {
  Brightness4,
  Brightness7,
  Dashboard as DashboardIcon,
  Task as TaskIcon,
  Group as GroupIcon,
  EmojiEvents as EmojiEventsIcon,
  Person as PersonIcon,
  Logout as LogoutIcon,
} from '@mui/icons-material';
import { useThemeContext } from '../../theme/useTheme';
import { useAuthStore } from '../../store/useAuthStore';
import { useEffect, useState } from 'react';
import { signalRService } from '../../services/signalRService';
import { enqueueSnackbar } from 'notistack';

export default function MainLayout() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const { mode, toggleTheme } = useThemeContext();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const token = useAuthStore((state) => state.token);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  // Monitor the changes of tokens and automatically 
  // manage the connection and disconnection of SignalR
  useEffect(() => {
    // If there is a token and you have logged in, start SignalR
    if (isAuthenticated && token) {
      signalRService.connect(token)
        .then(() => console.log('SignalR started globally!'))
        .catch(err => {
          console.error('Failed to start SignalR:', err);
          enqueueSnackbar("The real-time service connection failed. Please refresh and try again...", { variant: 'error' });
        });
    } else {
      // If there is no token (for example, the user clicks to log out), 
      // the connection will be disconnected
      signalRService.disconnect();
    }

  }, [token, isAuthenticated]);
  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    handleMenuClose();
    logout();
  };

  // Navigation item
  const navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: <DashboardIcon /> },
    { path: '/my-missions', label: 'My Missions', icon: <TaskIcon /> },
    { path: '/teams', label: 'Teams', icon: <GroupIcon /> },
    { path: '/leaderboard', label: 'Leaderboard', icon: <EmojiEventsIcon /> },
  ];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="sticky" color="primary" elevation={0}>
        <Toolbar>
          <Typography
            variant="h6"
            component={NavLink}
            to="/dashboard"
            sx={{
              fontWeight: 700,
              textDecoration: 'none',
              color: 'inherit',
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              mr:4,
            }}
          >
            🌿 Kaitiaki Quest
          </Typography>

          {/* Desktop Navigation */}
          {!isMobile && (
            <Box sx={{ display: 'flex', gap: 1, mr: 2 }}>
              {navItems.map((item) => (
                <Button
                  key={item.path}
                  component={NavLink}
                  to={item.path}
                  color="inherit"
                  sx={{
                    textTransform: 'none',
                    fontWeight: 600,
                    '&.active': {
                      bgcolor: 'rgba(255,255,255,0.15)',
                    },
                  }}
                >
                  {item.label}
                </Button>
              ))}
            </Box>
          )}
         {/* Add a placeholder Box */}
          <Box sx={{ flexGrow: 1 }} />

          <IconButton color="inherit" onClick={toggleTheme}>
            {mode === 'light' ? <Brightness4 /> : <Brightness7 />}
          </IconButton>

          {/* User avatar */}
          <IconButton onClick={handleMenuOpen} sx={{ ml: 1 }}>
            <Avatar sx={{ bgcolor: 'secondary.main', width: 32, height: 32 }}>
              {user?.userName?.charAt(0).toUpperCase() || 'U'}
            </Avatar>
          </IconButton>

          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={handleMenuClose}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          >
            <MenuItem component={NavLink} to="/profile" onClick={handleMenuClose}>
              <ListItemIcon>
                <PersonIcon fontSize="small" />
              </ListItemIcon>
              Profile
            </MenuItem>
            <Divider />
            <MenuItem onClick={handleLogout}>
              <ListItemIcon>
                <LogoutIcon fontSize="small" color="error" />
              </ListItemIcon>
              <Typography color="error">Logout</Typography>
            </MenuItem>
          </Menu>
        </Toolbar>

        {/* Mobile Navigation */}
        {isMobile && (
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-around',
              py: 1,
              bgcolor: 'rgba(0,0,0,0.05)',
            }}
          >
            {navItems.map((item) => (
              <Button
                key={item.path}
                component={NavLink}
                to={item.path}
                color="inherit"
                size="small"
                sx={{
                  textTransform: 'none',
                  fontSize: '0.65rem',
                  flexDirection: 'column',
                  flex:1,
                  minWidth: 0,
                  px: 0.5,
                  whiteSpace: 'nowrap',
                  gap: 0.5,
                  '&.active': {
                    color: 'secondary.main',
                  },
                }}
              >
                {item.icon}
                {item.label}
              </Button>
            ))}
          </Box>
        )}
      </AppBar>

      <Container maxWidth="lg" sx={{ flex: 1, py: 4 }}>
        <Outlet />
      </Container>

      <Box
        component="footer"
        sx={{
          py: 2,
          textAlign: 'center',
          borderTop: '1px solid',
          borderColor: 'divider',
          bgcolor: 'background.paper',
        }}
      >
        <Typography variant="body2" color="text.secondary">
          🌱 Kaitiaki Quest — Protecting Aotearoa, one mission at a time.
        </Typography>
      </Box>
    </Box>
  );
};

