import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    CheckCircle as CheckCircleIcon,
    CloudUpload as CloudUploadIcon,
    History as HistoryIcon,
    Logout as LogoutIcon,
    PlayArrow as PlayArrowIcon,
    Settings as SettingsIcon,
    Storage as StorageIcon,
    Transform as TransformIcon,
} from '@mui/icons-material';
import {
    AppBar,
    Avatar,
    Box,
    Button,
    Card,
    CardContent,
    Container,
    Grid,
    Paper,
    Toolbar,
    Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';

function HomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
  };

  const features = [
    {
      icon: <StorageIcon sx={{ fontSize: 40 }} />,
      title: 'Source Systems',
      description: 'Configure connections to Agresso/ABW and other ERP systems',
      path: '/admin/source-systems',
      color: '#003d4d',
    },
    {
      icon: <PlayArrowIcon sx={{ fontSize: 40 }} />,
      title: 'Run Worker',
      description: 'Manually trigger GL07 transaction processing',
      path: '/admin/run-worker',
      color: '#00717f',
    },
    {
      icon: <HistoryIcon sx={{ fontSize: 40 }} />,
      title: 'Processing Logs',
      description: 'View history of processed files and transactions',
      path: '/admin/processing-logs',
      color: '#00a0b0',
    },
    {
      icon: <SettingsIcon sx={{ fontSize: 40 }} />,
      title: 'Settings',
      description: 'Configure application settings and preferences',
      path: '/admin/settings',
      color: '#5c6970',
    },
  ];

  const workflowSteps = [
    { icon: <CloudUploadIcon />, label: 'XML files placed in inbox folder' },
    { icon: <TransformIcon />, label: 'Transform Agresso XML to Unit4 JSON' },
    { icon: <CheckCircleIcon />, label: 'Post to Unit4 REST API' },
  ];

  return (
    <Box sx={{ flexGrow: 1, minHeight: '100vh', bgcolor: '#f5f7fa' }}>
      <AppBar position="static" elevation={0} sx={{ bgcolor: '#003d4d' }}>
        <Toolbar sx={{ minHeight: 64 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', mr: 3 }}>
            <Box
              sx={{
                width: 40,
                height: 40,
                borderRadius: 1,
                bgcolor: 'white',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                mr: 2,
              }}
            >
              <TransformIcon sx={{ color: '#003d4d', fontSize: 24 }} />
            </Box>
            <Typography variant="h6" component="div" fontWeight={600} sx={{ color: 'white' }}>
              GL07 Gateway
            </Typography>
          </Box>
          <Box sx={{ flexGrow: 1 }} />
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', bgcolor: 'rgba(255,255,255,0.1)', px: 2, py: 0.75, borderRadius: 2 }}>
              <Avatar sx={{ width: 28, height: 28, mr: 1, bgcolor: '#00a0b0', fontSize: 14 }}>
                {(user?.firstName?.[0] || user?.username?.[0] || 'U').toUpperCase()}
              </Avatar>
              <Typography variant="body2" fontWeight={500} sx={{ color: 'white' }}>
                {user?.firstName || user?.username || 'User'}
              </Typography>
            </Box>
            <Button
              color="inherit"
              onClick={handleLogout}
              startIcon={<LogoutIcon />}
              size="small"
              sx={{ 
                bgcolor: 'rgba(255,255,255,0.1)', 
                '&:hover': { bgcolor: 'rgba(255,255,255,0.2)' },
                textTransform: 'none',
              }}
            >
              Logout
            </Button>
          </Box>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 4 }}>
        {/* Hero Section */}
        <Paper 
          elevation={0} 
          sx={{ 
            p: 5, 
            mb: 4, 
            background: 'linear-gradient(135deg, #004d5c 0%, #003d4d 50%, #002a36 100%)',
            color: 'white',
            borderRadius: 4,
            position: 'relative',
            overflow: 'hidden',
          }}
        >
          {/* Background decoration */}
          <Box 
            sx={{ 
              position: 'absolute', 
              top: -50, 
              right: -50, 
              width: 300, 
              height: 300, 
              borderRadius: '50%', 
              bgcolor: 'rgba(255,255,255,0.03)',
            }} 
          />
          <Box 
            sx={{ 
              position: 'absolute', 
              bottom: -100, 
              left: -100, 
              width: 400, 
              height: 400, 
              borderRadius: '50%', 
              bgcolor: 'rgba(255,255,255,0.02)',
            }} 
          />
          <Grid container spacing={3} alignItems="center" sx={{ position: 'relative', zIndex: 1 }}>
            <Grid size={{ xs: 12, md: 8 }}>
              <Typography variant="h3" fontWeight={700} gutterBottom sx={{ color: '#ffffff' }}>
                GL07 Transaction Gateway
              </Typography>
              <Typography variant="h6" sx={{ color: 'rgba(255,255,255,0.85)', mb: 3, fontWeight: 400 }}>
                Automated transformation and posting of Agresso GL07 transactions to Unit4 ERP
              </Typography>
              <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                {workflowSteps.map((step, index) => (
                  <Box 
                    key={index} 
                    sx={{ 
                      display: 'flex', 
                      alignItems: 'center', 
                      bgcolor: 'rgba(0,77,92,0.9)', 
                      px: 2, 
                      py: 1, 
                      borderRadius: 2,
                      border: '1px solid rgba(255,255,255,0.3)',
                      color: '#ffffff',
                    }}
                  >
                    <Box sx={{ color: '#ffffff', display: 'flex' }}>{step.icon}</Box>
                    <Typography variant="body2" sx={{ ml: 1, color: '#ffffff', fontWeight: 500 }}>
                      {step.label}
                    </Typography>
                  </Box>
                ))}
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }} sx={{ textAlign: 'center' }}>
              <Box 
                sx={{ 
                  width: 160, 
                  height: 160, 
                  mx: 'auto',
                  bgcolor: 'rgba(255,255,255,0.08)',
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  border: '2px solid rgba(255,255,255,0.15)',
                }}
              >
                <TransformIcon sx={{ fontSize: 80, opacity: 0.9 }} />
              </Box>
            </Grid>
          </Grid>
        </Paper>

        {/* Quick Actions */}
        <Typography variant="h5" fontWeight={600} gutterBottom sx={{ mb: 3 }}>
          Quick Actions
        </Typography>
        <Grid container spacing={3}>
          {features.map((feature) => (
            <Grid key={feature.title} size={{ xs: 12, sm: 6, md: 3 }}>
              <Card 
                sx={{ 
                  height: '100%', 
                  cursor: 'pointer',
                  transition: 'all 0.2s ease-in-out',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 4,
                  },
                }}
                onClick={() => navigate(feature.path)}
              >
                <CardContent sx={{ textAlign: 'center', py: 3 }}>
                  <Box 
                    sx={{ 
                      color: feature.color,
                      mb: 2,
                    }}
                  >
                    {feature.icon}
                  </Box>
                  <Typography variant="h6" fontWeight={600} gutterBottom>
                    {feature.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {feature.description}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>

        {/* How It Works Section */}
        <Paper elevation={0} sx={{ p: 4, mt: 4, bgcolor: 'white', borderRadius: 4 }}>
          <Typography variant="h5" fontWeight={600} gutterBottom>
            How It Works
          </Typography>
          <Grid container spacing={4} sx={{ mt: 1 }}>
            <Grid size={{ xs: 12, md: 4 }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start' }}>
                <Avatar sx={{ bgcolor: '#003d4d', mr: 2, fontWeight: 700 }}>1</Avatar>
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    Configure Source Systems
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Set up folder paths for each Agresso/ABW source system. The gateway monitors 
                    inbox folders for new XML files.
                  </Typography>
                </Box>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start' }}>
                <Avatar sx={{ bgcolor: '#00717f', mr: 2, fontWeight: 700 }}>2</Avatar>
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    Automatic Transformation
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    GL07 XML transactions are automatically transformed to Unit4 JSON format, 
                    mapping vouchers, accounts, and dimensions.
                  </Typography>
                </Box>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start' }}>
                <Avatar sx={{ bgcolor: '#00a0b0', mr: 2, fontWeight: 700 }}>3</Avatar>
                <Box>
                  <Typography variant="subtitle1" fontWeight={600}>
                    Post to Unit4
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Transformed transactions are posted to the Unit4 REST API. Processed files 
                    are archived with JSON output for auditing.
                  </Typography>
                </Box>
              </Box>
            </Grid>
          </Grid>
        </Paper>

        {/* Footer */}
        <Box sx={{ mt: 4, textAlign: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            Stena Fastigheter GL07 Gateway • Powered by Nexus Platform
          </Typography>
        </Box>
      </Container>
    </Box>
  );
}

export default HomePage;
