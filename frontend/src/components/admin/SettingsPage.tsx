import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    Add as AddIcon,
    CheckCircle as CheckCircleIcon,
    Delete as DeleteIcon,
    ExpandMore as ExpandMoreIcon,
    Lock as LockIcon,
    Refresh as RefreshIcon,
    Save as SaveIcon,
    Visibility as VisibilityIcon,
    VisibilityOff as VisibilityOffIcon,
} from '@mui/icons-material';
import {
    Accordion,
    AccordionDetails,
    AccordionSummary,
    Alert,
    Box,
    Button,
    CircularProgress,
    FormControl,
    IconButton,
    InputAdornment,
    MenuItem,
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
  locked?: boolean;
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
    key: 'Unit4:BatchEndpoint',
    label: 'Batch Endpoint',
    description: 'Endpoint path for transaction batch posting (e.g., /v1/financial-transaction-batch)',
    type: 'text',
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
  // GL07 Report Settings
  {
    key: 'GL07:DefaultReportId',
    label: 'Default Report ID',
    description: 'Default Report ID for GL07 report setups (e.g., BI202)',
    type: 'text',
    required: true,
    category: 'GL07',
  },
  {
    key: 'GL07:DefaultReportName',
    label: 'Default Report Name',
    description: 'Fixed report name for GL07 (cannot be changed)',
    type: 'text',
    category: 'GL07',
    locked: true,
  },
  {
    key: 'GL07:DefaultUserId',
    label: 'Default User ID',
    description: 'Default User ID for GL07 report setups',
    type: 'text',
    category: 'GL07',
  },
  {
    key: 'GL07:DefaultCompanyId',
    label: 'Default Company ID',
    description: 'Default Company ID for GL07 report setups',
    type: 'text',
    category: 'GL07',
  },
];

const CATEGORIES = [
  { key: 'Unit4', label: 'Unit4 API', description: 'Unit4 ERP connection settings' },
  { key: 'FileSource', label: 'File Source', description: 'Configure where to read XML files from' },
  // Hidden: AzureStorage - kept in code for backward compatibility but hidden from UI
  // { key: 'AzureStorage', label: 'Azure Storage', description: 'Azure Blob Storage connection' },
  { key: 'AzureFileShare', label: 'Azure File Share', description: 'Azure File Share connections for source systems' },
  { key: 'GL07', label: 'GL07 Report', description: 'GL07 report configuration defaults' },
];

// Prefix for Azure File Share connections
const AZURE_FILE_SHARE_PREFIX = 'AzureFileShare:';

