using System.Diagnostics;
using System.Text.Json;
using Arribatec.Nexus.Client.TaskExecution;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Repositories;
using A1arErpSfabGl07Gateway.Api.Services;

namespace A1arErpSfabGl07Gateway.Api.Workers;

/// <summary>
/// Parameters for the GL07 processing task.
/// </summary>
public record GL07ProcessingParameters
{
    /// <summary>
    /// Optional: Process only specific source system by code.
    /// If null, all active source systems are processed.
    /// </summary>
    public string? SourceSystemCode { get; init; }

    /// <summary>
    /// Optional: Process only a specific file by name.
    /// If null, all files in the inbox are processed.
    /// </summary>
    public string? Filename { get; init; }

    /// <summary>
    /// If true, only validates files without posting to Unit4.
    /// </summary>
    public bool DryRun { get; init; } = false;
}

/// <summary>
/// Background worker that processes files from all active source systems,
/// transforms them to Unit4 format, and posts to Unit4 REST API.
/// </summary>
[TaskHandler("gl07-process",
    Name = "GL07 Transaction Processing",
    Description = "Processes XML files from source systems and posts to Unit4 API")]
public class GL07ProcessingWorker : ITaskHandler<GL07ProcessingParameters>
{
    private readonly ILogger<GL07ProcessingWorker> _logger;
    private readonly ITaskContext _context;
    private readonly ISourceSystemRepository _sourceSystemRepository;
    private readonly IProcessingLogRepository _processingLogRepository;
    private readonly IFileSourceServiceFactory _fileSourceFactory;
    private readonly ITransformationServiceFactory _transformerFactory;
    private readonly IUnit4ApiClient _unit4Client;

    public GL07ProcessingWorker(
        ILogger<GL07ProcessingWorker> logger,
        ITaskContext context,
        ISourceSystemRepository sourceSystemRepository,
        IProcessingLogRepository processingLogRepository,
        IFileSourceServiceFactory fileSourceFactory,
        ITransformationServiceFactory transformerFactory,
        IUnit4ApiClient unit4Client)
    {
        _logger = logger;
        _context = context;
        _sourceSystemRepository = sourceSystemRepository;
        _processingLogRepository = processingLogRepository;
        _fileSourceFactory = fileSourceFactory;
        _transformerFactory = transformerFactory;
        _unit4Client = unit4Client;
    }

    public async Task ExecuteAsync(GL07ProcessingParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║              GL07 Transaction Processing Started               ║");
        _logger.LogInformation("╠════════════════════════════════════════════════════════════════╣");
        _logger.LogInformation("║  Task ID: {TaskId}", _context.TaskExecutionId);
        _logger.LogInformation("║  Dry Run: {DryRun}", parameters.DryRun);
        _logger.LogInformation("║  Source Filter: {Filter}", parameters.SourceSystemCode ?? "All active systems");
        _logger.LogInformation("║  Filename Filter: {Filename}", parameters.Filename ?? "All files");
        _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

        var overallStopwatch = Stopwatch.StartNew();
        var totalFilesProcessed = 0;
        var totalFilesSuccess = 0;
        var totalFilesError = 0;

        try
        {
            // Get source systems to process
            var sourceSystems = await GetSourceSystemsAsync(parameters.SourceSystemCode);

            if (!sourceSystems.Any())
            {
                _logger.LogWarning("No active source systems found to process");
                return;
            }

            _logger.LogInformation("Processing {Count} source system(s)", sourceSystems.Count());

            foreach (var sourceSystem in sourceSystems)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (processed, success, errors) = await ProcessSourceSystemAsync(
                    sourceSystem, parameters.DryRun, parameters.Filename, cancellationToken);

                totalFilesProcessed += processed;
                totalFilesSuccess += success;
                totalFilesError += errors;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GL07 processing was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GL07 processing failed with unexpected error");
            throw;
        }
        finally
        {
            overallStopwatch.Stop();

            _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║              GL07 Processing Summary                           ║");
            _logger.LogInformation("╠════════════════════════════════════════════════════════════════╣");
            _logger.LogInformation("║  Total Files Processed: {Total}", totalFilesProcessed);
            _logger.LogInformation("║  Successful: {Success}", totalFilesSuccess);
            _logger.LogInformation("║  Failed: {Errors}", totalFilesError);
            _logger.LogInformation("║  Duration: {Duration}ms", overallStopwatch.ElapsedMilliseconds);
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
        }
    }

    private async Task<IEnumerable<SourceSystem>> GetSourceSystemsAsync(string? sourceSystemCode)
    {
        if (!string.IsNullOrEmpty(sourceSystemCode))
        {
            var specific = await _sourceSystemRepository.GetByCodeAsync(sourceSystemCode);
            if (specific == null)
            {
                _logger.LogWarning("Source system not found: {Code}", sourceSystemCode);
                return Enumerable.Empty<SourceSystem>();
            }
            if (!specific.IsActive)
            {
                _logger.LogWarning("Source system is not active: {Code}", sourceSystemCode);
                return Enumerable.Empty<SourceSystem>();
            }
            return new[] { specific };
        }

        return await _sourceSystemRepository.GetActiveAsync();
    }

