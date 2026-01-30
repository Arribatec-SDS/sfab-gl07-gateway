import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Paper,
  Typography,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  TextField,
  Button,
  IconButton,
  Alert,
  Snackbar,
  CircularProgress,
  Chip,
  InputAdornment,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  Save as SaveIcon,
  Refresh as RefreshIcon,
  Visibility as VisibilityIcon,
  VisibilityOff as VisibilityOffIcon,
  Lock as LockIcon,
  Add as AddIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { createApiClient } from '../../utils/api';
import { useAuth } from '@arribatec-sds/arribatec-nexus-react';

interface AppSetting {
  id: number;
  category: string;
  key: string;
  value: string;
  description: string | null;
  isEncrypted: boolean;
  createdAt: string;
  updatedAt: string;
}

interface SettingsByCategory {
  [category: string]: AppSetting[];
}

interface CreateSettingRequest {
  category: string;
  key: string;
  value: string;
  description: string;
  isEncrypted: boolean;
}

export default function SettingsPage() {
  const { getToken } = useAuth();
  const [settings, setSettings] = useState<SettingsByCategory>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [expandedCategories, setExpandedCategories] = useState<string[]>([]);
  const [editedValues, setEditedValues] = useState<Record<string, string>>({});
  const [showPassword, setShowPassword] = useState<Record<string, boolean>>({});
  const [addDialogOpen, setAddDialogOpen] = useState(false);
  const [newSetting, setNewSetting] = useState<CreateSettingRequest>({
    category: '',
    key: '',
    value: '',
    description: '',
    isEncrypted: false,
  });

  const fetchSettings = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      const response = await apiClient.get<AppSetting[]>('/settings');
      
      // Group by category
      const grouped = response.data.reduce((acc: SettingsByCategory, setting) => {
        if (!acc[setting.category]) {
          acc[setting.category] = [];
        }
        acc[setting.category].push(setting);
        return acc;
      }, {});
      
      setSettings(grouped);
      
      // Expand all categories by default
      setExpandedCategories(Object.keys(grouped));
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load settings';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [getToken]);

  useEffect(() => {
    fetchSettings();
  }, [fetchSettings]);

  const handleCategoryToggle = (category: string) => {
    setExpandedCategories(prev =>
      prev.includes(category)
        ? prev.filter(c => c !== category)
        : [...prev, category]
    );
  };

  const handleValueChange = (key: string, value: string) => {
    setEditedValues(prev => ({ ...prev, [key]: value }));
  };

  const handleSave = async (setting: AppSetting) => {
    const newValue = editedValues[setting.key];
    if (newValue === undefined || newValue === setting.value) return;
    
    setSaving(setting.key);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      await apiClient.put(`/settings/${setting.key}`, {
        value: newValue,
        description: setting.description,
        isEncrypted: setting.isEncrypted,
      });
      
      setSuccess(`Setting "${setting.key}" saved successfully`);
      setEditedValues(prev => {
        const updated = { ...prev };
        delete updated[setting.key];
        return updated;
      });
      
      // Refresh settings
      await fetchSettings();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : `Failed to save "${setting.key}"`;
      setError(errorMessage);
    } finally {
      setSaving(null);
    }
  };

  const handleDelete = async (key: string) => {
    if (!confirm(`Are you sure you want to delete setting "${key}"?`)) return;
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      await apiClient.delete(`/settings/${key}`);
      setSuccess(`Setting "${key}" deleted successfully`);
      await fetchSettings();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : `Failed to delete "${key}"`;
      setError(errorMessage);
    }
  };

  const handleAddSetting = async () => {
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      await apiClient.post('/settings', newSetting);
      setSuccess('Setting created successfully');
      setAddDialogOpen(false);
      setNewSetting({
        category: '',
        key: '',
        value: '',
        description: '',
        isEncrypted: false,
      });
      await fetchSettings();
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to create setting';
      setError(errorMessage);
    }
  };

  const togglePasswordVisibility = (key: string) => {
    setShowPassword(prev => ({ ...prev, [key]: !prev[key] }));
  };

  const getCurrentValue = (setting: AppSetting): string => {
    return editedValues[setting.key] !== undefined ? editedValues[setting.key] : setting.value;
  };

  const hasChanges = (setting: AppSetting): boolean => {
    return editedValues[setting.key] !== undefined && editedValues[setting.key] !== setting.value;
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
          Application Settings
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            startIcon={<AddIcon />}
            variant="contained"
            onClick={() => setAddDialogOpen(true)}
          >
            Add Setting
          </Button>
          <Button
            startIcon={<RefreshIcon />}
            variant="outlined"
            onClick={fetchSettings}
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

      {Object.keys(settings).length === 0 ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography color="text.secondary">
            No settings configured. Click "Add Setting" to create one.
          </Typography>
        </Paper>
      ) : (
        Object.entries(settings).map(([category, categorySettings]) => (
          <Accordion
            key={category}
            expanded={expandedCategories.includes(category)}
            onChange={() => handleCategoryToggle(category)}
            sx={{ mb: 1 }}
          >
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="h6" sx={{ fontWeight: 500 }}>
                {category}
              </Typography>
              <Chip
                label={categorySettings.length}
                size="small"
                sx={{ ml: 2 }}
              />
            </AccordionSummary>
            <AccordionDetails>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {categorySettings.map((setting) => (
                  <Paper
                    key={setting.id}
                    sx={{
                      p: 2,
                      backgroundColor: hasChanges(setting) ? 'action.hover' : 'background.paper',
                    }}
                    variant="outlined"
                  >
                    <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
                      <Box sx={{ flex: 1 }}>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                          <Typography variant="subtitle1" fontWeight={500}>
                            {setting.key}
                          </Typography>
                          {setting.isEncrypted && (
                            <Tooltip title="Encrypted value">
                              <LockIcon fontSize="small" color="primary" />
                            </Tooltip>
                          )}
                        </Box>
                        {setting.description && (
                          <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                            {setting.description}
                          </Typography>
                        )}
                        <TextField
                          fullWidth
                          size="small"
                          type={setting.isEncrypted && !showPassword[setting.key] ? 'password' : 'text'}
                          value={getCurrentValue(setting)}
                          onChange={(e) => handleValueChange(setting.key, e.target.value)}
                          placeholder={setting.isEncrypted ? '••••••••' : 'Enter value'}
                          slotProps={{
                            input: {
                              endAdornment: setting.isEncrypted ? (
                                <InputAdornment position="end">
                                  <IconButton
                                    size="small"
                                    onClick={() => togglePasswordVisibility(setting.key)}
                                    edge="end"
                                  >
                                    {showPassword[setting.key] ? <VisibilityOffIcon /> : <VisibilityIcon />}
                                  </IconButton>
                                </InputAdornment>
                              ) : undefined,
                            },
                          }}
                        />
                      </Box>
                      <Box sx={{ display: 'flex', gap: 1, pt: 3 }}>
                        <Tooltip title="Save changes">
                          <span>
                            <IconButton
                              color="primary"
                              onClick={() => handleSave(setting)}
                              disabled={!hasChanges(setting) || saving === setting.key}
                            >
                              {saving === setting.key ? (
                                <CircularProgress size={24} />
                              ) : (
                                <SaveIcon />
                              )}
                            </IconButton>
                          </span>
                        </Tooltip>
                        <Tooltip title="Delete setting">
                          <IconButton
                            color="error"
                            onClick={() => handleDelete(setting.key)}
                          >
                            <DeleteIcon />
                          </IconButton>
                        </Tooltip>
                      </Box>
                    </Box>
                  </Paper>
                ))}
              </Box>
            </AccordionDetails>
          </Accordion>
        ))
      )}

      {/* Add Setting Dialog */}
      <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add New Setting</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
            <TextField
              label="Category"
              fullWidth
              value={newSetting.category}
              onChange={(e) => setNewSetting(prev => ({ ...prev, category: e.target.value }))}
              placeholder="e.g., Unit4, FileSource, General"
            />
            <TextField
              label="Key"
              fullWidth
              value={newSetting.key}
              onChange={(e) => setNewSetting(prev => ({ ...prev, key: e.target.value }))}
              placeholder="e.g., Unit4.ApiUrl"
            />
            <TextField
              label="Value"
              fullWidth
              value={newSetting.value}
              onChange={(e) => setNewSetting(prev => ({ ...prev, value: e.target.value }))}
              type={newSetting.isEncrypted ? 'password' : 'text'}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={2}
              value={newSetting.description}
              onChange={(e) => setNewSetting(prev => ({ ...prev, description: e.target.value }))}
            />
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <input
                type="checkbox"
                id="isEncrypted"
                checked={newSetting.isEncrypted}
                onChange={(e) => setNewSetting(prev => ({ ...prev, isEncrypted: e.target.checked }))}
              />
              <label htmlFor="isEncrypted">
                <Typography variant="body2">
                  Encrypt this value (for sensitive data like passwords, API keys)
                </Typography>
              </label>
            </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAddDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleAddSetting}
            disabled={!newSetting.category || !newSetting.key || !newSetting.value}
          >
            Add Setting
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
