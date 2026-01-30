import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import { useState, useEffect } from 'react';
import axios from 'axios';
import { buildApiPath } from '@/utils/api';
import MasterApiTest from './MasterApiTest';
import {
  Container,
  Typography,
  Card,
  CardContent,
  Button,
  CircularProgress,
  Alert,
  Box,
  Grid,
  Chip,
  AppBar,
  Toolbar,
  Avatar,
  Divider
} from '@mui/material';
import {
  Logout as LogoutIcon,
  Person as PersonIcon,
  Api as ApiIcon,
  Security as SecurityIcon
} from '@mui/icons-material';

interface ApiData {
  message: string;
  user: any;
  context?: {
    tenantId?: string;
    currentTenant?: string;
    tenantShortName?: string;
    productId?: string;
    headers?: {
      tenantId?: string;
      productId?: string;
      tenantShortName?: string;
    };
    host?: string;
    subdomain?: string;
  };
  allClaims?: Array<{ Type: string; Value: string }>;
  tenantAccess?: any[];
  timestamp: string;
}

function HomePage() {
  const { user, logout, getToken } = useAuth();
  const [apiData, setApiData] = useState<ApiData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = await getToken();
        // Use buildApiPath to dynamically construct the API path
        const response = await axios.get(buildApiPath('/user'), {
          headers: {
            Authorization: `Bearer ${token}`
          }
        });

        setApiData(response.data);
        setError(null);
      } catch (err: any) {
        console.error('Failed to fetch data:', err);
        setError(err.response?.data?.message || err.message || 'Failed to fetch data');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const handleLogout = () => {
    logout();
  };

  return (
    <Box sx={{ flexGrow: 1 }}>
      <AppBar position="static">
        <Toolbar>
          <Avatar sx={{ mr: 2, bgcolor: 'secondary.main' }}>
            <PersonIcon />
          </Avatar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1, color: 'white' }}>
            Stena Fastigheter – GL07 Gateway
          </Typography>
          <Button
            color="inherit"
            onClick={handleLogout}
            startIcon={<LogoutIcon />}
          >
            Logout
          </Button>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Welcome to the Stena Fastigheter – GL07 Gateway
        </Typography>

        {/* Master API Test Section (for development) */}
        {process.env.NODE_ENV === 'development' && (
          <>
            <Card sx={{ mb: 3, bgcolor: 'warning.light', color: 'warning.contrastText' }}>
              <CardContent>
                <MasterApiTest />
              </CardContent>
            </Card>
            <Divider sx={{ mb: 3 }} />
          </>
        )}

        <Grid container spacing={3}>
          {/* User Information Card */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <PersonIcon sx={{ mr: 1, color: 'primary.main' }} />
                  <Typography variant="h5" component="h2">
                    User Information
                  </Typography>
                </Box>
                {user ? (
                  <Box>
                    <Typography variant="body1" gutterBottom>
                      <strong>Username:</strong> {user.username}
                    </Typography>
                    <Typography variant="body1" gutterBottom>
                      <strong>Email:</strong> {user.email}
                    </Typography>
                    <Typography variant="body1" gutterBottom>
                      <strong>First Name:</strong> {user.firstName}
                    </Typography>
                    <Typography variant="body1" gutterBottom>
                      <strong>Last Name:</strong> {user.lastName}
                    </Typography>
                  </Box>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    No user information available
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* API Response Card */}
          <Grid size={{ xs: 12, md: 6 }}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <ApiIcon sx={{ mr: 1, color: 'primary.main' }} />
                  <Typography variant="h5" component="h2">
                    API Response
                  </Typography>
                </Box>
                
                {loading && (
                  <Box sx={{ display: 'flex', justifyContent: 'center', p: 2 }}>
                    <CircularProgress />
                  </Box>
                )}
                
                {error && (
                  <Alert severity="error" sx={{ mb: 2 }}>
                    {error}
                  </Alert>
                )}
                
                {apiData && (
                  <Box>
                    <Typography variant="body1" gutterBottom>
                      <strong>Message:</strong> {apiData.message}
                    </Typography>
                    <Typography variant="body1" gutterBottom>
                      <strong>Timestamp:</strong> {new Date(apiData.timestamp).toLocaleString()}
                    </Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Context Information Card */}
          {apiData?.context && (
            <Grid size={12}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <SecurityIcon sx={{ mr: 1, color: 'primary.main' }} />
                    <Typography variant="h5" component="h2">
                      Context Information
                    </Typography>
                  </Box>
                  
                  <Grid container spacing={2}>
                    {/* Tenant Information */}
                    <Grid size={{ xs: 12, md: 6 }}>
                      <Typography variant="h6" gutterBottom color="primary">
                        Tenant Context
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        <strong>Tenant ID:</strong> {apiData.context.tenantId || 'N/A'}
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        <strong>Current Tenant:</strong> {apiData.context.currentTenant || 'N/A'}
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        <strong>Tenant Short Name:</strong> {apiData.context.tenantShortName || 'N/A'}
                      </Typography>
                    </Grid>

                    {/* Product Information */}
                    <Grid size={{ xs: 12, md: 6 }}>
                      <Typography variant="h6" gutterBottom color="primary">
                        Product Context
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        <strong>Product ID:</strong> {apiData.context.productId || 'N/A'}
                      </Typography>
                    </Grid>

                    {/* Host Information */}
                    <Grid size={{ xs: 12, md: 6 }}>
                      <Typography variant="h6" gutterBottom color="primary">
                        Host Information
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        <strong>Host:</strong> {apiData.context.host || 'N/A'}
                      </Typography>
                      <Typography variant="body2" gutterBottom>
                        <strong>Subdomain:</strong> {apiData.context.subdomain || 'N/A'}
                      </Typography>
                    </Grid>

                    {/* Headers */}
                    {apiData.context.headers && (
                      <Grid size={{ xs: 12, md: 6 }}>
                        <Typography variant="h6" gutterBottom color="primary">
                          Request Headers
                        </Typography>
                        <Typography variant="body2" gutterBottom>
                          <strong>X-Tenant-Id:</strong> {apiData.context.headers.tenantId || 'N/A'}
                        </Typography>
                        <Typography variant="body2" gutterBottom>
                          <strong>X-Product-Id:</strong> {apiData.context.headers.productId || 'N/A'}
                        </Typography>
                        <Typography variant="body2" gutterBottom>
                          <strong>X-Tenant-ShortName:</strong> {apiData.context.headers.tenantShortName || 'N/A'}
                        </Typography>
                      </Grid>
                    )}
                  </Grid>
                </CardContent>
              </Card>
            </Grid>
          )}

          {/* User Details from API */}
          {apiData?.user && (
            <Grid size={{ xs: 12, md: 6 }}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    User Details from API
                  </Typography>
                  <Box
                    component="pre"
                    sx={{
                      backgroundColor: 'grey.100',
                      p: 2,
                      borderRadius: 1,
                      overflow: 'auto',
                      fontSize: '0.875rem',
                      fontFamily: 'monospace'
                    }}
                  >
                    {JSON.stringify(apiData.user, null, 2)}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          )}

          {/* All JWT Claims */}
          {apiData?.allClaims && apiData.allClaims.length > 0 && (
            <Grid size={{ xs: 12, md: 6 }}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    All JWT Claims
                  </Typography>
                  <Box
                    component="pre"
                    sx={{
                      backgroundColor: 'grey.100',
                      p: 2,
                      borderRadius: 1,
                      overflow: 'auto',
                      maxHeight: 400,
                      fontSize: '0.875rem',
                      fontFamily: 'monospace'
                    }}
                  >
                    {JSON.stringify(apiData.allClaims, null, 2)}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          )}

          {/* Tenant Access */}
          {apiData?.tenantAccess && apiData.tenantAccess.length > 0 && (
            <Grid size={12}>
              <Card>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <SecurityIcon sx={{ mr: 1, color: 'primary.main' }} />
                    <Typography variant="h6">
                      Tenant Access
                    </Typography>
                    <Chip 
                      label={apiData.tenantAccess.length} 
                      color="primary" 
                      size="small" 
                      sx={{ ml: 1 }}
                    />
                  </Box>
                  <Box
                    component="pre"
                    sx={{
                      backgroundColor: 'grey.100',
                      p: 2,
                      borderRadius: 1,
                      overflow: 'auto',
                      maxHeight: 300,
                      fontSize: '0.875rem',
                      fontFamily: 'monospace'
                    }}
                  >
                    {JSON.stringify(apiData.tenantAccess, null, 2)}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          )}
        </Grid>
      </Container>
    </Box>
  );
}

export default HomePage;
