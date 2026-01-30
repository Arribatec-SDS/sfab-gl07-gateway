import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    CheckCircle as CheckCircleIcon,
    Error as ErrorIcon,
    Folder as FolderIcon,
    PlayArrow as PlayArrowIcon,
    Refresh as RefreshIcon,
    Schedule as ScheduleIcon,
    Stop as StopIcon,
} from '@mui/icons-material';
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Divider,
    FormControl,
    FormControlLabel,
    InputLabel,
    LinearProgress,
    List,
    ListItem,
    ListItemIcon,
    ListItemText,
    MenuItem,
    Paper,
    Select,
    Snackbar,
    Switch,
    Typography,
} from '@mui/material';
import { useCallback, useEffect, useState } from 'react';
import { createApiClient } from '../../utils/api';

interface SourceSystem {
  id: number;
  systemCode: string;
  systemName: string;
  folderPath: string;
  filePattern: string;
  transformerType: string;
  isActive: boolean;
  description: string | null;
}

interface TaskRunResponse {
  taskExecutionId: string;
  status: string;
  message: string;
}

interface TaskStatus {
  taskExecutionId: string;
  status: 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';
  startedAt: string | null;
  completedAt: string | null;
  message: string | null;
  progress: number;
}

export default function RunWorkerPage() {
  const { getToken } = useAuth();
  const [sourceSystems, setSourceSystems] = useState<SourceSystem[]>([]);
  const [selectedSystem, setSelectedSystem] = useState<string>('');
  const [dryRun, setDryRun] = useState(false);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [taskStatus, setTaskStatus] = useState<TaskStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [pollingInterval, setPollingInterval] = useState<NodeJS.Timeout | null>(null);

  // Fetch source systems for dropdown
  const fetchSourceSystems = useCallback(async () => {
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const response = await apiClient.get<SourceSystem[]>('/sourcesystems');
      setSourceSystems(response.data.filter(s => s.isActive));
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load source systems';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [getToken]);

  useEffect(() => {
    fetchSourceSystems();
  }, [fetchSourceSystems]);

  // Cleanup polling on unmount
  useEffect(() => {
    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [pollingInterval]);

  // Poll for task status
  const pollTaskStatus = useCallback(async (taskExecutionId: string) => {
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const response = await apiClient.get<TaskStatus>(`/worker/status/${taskExecutionId}`);
      setTaskStatus(response.data);

      // Stop polling if task is completed or failed
      if (['Completed', 'Failed', 'Cancelled'].includes(response.data.status)) {
        if (pollingInterval) {
          clearInterval(pollingInterval);
          setPollingInterval(null);
        }
        setRunning(false);

        if (response.data.status === 'Completed') {
          setSuccess('Worker completed successfully!');
        } else if (response.data.status === 'Failed') {
          setError(`Worker failed: ${response.data.message || 'Unknown error'}`);
        }
      }
    } catch (err) {
      console.error('Failed to poll task status:', err);
    }
  }, [getToken, pollingInterval]);

  const handleStartWorker = async () => {
    setRunning(true);
    setTaskStatus(null);
    setError(null);

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const params = {
        sourceSystemCode: selectedSystem || null,
        dryRun: dryRun,
      };

      const response = await apiClient.post<TaskRunResponse>('/worker/run', params);

      setTaskStatus({
        taskExecutionId: response.data.taskExecutionId,
        status: 'Running',
        startedAt: new Date().toISOString(),
        completedAt: null,
        message: 'Task started...',
        progress: 0,
      });

      // Start polling for status
      const interval = setInterval(() => {
        pollTaskStatus(response.data.taskExecutionId);
      }, 2000);
      setPollingInterval(interval);

    } catch (err: unknown) {
      setRunning(false);
      const errorMessage = err instanceof Error ? err.message : 'Failed to start worker';
      setError(errorMessage);
    }
  };

  const handleStopWorker = async () => {
    if (!taskStatus?.taskExecutionId) return;

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      await apiClient.post(`/worker/cancel/${taskStatus.taskExecutionId}`);
      setSuccess('Cancellation requested');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to cancel worker';
      setError(errorMessage);
    }
  };

  const getStatusColor = (status: string): 'success' | 'error' | 'warning' | 'info' | 'default' => {
    switch (status) {
      case 'Completed': return 'success';
      case 'Failed': return 'error';
      case 'Cancelled': return 'warning';
      case 'Running': return 'info';
      default: return 'default';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed': return <CheckCircleIcon color="success" />;
      case 'Failed': return <ErrorIcon color="error" />;
      case 'Running': return <CircularProgress size={20} />;
      default: return <ScheduleIcon />;
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h5" component="h1">
          Run GL07 Worker
        </Typography>
        <Button
          startIcon={<RefreshIcon />}
          variant="outlined"
          onClick={fetchSourceSystems}
        >
          Refresh
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 3, flexDirection: { xs: 'column', md: 'row' } }}>
        {/* Configuration Panel */}
        <Paper sx={{ p: 3, flex: 1 }}>
          <Typography variant="h6" gutterBottom>
            Configuration
          </Typography>
          <Divider sx={{ mb: 3 }} />

          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            <FormControl fullWidth>
              <InputLabel>Source System</InputLabel>
              <Select
                value={selectedSystem}
                label="Source System"
                onChange={(e) => setSelectedSystem(e.target.value)}
                disabled={running}
              >
                <MenuItem value="">
                  <em>All Active Systems</em>
                </MenuItem>
                {sourceSystems.map((system) => (
                  <MenuItem key={system.id} value={system.systemCode}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <FolderIcon fontSize="small" />
                      {system.systemName}
                      <Chip
                        label={system.systemCode}
                        size="small"
                        sx={{ ml: 1 }}
                      />
                    </Box>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControlLabel
              control={
                <Switch
                  checked={dryRun}
                  onChange={(e) => setDryRun(e.target.checked)}
                  disabled={running}
                />
              }
              label={
                <Box>
                  <Typography variant="body1">Dry Run Mode</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Validate files without posting to Unit4
                  </Typography>
                </Box>
              }
            />

            <Divider />

            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button
                variant="contained"
                color="primary"
                size="large"
                startIcon={running ? <CircularProgress size={20} color="inherit" /> : <PlayArrowIcon />}
                onClick={handleStartWorker}
                disabled={running}
                fullWidth
              >
                {running ? 'Running...' : 'Start Worker'}
              </Button>
              {running && (
                <Button
                  variant="outlined"
                  color="error"
                  size="large"
                  startIcon={<StopIcon />}
                  onClick={handleStopWorker}
                >
                  Cancel
                </Button>
              )}
            </Box>
          </Box>
        </Paper>

        {/* Status Panel */}
        <Paper sx={{ p: 3, flex: 1 }}>
          <Typography variant="h6" gutterBottom>
            Execution Status
          </Typography>
          <Divider sx={{ mb: 3 }} />

          {!taskStatus ? (
            <Box sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
              <ScheduleIcon sx={{ fontSize: 48, mb: 2, opacity: 0.5 }} />
              <Typography>
                Configure parameters and click "Start Worker" to begin processing.
              </Typography>
            </Box>
          ) : (
            <Box>
              <Card variant="outlined" sx={{ mb: 2 }}>
                <CardContent>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                    {getStatusIcon(taskStatus.status)}
                    <Typography variant="h6">
                      {taskStatus.status}
                    </Typography>
                    <Chip
                      label={taskStatus.status}
                      size="small"
                      color={getStatusColor(taskStatus.status)}
                    />
                  </Box>

                  {taskStatus.status === 'Running' && (
                    <Box sx={{ mb: 2 }}>
                      <LinearProgress />
                      <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
                        Processing files...
                      </Typography>
                    </Box>
                  )}

                  <List dense>
                    <ListItem>
                      <ListItemIcon>
                        <ScheduleIcon fontSize="small" />
                      </ListItemIcon>
                      <ListItemText
                        primary="Task ID"
                        secondary={taskStatus.taskExecutionId}
                      />
                    </ListItem>
                    {taskStatus.startedAt && (
                      <ListItem>
                        <ListItemIcon>
                          <PlayArrowIcon fontSize="small" />
                        </ListItemIcon>
                        <ListItemText
                          primary="Started"
                          secondary={new Date(taskStatus.startedAt).toLocaleString()}
                        />
                      </ListItem>
                    )}
                    {taskStatus.completedAt && (
                      <ListItem>
                        <ListItemIcon>
                          <CheckCircleIcon fontSize="small" />
                        </ListItemIcon>
                        <ListItemText
                          primary="Completed"
                          secondary={new Date(taskStatus.completedAt).toLocaleString()}
                        />
                      </ListItem>
                    )}
                    {taskStatus.message && (
                      <ListItem>
                        <ListItemText
                          primary="Message"
                          secondary={taskStatus.message}
                        />
                      </ListItem>
                    )}
                  </List>
                </CardContent>
              </Card>

              {dryRun && (
                <Alert severity="info" sx={{ mb: 2 }}>
                  <strong>Dry Run Mode:</strong> Files will be validated but NOT posted to Unit4.
                </Alert>
              )}

              {taskStatus.status === 'Completed' && (
                <Alert severity="success">
                  Processing completed successfully. Check the Processing Logs for details.
                </Alert>
              )}
            </Box>
          )}
        </Paper>
      </Box>

      {/* Info Card */}
      <Paper sx={{ p: 3, mt: 3 }}>
        <Typography variant="h6" gutterBottom>
          About the GL07 Worker
        </Typography>
        <Typography variant="body2" color="text.secondary" paragraph>
          The GL07 worker processes XML files from configured source systems and transforms them
          to Unit4 REST API format. Files are moved to <code>done/</code> on success or{' '}
          <code>error/</code> on failure.
        </Typography>
        <Typography variant="body2" color="text.secondary">
          <strong>Tips:</strong>
        </Typography>
        <ul style={{ margin: 0, paddingLeft: 20 }}>
          <li>
            <Typography variant="body2" color="text.secondary">
              Use "All Active Systems" to process files from all configured sources
            </Typography>
          </li>
          <li>
            <Typography variant="body2" color="text.secondary">
              Enable "Dry Run" to test transformations without posting to Unit4
            </Typography>
          </li>
          <li>
            <Typography variant="body2" color="text.secondary">
              Check Processing Logs for detailed results after each run
            </Typography>
          </li>
        </ul>
      </Paper>

      {/* Success Snackbar */}
      <Snackbar
        open={!!success}
        autoHideDuration={3000}
        onClose={() => setSuccess(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      </Snackbar>
    </Box>
  );
}
