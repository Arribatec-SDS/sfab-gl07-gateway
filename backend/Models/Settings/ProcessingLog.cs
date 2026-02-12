namespace A1arErpSfabGl07Gateway.Api.Models.Settings;

/// <summary>
/// Represents a log entry for file processing.
/// </summary>
public class ProcessingLog
{
    public int Id { get; set; }
    public int SourceSystemId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing"; // 'Success', 'Error', 'Processing'
    public int? VoucherCount { get; set; }
    public int? TransactionCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int? DurationMs { get; set; }
    public Guid? TaskExecutionId { get; set; }

    // Navigation property
    public SourceSystem? SourceSystem { get; set; }
}

/// <summary>
/// DTO for processing log entries.
/// </summary>
public class ProcessingLogDto
{
    public int Id { get; set; }
    public int SourceSystemId { get; set; }
    public string? SourceSystemName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? VoucherCount { get; set; }
    public int? TransactionCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int? DurationMs { get; set; }
    public Guid? TaskExecutionId { get; set; }

    public static ProcessingLogDto FromEntity(ProcessingLog entity)
    {
        return new ProcessingLogDto
        {
            Id = entity.Id,
            SourceSystemId = entity.SourceSystemId,
            SourceSystemName = entity.SourceSystem?.SystemName,
            FileName = entity.FileName,
            Status = entity.Status,
            VoucherCount = entity.VoucherCount,
            TransactionCount = entity.TransactionCount,
            ErrorMessage = entity.ErrorMessage,
            ProcessedAt = entity.ProcessedAt,
            DurationMs = entity.DurationMs,
            TaskExecutionId = entity.TaskExecutionId
        };
    }
}

/// <summary>
/// DTO for grouped execution logs - one row per TaskExecutionId with nested source system entries.
/// </summary>
public class ExecutionLogDto
{
    public Guid? TaskExecutionId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalVouchers { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalDurationMs { get; set; }
    public int SourceSystemCount { get; set; }
    public List<SourceSystemLogDto> SourceSystems { get; set; } = new();
}

/// <summary>
/// DTO for source system entries within an execution.
/// </summary>
public class SourceSystemLogDto
{
    public int Id { get; set; }
    public int SourceSystemId { get; set; }
    public string? SourceSystemName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? VoucherCount { get; set; }
    public int? TransactionCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int? DurationMs { get; set; }
}
