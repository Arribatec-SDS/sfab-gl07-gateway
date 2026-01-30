import { useState } from 'react';
import { 
  Button, 
  Box, 
  Typography, 
  Alert 
} from '@mui/material';
import { createMasterApiClient } from '../utils/api';

/**
 * Test component to demonstrate Master API error handling
 * This is for development/testing purposes only
 */
function MasterApiTest() {
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<string | null>(null);

  const testKeycloakConfigEndpoint = async () => {
    setIsLoading(true);
    setResult(null);

    try {
      const masterApiClient = createMasterApiClient();
      
      // This should trigger the specific error we're handling
      const url = `${window.location.host}${window.location.pathname}`;
      const response = await masterApiClient.get(`/keycloak-config?url=${encodeURIComponent(url)}`);
      
      setResult('Success: ' + JSON.stringify(response.data));
    } catch (error) {
      setResult('Error caught locally: ' + (error as Error).message);
      // The error will also trigger our global error handlers
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Master API Error Test
      </Typography>
      
      <Alert severity="info" sx={{ mb: 2 }}>
        This button will test the Master API keycloak-config endpoint that is used during authentication.
        If the Master API is not running, it will trigger the error handling UI.
      </Alert>
      
      <Button
        variant="contained"
        onClick={testKeycloakConfigEndpoint}
        disabled={isLoading}
        sx={{ mb: 2 }}
      >
        {isLoading ? 'Testing...' : 'Test Keycloak Config Endpoint'}
      </Button>
      
      {result && (
        <Alert severity={result.startsWith('Success') ? 'success' : 'error'} sx={{ mt: 2 }}>
          <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap' }}>
            {result}
          </Typography>
        </Alert>
      )}
      
      <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
        Expected URL: {window.location.host}{window.location.pathname}api/master/keycloak-config
      </Typography>
    </Box>
  );
}

export default MasterApiTest;