import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    Add as AddIcon,
    Close as CloseIcon,
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
    Divider,
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

interface AppSetting {
  id: number;
  paramName: string;
  paramValue: string;
  category: string;
}

interface Gl07ReportSetupParameter {
  id: number;
  parameterId: string;
  parameterValue: string;
}

interface Gl07ReportSetup {
  id: number;
  setupCode: string;
  reportId: string | null;
  reportName: string | null;
  variant: number | null;
  userId: string | null;
  companyId: string | null;
  priority: number;
  emailConfirmation: boolean;
  status: string | null;
  outputType: number;
  description: string | null;
  isActive: boolean;
  parameters: Gl07ReportSetupParameter[];
}

interface Gl07ReportSetupRequest {
  setupCode: string;
  reportId: string;
  reportName: string;
  variant: number | null;
  userId: string;
  companyId: string;
  priority: number;
  emailConfirmation: boolean;
  status: string;
  outputType: number;
  description: string;
  isActive: boolean;
  parameters: { parameterId: string; parameterValue: string }[];
}

const emptySetup: Gl07ReportSetupRequest = {
  setupCode: '',
  reportId: '',
  reportName: '',
  variant: null,
  userId: '',
  companyId: '',
  priority: 0,
  emailConfirmation: false,
  status: 'N',
  outputType: 0,
  description: '',
  isActive: true,
  parameters: [],
};

// Default values for GL07 settings (fallback if not configured in database)
const DEFAULT_REPORT_ID = 'BI202';
const DEFAULT_REPORT_NAME = 'GL07';

