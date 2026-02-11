import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    Add as AddIcon,
    Close as CloseIcon,
    Delete as DeleteIcon,
    Edit as EditIcon,
    Info as InfoIcon,
    Pause as PauseIcon,
    PlayArrow as PlayArrowIcon,
    Refresh as RefreshIcon,
} from '@mui/icons-material';
import {
    Alert,
    Box,
    Button,
    Chip,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    FormControl,
    FormControlLabel,
    Grid,
    IconButton,
    InputAdornment,
    InputLabel,
    MenuItem,
    Paper,
    Select,
    Snackbar,
    Switch,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Tooltip,
    Typography,
} from '@mui/material';
import { useCallback, useEffect, useState } from 'react';
import { createApiClient } from '../../utils/api';

interface Gl07ReportSetup {
  id: number;
  setupCode: string;
  reportName: string | null;
  isActive: boolean;
}

interface SourceSystem {
  id: number;
  systemCode: string;
  systemName: string;
  description: string | null;
  provider: string;
  folderPath: string;
  azureFileShareConnectionName: string | null;
  filePattern: string;
  transformerType: string;
  isActive: boolean;
  gl07ReportSetupId: number;
  gl07ReportSetup?: Gl07ReportSetup;
  interface: string | null;
  transactionType: string | null;
  batchId: string | null;
  defaultCurrency: string | null;
}

interface SourceSystemRequest {
  systemCode: string;
  systemName: string;
  description: string;
  provider: string;
  folderPath: string;
  azureFileShareConnectionName: string;
  filePattern: string;
  transformerType: string;
  isActive: boolean;
  gl07ReportSetupId: number;
  interface: string;
  transactionType: string;
  batchId: string;
  defaultCurrency: string;
}

// Helper to join paths without double slashes
const joinPaths = (basePath: string, relativePath: string): string => {
  const base = basePath.replace(/[\\/]+$/, ''); // Remove trailing slashes
  const rel = relativePath.replace(/^[\\/]+/, ''); // Remove leading slashes
  // Use backslash if base path contains backslash (Windows), otherwise forward slash
  const separator = basePath.includes('\\') ? '\\' : '/';
  return `${base}${separator}${rel}`;
};

const emptySourceSystem: SourceSystemRequest = {
  systemCode: '',
  systemName: '',
  description: '',
  provider: 'Local',
  folderPath: '',
  azureFileShareConnectionName: '',
  filePattern: '*.xml',
  transformerType: 'ABWTransaction',
  isActive: true,
  gl07ReportSetupId: 0,
  interface: '',
  transactionType: '',
  batchId: '',
  defaultCurrency: '',
};

