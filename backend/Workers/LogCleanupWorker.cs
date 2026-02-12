using Arribatec.Nexus.Client.TaskExecution;
using A1arErpSfabGl07Gateway.Api.Repositories;
using A1arErpSfabGl07Gateway.Api.Services;

namespace A1arErpSfabGl07Gateway.Api.Workers;

/// <summary>
/// Parameters for the log cleanup task.
/// </summary>
public record LogCleanupParameters
{
    /// <summary>
    /// Override retention days. If null, uses AppSettings value.
    /// </summary>
    public int? RetentionDays { get; init; }
}

/// <summary>
/// Background worker that cleans up old processing logs.
/// </summary>
[TaskHandler("log-cleanup",
    Name = "Log Cleanup",
    Description = "Deletes processing logs older than the configured retention period")]
public class LogCleanupWorker : ITaskHandler<LogCleanupParameters>
{
    private const int MinimumRetentionDays = 7;
    private const int DefaultRetentionDays = 90;

    private readonly ILogger<LogCleanupWorker> _logger;
    private readonly ITaskContext _context;
    private readonly IProcessingLogRepository _processingLogRepository;
    private readonly IAppSettingsService _appSettingsService;

    public LogCleanupWorker(
        ILogger<LogCleanupWorker> logger,
        ITaskContext context,
        IProcessingLogRepository processingLogRepository,
        IAppSettingsService appSettingsService)
    {
        _logger = logger;
        _context = context;
        _processingLogRepository = processingLogRepository;
        _appSettingsService = appSettingsService;
    }

    public async Task ExecuteAsync(LogCleanupParameters parameters, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║                    Log Cleanup Started                         ║");
        _logger.LogInformation("╠════════════════════════════════════════════════════════════════╣");
        _logger.LogInformation("║  Task ID: {TaskId}", _context.TaskExecutionId);
        _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

        try
        {
            // Get retention days from parameter or settings
            var retentionDays = parameters.RetentionDays
                ?? await GetRetentionDaysFromSettingsAsync();

            // Enforce minimum retention
            if (retentionDays < MinimumRetentionDays)
            {
                _logger.LogWarning("Retention days {Requested} is below minimum. Using minimum of {Minimum} days.",
                    retentionDays, MinimumRetentionDays);
                retentionDays = MinimumRetentionDays;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            _logger.LogInformation("Deleting processing logs older than {CutoffDate} ({RetentionDays} days retention)",
                cutoffDate.ToString("yyyy-MM-dd HH:mm:ss"), retentionDays);

            var deletedCount = await _processingLogRepository.DeleteOlderThanAsync(cutoffDate);

            _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║                    Log Cleanup Complete                        ║");
            _logger.LogInformation("╠════════════════════════════════════════════════════════════════╣");
            _logger.LogInformation("║  Deleted: {DeletedCount} log entries", deletedCount);
            _logger.LogInformation("║  Retention: {RetentionDays} days", retentionDays);
            _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log cleanup failed");
            throw;
        }
    }

    private async Task<int> GetRetentionDaysFromSettingsAsync()
    {
        try
        {
            var value = await _appSettingsService.GetValueAsync("LogRetention:Days");
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var days))
            {
                return days;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read LogRetention:Days setting, using default");
        }

        return DefaultRetentionDays;
    }
}
