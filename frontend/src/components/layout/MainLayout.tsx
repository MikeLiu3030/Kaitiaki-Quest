import { Outlet } from 'react-router-dom';
import { Box, AppBar, Toolbar, Typography, IconButton, Container } from '@mui/material';
import { Brightness4, Brightness7 } from '@mui/icons-material';
import { useThemeContext } from '../../theme/useTheme';
import { useAuthStore } from '../../store/useAuthStore';

const MainLayout = () => {
  const { mode, toggleTheme } = useThemeContext();
  const userName = useAuthStore((state) => state.user?.userName);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="sticky" color="primary" elevation={0}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 700 }}>
            Kaitiaki Quest
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Welcome Back {' '}
            <strong style={{ color: '#f7f3f2', fontWeight: 700 }}>{userName}</strong>
            </Typography>
          <IconButton color="inherit" onClick={toggleTheme}>
            {mode === 'light' ? <Brightness4 /> : <Brightness7 />}
          </IconButton>
        </Toolbar>
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
          Kaitiaki Quest — Protecting Aotearoa, one mission at a time.
        </Typography>
      </Box>
    </Box>
  );
};

export default MainLayout;