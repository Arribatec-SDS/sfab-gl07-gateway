import { useAuth } from '@arribatec-sds/arribatec-nexus-react';
import {
    CheckCircle as CheckCircleIcon,
    Download as DownloadIcon,
    Error as ErrorIcon,
    ExpandLess as ExpandLessIcon,
    ExpandMore as ExpandMoreIcon,
    Info as InfoIcon,
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

interface ProcessingLog {
  id: number;
  sourceSystemId: number;
  sourceSystemName: string | null;
  fileName: string;
  status: string;
  voucherCount: number | null;
  transactionCount: number | null;
  errorMessage: string | null;
  processedAt: string;
  durationMs: number | null;
  taskExecutionId: string | null;
}

type LogStatus = 'Success' | 'Failed' | 'Warning' | 'Processing' | '';

export default function ProcessingLogsPage() {
  const { getToken } = useAuth();
  const [logs, setLogs] = useState<ProcessingLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);
  const [statusFilter, setStatusFilter] = useState<LogStatus>('');
  const [expandedRows, setExpandedRows] = useState<Set<number>>(new Set());
  const [downloadingLogId, setDownloadingLogId] = useState<number | null>(null);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      const params = new URLSearchParams();
      params.append('page', (page + 1).toString());
      params.append('pageSize', rowsPerPage.toString());
      if (statusFilter) {
        params.append('status', statusFilter);
      }
      
      const response = await apiClient.get<ProcessingLog[]>(`/processinglogs?${params.toString()}`);
      setLogs(response.data);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load processing logs';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [getToken, page, rowsPerPage, statusFilter]);

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

  const toggleRowExpansion = (id: number) => {
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

  const handleDownloadLog = async (log: ProcessingLog) => {
    if (!log.taskExecutionId) return;
    
    setDownloadingLogId(log.id);
    try {
      const token = await getToken();
      const apiClient = createApiClient();
      apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      const response = await apiClient.get(`/worker/logs/${log.taskExecutionId}`, {
        responseType: 'blob',
      });
      
      // Create download link
      const blob = new Blob([response.data], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      // Format: log_YYYY-MM-DD_HH-MM-SS.log
      const processedDate = new Date(log.processedAt.endsWith('Z') ? log.processedAt : log.processedAt + 'Z');
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
      setDownloadingLogId(null);
    }
  };

  const getStatusIcon = (status: string): React.ReactElement | undefined => {
    switch (status) {
      case 'Success':
        return <CheckCircleIcon sx={{ color: 'success.main' }} />;
      case 'Failed':
        return <ErrorIcon sx={{ color: 'error.main' }} />;
      case 'Warning':
        return <WarningIcon sx={{ color: 'warning.main' }} />;
      case 'Processing':
        return <InfoIcon sx={{ color: 'info.main' }} />;
      default:
        return undefined;
    }
  };

  const getStatusColor = (status: string): 'success' | 'error' | 'warning' | 'info' | 'default' => {
    switch (status) {
      case 'Success':
        return 'success';
      case 'Failed':
        return 'error';
      case 'Warning':
        return 'warning';
      case 'Processing':
        return 'info';
      default:
        return 'default';
    }
  };

  const formatDate = (dateString: string): string => {
    // Database stores UTC, append 'Z' to tell JavaScript it's UTC
    const utcDate = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    return new Date(utcDate).toLocaleString();
  };

  if (loading && logs.length === 0) {
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
            <MenuItem value="Failed">Failed</MenuItem>
            <MenuItem value="Warning">Warning</MenuItem>
            <MenuItem value="Processing">Processing</MenuItem>
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

      {logs.length === 0 ? (
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
                  <TableCell>File Name</TableCell>
                  <TableCell>Source System</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Vouchers / Trans</TableCell>
                  <TableCell>Processed At</TableCell>
                  <TableCell width={60} align="center">Log</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {logs.map((log) => (
                  <React.Fragment key={log.id}>
                    <TableRow hover>
                      <TableCell>
                        <IconButton
                          size="small"
                          onClick={() => toggleRowExpansion(log.id)}
                        >
                          {expandedRows.has(log.id) ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                        </IconButton>
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" fontWeight={500}>
                          {log.fileName}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        {log.sourceSystemName || `System #${log.sourceSystemId}`}
                      </TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(log.status)}
                          label={log.status}
                          size="small"
                          color={getStatusColor(log.status)}
                        />
                      </TableCell>
                      <TableCell>
                        {log.voucherCount ?? 0} / {log.transactionCount ?? 0}
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {formatDate(log.processedAt)}
                        </Typography>
                      </TableCell>
                      <TableCell align="center">
                        {log.taskExecutionId ? (
                          <Tooltip title="Download execution log">
                            <IconButton
                              size="small"
                              onClick={() => handleDownloadLog(log)}
                              disabled={downloadingLogId === log.id}
                            >
                              {downloadingLogId === log.id ? (
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
                        <Collapse in={expandedRows.has(log.id)} timeout="auto" unmountOnExit>
                          <Box sx={{ py: 2, px: 2, backgroundColor: 'grey.50' }}>
                            <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap', mb: 2 }}>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Vouchers</Typography>
                                <Typography variant="body2" fontWeight={500}>{log.voucherCount ?? 0}</Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Transactions</Typography>
                                <Typography variant="body2" fontWeight={500}>{log.transactionCount ?? 0}</Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Duration</Typography>
                                <Typography variant="body2" fontWeight={500}>{log.durationMs ?? 0} ms</Typography>
                              </Box>
                            </Box>
                            {log.errorMessage && (
                              <Box>
                                <Typography 
                                  variant="subtitle2" 
                                  color={log.status === 'Error' ? 'error' : 'text.secondary'} 
                                  gutterBottom
                                >
                                  {log.status === 'Error' ? 'Error Message' : 'Notes'}
                                </Typography>
                                <Paper
                                  variant="outlined"
                                  sx={{
                                    p: 1,
                                    backgroundColor: log.status === 'Error' ? 'error.50' : 'grey.100',
                                    borderColor: log.status === 'Error' ? 'error.200' : 'grey.300',
                                  }}
                                >
                                  <Typography variant="body2" color={log.status === 'Error' ? 'error.main' : 'text.secondary'}>
                                    {log.errorMessage}
                                  </Typography>
                                </Paper>
                              </Box>
                            )}
                          </Box>
                        </Collapse>
                      </TableCell>
                    </TableRow>
                  </React.Fragment>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            rowsPerPageOptions={[10, 25, 50, 100]}
            component="div"
            count={-1} // Unknown total
            rowsPerPage={rowsPerPage}
            page={page}
            onPageChange={handleChangePage}
            onRowsPerPageChange={handleChangeRowsPerPage}
            labelDisplayedRows={({ from, to }) => `${from}–${to}`}
          />
        </Paper>
      )}

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
