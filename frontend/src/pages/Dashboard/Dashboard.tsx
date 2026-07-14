
import  { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Typography,
  Tabs,
  Tab,
  Alert,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  EmojiEvents as EmojiEventsIcon,
  LocalFireDepartment as LocalFireDepartmentIcon,
  CheckCircle as CheckCircleIcon,
  TrendingUp as TrendingUpIcon,
} from '@mui/icons-material';
import { useAuthStore } from '../../store/useAuthStore';
import { missionApi } from '../../api/missionApi';
import type { EcoMission, UserStats } from '../../types/mission';
import MissionCard, { MissionCardSkeleton } from '../../components/missions/MissionCard';
import StatsCard from '../../components/missions/StatsCard';
import axios from 'axios';



export default function Dashboard(){
    const theme = useTheme();
    const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
    const { user } = useAuthStore();

    // State
    const [missions, setMissions] = useState<EcoMission[]>([]);
    const [acceptedMissionIds, setAcceptedMissionIds] = useState<Set<number>>(new Set());
    const [stats, setStats] = useState<UserStats | null>(null);
    const [categories, setCategories] = useState<string[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isAccepting, setIsAccepting] = useState<number | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [tabValue, setTabValue] = useState(0);
    const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  // Load data
    useEffect(() => {
    const initData = async () => {
        try {
        const [statsRes, categoriesRes] = await Promise.all([
            missionApi.getMyStats(),
            missionApi.getCategories(),
        ]);
        console.log("statsRes:", statsRes.data)
        setStats(statsRes.data);
        setCategories(categoriesRes.data ?? []);
        } catch(err:unknown) { 
            if (axios.isAxiosError(err)) {
            setError(err.response?.data?.message || err.message);
            } else {
            setError('An error occurred while fetching data.');
            }
        }
    };
    initData();
    }, []);

    // Load missions
    useEffect(() => {
    const fetchMissions = async () => {
        setIsLoading(true);
        try {
            const res = await missionApi.getMissions({ category: selectedCategory || undefined });
            setMissions(res.data ?? []);
        } catch(err:unknown) { 
            if (axios.isAxiosError(err)) {
                setError(err.response?.data?.message || err.message);
            } else {
                setError('An error occurred while fetching data.');
            }
         } 
        finally { 
            setIsLoading(false);
        }
    };
    fetchMissions();
    }, [selectedCategory]);



    // Load accepted missions by user
    useEffect(() => {
        const loadAcceptedMissions = async () => {
            try {
                const res = await missionApi.getMyMissions('Pending');
                const ids = new Set(res.data?.map((m) => m.ecoMissionId));
                setAcceptedMissionIds(ids);
            } catch (err) {
                if (axios.isAxiosError(err)) {
                    setError(err.response?.data?.message || err.message);
                } else {
                    setError('An error occurred while loading accepted mission.');
                }
            }
        };
        loadAcceptedMissions();
    }, []);

    // Accepte mission
    const handleAcceptMission = async (missionId: number) => {
        setIsAccepting(missionId);
        try {
            await missionApi.acceptMission({ ecoMissionId: missionId });
            setAcceptedMissionIds((prev) => new Set(prev).add(missionId));

            // Refresh statistics
            const statsRes = await missionApi.getMyStats();
            setStats(statsRes.data);
        } catch (err: unknown) {
            if (axios.isAxiosError(err)) {
                setError(err.response?.data?.message || err.message);
            } else {
                setError('An error occurred while accepting mission.');
            }
        } finally {
            setIsAccepting(null);
        }
    };

    // Filter mission
    const filteredMissions = missions.filter((mission) => {
        if (tabValue === 1) return mission.isDaily;
        if (tabValue === 2) return !mission.isDaily;
        return true;
    });


    // Rendering loading status
    if (isLoading) {
        return (
        <Box>
            <Typography variant="h4" sx={{ fontWeight: 700, mb: 3 }}>
            Dashboard
            </Typography>
            <Grid container spacing={3}>
            {[1, 2, 3, 4].map((i) => (
                <Grid size={{ xs: 12, sm: 6, md: 4 }} key={i}>
                <MissionCardSkeleton />
                </Grid>
            ))}
            </Grid>
        </Box>
        );
    }

    // empty messages
    const emptyMessages = {
        0: "No missions available at the moment. Check back later! 🌱",
        1: "No daily missions today. Check back tomorrow! 🌙",
        2: "No weekly missions available. 🌿"
    };

    return (
        <Box>
            {/* Welcome message */}
            <Box sx={{ mb: 4 }}>
                <Typography variant="h4" sx={{ fontWeight: 700 }}>
                Welcome back, {user?.userName || 'Kaitiaki'}! 🌿
                </Typography>
                <Typography variant="body1" color="text.secondary">
                Complete missions to earn XP and protect Aotearoa.
                </Typography>
            </Box>

            {/* stats card */}
            <Grid container spacing={2} sx={{ mb: 4 }}>
                <Grid size={{xs:6, sm:3}}>
                <StatsCard
                    icon={<EmojiEventsIcon color="primary" />}
                    label="Total XP"
                    value={stats?.totalXP || 0}
                    color="primary.main"
                />
                </Grid>
                <Grid size={{xs:6, sm:3}}>
                <StatsCard
                    icon={<LocalFireDepartmentIcon sx={{ color: '#FF6B35' }} />}
                    label="Streak"
                    value={`${stats?.currentStreak || 0} days`}
                    color="#FF6B35"
                />
                </Grid>
                <Grid size={{xs:6, sm:3}}>
                <StatsCard
                    icon={<CheckCircleIcon color="success" />}
                    label="Completed"
                    value={stats?.totalMissions || 0}
                    color="success.main"
                />
                </Grid>
                <Grid size={{xs:6, sm:3}}>
                <StatsCard
                    icon={<TrendingUpIcon color="info" />}
                    label="Level"
                    value={stats?.level || 1}
                    color="info.main"
                />
                </Grid>
            </Grid>

            {/* error message */}
            {error && (
                <Alert severity="error" sx={{ mb: 3 }}>
                {error}
                </Alert>
            )}

            {/* Category filtering */}
            <Box sx={{ display: 'flex', gap: 1, mb: 2, flexWrap: 'wrap' }}>
                <Typography
                variant="body2"
                sx={{
                    px: 2,
                    py: 1,
                    borderRadius: 2,
                    cursor: 'pointer',
                    fontWeight: selectedCategory === null ? 600 : 400,
                    bgcolor: selectedCategory === null ? 'primary.main' : 'transparent',
                    color: selectedCategory === null ? 'white' : 'text.primary',
                    '&:hover': { opacity: 0.8 },
                }}
                onClick={() => {
                    setSelectedCategory(null);
                }}
                >
                All
                </Typography>
                {categories.map((cat) => (
                <Typography
                    key={cat}
                    variant="body2"
                    sx={{
                    px: 2,
                    py: 1,
                    borderRadius: 2,
                    cursor: 'pointer',
                    fontWeight: selectedCategory === cat ? 600 : 400,
                    bgcolor: selectedCategory === cat ? 'primary.main' : 'transparent',
                    color: selectedCategory === cat ? 'white' : 'text.primary',
                    '&:hover': { opacity: 0.8 },
                    }}
                    onClick={() => {
                    setSelectedCategory(cat);
                    }}
                >
                    {cat}
                </Typography>
                ))}
            </Box>

            {/* Tab switch：All / Daily / Weekly */}
            <Tabs
                value={tabValue}
                onChange={(_, newValue) => setTabValue(newValue)}
                sx={{ mb: 2 }}
                variant={isMobile ? 'fullWidth' : 'standard'}
            >
                <Tab label="All Missions" />
                <Tab label="🔥 Daily" />
                <Tab label="Weekly" />
            </Tabs>

            {/* mission list */}
            <Box sx={{ pt: 3 }}>
            {filteredMissions.length > 0 ? (
                <Grid container spacing={3}>
                {filteredMissions.map((mission) => (
                    <Grid size={{xs:12, sm:6, md:4}} key={mission.id}>
                    <MissionCard
                        mission={mission}
                        onAccept={handleAcceptMission}
                        isAccepting={isAccepting === mission.id}
                        isAccepted={acceptedMissionIds.has(mission.id)}
                    />
                    </Grid>
                ))}
                </Grid>
            ) : (
                <Typography variant="body1" color="text.secondary" sx={{ textAlign: 'center', py: 8 }}>
                {emptyMessages[tabValue as keyof typeof emptyMessages]}
                </Typography>
            )}
            </Box>
        </Box>
    );
};


