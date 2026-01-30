import React from 'react';
import {
  Container,
  Paper,
  Typography,
  Button,
  Box,
  Alert,
  AlertTitle
} from '@mui/material';
import {
  Error as ErrorIcon,
  Refresh as RefreshIcon
} from '@mui/icons-material';

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
  errorInfo?: React.ErrorInfo;
}

interface ErrorBoundaryProps {
  children: React.ReactNode;
}

class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return {
      hasError: true,
      error
    };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    this.setState({
      error,
      errorInfo
    });
  }

  handleReload = () => {
    window.location.reload();
  };

  render() {
    if (this.state.hasError) {
      return (
        <Container component="main" maxWidth="md">
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
              <ErrorIcon sx={{ fontSize: 60, color: 'error.main', mb: 2 }} />
              
              <Typography component="h1" variant="h4" gutterBottom color="error">
                Application Error
              </Typography>
              
              <Alert severity="error" sx={{ width: '100%', mb: 3 }}>
                <AlertTitle>Something went wrong</AlertTitle>
                {this.state.error?.message && (
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    <strong>Error:</strong> {this.state.error.message}
                  </Typography>
                )}
              </Alert>
              
              {process.env.NODE_ENV === 'development' && this.state.error && (
                <Box sx={{ width: '100%', mb: 3 }}>
                  <Typography variant="h6" gutterBottom>
                    Debug Information:
                  </Typography>
                  <Paper variant="outlined" sx={{ p: 2, backgroundColor: '#f5f5f5' }}>
                    <Typography component="pre" variant="body2" sx={{ fontSize: '0.8rem', whiteSpace: 'pre-wrap' }}>
                      {this.state.error.stack}
                    </Typography>
                  </Paper>
                </Box>
              )}
              
              <Button
                onClick={this.handleReload}
                variant="contained"
                startIcon={<RefreshIcon />}
                size="large"
                sx={{ mt: 2 }}
              >
                Reload Application
              </Button>
            </Paper>
          </Box>
        </Container>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;