    private async Task<(int processed, int success, int errors)> ProcessSourceSystemAsync(
        SourceSystem sourceSystem,
        bool dryRun,
        string? filenameFilter,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("────────────────────────────────────────────────────────────────");
        _logger.LogInformation("Processing source system: {Name} ({Code})",
            sourceSystem.SystemName, sourceSystem.SystemCode);
        _logger.LogInformation("  Provider: {Provider}", sourceSystem.Provider);
        _logger.LogInformation("  Folder: {Folder}", sourceSystem.FolderPath);
        _logger.LogInformation("  Transformer: {Transformer}", sourceSystem.TransformerType);
        _logger.LogInformation("  Pattern: {Pattern}", sourceSystem.FilePattern);

        // Log GL07 configuration
        _logger.LogInformation("  ── GL07 Configuration ──");
        if (sourceSystem.Gl07ReportSetup != null)
        {
            _logger.LogInformation("  Report Setup: {SetupCode} ({SetupName})",
                sourceSystem.Gl07ReportSetup.SetupCode, sourceSystem.Gl07ReportSetup.SetupName);
            _logger.LogInformation("  Report ID: {ReportId}", sourceSystem.Gl07ReportSetup.ReportId);
            _logger.LogInformation("  Company: {CompanyId}, User: {UserId}",
                sourceSystem.Gl07ReportSetup.CompanyId, sourceSystem.Gl07ReportSetup.UserId);
        }
        else
        {
            _logger.LogWarning("  Report Setup: NOT LOADED (ID: {SetupId})", sourceSystem.Gl07ReportSetupId);
        }
        _logger.LogInformation("  Interface: {Interface}", sourceSystem.Interface ?? "(from source file)");
        _logger.LogInformation("  TransactionType: {TransType}", sourceSystem.TransactionType ?? "(from source file)");
        _logger.LogInformation("  BatchId: {BatchId}", sourceSystem.BatchId ?? "(from source file)");

        var processed = 0;
        var success = 0;
        var errors = 0;
        var logEntries = new List<ProcessingLog>();

        try
        {
            // Get the file source service for this source system's provider
            var fileSourceService = _fileSourceFactory.GetFileSourceService(sourceSystem);

            // Get the transformer for this source system
            var transformer = _transformerFactory.GetTransformer(sourceSystem.TransformerType);

            // List files in inbox
            var files = (await fileSourceService.ListFilesAsync(sourceSystem)).ToList();

            // Filter to specific file if filename parameter is provided
            if (!string.IsNullOrEmpty(filenameFilter))
            {
                files = files.Where(f => f.Equals(filenameFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (!files.Any())
                {
                    _logger.LogWarning("  Specified file not found in inbox: {Filename}", filenameFilter);
                    return (0, 0, 0);
                }
                _logger.LogInformation("  Filtered to specific file: {Filename}", filenameFilter);
            }

            if (!files.Any())
            {
                _logger.LogInformation("  No files to process in {Folder}/inbox", sourceSystem.FolderPath);

                // Create a log entry even when no files to process, so the execution log is accessible
                var noFilesLogEntry = new ProcessingLog
                {
                    SourceSystemId = sourceSystem.Id,
                    FileName = "(no files)",
                    Status = "Success",
                    VoucherCount = 0,
                    TransactionCount = 0,
                    ErrorMessage = "No files to process",
                    DurationMs = 0,
                    TaskExecutionId = _context.TaskExecutionId
                };
                await _processingLogRepository.CreateAsync(noFilesLogEntry);

                return (0, 0, 0);
            }

            _logger.LogInformation("  Found {Count} file(s) to process", files.Count);

            foreach (var fileName in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (fileSuccess, logEntry, fileException) = await ProcessFileAsync(
                    sourceSystem, fileName, transformer, fileSourceService, dryRun, cancellationToken);

                logEntries.Add(logEntry);
                processed++;

                if (fileSuccess)
                {
                    success++;
                }
                else
                {
                    errors++;
                    // Continue processing remaining files even on error
                    if (fileException != null)
                    {
                        _logger.LogError(fileException, "  File {FileName} failed, continuing with next file", fileName);
                    }
                    else
                    {
                        _logger.LogWarning("  File {FileName} failed (Unit4 API error), continuing with next file", fileName);
                    }
                }
            }
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Transformer not found for source system: {Code}", sourceSystem.SystemCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing source system: {Code}", sourceSystem.SystemCode);
        }
        finally
        {
            // Batch save all log entries at the end (even if processing failed)
            if (logEntries.Any())
            {
                try
                {
                    _logger.LogDebug("  Saving {Count} processing log entries", logEntries.Count);
                    await _processingLogRepository.CreateBatchAsync(logEntries);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "  Failed to save processing log entries");
                }
            }
        }

        _logger.LogInformation("  Source system {Code} complete: {Success} success, {Errors} errors",
            sourceSystem.SystemCode, success, errors);

        return (processed, success, errors);
    }

