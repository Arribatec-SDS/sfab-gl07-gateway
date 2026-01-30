import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from '@arribatec-sds/arribatec-nexus-react';
import { createMultiTenantKeycloakConfig } from '@arribatec-sds/arribatec-nexus-react';
import ErrorBoundary from './components/ErrorBoundary';
import { ThemeProvider, CssBaseline } from '@mui/material';
import arribatecTheme from './theme/arribatecTheme';
import HomePage from './components/HomePage';
import LoginPage from './components/LoginPage';

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
    return (<div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        Loading authentication...
      </div>);
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
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
              <Route
                path="/"
                element={
                  <ProtectedRoute>
                    <HomePage />
                  </ProtectedRoute>
                }
              />
            </Routes>
          </Router>
        </AuthProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
