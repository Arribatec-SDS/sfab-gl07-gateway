import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    CheckCircle as CheckCircleIcon,
    Download as DownloadIcon,
    Error as ErrorIcon,
    ExpandLess as ExpandLessIcon,
    ExpandMore as ExpandMoreIcon,
    Refresh as RefreshIcon,
    Warning as WarningIcon,
} from '@mui/icons-material';
import {
    Alert,
    Box,
    Button,
    Chip,
    CircularProgress,
    Collapse,
    IconButton,
    MenuItem,
    Paper,
    Snackbar,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TablePagination,
    TableRow,
    TextField,
    Tooltip,
    Typography,
} from '@mui/material';
import React, { useCallback, useEffect, useState } from 'react';
import { createApiClient } from '../../utils/api';

interface SourceSystemLog {
  id: number;
  sourceSystemId: number;
  sourceSystemName: string | null;
  fileName: string;
  status: string;
  voucherCount: number | null;
  transactionCount: number | null;
  errorMessage: string | null;
  durationMs: number | null;
}

interface ExecutionLog {
  taskExecutionId: string | null;
  processedAt: string;
  status: string;
  totalVouchers: number;
  totalTransactions: number;
  totalDurationMs: number;
  sourceSystemCount: number;
  sourceSystems: SourceSystemLog[];
}

type LogStatus = 'Success' | 'Error' | 'Warning' | '';

