import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    CheckCircle as CheckCircleIcon,
    Refresh as RefreshIcon,
    Save as SaveIcon,
    Visibility as VisibilityIcon,
    VisibilityOff as VisibilityOffIcon,
} from '@mui/icons-material';
import {
    Alert,
    Box,
    Button,
    CircularProgress,
    FormControl,
    IconButton,
    InputAdornment,
    MenuItem,
    Paper,
    Select,
    Snackbar,
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

// Fixed setting definitions
interface SettingDefinition {
  key: string;
  label: string;
  description: string;
  type: 'text' | 'password' | 'url' | 'dropdown';
  options?: string[];
  required?: boolean;
  category: string;
  showWhen?: { key: string; value: string };
}

const SETTING_DEFINITIONS: SettingDefinition[] = [
  // File Source Settings (Local base path always configured)
  {
    key: 'FileSource:LocalBasePath',
    label: 'Local Base Path',
    description: 'Base folder path for local file sources (e.g., C:\\Temp\\Data)',
    type: 'text',
    required: true,
    category: 'FileSource',
  },
  // Azure Storage Settings (always configured, used by source systems with AzureBlob provider)
  {
    key: 'AzureStorage:ConnectionString',
    label: 'Connection String',
    description: 'Azure Blob Storage connection string (for Azure source systems)',
    type: 'password',
    category: 'AzureStorage',
  },
  {
    key: 'AzureStorage:ContainerName',
    label: 'Container Name',
    description: 'Azure Blob container name',
    type: 'text',
    category: 'AzureStorage',
  },
  // Unit4 API Settings
  {
    key: 'Unit4:BaseUrl',
    label: 'API Base URL',
    description: 'Unit4 REST API endpoint',
    type: 'url',
    required: true,
    category: 'Unit4',
  },
  {
    key: 'Unit4:TokenUrl',
    label: 'Token URL',
    description: 'OAuth2 token endpoint URL',
    type: 'url',
    required: true,
    category: 'Unit4',
  },
  {
    key: 'Unit4:ClientId',
    label: 'Client ID',
    description: 'OAuth2 client ID for Unit4',
    type: 'text',
    required: true,
    category: 'Unit4',
  },
  {
    key: 'Unit4:ClientSecret',
    label: 'Client Secret',
    description: 'OAuth2 client secret',
    type: 'password',
    required: true,
    category: 'Unit4',
  },
  {
    key: 'Unit4:GrantType',
    label: 'Grant Type',
    description: 'OAuth2 grant type (e.g., client_credentials)',
    type: 'text',
    category: 'Unit4',
  },
  {
    key: 'Unit4:Scope',
    label: 'OAuth Scope',
    description: 'OAuth2 scope for token requests',
    type: 'text',
    category: 'Unit4',
  },
  {
    key: 'Unit4:CompanyId',
    label: 'Company ID',
    description: 'Default company ID for transactions',
    type: 'text',
    category: 'Unit4',
  },
];

const CATEGORIES = [
  { key: 'Unit4', label: 'Unit4 API', description: 'Unit4 ERP connection settings' },
  { key: 'FileSource', label: 'File Source', description: 'Configure where to read XML files from' },
  { key: 'AzureStorage', label: 'Azure Storage', description: 'Azure Blob Storage connection' },
];

interface SettingValue {
  key: string;
  value: string;
}

export default function SettingsPage() {
  const { getToken } = useAuth();
  const [values, setValues] = useState<Record<string, string>>({});
  const [originalValues, setOriginalValues] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [showPasswords, setShowPasswords] = useState<Record<string, boolean>>({});

  const fetchSettings = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      const response = await apiClient.get<SettingValue[]>('/settings');
      
      const valueMap: Record<string, string> = {};
      response.data.forEach((s) => {
        valueMap[s.key] = s.value;
      });
      
      // Set defaults for missing settings
      SETTING_DEFINITIONS.forEach((def) => {
        if (valueMap[def.key] === undefined) {
          valueMap[def.key] = def.type === 'dropdown' && def.options ? def.options[0] : '';
        }
      });

      setValues(valueMap);
      setOriginalValues(valueMap);
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

  const handleValueChange = (key: string, value: string) => {
    setValues((prev) => ({ ...prev, [key]: value }));
  };

  const hasChanges = () => {
    return Object.keys(values).some((key) => values[key] !== originalValues[key]);
  };

  const handleSaveAll = async () => {
    setSaving(true);
    setError(null);

    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;

      // Save only changed settings
      const changedSettings = Object.keys(values).filter(
        (key) => values[key] !== originalValues[key]
      );

      for (const key of changedSettings) {
        const def = SETTING_DEFINITIONS.find((d) => d.key === key);
        await apiClient.put(`/settings/${encodeURIComponent(key)}`, {
          value: values[key],
          description: def?.description || '',
          isEncrypted: def?.type === 'password',
        });
      }

      setOriginalValues({ ...values });
      setSuccess(`${changedSettings.length} setting(s) saved successfully`);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save settings';
      setError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    setValues({ ...originalValues });
  };

  const togglePasswordVisibility = (key: string) => {
    setShowPasswords((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const shouldShowSetting = (def: SettingDefinition): boolean => {
    if (!def.showWhen) return true;
    return values[def.showWhen.key] === def.showWhen.value;
  };

  const renderInput = (def: SettingDefinition) => {
    const value = values[def.key] || '';

    if (def.type === 'dropdown' && def.options) {
      return (
        <FormControl size="small" fullWidth>
          <Select
            value={value}
            onChange={(e) => handleValueChange(def.key, e.target.value)}
          >
            {def.options.map((opt) => (
              <MenuItem key={opt} value={opt}>
                {opt}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      );
    }

    if (def.type === 'password') {
      return (
        <TextField
          size="small"
          fullWidth
          type={showPasswords[def.key] ? 'text' : 'password'}
          value={value}
          onChange={(e) => handleValueChange(def.key, e.target.value)}
          placeholder="Enter value"
          slotProps={{
            input: {
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    size="small"
                    onClick={() => togglePasswordVisibility(def.key)}
                    edge="end"
                  >
                    {showPasswords[def.key] ? <VisibilityOffIcon /> : <VisibilityIcon />}
                  </IconButton>
                </InputAdornment>
              ),
            },
          }}
        />
      );
    }

    return (
      <TextField
        size="small"
        fullWidth
        type={def.type === 'url' ? 'url' : 'text'}
        value={value}
        onChange={(e) => handleValueChange(def.key, e.target.value)}
        placeholder={def.type === 'url' ? 'https://...' : 'Enter value'}
      />
    );
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
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4" fontWeight={600}>
            Settings
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Configure application settings for file sources and Unit4 integration
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={handleReset}
            disabled={!hasChanges() || saving}
          >
            Reset
          </Button>
          <Button
            variant="contained"
            startIcon={saving ? <CircularProgress size={20} color="inherit" /> : <SaveIcon />}
            onClick={handleSaveAll}
            disabled={!hasChanges() || saving}
            sx={{ bgcolor: '#003d4d', '&:hover': { bgcolor: '#00717f' } }}
          >
            Save Changes
          </Button>
        </Box>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Settings Tables by Category */}
      {CATEGORIES.map((category) => {
        const categorySettings = SETTING_DEFINITIONS.filter(
          (def) => def.category === category.key && shouldShowSetting(def)
        );

        if (categorySettings.length === 0) return null;

        return (
          <Paper key={category.key} sx={{ mb: 3, overflow: 'hidden' }}>
            <Box sx={{ bgcolor: '#003d4d', px: 3, py: 2 }}>
              <Typography variant="h6" fontWeight={600} sx={{ color: '#ffffff' }}>
                {category.label}
              </Typography>
              <Typography variant="body2" sx={{ color: 'rgba(255, 255, 255, 0.9)' }}>
                {category.description}
              </Typography>
            </Box>
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow sx={{ bgcolor: 'grey.50' }}>
                    <TableCell width="30%">
                      <Typography variant="subtitle2" fontWeight={600}>
                        Setting
                      </Typography>
                    </TableCell>
                    <TableCell width="50%">
                      <Typography variant="subtitle2" fontWeight={600}>
                        Value
                      </Typography>
                    </TableCell>
                    <TableCell width="20%">
                      <Typography variant="subtitle2" fontWeight={600}>
                        Status
                      </Typography>
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {categorySettings.map((def) => {
                    const isChanged = values[def.key] !== originalValues[def.key];
                    const isEmpty = !values[def.key];

                    return (
                      <TableRow
                        key={def.key}
                        sx={{
                          bgcolor: isChanged ? 'action.hover' : 'inherit',
                          '&:last-child td': { borderBottom: 0 },
                        }}
                      >
                        <TableCell>
                          <Typography variant="body2" fontWeight={500}>
                            {def.label}
                            {def.required && (
                              <Typography component="span" color="error.main" sx={{ ml: 0.5 }}>
                                *
                              </Typography>
                            )}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {def.description}
                          </Typography>
                        </TableCell>
                        <TableCell>{renderInput(def)}</TableCell>
                        <TableCell>
                          {isChanged ? (
                            <Typography variant="caption" color="warning.main" fontWeight={500}>
                              Modified
                            </Typography>
                          ) : isEmpty && def.required ? (
                            <Typography variant="caption" color="error.main" fontWeight={500}>
                              Required
                            </Typography>
                          ) : !isEmpty ? (
                            <Tooltip title="Configured">
                              <CheckCircleIcon fontSize="small" color="success" />
                            </Tooltip>
                          ) : (
                            <Typography variant="caption" color="text.secondary">
                              Not set
                            </Typography>
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        );
      })}

      {/* Success Snackbar */}
      <Snackbar
        open={!!success}
        autoHideDuration={4000}
        onClose={() => setSuccess(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert severity="success" onClose={() => setSuccess(null)}>
          {success}
        </Alert>
      </Snackbar>
    </Box>
  );
}