    private async Task<(bool success, ProcessingLog logEntry, Exception? exception)> ProcessFileAsync(
        SourceSystem sourceSystem,
        string fileName,
        ITransformationService transformer,
        IFileSourceService fileSourceService,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var logEntry = new ProcessingLog
        {
            SourceSystemId = sourceSystem.Id,
            FileName = fileName,
            Status = "Processing",
            TaskExecutionId = _context.TaskExecutionId
        };

        try
        {
            _logger.LogInformation("    Processing file: {FileName}", fileName);

            // Download file content
            var downloadStopwatch = Stopwatch.StartNew();
            var content = await fileSourceService.DownloadAsStringAsync(sourceSystem, fileName);
            downloadStopwatch.Stop();

            var fileSizeFormatted = FormatFileSize(content.Length);
            _logger.LogInformation("    Downloaded {Size} from {Provider} in {Duration}ms",
                fileSizeFormatted, sourceSystem.Provider, downloadStopwatch.ElapsedMilliseconds);

            // Transform to Unit4 format (passing SourceSystem for configuration)
            // Returns an array - one item per XML Transaction element
            var requests = transformer.Transform(content, sourceSystem);

            var transactionCount = requests.Count;
            var voucherCount = requests
                .Select(r => r.TransactionInformation?.TransactionNumber)
                .Distinct()
                .Count();

            logEntry.VoucherCount = voucherCount;
            logEntry.TransactionCount = transactionCount;

            _logger.LogInformation("    Transformed: BatchId={BatchId}, Interface={Interface}, Vouchers={VoucherCount}, Rows={RowCount}",
                requests.FirstOrDefault()?.BatchInformation?.BatchId,
                requests.FirstOrDefault()?.BatchInformation?.Interface,
                voucherCount,
                transactionCount);

            if (dryRun)
            {
                _logger.LogInformation("    [DRY RUN] Would post {Count} rows to Unit4 API (file will remain in inbox)", transactionCount);
                logEntry.Status = "Success";
                logEntry.ErrorMessage = "Dry run - not posted, file preserved in inbox";
            }
            else
            {
                // Post to Unit4 API (array of transactions)
                var response = await _unit4Client.PostTransactionBatchAsync(requests);

                if (response.Status == "Success" || response.Status == "Accepted")
                {
                    logEntry.Status = "Success";
                    _logger.LogInformation("    ✓ Posted {Count} rows to Unit4 successfully", transactionCount);
                }
                else
                {
                    logEntry.Status = "Error";
                    logEntry.ErrorMessage = response.Message ??
                        string.Join("; ", response.Errors?.Select(e => e.Message) ?? Array.Empty<string>());
                    _logger.LogError("    ✗ Unit4 API error: {Message}", logEntry.ErrorMessage);
                }
            }

            // Move file based on result (skip during dry run to preserve files in inbox)
            if (!dryRun)
            {
                if (logEntry.Status == "Success")
                {
                    await fileSourceService.MoveToProcessedAsync(sourceSystem, fileName);
                    _logger.LogDebug("    Moved to archive folder");

                    // Save the transformed JSON alongside the XML (array format)
                    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                    var jsonContent = JsonSerializer.Serialize(requests, jsonOptions);
                    await fileSourceService.SaveJsonToArchiveAsync(sourceSystem, fileName, jsonContent);
                    _logger.LogDebug("    Saved JSON to archive folder");
                }
                else
                {
                    await fileSourceService.MoveToErrorAsync(sourceSystem, fileName);
                    _logger.LogDebug("    Moved to error folder");
                }
            }
            else
            {
                _logger.LogInformation("    [DRY RUN] File preserved in inbox - not moved");
            }

            stopwatch.Stop();
            logEntry.DurationMs = (int)stopwatch.ElapsedMilliseconds;

            return (logEntry.Status == "Success", logEntry, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logEntry.Status = "Error";
            logEntry.ErrorMessage = ex.Message;
            logEntry.DurationMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "    ✗ Error processing file: {FileName} - {ErrorMessage}", fileName, ex.Message);

            // Move to error folder (skip during dry run)
            if (!dryRun)
            {
                try
                {
                    await fileSourceService.MoveToErrorAsync(sourceSystem, fileName);
                }
                catch (Exception moveEx)
                {
                    _logger.LogError(moveEx, "    Failed to move file to error folder");
                }
            }
            else
            {
                _logger.LogInformation("    [DRY RUN] File preserved in inbox despite error");
            }

            return (false, logEntry, ex);
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {suffixes[order]}";
    }
}