export default function Gl07ReportSetupsPage() {
  const { getToken } = useAuth();
  const [setups, setSetups] = useState<Gl07ReportSetup[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingSetup, setEditingSetup] = useState<Gl07ReportSetup | null>(null);
  const [formData, setFormData] = useState<Gl07ReportSetupRequest>(emptySetup);
  const [saving, setSaving] = useState(false);
  const [setupCodeError, setSetupCodeError] = useState<string | null>(null);
  const [defaultReportId, setDefaultReportId] = useState<string>(DEFAULT_REPORT_ID);
  const [defaultReportName] = useState<string>(DEFAULT_REPORT_NAME);
  const [defaultUserId, setDefaultUserId] = useState<string>('');
  const [defaultCompanyId, setDefaultCompanyId] = useState<string>('');

  // Fetch default GL07 settings from API
  const fetchDefaultSettings = useCallback(async () => {
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const response = await apiClient.get<AppSetting[]>('/settings');
      const settings = response.data;

      const reportIdSetting = settings.find(s => s.paramName === 'GL07:DefaultReportId');
      if (reportIdSetting?.paramValue) {
        setDefaultReportId(reportIdSetting.paramValue);
      }

      // Use GL07 settings for UserId and CompanyId
      const userIdSetting = settings.find(s => s.paramName === 'GL07:DefaultUserId');
      if (userIdSetting?.paramValue) {
        setDefaultUserId(userIdSetting.paramValue);
      }

      const companyIdSetting = settings.find(s => s.paramName === 'GL07:DefaultCompanyId');
      if (companyIdSetting?.paramValue) {
        setDefaultCompanyId(companyIdSetting.paramValue);
      }
    } catch (err) {
      // Use defaults if settings fetch fails
      console.warn('Failed to fetch GL07 default settings, using defaults');
    }
  }, [getToken]);

  const fetchSetups = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const response = await apiClient.get<Gl07ReportSetup[]>('/gl07reportsetups');
      setSetups(response.data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load GL07 report setups';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [getToken]);

  useEffect(() => {
    fetchSetups();
    fetchDefaultSettings();
  }, [fetchSetups, fetchDefaultSettings]);

  const checkSetupCode = useCallback(async (code: string, excludeId?: number) => {
    if (!code) {
      setSetupCodeError(null);
      return true;
    }

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const params = excludeId ? { code, excludeId } : { code };
      const response = await apiClient.get<{ isUnique: boolean }>('/gl07reportsetups/check-code', { params });

      if (!response.data.isUnique) {
        setSetupCodeError('This setup code is already in use');
        return false;
      }
      setSetupCodeError(null);
      return true;
    } catch (err) {
      setSetupCodeError('Failed to validate code');
      return false;
    }
  }, [getToken]);

  const handleOpenDialog = (setup?: Gl07ReportSetup) => {
    setSetupCodeError(null);

    if (setup) {
      setEditingSetup(setup);
      setFormData({
        setupCode: setup.setupCode,
        reportId: setup.reportId || defaultReportId,
        reportName: setup.reportName || defaultReportName,
        variant: setup.variant,
        userId: setup.userId || '',
        companyId: setup.companyId || '',
        priority: 0,
        emailConfirmation: setup.emailConfirmation ?? false,
        status: 'N',
        outputType: 0,
        description: setup.description || '',
        isActive: setup.isActive,
        parameters: setup.parameters?.map(p => ({
          parameterId: p.parameterId,
          parameterValue: p.parameterValue,
        })) || [],
      });
    } else {
      setEditingSetup(null);
      setFormData({
        ...emptySetup,
        reportId: defaultReportId,
        reportName: defaultReportName,
        userId: defaultUserId,
        companyId: defaultCompanyId,
      });
    }
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setEditingSetup(null);
    setFormData(emptySetup);
    setSetupCodeError(null);
  };

  const handleSave = async () => {
    // Validate setup code uniqueness for new setups
    if (!editingSetup) {
      const isUnique = await checkSetupCode(formData.setupCode);
      if (!isUnique) return;
    }

    setSaving(true);

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      if (editingSetup) {
        await apiClient.put(`/gl07reportsetups/${editingSetup.id}`, formData);
        setSuccess('GL07 report setup updated successfully');
      } else {
        await apiClient.post('/gl07reportsetups', formData);
        setSuccess('GL07 report setup created successfully');
      }

      handleCloseDialog();
      await fetchSetups();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save GL07 report setup';
      setError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number, code: string) => {
    if (!confirm(`Are you sure you want to delete GL07 report setup "${code}"? This may affect source systems using this setup.`)) return;

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      await apiClient.delete(`/gl07reportsetups/${id}`);
      setSuccess('GL07 report setup deleted successfully');
      await fetchSetups();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to delete GL07 report setup';
      setError(errorMessage);
    }
  };

  const handleToggleActive = async (setup: Gl07ReportSetup) => {
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      await apiClient.put(`/gl07reportsetups/${setup.id}`, {
        setupCode: setup.setupCode,
        reportId: setup.reportId,
        reportName: setup.reportName,
        variant: setup.variant,
        userId: setup.userId,
        companyId: setup.companyId,
        priority: setup.priority,
        emailConfirmation: setup.emailConfirmation,
        status: setup.status,
        outputType: setup.outputType,
        description: setup.description,
        isActive: !setup.isActive,
        parameters: setup.parameters?.map(p => ({
          parameterId: p.parameterId,
          parameterValue: p.parameterValue,
        })) || [],
      });

      setSuccess(`GL07 report setup ${setup.isActive ? 'deactivated' : 'activated'} successfully`);
      await fetchSetups();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update GL07 report setup';
      setError(errorMessage);
    }
  };

  const handleAddParameter = () => {
    setFormData(prev => ({
      ...prev,
      parameters: [...prev.parameters, { parameterId: '', parameterValue: '' }],
    }));
  };

  const handleRemoveParameter = (index: number) => {
    setFormData(prev => ({
      ...prev,
      parameters: prev.parameters.filter((_, i) => i !== index),
    }));
  };

  const handleParameterChange = (index: number, field: 'parameterId' | 'parameterValue', value: string) => {
    setFormData(prev => ({
      ...prev,
      parameters: prev.parameters.map((p, i) => 
        i === index ? { ...p, [field]: value } : p
      ),
    }));
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
          GL07 Report Setups
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            startIcon={<AddIcon />}
            variant="contained"
            onClick={() => handleOpenDialog()}
          >
            Add Setup
          </Button>
          <Button
            startIcon={<RefreshIcon />}
            variant="outlined"
            onClick={fetchSetups}
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

      {setups.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography color="text.secondary">
            No GL07 report setups configured. Click "Add Setup" to create one.
          </Typography>
        </Paper>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Setup Code</TableCell>
                <TableCell>Report Name</TableCell>
                <TableCell>Report ID</TableCell>
                <TableCell>Variant</TableCell>
                <TableCell>Company</TableCell>
                <TableCell>Parameters</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {setups.map((setup) => (
                <TableRow key={setup.id} hover>
                  <TableCell>
                    <Typography variant="body2" fontWeight={500}>
                      {setup.setupCode}
                    </Typography>
                    {setup.description && (
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        {setup.description}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{setup.reportName || '-'}</TableCell>
                  <TableCell>
                    <code>{setup.reportId || '-'}</code>
                  </TableCell>
                  <TableCell>{setup.variant || '-'}</TableCell>
                  <TableCell>{setup.companyId || '-'}</TableCell>
                  <TableCell>
                    {setup.parameters && setup.parameters.length > 0 ? (
                      <Chip
                        label={`${setup.parameters.length} param(s)`}
                        size="small"
                        variant="outlined"
                      />
                    ) : (
                      <Typography variant="caption" color="text.secondary">None</Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={setup.isActive ? 'Active' : 'Inactive'}
                      size="small"
                      color={setup.isActive ? 'success' : 'default'}
                      icon={setup.isActive ? <PlayArrowIcon /> : <PauseIcon />}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Tooltip title={setup.isActive ? 'Deactivate' : 'Activate'}>
                      <IconButton
                        size="small"
                        onClick={() => handleToggleActive(setup)}
                        color={setup.isActive ? 'warning' : 'success'}
                      >
                        {setup.isActive ? <PauseIcon /> : <PlayArrowIcon />}
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit">
                      <IconButton
                        size="small"
                        onClick={() => handleOpenDialog(setup)}
                      >
                        <EditIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Delete">
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(setup.id, setup.setupCode)}
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
        maxWidth="md" 
        fullWidth
        disableEscapeKeyDown
        onClose={(_, reason) => {
          if (reason !== 'backdropClick') {
            handleCloseDialog();
          }
        }}
      >
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          {editingSetup ? 'Edit GL07 Report Setup' : 'Add GL07 Report Setup'}
          <IconButton onClick={handleCloseDialog} size="small">
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            {/* Basic Information */}
            <Typography variant="subtitle2" sx={{ color: '#003d4d' }}>
              Basic Information
            </Typography>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Setup Code"
                fullWidth
                required
                value={formData.setupCode}
                onChange={(e) => {
                  setFormData(prev => ({ ...prev, setupCode: e.target.value }));
                  // Debounce validation
                  setTimeout(() => {
                    if (!editingSetup) checkSetupCode(e.target.value);
                  }, 500);
                }}
                placeholder="e.g., gl07-standard"
                helperText={setupCodeError || 'Unique identifier for this setup'}
                error={!!setupCodeError}
                disabled={!!editingSetup}
              />
            </Box>
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={2}
              value={formData.description}
              onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Optional description"
            />

            {/* Report Configuration */}
            <Typography variant="subtitle2" sx={{ color: '#003d4d', mt: 2 }}>
              Report Configuration
            </Typography>
            
            {/* Report ID and Report Name - Fixed values */}
            <Box sx={{ 
              mb: 2, 
              p: 2, 
              borderRadius: 1, 
              bgcolor: '#003d4d',
              color: '#ffffff',
            }}>
              <Typography variant="body2" sx={{ color: '#ffffff' }}>
                <strong>Report ID:</strong> {defaultReportId} | <strong>Report Name:</strong> {defaultReportName}
              </Typography>
              <Typography variant="caption" sx={{ color: 'rgba(255, 255, 255, 0.8)' }}>
                Report ID is configured in Settings. Report Name is fixed.
              </Typography>
            </Box>
            
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Variant"
                fullWidth
                type="number"
                value={formData.variant ?? ''}
                onChange={(e) => setFormData(prev => ({ ...prev, variant: e.target.value ? parseInt(e.target.value) : null }))}
                placeholder="e.g., 1"
                helperText="Unit4 variant number (optional)"
              />
              <TextField
                label="User ID"
                fullWidth
                required
                value={formData.userId}
                onChange={(e) => setFormData(prev => ({ ...prev, userId: e.target.value }))}
                placeholder="e.g., BATCH"
                helperText={formData.userId && formData.userId === defaultUserId && defaultUserId !== '' 
                  ? "Unit4 user ID (default from settings)" 
                  : "Unit4 user ID"}
              />
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Company ID"
                fullWidth
                required
                value={formData.companyId}
                onChange={(e) => setFormData(prev => ({ ...prev, companyId: e.target.value }))}
                placeholder="e.g., 01"
                helperText={formData.companyId && formData.companyId === defaultCompanyId && defaultCompanyId !== '' 
                  ? "Unit4 company ID (default from settings)" 
                  : "Unit4 company ID"}
              />
            </Box>

            {/* Parameters Section */}
            <Divider sx={{ my: 2 }} />
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="subtitle2" sx={{ color: '#003d4d' }}>
                Report Parameters
              </Typography>
              <Button
                size="small"
                startIcon={<AddIcon />}
                onClick={handleAddParameter}
                sx={{ color: '#003d4d' }}
              >
                Add Parameter
              </Button>
            </Box>
            {formData.parameters.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                No parameters defined. Click "Add Parameter" to add one.
              </Typography>
            ) : (
              formData.parameters.map((param, index) => (
                <Box key={index} sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                  <TextField
                    label="Parameter ID"
                    fullWidth
                    value={param.parameterId}
                    onChange={(e) => handleParameterChange(index, 'parameterId', e.target.value)}
                    placeholder="e.g., from_date"
                    size="small"
                  />
                  <TextField
                    label="Parameter Value"
                    fullWidth
                    value={param.parameterValue}
                    onChange={(e) => handleParameterChange(index, 'parameterValue', e.target.value)}
                    placeholder="e.g., 2024-01-01"
                    size="small"
                  />
                  <IconButton
                    color="error"
                    onClick={() => handleRemoveParameter(index)}
                    size="small"
                  >
                    <DeleteIcon />
                  </IconButton>
                </Box>
              ))
            )}

            <FormControlLabel
              control={
                <Switch
                  checked={formData.isActive}
                  onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                />
              }
              label="Active"
              sx={{ mt: 2 }}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCloseDialog}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleSave}
            disabled={!formData.setupCode || !formData.reportId || !formData.reportName || !formData.userId || !formData.companyId || !!setupCodeError || saving}
          >
            {saving ? <CircularProgress size={24} color="inherit" /> : (editingSetup ? 'Update' : 'Create')}
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