export default function SourceSystemsPage() {
  const { getToken } = useAuth();
  const [sourceSystems, setSourceSystems] = useState<SourceSystem[]>([]);
  const [gl07ReportSetups, setGl07ReportSetups] = useState<Gl07ReportSetup[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingSystem, setEditingSystem] = useState<SourceSystem | null>(null);
  const [formData, setFormData] = useState<SourceSystemRequest>(emptySourceSystem);
  const [localBasePath, setLocalBasePath] = useState<string>('');
  const [azureContainerName, setAzureContainerName] = useState<string>('');
  const [azureFileShareConnectionNames, setAzureFileShareConnectionNames] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);

  const fetchSourceSystems = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      // Fetch source systems, GL07 report setups, and settings in parallel
      const [systemsResponse, setupsResponse, settingsResponse] = await Promise.all([
        apiClient.get<SourceSystem[]>('/sourcesystems'),
        apiClient.get<Gl07ReportSetup[]>('/gl07reportsetups/active'),
        apiClient.get<{ paramName: string; paramValue: string }[]>('/settings')
      ]);
      
      setSourceSystems(systemsResponse.data);
      setGl07ReportSetups(setupsResponse.data);
      
      // Extract base paths and connection names from settings
      const settings = settingsResponse.data;
      const localPath = settings.find(s => s.paramName === 'FileSource:LocalBasePath')?.paramValue || '';
      const containerName = settings.find(s => s.paramName === 'AzureStorage:ContainerName')?.paramValue || '';
      setLocalBasePath(localPath);
      setAzureContainerName(containerName);
      
      // Extract Azure File Share connection names (settings like "AzureFileShare:{name}:Url")
      const connectionNames = settings
        .filter(s => s.paramName.match(/^AzureFileShare:[^:]+:Url$/))
        .map(s => s.paramName.replace(/^AzureFileShare:/, '').replace(/:Url$/, ''));
      setAzureFileShareConnectionNames(connectionNames);
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

  const handleOpenDialog = (system?: SourceSystem) => {
    if (system) {
      setEditingSystem(system);
      setFormData({
        systemCode: system.systemCode,
        systemName: system.systemName,
        description: system.description || '',
        provider: system.provider || 'Local',
        folderPath: system.folderPath,
        azureFileShareConnectionName: system.azureFileShareConnectionName || '',
        filePattern: system.filePattern,
        transformerType: system.transformerType,
        isActive: system.isActive,
        gl07ReportSetupId: system.gl07ReportSetupId,
        interface: system.interface || '',
        transactionType: system.transactionType || '',
        batchId: system.batchId || '',
        defaultCurrency: system.defaultCurrency || '',
      });
    } else {
      setEditingSystem(null);
      setFormData(emptySourceSystem);
    }
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingSystem(null);
    setFormData(emptySourceSystem);
  };

  const handleSave = async () => {
    setSaving(true);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      if (editingSystem) {
        await apiClient.put(`/sourcesystems/${editingSystem.id}`, formData);
        setSuccess('Source system updated successfully');
      } else {
        await apiClient.post('/sourcesystems', formData);
        setSuccess('Source system created successfully');
      }
      
      handleCloseDialog();
      await fetchSourceSystems();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save source system';
      setError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`Are you sure you want to delete source system "${name}"?`)) return;
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      await apiClient.delete(`/sourcesystems/${id}`);
      setSuccess('Source system deleted successfully');
      await fetchSourceSystems();
    } catch (err: unknown) {
      // Extract error message from axios response or fallback to generic message
      let errorMessage = 'Failed to delete source system';
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { message?: string } } };
        errorMessage = axiosErr.response?.data?.message || errorMessage;
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }
      setError(errorMessage);
    }
  };

  const handleToggleActive = async (system: SourceSystem) => {
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      await apiClient.put(`/sourcesystems/${system.id}`, {
        systemCode: system.systemCode,
        systemName: system.systemName,
        description: system.description || '',
        provider: system.provider,
        folderPath: system.folderPath,
        azureFileShareConnectionName: system.azureFileShareConnectionName || '',
        filePattern: system.filePattern,
        transformerType: system.transformerType,
        isActive: !system.isActive,
        gl07ReportSetupId: system.gl07ReportSetupId,
        interface: system.interface || '',
        transactionType: system.transactionType || '',
        batchId: system.batchId || '',
        defaultCurrency: system.defaultCurrency || '',
      });
      
      setSuccess(`Source system ${system.isActive ? 'deactivated' : 'activated'} successfully`);
      await fetchSourceSystems();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update source system';
      setError(errorMessage);
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
          Source Systems
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            startIcon={<AddIcon />}
            variant="contained"
            onClick={() => handleOpenDialog()}
          >
            Add Source System
          </Button>
          <Button
            startIcon={<RefreshIcon />}
            variant="outlined"
            onClick={fetchSourceSystems}
          >
            Refresh
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {sourceSystems.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography color="text.secondary">
            No source systems configured. Click "Add Source System" to create one.
          </Typography>
        </Paper>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Code</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>GL07 Setup</TableCell>
                <TableCell>Transformer</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sourceSystems.map((system) => (
                <TableRow key={system.id} hover>
                  <TableCell>
                    <Typography variant="body2" fontWeight={500}>
                      {system.systemCode}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {system.systemName}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {system.gl07ReportSetup?.setupCode || <em style={{ color: '#999' }}>Not assigned</em>}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" fontFamily="monospace">
                      {system.transformerType || '-'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={system.isActive ? 'Active' : 'Inactive'}
                      size="small"
                      color={system.isActive ? 'success' : 'default'}
                      icon={system.isActive ? <PlayArrowIcon /> : <PauseIcon />}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title={system.isActive ? 'Deactivate' : 'Activate'}>
                      <IconButton
                        size="small"
                        onClick={() => handleToggleActive(system)}
                        color={system.isActive ? 'warning' : 'success'}
                      >
                        {system.isActive ? <PauseIcon /> : <PlayArrowIcon />}
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit">
                      <IconButton
                        size="small"
                        onClick={() => handleOpenDialog(system)}
                      >
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(system.id, system.systemCode)}
                      >
                        <DeleteIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Add/Edit Dialog */}
      <Dialog 
        open={dialogOpen} 
        maxWidth="sm" 
        fullWidth
        disableEscapeKeyDown
        onClose={(_, reason) => {
          if (reason !== 'backdropClick') {
            handleCloseDialog();
          }
        }}
      >
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          {editingSystem ? 'Edit Source System' : 'Add Source System'}
          <IconButton onClick={handleCloseDialog} size="small">
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent>
          <Grid container spacing={2} sx={{ mt: 0.5 }}>
            {/* Basic Information Section */}
            <Grid size={12}>
              <Typography variant="subtitle2" color="primary">
                Basic Information
              </Typography>
            </Grid>
            <Grid size={6}>
              <TextField
                label="System Code"
                fullWidth
                required
                size="small"
                value={formData.systemCode}
                onChange={(e) => setFormData(prev => ({ ...prev, systemCode: e.target.value }))}
                placeholder="e.g., ERP_PROD"
                disabled={!!editingSystem}
              />
            </Grid>
            <Grid size={6}>
              <TextField
                label="System Name"
                fullWidth
                required
                size="small"
                value={formData.systemName}
                onChange={(e) => setFormData(prev => ({ ...prev, systemName: e.target.value }))}
                placeholder="e.g., Production ERP"
              />
            </Grid>
            <Grid size={12}>
              <TextField
                label="Description"
                fullWidth
                size="small"
                value={formData.description}
                onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Optional description"
              />
            </Grid>

            {/* GL07 Configuration Section */}
            <Grid size={12}>
              <Typography variant="subtitle2" color="primary" sx={{ mt: 1 }}>
                GL07 Report Configuration
              </Typography>
            </Grid>
            <Grid size={6}>
              <TextField
                label="GL07 Report Setup"
                fullWidth
                required
                size="small"
                select
                value={formData.gl07ReportSetupId || ''}
                onChange={(e) => setFormData(prev => ({ ...prev, gl07ReportSetupId: Number(e.target.value) }))}
                slotProps={{
                  select: { native: true },
                  inputLabel: { shrink: true },
                }}
                error={formData.gl07ReportSetupId === 0}
              >
                <option value="">-- Select Setup --</option>
                {gl07ReportSetups.map((setup) => (
                  <option key={setup.id} value={setup.id}>
                    {setup.setupCode}
                  </option>
                ))}
              </TextField>
            </Grid>
            <Grid size={6}>
              <TextField
                label="Transformer Type"
                fullWidth
                required
                size="small"
                select
                value={formData.transformerType}
                onChange={(e) => setFormData(prev => ({ ...prev, transformerType: e.target.value }))}
                slotProps={{ select: { native: true } }}
              >
                <option value="ABWTransaction">ABW Transaction</option>
              </TextField>
            </Grid>
            <Grid size={3}>
              <TextField
                label="Interface"
                fullWidth
                size="small"
                value={formData.interface}
                onChange={(e) => setFormData(prev => ({ ...prev, interface: e.target.value }))}
                placeholder="e.g., GL07"
              />
            </Grid>
            <Grid size={3}>
              <TextField
                label="Trans. Type"
                fullWidth
                size="small"
                value={formData.transactionType}
                onChange={(e) => setFormData(prev => ({ ...prev, transactionType: e.target.value }))}
                placeholder="e.g., GL"
              />
            </Grid>
            <Grid size={3}>
              <TextField
                label="Batch ID Prefix"
                fullWidth
                size="small"
                value={formData.batchId}
                onChange={(e) => setFormData(prev => ({ ...prev, batchId: e.target.value.slice(0, 10) }))}
                placeholder="e.g., SFAB"
                helperText={`${formData.batchId.length}/10 chars`}
                error={formData.batchId.length > 10}
                slotProps={{
                  input: { inputProps: { maxLength: 10 } }
                }}
              />
            </Grid>
            <Grid size={3}>
              <TextField
                label="Default Currency"
                fullWidth
                size="small"
                value={formData.defaultCurrency}
                onChange={(e) => setFormData(prev => ({ ...prev, defaultCurrency: e.target.value.toUpperCase().slice(0, 3) }))}
                placeholder="e.g., SEK"
                helperText="Fallback: SEK"
                slotProps={{
                  input: { inputProps: { maxLength: 3 } }
                }}
              />
            </Grid>

            {/* File Source Configuration Section */}
            <Grid size={12}>
              <Typography variant="subtitle2" color="primary" sx={{ mt: 1 }}>
                File Source Configuration
              </Typography>
            </Grid>
            <Grid size={6}>
              <TextField
                label="Provider"
                fullWidth
                required
                size="small"
                select
                value={formData.provider}
                onChange={(e) => setFormData(prev => ({ ...prev, provider: e.target.value }))}
                slotProps={{ select: { native: true } }}
              >
                <option value="Local">Local Filesystem</option>
                {/* AzureBlob hidden - use AzureFileShare instead */}
                <option value="AzureFileShare">Azure File Share</option>
              </TextField>
            </Grid>
            <Grid size={6}>
              <TextField
                label="File Pattern"
                fullWidth
                required
                size="small"
                value={formData.filePattern}
                onChange={(e) => setFormData(prev => ({ ...prev, filePattern: e.target.value }))}
                placeholder="*.xml"
              />
            </Grid>
            {formData.provider === 'AzureFileShare' && (
              <>
                <Grid size={12}>
                  <FormControl fullWidth size="small" required error={formData.provider === 'AzureFileShare' && !formData.azureFileShareConnectionName}>
                    <InputLabel>Azure File Share Connection</InputLabel>
                    <Select
                      value={formData.azureFileShareConnectionName}
                      onChange={(e) => setFormData(prev => ({ ...prev, azureFileShareConnectionName: e.target.value }))}
                      label="Azure File Share Connection"
                    >
                      {azureFileShareConnectionNames.map(connName => (
                        <MenuItem key={connName} value={connName}>
                          {connName}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  {azureFileShareConnectionNames.length === 0 && (
                    <Typography variant="caption" color="warning.main" sx={{ mt: 0.5, display: 'block' }}>
                      No connections configured. Go to Settings &gt; Azure File Share to add a connection.
                    </Typography>
                  )}
                </Grid>
              </>
            )}
            <Grid size={12}>
              <TextField
                label="Folder Path"
                fullWidth
                required={formData.provider !== 'AzureFileShare'}
                size="small"
                value={formData.folderPath}
                onChange={(e) => setFormData(prev => ({ ...prev, folderPath: e.target.value }))}
                placeholder={formData.provider === 'AzureFileShare' ? 'GL07' : (formData.provider === 'AzureBlob' ? 'erp/inbox' : 'erp')}
                helperText={formData.provider === 'AzureFileShare' ? 'Root folder in share (e.g., GL07). Subfolders: inbox, archive, error' : undefined}
                slotProps={{
                  input: {
                    endAdornment: formData.provider !== 'AzureFileShare' ? (
                      <InputAdornment position="end">
                        <Tooltip 
                          title={
                            <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>
                              {formData.provider === 'AzureBlob'
                                ? joinPaths(azureContainerName || '(container)', formData.folderPath || '(folder)')
                                : joinPaths(localBasePath || '(base path)', formData.folderPath || '(folder)')}
                            </Typography>
                          }
                          arrow
                          slotProps={{
                            tooltip: {
                              sx: {
                                bgcolor: 'white',
                                color: '#18272F',
                                border: '1px solid #ccc',
                                boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
                                '& .MuiTooltip-arrow': {
                                  color: 'white',
                                  '&::before': {
                                    border: '1px solid #ccc',
                                  },
                                },
                              },
                            },
                          }}
                        >
                          <InfoIcon fontSize="small" color="action" sx={{ cursor: 'pointer' }} />
                        </Tooltip>
                      </InputAdornment>
                    ) : undefined,
                  },
                }}
              />
            </Grid>
            <Grid size={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={formData.isActive}
                    onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                  />
                }
                label="Active"
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleSave}
            disabled={
              !formData.systemCode || 
              !formData.systemName || 
              (formData.provider !== 'AzureFileShare' && !formData.folderPath) ||
              (formData.provider === 'AzureFileShare' && !formData.azureFileShareConnectionName) ||
              formData.gl07ReportSetupId === 0 ||
              saving
            }
          >
            {saving ? <CircularProgress size={24} color="inherit" /> : (editingSystem ? 'Update' : 'Create')}
          </Button>
        </DialogActions>
      </Dialog>

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
