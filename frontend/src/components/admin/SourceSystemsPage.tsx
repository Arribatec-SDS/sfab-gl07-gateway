import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    Add as AddIcon,
    Delete as DeleteIcon,
    Edit as EditIcon,
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
    FormControlLabel,
    IconButton,
    Paper,
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

interface SourceSystem {
  id: number;
  systemCode: string;
  systemName: string;
  description: string | null;
  provider: string;
  folderPath: string;
  filePattern: string;
  transformerType: string;
  isActive: boolean;
}

interface SourceSystemRequest {
  systemCode: string;
  systemName: string;
  description: string;
  provider: string;
  folderPath: string;
  filePattern: string;
  transformerType: string;
  isActive: boolean;
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
  filePattern: '*.xml',
  transformerType: 'ABWTransaction',
  isActive: true,
};

export default function SourceSystemsPage() {
  const { getToken } = useAuth();
  const [sourceSystems, setSourceSystems] = useState<SourceSystem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingSystem, setEditingSystem] = useState<SourceSystem | null>(null);
  const [formData, setFormData] = useState<SourceSystemRequest>(emptySourceSystem);
  const [localBasePath, setLocalBasePath] = useState<string>('');
  const [azureContainerName, setAzureContainerName] = useState<string>('');
  const [saving, setSaving] = useState(false);

  const fetchSourceSystems = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      // Fetch source systems and settings in parallel
      const [systemsResponse, settingsResponse] = await Promise.all([
        apiClient.get<SourceSystem[]>('/sourcesystems'),
        apiClient.get<{ paramName: string; paramValue: string }[]>('/settings')
      ]);
      
      setSourceSystems(systemsResponse.data);
      
      // Extract base paths from settings
      const settings = settingsResponse.data;
      const localPath = settings.find(s => s.paramName === 'FileSource:LocalBasePath')?.paramValue || '';
      const containerName = settings.find(s => s.paramName === 'AzureStorage:ContainerName')?.paramValue || '';
      setLocalBasePath(localPath);
      setAzureContainerName(containerName);
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
        filePattern: system.filePattern,
        transformerType: system.transformerType,
        isActive: system.isActive,
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
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete source system';
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
        description: system.description,
        folderPath: system.folderPath,
        filePattern: system.filePattern,
        transformerType: system.transformerType,
        isActive: !system.isActive,
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
                <TableCell>Provider</TableCell>
                <TableCell>Folder Path</TableCell>
                <TableCell>Pattern</TableCell>
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
                    {system.description && (
                      <Typography variant="caption" color="text.secondary">
                        {system.description}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip 
                      label={system.provider || 'Local'} 
                      size="small" 
                      color={system.provider === 'AzureBlob' ? 'info' : 'default'}
                    />
                  </TableCell>
                  <TableCell>
                    <Tooltip title="Full path">
                      <Typography variant="body2" sx={{ maxWidth: 300, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {system.provider === 'AzureBlob' 
                          ? joinPaths(azureContainerName, system.folderPath)
                          : joinPaths(localBasePath, system.folderPath)}
                      </Typography>
                    </Tooltip>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {system.folderPath}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <code>{system.filePattern}</code>
                  </TableCell>
                  <TableCell>{system.transformerType}</TableCell>
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
      <Dialog open={dialogOpen} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingSystem ? 'Edit Source System' : 'Add Source System'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            <TextField
              label="System Code"
              fullWidth
              required
              value={formData.systemCode}
              onChange={(e) => setFormData(prev => ({ ...prev, systemCode: e.target.value }))}
              placeholder="e.g., ERP_PROD"
              helperText="Unique identifier for this source system"
              disabled={!!editingSystem}
            />
            <TextField
              label="System Name"
              fullWidth
              required
              value={formData.systemName}
              onChange={(e) => setFormData(prev => ({ ...prev, systemName: e.target.value }))}
              placeholder="e.g., Production ERP System"
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={2}
              value={formData.description}
              onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Optional description"
            />
            <TextField
              label="File Source Provider"
              fullWidth
              required
              select
              value={formData.provider}
              onChange={(e) => setFormData(prev => ({ ...prev, provider: e.target.value }))}
              slotProps={{
                select: {
                  native: true,
                },
              }}
              helperText="Where files are stored: Local filesystem or Azure Blob Storage"
            >
              <option value="Local">Local Filesystem</option>
              <option value="AzureBlob">Azure Blob Storage</option>
            </TextField>
            <TextField
              label="Relative Folder Path"
              fullWidth
              required
              value={formData.folderPath}
              onChange={(e) => setFormData(prev => ({ ...prev, folderPath: e.target.value }))}
              placeholder={formData.provider === 'AzureBlob' ? 'erp/inbox' : 'erp'}
              helperText={
                formData.provider === 'AzureBlob' 
                  ? 'Blob prefix path within the container (e.g., erp/inbox)' 
                  : 'Relative folder path from base path (e.g., erp)'
              }
            />
            {/* Full path preview */}
            <Box sx={{ 
              p: 1.5, 
              bgcolor: 'action.hover', 
              borderRadius: 1,
              border: '1px solid',
              borderColor: 'divider'
            }}>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                Full Path Preview
              </Typography>
              <Typography variant="body2" sx={{ fontFamily: 'monospace', wordBreak: 'break-all' }}>
                {formData.provider === 'AzureBlob'
                  ? joinPaths(azureContainerName || '(container)', formData.folderPath || '(folder)')
                  : joinPaths(localBasePath || '(base path)', formData.folderPath || '(folder)')}
              </Typography>
            </Box>
            <TextField
              label="File Pattern"
              fullWidth
              required
              value={formData.filePattern}
              onChange={(e) => setFormData(prev => ({ ...prev, filePattern: e.target.value }))}
              placeholder="*.xml"
              helperText="File pattern to match (e.g., *.xml, GL07_*.xml)"
            />
            <TextField
              label="Transformer Type"
              fullWidth
              required
              select
              value={formData.transformerType}
              onChange={(e) => setFormData(prev => ({ ...prev, transformerType: e.target.value }))}
              slotProps={{
                select: {
                  native: true,
                },
              }}
              helperText="Transformation strategy for converting XML to Unit4 format"
            >
              <option value="ABWTransaction">ABW Transaction (Default)</option>
            </TextField>
            <FormControlLabel
              control={
                <Switch
                  checked={formData.isActive}
                  onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                />
              }
              label="Active"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleSave}
            disabled={!formData.systemCode || !formData.systemName || !formData.folderPath || saving}
          >
            {saving ? <CircularProgress size={24} /> : (editingSystem ? 'Update' : 'Create')}
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