// Connection type for Azure File Share
interface FileShareConnection {
  url: string;
  token: string;
}

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
  
  // Azure File Share connection management
  const [fileShareConnections, setFileShareConnections] = useState<Record<string, FileShareConnection>>({});
  const [originalFileShareConnections, setOriginalFileShareConnections] = useState<Record<string, FileShareConnection>>({});
  const [newConnectionName, setNewConnectionName] = useState('');
  const [newConnectionUrl, setNewConnectionUrl] = useState('');
  const [newConnectionToken, setNewConnectionToken] = useState('');
  const [deletedConnections, setDeletedConnections] = useState<string[]>([]);
  const [showTokenValues, setShowTokenValues] = useState<Record<string, boolean>>({});

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
      
      // Extract Azure File Share connections
      // Settings are stored as: AzureFileShare:{name}:Url and AzureFileShare:{name}:Token
      const connectionMap: Record<string, FileShareConnection> = {};
      const connectionNames = new Set<string>();
      
      Object.keys(valueMap).forEach((key) => {
        if (key.startsWith(AZURE_FILE_SHARE_PREFIX) && key !== 'AzureFileShare') {
          // Parse: AzureFileShare:{name}:Url or AzureFileShare:{name}:Token
          const parts = key.split(':');
          if (parts.length === 3) {
            const connName = parts[1];
            const field = parts[2]; // 'Url' or 'Token'
            connectionNames.add(connName);
            
            if (!connectionMap[connName]) {
              connectionMap[connName] = { url: '', token: '' };
            }
            
            if (field === 'Url') {
              connectionMap[connName].url = valueMap[key];
            } else if (field === 'Token') {
              connectionMap[connName].token = valueMap[key];
            }
          }
        }
      });
      setFileShareConnections(connectionMap);
      setOriginalFileShareConnections(JSON.parse(JSON.stringify(connectionMap)));
      setDeletedConnections([]);

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
    // Check regular settings
    const settingsChanged = Object.keys(values).some((key) => values[key] !== originalValues[key]);
    
    // Check connection changes
    const connectionsChanged = Object.keys(fileShareConnections).some(
      (name) => {
        const current = fileShareConnections[name];
        const original = originalFileShareConnections[name];
        return !original || current.url !== original.url || current.token !== original.token;
      }
    );
    const connectionsAdded = Object.keys(fileShareConnections).some(
      (name) => !(name in originalFileShareConnections)
    );
    const connectionsDeleted = deletedConnections.length > 0;
    
    return settingsChanged || connectionsChanged || connectionsAdded || connectionsDeleted;
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
      
      // Save new/changed Azure File Share connections
      let connectionChanges = 0;
      for (const connName of Object.keys(fileShareConnections)) {
        const current = fileShareConnections[connName];
        const original = originalFileShareConnections[connName];
        
        // Save URL if changed
        if (!original || current.url !== original.url) {
          const urlKey = `${AZURE_FILE_SHARE_PREFIX}${connName}:Url`;
          await apiClient.put(`/settings/${encodeURIComponent(urlKey)}`, {
            value: current.url,
            description: `URL for Azure File Share connection: ${connName}`,
            isEncrypted: false,
          });
          connectionChanges++;
        }
        
        // Save Token if changed
        if (!original || current.token !== original.token) {
          const tokenKey = `${AZURE_FILE_SHARE_PREFIX}${connName}:Token`;
          await apiClient.put(`/settings/${encodeURIComponent(tokenKey)}`, {
            value: current.token,
            description: `SAS token for Azure File Share connection: ${connName}`,
            isEncrypted: true,
          });
          connectionChanges++;
        }
      }
      
      // Delete removed connections
      for (const connName of deletedConnections) {
        const urlKey = `${AZURE_FILE_SHARE_PREFIX}${connName}:Url`;
        const tokenKey = `${AZURE_FILE_SHARE_PREFIX}${connName}:Token`;
        try {
          await apiClient.delete(`/settings/${encodeURIComponent(urlKey)}`);
          await apiClient.delete(`/settings/${encodeURIComponent(tokenKey)}`);
        } catch {
          // Ignore errors when deleting (might not exist)
        }
      }

      setOriginalValues({ ...values });
      setOriginalFileShareConnections(JSON.parse(JSON.stringify(fileShareConnections)));
      setDeletedConnections([]);
      
      const totalChanges = changedSettings.length + connectionChanges + deletedConnections.length;
      setSuccess(`${totalChanges} setting(s) saved successfully`);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save settings';
      setError(errorMessage);
    } finally {
      setSaving(false);
    }
  };

  const handleReset = () => {
    setValues({ ...originalValues });
    setFileShareConnections(JSON.parse(JSON.stringify(originalFileShareConnections)));
    setDeletedConnections([]);
    setNewConnectionName('');
    setNewConnectionUrl('');
    setNewConnectionToken('');
  };

  const togglePasswordVisibility = (key: string) => {
    setShowPasswords((prev) => ({ ...prev, [key]: !prev[key] }));
  };
  
  // Azure File Share connection handlers
  const handleAddConnection = () => {
    const trimmedName = newConnectionName.trim();
    if (!trimmedName || !newConnectionUrl || !newConnectionToken) return;
    
    // Validate connection name (alphanumeric, dash, underscore)
    if (!/^[a-zA-Z0-9_-]+$/.test(trimmedName)) {
      setError('Connection name can only contain letters, numbers, dashes, and underscores');
      return;
    }
    
    if (trimmedName in fileShareConnections) {
      setError(`Connection '${trimmedName}' already exists`);
      return;
    }
    
    setFileShareConnections((prev) => ({ 
      ...prev, 
      [trimmedName]: { url: newConnectionUrl, token: newConnectionToken } 
    }));
    // Remove from deleted list if it was previously deleted
    setDeletedConnections((prev) => prev.filter((n) => n !== trimmedName));
    setNewConnectionName('');
    setNewConnectionUrl('');
    setNewConnectionToken('');
  };
  
  const handleDeleteConnection = (connName: string) => {
    setFileShareConnections((prev) => {
      const updated = { ...prev };
      delete updated[connName];
      return updated;
    });
    // Track deletion only if it was an original connection
    if (connName in originalFileShareConnections) {
      setDeletedConnections((prev) => [...prev, connName]);
    }
  };
  
  const handleConnectionUrlChange = (connName: string, url: string) => {
    setFileShareConnections((prev) => ({ 
      ...prev, 
      [connName]: { ...prev[connName], url } 
    }));
  };
  
  const handleConnectionTokenChange = (connName: string, token: string) => {
    setFileShareConnections((prev) => ({ 
      ...prev, 
      [connName]: { ...prev[connName], token } 
    }));
  };
  
  const toggleTokenVisibility = (connName: string) => {
    setShowTokenValues((prev) => ({ ...prev, [connName]: !prev[connName] }));
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
        disabled={def.locked}
        slotProps={{
          input: {
            endAdornment: def.locked ? (
              <InputAdornment position="end">
                <Tooltip title="This value is fixed and cannot be changed">
                  <LockIcon fontSize="small" color="action" />
                </Tooltip>
              </InputAdornment>
            ) : undefined,
          },
        }}
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
      {CATEGORIES.map((category, index) => {
        // Special handling for Azure File Share - dynamic connection management
        if (category.key === 'AzureFileShare') {
          const connectionNames = Object.keys(fileShareConnections);
          const hasConnectionChanges = connectionNames.some((name) => {
            const current = fileShareConnections[name];
            const original = originalFileShareConnections[name];
            return !original || current.url !== original.url || current.token !== original.token;
          }) || deletedConnections.length > 0;
          
          return (
            <Accordion 
              key={category.key} 
              defaultExpanded={false}
              sx={{ 
                mb: 2, 
                '&:before': { display: 'none' },
                boxShadow: 1,
              }}
            >
              <AccordionSummary
                expandIcon={<ExpandMoreIcon sx={{ color: '#ffffff' }} />}
                sx={{ 
                  bgcolor: '#003d4d',
                  '&:hover': { bgcolor: '#00515f' },
                  '&.Mui-expanded': { bgcolor: '#003d4d' },
                }}
              >
                <Box>
                  <Typography variant="h6" fontWeight={600} sx={{ color: '#ffffff' }}>
                    {category.label}
                    {hasConnectionChanges && (
                      <Typography component="span" sx={{ ml: 1, color: 'warning.light', fontSize: '0.75rem' }}>
                        (modified)
                      </Typography>
                    )}
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'rgba(255, 255, 255, 0.9)' }}>
                    {category.description}
                  </Typography>
                </Box>
              </AccordionSummary>
              <AccordionDetails sx={{ p: 2 }}>
                {/* Add new connection form */}
                <Box sx={{ display: 'flex', gap: 2, mb: 2, alignItems: 'flex-start', flexWrap: 'wrap' }}>
                  <TextField
                    size="small"
                    label="Connection Name"
                    placeholder="e.g., pigello-prod"
                    value={newConnectionName}
                    onChange={(e) => setNewConnectionName(e.target.value)}
                    helperText="Alphanumeric, dashes, underscores"
                    sx={{ flex: '1 1 150px', minWidth: 150 }}
                  />
                  <TextField
                    size="small"
                    label="File Share URL"
                    placeholder="https://account.file.core.windows.net/share"
                    value={newConnectionUrl}
                    onChange={(e) => setNewConnectionUrl(e.target.value)}
                    helperText="Base URL without SAS token"
                    sx={{ flex: '2 1 250px', minWidth: 250 }}
                  />
                  <TextField
                    size="small"
                    label="SAS Token"
                    placeholder="sv=2022-11-02&ss=f&srt=..."
                    type="password"
                    value={newConnectionToken}
                    onChange={(e) => setNewConnectionToken(e.target.value)}
                    helperText="Will be encrypted"
                    sx={{ flex: '2 1 250px', minWidth: 250 }}
                  />
                  <Button
                    variant="contained"
                    startIcon={<AddIcon />}
                    onClick={handleAddConnection}
                    disabled={!newConnectionName.trim() || !newConnectionUrl || !newConnectionToken}
                    sx={{ mt: 0.5 }}
                  >
                    Add
                  </Button>
                </Box>
                
                {/* Existing connections table */}
                {connectionNames.length > 0 ? (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow sx={{ bgcolor: 'grey.50' }}>
                          <TableCell width="15%">
                            <Typography variant="subtitle2" fontWeight={600}>Connection Name</Typography>
                          </TableCell>
                          <TableCell width="35%">
                            <Typography variant="subtitle2" fontWeight={600}>URL</Typography>
                          </TableCell>
                          <TableCell width="35%">
                            <Typography variant="subtitle2" fontWeight={600}>SAS Token</Typography>
                          </TableCell>
                          <TableCell width="8%">
                            <Typography variant="subtitle2" fontWeight={600}>Status</Typography>
                          </TableCell>
                          <TableCell width="7%" align="center">
                            <Typography variant="subtitle2" fontWeight={600}>Actions</Typography>
                          </TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {connectionNames.map((connName) => {
                          const current = fileShareConnections[connName];
                          const original = originalFileShareConnections[connName];
                          const isNew = !original;
                          const isModified = !isNew && (current.url !== original.url || current.token !== original.token);
                          
                          return (
                            <TableRow 
                              key={connName}
                              sx={{ bgcolor: (isNew || isModified) ? 'action.hover' : 'inherit' }}
                            >
                              <TableCell>
                                <Typography variant="body2" fontWeight={500}>
                                  {connName}
                                </Typography>
                              </TableCell>
                              <TableCell>
                                <TextField
                                  size="small"
                                  fullWidth
                                  value={current.url}
                                  onChange={(e) => handleConnectionUrlChange(connName, e.target.value)}
                                  placeholder="https://account.file.core.windows.net/share"
                                />
                              </TableCell>
                              <TableCell>
                                <TextField
                                  size="small"
                                  fullWidth
                                  type={showTokenValues[connName] ? 'text' : 'password'}
                                  value={current.token}
                                  onChange={(e) => handleConnectionTokenChange(connName, e.target.value)}
                                  slotProps={{
                                    input: {
                                      endAdornment: (
                                        <InputAdornment position="end">
                                          <IconButton
                                            size="small"
                                            onClick={() => toggleTokenVisibility(connName)}
                                          >
                                            {showTokenValues[connName] ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                                          </IconButton>
                                        </InputAdornment>
                                      ),
                                    },
                                  }}
                                />
                              </TableCell>
                              <TableCell>
                                {isNew ? (
                                  <Typography variant="caption" color="success.main" fontWeight={500}>New</Typography>
                                ) : isModified ? (
                                  <Typography variant="caption" color="warning.main" fontWeight={500}>Modified</Typography>
                                ) : (
                                  <Tooltip title="Configured">
                                    <CheckCircleIcon fontSize="small" color="success" />
                                  </Tooltip>
                                )}
                              </TableCell>
                              <TableCell align="center">
                                <IconButton
                                  size="small"
                                  color="error"
                                  onClick={() => handleDeleteConnection(connName)}
                                >
                                  <DeleteIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                          );
                        })}
                      </TableBody>
                    </Table>
                  </TableContainer>
                ) : (
                  <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                    No connections configured. Add a connection above to enable Azure File Share source systems.
                  </Typography>
                )}
                
                {deletedConnections.length > 0 && (
                  <Alert severity="warning" sx={{ mt: 2 }}>
                    {deletedConnections.length} connection(s) will be deleted when you save: {deletedConnections.join(', ')}
                  </Alert>
                )}
              </AccordionDetails>
            </Accordion>
          );
        }
        
        // Normal category rendering
        const categorySettings = SETTING_DEFINITIONS.filter(
          (def) => def.category === category.key && shouldShowSetting(def)
        );

        if (categorySettings.length === 0) return null;

        return (
          <Accordion 
            key={category.key} 
            defaultExpanded={index === 0}
            sx={{ 
              mb: 2, 
              '&:before': { display: 'none' },
              boxShadow: 1,
            }}
          >
            <AccordionSummary
              expandIcon={<ExpandMoreIcon sx={{ color: '#ffffff' }} />}
              sx={{ 
                bgcolor: '#003d4d',
                '&:hover': { bgcolor: '#00515f' },
                '&.Mui-expanded': { bgcolor: '#003d4d' },
              }}
            >
              <Box>
                <Typography variant="h6" fontWeight={600} sx={{ color: '#ffffff' }}>
                  {category.label}
                </Typography>
                <Typography variant="body2" sx={{ color: 'rgba(255, 255, 255, 0.9)' }}>
                  {category.description}
                </Typography>
              </Box>
            </AccordionSummary>
            <AccordionDetails sx={{ p: 0 }}>
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
            </AccordionDetails>
          </Accordion>
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
