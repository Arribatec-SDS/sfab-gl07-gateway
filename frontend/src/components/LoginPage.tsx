import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    CheckCircle as CheckIcon,
    Error as ErrorIcon,
    VpnKey as KeyIcon,
    Login as LoginIcon,
    Security as SecurityIcon,
    Warning as WarningIcon
} from '@mui/icons-material';
import {
    Alert,
    AlertTitle,
    Avatar,
    Box,
    Button,
    CircularProgress,
    Container,
    Divider,
    List,
    ListItem,
    ListItemIcon,
    ListItemText,
    Paper,
    Typography
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAdminAppUrl } from '../utils/masterApiErrors';

interface StartupError {
  message: string;
  details?: string;
  isKeycloakConfigError?: boolean;
  suggestions?: string[];
  endpoint?: string;
}

function LoginPage() {
  const { 
    login, 
    isAuthenticated, 
    loading, 
    error,
    clearError, 
    retryInitialization 
  } = useAuth();
  const navigate = useNavigate();
  const [loginError, setLoginError] = useState<string | null>(null);
  const [startupError, setStartupError] = useState<StartupError | null>(null);

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/');
    }
  }, [isAuthenticated, navigate]);

  useEffect(() => {
    // Handle auth library errors from the enhanced v2.1.1 useAuth hook
    if (error) {
      console.error('Auth library error detected:', error);
      
      if (error.type === 'config' || error.endpoint?.includes('keycloak-config')) {
        setStartupError({
          message: `Failed to retrieve Keycloak configuration: ${error.message}`,
          details: `Endpoint: ${error.endpoint || 'N/A'}\nContext: ${error.context || 'N/A'}\nTimestamp: ${new Date(error.timestamp || Date.now()).toISOString()}`,
          isKeycloakConfigError: true,
          suggestions: [
            'Ensure the configuration of product and tenant is correct',
            'Verify the Master API service is running and accessible',
            'Check if the Keycloak service is running'
          ],
          endpoint: error.endpoint
        });
      } else {
        setLoginError(error.message);
      }
    }
  }, [error]);

  const handleLogin = async () => {
    try {
      setLoginError(null);
      setStartupError(null); // Clear startup errors when attempting login
      
      console.log('Attempting to login...');
      await login();
    } catch (error) {
      console.error('Login failed:', error);
      const errorMessage = error instanceof Error ? error.message : 'Login failed. Please try again.';
      setLoginError(errorMessage);
    }
  };

  const handleRefreshPage = async () => {
    console.log('Retrying authentication initialization...');
    setStartupError(null);
    setLoginError(null);
    
    try {
      // Use the new auth library retry function if available
      if (retryInitialization) {
        await retryInitialization();
        console.log('Auth library retry completed');
      } else {
        console.log('Auth library retryInitialization not available, falling back to page reload');
        window.location.reload();
      }
    } catch (error) {
      console.error('Retry failed:', error);
      setLoginError('Retry failed. Please refresh the page manually.');
    }
  };

  const handleClearError = () => {
    console.log('Clearing authentication errors...');
    
    // Clear local state
    setStartupError(null);
    setLoginError(null);
    
    // Clear auth library error if available
    if (clearError) {
      clearError();
    }
  };

  // Show loading state during authentication
  if (loading) {
    return (
      <Container component="main" maxWidth="sm">
        <Box
          sx={{
            marginTop: 8,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            minHeight: '100vh',
            justifyContent: 'center'
          }}
        >
          <Paper
            elevation={3}
            sx={{
              padding: 4,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              width: '100%'
            }}
          >
            <CircularProgress size={60} sx={{ mb: 3 }} />
            <Typography variant="h6" gutterBottom>
              Authenticating...
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Please wait while we connect to the authentication service
            </Typography>
          </Paper>
        </Box>
      </Container>
    );
  }

  return (
    <Container component="main" maxWidth="sm">
      <Box
        sx={{
          marginTop: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          minHeight: '100vh',
          justifyContent: 'center'
        }}
      >
        <Paper
          elevation={3}
          sx={{
            padding: 4,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            width: '100%'
          }}
        >
          <Avatar sx={{ m: 1, bgcolor: 'primary.main', width: 56, height: 56 }}>
            <SecurityIcon fontSize="large" />
          </Avatar>
          
          <Typography component="h1" variant="h4" gutterBottom>
            Arribatec GL07 Gateway
          </Typography>
          
          <Typography variant="h6" color="text.secondary" align="center" sx={{ mb: 3 }}>
            Please log in to continue
          </Typography>

          {/* Show startup errors (Master API failures, etc.) */}
          {startupError && (
            <Alert 
              severity="error" 
              sx={{ width: '100%', mb: 3 }} 
              icon={startupError.isKeycloakConfigError ? <KeyIcon /> : <ErrorIcon />}
            >
              <AlertTitle>
                {startupError.isKeycloakConfigError ? 'Authentication Service Error' : 'Startup Error'}
              </AlertTitle>
              <Typography variant="body2" sx={{ mt: 1 }}>
                {startupError.message}
              </Typography>
              {startupError.endpoint && (
                <Typography variant="body2" sx={{ mt: 1, fontFamily: 'monospace', fontSize: '0.8rem' }}>
                  <strong>Failed Endpoint:</strong> {startupError.endpoint}
                </Typography>
              )}
              
              {startupError.isKeycloakConfigError && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2" gutterBottom>
                    <strong>What this means:</strong> The application couldn't retrieve authentication 
                    configuration from the Master API service during startup.
                  </Typography>
                </Box>
              )}

              {startupError.suggestions && startupError.suggestions.length > 0 && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2" gutterBottom>
                    <strong>Possible solutions:</strong>
                  </Typography>
                  <List dense sx={{ mt: 1, pl: 2 }}>
                    {startupError.suggestions.map((suggestion, index) => {
                      // Check if this is the configuration suggestion
                      const isConfigSuggestion = suggestion.includes('configuration of product and tenant is correct');
                      
                      return (
                        <ListItem key={index} sx={{ pl: 0, py: 0.25 }}>
                          <ListItemIcon sx={{ minWidth: 24 }}>
                            <CheckIcon color="error" fontSize="small" />
                          </ListItemIcon>
                          <ListItemText 
                            primary={
                              isConfigSuggestion ? (
                                <Box>
                                  <Typography component="span" variant="body2" sx={{ fontSize: '0.875rem' }}>
                                    {suggestion}
                                  </Typography>
                                  <Typography component="div" variant="body2" sx={{ fontSize: '0.8rem', mt: 0.5, fontStyle: 'italic' }}>
                                    → <a 
                                      href={getAdminAppUrl()} 
                                      target="_blank" 
                                      rel="noopener noreferrer"
                                      style={{ color: 'inherit', textDecoration: 'underline' }}
                                    >
                                      Open Admin App to configure
                                    </a>
                                  </Typography>
                                </Box>
                              ) : (
                                suggestion
                              )
                            }
                            primaryTypographyProps={!isConfigSuggestion ? { variant: 'body2', fontSize: '0.875rem' } : {}}
                          />
                        </ListItem>
                      );
                    })}
                  </List>
                </Box>
              )}
            </Alert>
          )}

          {/* Show login-specific errors */}
          {loginError && !startupError && (
            <Alert severity="warning" sx={{ width: '100%', mb: 3 }} icon={<WarningIcon />}>
              <Typography variant="body2">
                <strong>Login Error:</strong> {loginError}
              </Typography>
            </Alert>
          )}
          
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, width: '100%', alignItems: 'center' }}>
            <Button
              onClick={handleLogin}
              variant="contained"
              startIcon={<LoginIcon />}
              size="large"
              sx={{
                minWidth: 200,
                py: 1.5,
              }}
              disabled={loading}
            >
              {loading ? 'Connecting...' : 'Login with Keycloak'}
            </Button>

            {/* Show refresh option when there are startup errors */}
            {startupError && (
              <>
                <Divider sx={{ width: '100%', my: 1 }}>OR</Divider>
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', justifyContent: 'center' }}>
                  <Button
                    onClick={handleClearError}
                    variant="text"
                    color="primary"
                    size="small"
                  >
                    Clear Error
                  </Button>
                  <Button
                    onClick={handleRefreshPage}
                    variant="outlined"
                    size="medium"
                    sx={{ minWidth: 200 }}
                  >
                    Refresh & Retry
                  </Button>
                </Box>
              </>
            )}

          </Box>
          
          <Typography variant="body2" color="text.secondary" align="center" sx={{ mt: 2 }}>
            You will be redirected to Keycloak for secure authentication
          </Typography>

          {/* Additional help text for startup errors */}
          {startupError && (
            <Typography variant="body2" color="text.secondary" align="center" sx={{ mt: 2 }}>
              If the error persists after refreshing, please contact your system administrator.
              {startupError.isKeycloakConfigError && (
                <><br />The authentication service may be temporarily unavailable.</>
              )}
            </Typography>
          )}
        </Paper>
      </Box>
    </Container>
  );
}

export default LoginPage;