export default function ProcessingLogsPage() {
  const { getToken } = useAuth();
  const [executions, setExecutions] = useState<ExecutionLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [statusFilter, setStatusFilter] = useState<LogStatus>('');
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      const response = await apiClient.get<ExecutionLog[]>(`/processinglogs/grouped?limit=${rowsPerPage}`);
      let data = response.data;
      
      // Apply status filter client-side
      if (statusFilter) {
        data = data.filter(e => e.status === statusFilter);
      }
      
      setExecutions(data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load processing logs';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [getToken, rowsPerPage, statusFilter]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleStatusFilterChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setStatusFilter(event.target.value as LogStatus);
    setPage(0);
  };

  const toggleRowExpansion = (id: string) => {
    setExpandedRows(prev => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  const handleDownloadLog = async (execution: ExecutionLog) => {
    if (!execution.taskExecutionId) return;
    
    setDownloadingId(execution.taskExecutionId);
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      const response = await apiClient.get(`/worker/logs/${execution.taskExecutionId}`, {
        responseType: 'blob',
      });
      
      // Create download link
      const blob = new Blob([response.data], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      // Format: log_YYYY-MM-DD_HH-MM-SS.log
      const processedDate = new Date(execution.processedAt.endsWith('Z') ? execution.processedAt : execution.processedAt + 'Z');
      const timestamp = processedDate.toISOString().slice(0, 19).replace('T', '_').replace(/:/g, '-');
      link.download = `log_${timestamp}.log`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
      
      setSuccess('Log file downloaded successfully');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to download log file';
      setError(errorMessage);
    } finally {
      setDownloadingId(null);
    }
  };

  const getStatusIcon = (status: string): React.ReactElement | undefined => {
    switch (status) {
      case 'Success':
        return <CheckCircleIcon sx={{ color: 'success.main' }} />;
      case 'Error':
        return <ErrorIcon sx={{ color: 'error.main' }} />;
      case 'Warning':
        return <WarningIcon sx={{ color: 'warning.main' }} />;
      default:
        return undefined;
    }
  };

  const getStatusColor = (status: string): 'success' | 'error' | 'warning' | 'default' => {
    switch (status) {
      case 'Success':
        return 'success';
      case 'Error':
        return 'error';
      case 'Warning':
        return 'warning';
      default:
        return 'default';
    }
  };

  const formatDate = (dateString: string): string => {
    const utcDate = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    return new Date(utcDate).toLocaleString();
  };

  const formatDuration = (ms: number): string => {
    if (ms < 1000) return `${ms} ms`;
    return `${(ms / 1000).toFixed(1)} s`;
  };

  // Generate a unique key for each execution
  const getExecutionKey = (execution: ExecutionLog, index: number): string => {
    return execution.taskExecutionId || `no-task-${index}`;
  };

  if (loading && executions.length === 0) {
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
          Processing Logs
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <TextField
            select
            size="small"
            label="Status"
            value={statusFilter}
            onChange={handleStatusFilterChange}
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="">All Statuses</MenuItem>
            <MenuItem value="Success">Success</MenuItem>
            <MenuItem value="Error">Error</MenuItem>
            <MenuItem value="Warning">Warning</MenuItem>
          </TextField>
          <Button
            startIcon={<RefreshIcon />}
            variant="outlined"
            onClick={fetchLogs}
            disabled={loading}
          >
            {loading ? 'Loading...' : 'Refresh'}
          </Button>
        </Box>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {executions.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography color="text.secondary">
            No processing logs found.
          </Typography>
        </Paper>
      ) : (
        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell width={50} />
                  <TableCell>Processed At</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="center">Source Systems</TableCell>
                  <TableCell align="center">Vouchers / Trans</TableCell>
                  <TableCell align="center">Duration</TableCell>
                  <TableCell width={60} align="center">Log</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {executions.map((execution, index) => {
                  const key = getExecutionKey(execution, index);
                  const isExpanded = expandedRows.has(key);
                  
                  return (
                    <React.Fragment key={key}>
                      <TableRow hover sx={{ '& > *': { borderBottom: isExpanded ? 'none' : undefined } }}>
                        <TableCell>
                          <IconButton
                            size="small"
                            onClick={() => toggleRowExpansion(key)}
                          >
                            {isExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                          </IconButton>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontWeight={500}>
                            {formatDate(execution.processedAt)}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Chip
                            icon={getStatusIcon(execution.status)}
                            label={execution.status}
                            size="small"
                            color={getStatusColor(execution.status)}
                          />
                        </TableCell>
                        <TableCell align="center">
                          <Typography variant="body2">
                            {execution.sourceSystemCount}
                          </Typography>
                        </TableCell>
                        <TableCell align="center">
                          <Typography variant="body2">
                            {execution.totalVouchers} / {execution.totalTransactions}
                          </Typography>
                        </TableCell>
                        <TableCell align="center">
                          <Typography variant="body2" color="text.secondary">
                            {formatDuration(execution.totalDurationMs)}
                          </Typography>
                        </TableCell>
                        <TableCell align="center">
                          {execution.taskExecutionId ? (
                            <Tooltip title="Download execution log">
                              <IconButton
                                size="small"
                                onClick={() => handleDownloadLog(execution)}
                                disabled={downloadingId === execution.taskExecutionId}
                              >
                                {downloadingId === execution.taskExecutionId ? (
                                  <CircularProgress size={18} />
                                ) : (
                                  <DownloadIcon fontSize="small" />
                                )}
                              </IconButton>
                            </Tooltip>
                          ) : (
                            <Tooltip title="No execution log available">
                              <span>
                                <IconButton size="small" disabled>
                                  <DownloadIcon fontSize="small" />
                                </IconButton>
                              </span>
                            </Tooltip>
                          )}
                        </TableCell>
                      </TableRow>
                      <TableRow>
                        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={7}>
                          <Collapse in={isExpanded} timeout="auto" unmountOnExit>
                            <Box sx={{ py: 2, px: 2, backgroundColor: 'grey.50' }}>
                              <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 600 }}>
                                Source Systems
                              </Typography>
                              <Table size="small">
                                <TableHead>
                                  <TableRow>
                                    <TableCell>Source System</TableCell>
                                    <TableCell>File Name</TableCell>
                                    <TableCell>Status</TableCell>
                                    <TableCell align="center">Vouchers / Trans</TableCell>
                                    <TableCell align="center">Duration</TableCell>
                                  </TableRow>
                                </TableHead>
                                <TableBody>
                                  {execution.sourceSystems.map((ss) => (
                                    <TableRow key={ss.id}>
                                      <TableCell>
                                        <Typography variant="body2">
                                          {ss.sourceSystemName || `System #${ss.sourceSystemId}`}
                                        </Typography>
                                      </TableCell>
                                      <TableCell>
                                        <Typography variant="body2" color="text.secondary">
                                          {ss.fileName}
                                        </Typography>
                                      </TableCell>
                                      <TableCell>
                                        <Chip
                                          icon={getStatusIcon(ss.status)}
                                          label={ss.status}
                                          size="small"
                                          color={getStatusColor(ss.status)}
                                          variant="outlined"
                                        />
                                      </TableCell>
                                      <TableCell align="center">
                                        <Typography variant="body2">
                                          {ss.voucherCount ?? 0} / {ss.transactionCount ?? 0}
                                        </Typography>
                                      </TableCell>
                                      <TableCell align="center">
                                        <Typography variant="body2" color="text.secondary">
                                          {formatDuration(ss.durationMs ?? 0)}
                                        </Typography>
                                      </TableCell>
                                    </TableRow>
                                  ))}
                                </TableBody>
                              </Table>
                              {execution.sourceSystems.some(ss => ss.errorMessage) && (
                                <Box sx={{ mt: 2 }}>
                                  {execution.sourceSystems
                                    .filter(ss => ss.errorMessage)
                                    .map((ss) => (
                                      <Paper
                                        key={ss.id}
                                        variant="outlined"
                                        sx={{
                                          p: 1,
                                          mb: 1,
                                          backgroundColor: ss.status === 'Error' ? 'error.50' : 'grey.100',
                                          borderColor: ss.status === 'Error' ? 'error.200' : 'grey.300',
                                        }}
                                      >
                                        <Typography variant="caption" color="text.secondary">
                                          {ss.sourceSystemName}: 
                                        </Typography>
                                        <Typography variant="body2" color={ss.status === 'Error' ? 'error.main' : 'text.secondary'}>
                                          {ss.errorMessage}
                                        </Typography>
                                      </Paper>
                                    ))}
                                </Box>
                              )}
                            </Box>
                          </Collapse>
                        </TableCell>
                      </TableRow>
                    </React.Fragment>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            rowsPerPageOptions={[10, 25, 50, 100]}
            component="div"
            count={-1}
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
            labelDisplayedRows={({ from, to }) => `${from}–${to}`}
          />
        </Paper>
      )}

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
