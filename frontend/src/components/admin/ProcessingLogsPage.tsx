import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Paper,
  Typography,
  Button,
  Alert,
  Snackbar,
  CircularProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Chip,
  TextField,
  MenuItem,
  IconButton,
  Collapse,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
} from '@mui/icons-material';
import { createApiClient } from '../../utils/api';
import { useAuth } from '@arribatec-sds/arribatec-nexus-react';

interface SourceSystem {
  id: number;
  name: string;
}

interface ProcessingLog {
  id: number;
  sourceSystemId: number;
  sourceSystem: SourceSystem | null;
  fileName: string;
  status: string;
  message: string | null;
  recordsProcessed: number;
  unit4Response: string | null;
  processedAt: string;
  createdAt: string;
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
    return new Date(dateString).toLocaleString();
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
                  <TableCell>Records</TableCell>
                  <TableCell>Processed At</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {logs.map((log) => (
                  <>
                    <TableRow key={log.id} hover>
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
                        {log.sourceSystem?.name || `System #${log.sourceSystemId}`}
                      </TableCell>
                      <TableCell>
                        <Chip
                          icon={getStatusIcon(log.status)}
                          label={log.status}
                          size="small"
                          color={getStatusColor(log.status)}
                        />
                      </TableCell>
                      <TableCell>{log.recordsProcessed}</TableCell>
                      <TableCell>
                        <Typography variant="body2" color="text.secondary">
                          {formatDate(log.processedAt)}
                        </Typography>
                      </TableCell>
                    </TableRow>
                    <TableRow>
                      <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={6}>
                        <Collapse in={expandedRows.has(log.id)} timeout="auto" unmountOnExit>
                          <Box sx={{ py: 2, px: 2, backgroundColor: 'grey.50' }}>
                            {log.message && (
                              <Box sx={{ mb: 2 }}>
                                <Typography variant="subtitle2" gutterBottom>
                                  Message
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                  {log.message}
                                </Typography>
                              </Box>
                            )}
                            {log.unit4Response && (
                              <Box>
                                <Typography variant="subtitle2" gutterBottom>
                                  Unit4 Response
                                </Typography>
                                <Paper
                                  variant="outlined"
                                  sx={{
                                    p: 1,
                                    backgroundColor: 'background.paper',
                                    maxHeight: 200,
                                    overflow: 'auto',
                                  }}
                                >
                                  <pre style={{ margin: 0, fontSize: '0.75rem', whiteSpace: 'pre-wrap' }}>
                                    {log.unit4Response}
                                  </pre>
                                </Paper>
                              </Box>
                            )}
                            {!log.message && !log.unit4Response && (
                              <Typography variant="body2" color="text.secondary">
                                No additional details available.
                              </Typography>
                            )}
                          </Box>
                        </Collapse>
                      </TableCell>
                    </TableRow>
                  </>
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
