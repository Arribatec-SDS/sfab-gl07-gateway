import { AuthProvider, createMultiTenantKeycloakConfig, useAuth } from '@arribatec-sds/arribatec-nexus-react';
import { Alert, Box, CircularProgress, CssBaseline, ThemeProvider } from '@mui/material';
import { useEffect, useState } from 'react';
import { Navigate, Route, BrowserRouter as Router, Routes } from 'react-router-dom';
import ErrorBoundary from './components/ErrorBoundary';
import HomePage from './components/HomePage';
import LoginPage from './components/LoginPage';
import ProcessingLogsPage from './components/admin/ProcessingLogsPage';
import RunWorkerPage from './components/admin/RunWorkerPage';
import SettingsPage from './components/admin/SettingsPage';
import SourceSystemsPage from './components/admin/SourceSystemsPage';
import AdminLayout from './components/layout/AdminLayout';
import arribatecTheme from './theme/arribatecTheme';

// Helper to decode JWT token and extract roles
function decodeTokenRoles(token: string): string[] {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return [];
    
    let payload = parts[1];
    while (payload.length % 4 !== 0) {
      payload += '=';
    }
    const decoded = JSON.parse(atob(payload));
    
    // Collect roles from all possible locations in the token
    const directRoles = Array.isArray(decoded.roles) ? decoded.roles : [];
    const realmRoles = decoded.realm_access?.roles || [];
    
    return [...new Set([...directRoles, ...realmRoles])];
  } catch (e) {
    console.error('Failed to decode token roles:', e);
    return [];
  }
}

// Create auth configuration with enhanced error handling
const authConfig = createMultiTenantKeycloakConfig({
  useDynamicConfig: true,
  enableUserValidation: true,
  enableLogging: true,
  backendApiUrl: '/sfab-gl07-gateway/api',
  keycloak: {
    url: '',
    realm: '',
    clientId: ''
  }
});

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

function AdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading, getToken } = useAuth();
  const [tokenRoles, setTokenRoles] = useState<string[]>([]);
  const [rolesLoaded, setRolesLoaded] = useState(false);

  // Get roles from the JWT token
  useEffect(() => {
    const loadRoles = async () => {
      try {
        const token = await getToken();
        if (token) {
          const roles = decodeTokenRoles(token);
          setTokenRoles(roles);
          console.log('Token roles:', roles);
        }
      } catch (e) {
        console.error('Failed to get token:', e);
      }
      setRolesLoaded(true);
    };
    
    if (isAuthenticated) {
      loadRoles();
    }
  }, [isAuthenticated, getToken]);

  // Check for admin role in token
  const hasAdminRole = tokenRoles.includes('admin') || tokenRoles.includes('nexus-admin');

  // Debug: Log roles to console (remove in production)
  console.log('Admin check:', { hasAdminRole, tokenRoles });

  if (loading || (isAuthenticated && !rolesLoaded)) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!hasAdminRole) {
    return (
      <Box sx={{ p: 4 }}>
        <Alert severity="error">
          Access denied. You need administrator privileges to access this page.
        </Alert>
      </Box>
    );
  }

  return <>{children}</>;
}

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider theme={arribatecTheme}>
        <CssBaseline />
        <AuthProvider config={authConfig}>
          <Router basename="/sfab-gl07-gateway">
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              
              {/* Admin routes with layout */}
              <Route
                path="/admin"
                element={
                  <AdminRoute>
                    <AdminLayout />
                  </AdminRoute>
                }
              >
                <Route path="settings" element={<SettingsPage />} />
                <Route path="source-systems" element={<SourceSystemsPage />} />
                <Route path="logs" element={<ProcessingLogsPage />} />
                <Route path="run" element={<RunWorkerPage />} />
              </Route>

              {/* Main app with layout */}
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <AdminLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<HomePage />} />
              </Route>
            </Routes>
          </Router>
        </AuthProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
