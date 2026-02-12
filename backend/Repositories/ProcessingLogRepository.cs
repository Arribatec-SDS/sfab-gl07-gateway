using Dapper;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Services;

namespace A1arErpSfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for ProcessingLog using Dapper.
/// </summary>
public class ProcessingLogRepository : IProcessingLogRepository
{
    private readonly IScopedDbConnectionProvider _connectionProvider;
    private readonly ILogger<ProcessingLogRepository> _logger;

    public ProcessingLogRepository(
        IScopedDbConnectionProvider connectionProvider,
        ILogger<ProcessingLogRepository> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<ProcessingLog>> GetAllAsync(int? limit = 100)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        var sql = @"
            SELECT TOP (@Limit) 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs, p.TaskExecutionId,
                s.Id, s.SystemCode, s.SystemName, s.FolderPath, s.TransformerType, s.FilePattern, 
                s.IsActive, s.Description, s.CreatedAt, s.UpdatedAt
            FROM ProcessingLog p
            INNER JOIN SourceSystems s ON p.SourceSystemId = s.Id
            ORDER BY p.ProcessedAt DESC";

        return await connection.QueryAsync<ProcessingLog, SourceSystem, ProcessingLog>(
            sql,
            (log, sourceSystem) =>
            {
                log.SourceSystem = sourceSystem;
                return log;
            },
            new { Limit = limit ?? 100 },
            splitOn: "Id");
    }

    public async Task<IEnumerable<ProcessingLog>> GetBySourceSystemAsync(int sourceSystemId, int? limit = 100)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        var sql = @"
            SELECT TOP (@Limit) 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs, p.TaskExecutionId,
                s.Id, s.SystemCode, s.SystemName, s.FolderPath, s.TransformerType, s.FilePattern, 
                s.IsActive, s.Description, s.CreatedAt, s.UpdatedAt
            FROM ProcessingLog p
            INNER JOIN SourceSystems s ON p.SourceSystemId = s.Id
            WHERE p.SourceSystemId = @SourceSystemId
            ORDER BY p.ProcessedAt DESC";

        return await connection.QueryAsync<ProcessingLog, SourceSystem, ProcessingLog>(
            sql,
            (log, sourceSystem) =>
            {
                log.SourceSystem = sourceSystem;
                return log;
            },
            new { SourceSystemId = sourceSystemId, Limit = limit ?? 100 },
            splitOn: "Id");
    }

    public async Task<IEnumerable<ProcessingLog>> GetByStatusAsync(string status, int? limit = 100)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        var sql = @"
            SELECT TOP (@Limit) 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs, p.TaskExecutionId,
                s.Id, s.SystemCode, s.SystemName, s.FolderPath, s.TransformerType, s.FilePattern, 
                s.IsActive, s.Description, s.CreatedAt, s.UpdatedAt
            FROM ProcessingLog p
            INNER JOIN SourceSystems s ON p.SourceSystemId = s.Id
            WHERE p.Status = @Status
            ORDER BY p.ProcessedAt DESC";

        return await connection.QueryAsync<ProcessingLog, SourceSystem, ProcessingLog>(
            sql,
            (log, sourceSystem) =>
            {
                log.SourceSystem = sourceSystem;
                return log;
            },
            new { Status = status, Limit = limit ?? 100 },
            splitOn: "Id");
    }

    public async Task<ProcessingLog?> GetByIdAsync(int id)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        var sql = @"
            SELECT 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs, p.TaskExecutionId,
                s.Id, s.SystemCode, s.SystemName, s.FolderPath, s.TransformerType, s.FilePattern, 
                s.IsActive, s.Description, s.CreatedAt, s.UpdatedAt
            FROM ProcessingLog p
            INNER JOIN SourceSystems s ON p.SourceSystemId = s.Id
            WHERE p.Id = @Id";

        var result = await connection.QueryAsync<ProcessingLog, SourceSystem, ProcessingLog>(
            sql,
            (log, sourceSystem) =>
            {
                log.SourceSystem = sourceSystem;
                return log;
            },
            new { Id = id },
            splitOn: "Id");

        return result.FirstOrDefault();
    }

    public async Task<int> CreateAsync(ProcessingLog log)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(@"
            INSERT INTO ProcessingLog (SourceSystemId, FileName, Status, VoucherCount, TransactionCount, ErrorMessage, ProcessedAt, DurationMs, TaskExecutionId)
            OUTPUT INSERTED.Id
            VALUES (@SourceSystemId, @FileName, @Status, @VoucherCount, @TransactionCount, @ErrorMessage, GETUTCDATE(), @DurationMs, @TaskExecutionId)",
            log);
    }

    public async Task CreateBatchAsync(IEnumerable<ProcessingLog> logs)
    {
        var logList = logs.ToList();
        if (!logList.Any()) return;

        var connection = await _connectionProvider.GetConnectionAsync();
        await connection.ExecuteAsync(@"
            INSERT INTO ProcessingLog (SourceSystemId, FileName, Status, VoucherCount, TransactionCount, ErrorMessage, ProcessedAt, DurationMs, TaskExecutionId)
            VALUES (@SourceSystemId, @FileName, @Status, @VoucherCount, @TransactionCount, @ErrorMessage, GETUTCDATE(), @DurationMs, @TaskExecutionId)",
            logList);
    }

    public async Task UpdateAsync(ProcessingLog log)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await connection.ExecuteAsync(@"
            UPDATE ProcessingLog 
            SET Status = @Status, 
                VoucherCount = @VoucherCount, 
                TransactionCount = @TransactionCount, 
                ErrorMessage = @ErrorMessage, 
                DurationMs = @DurationMs 
            WHERE Id = @Id",
            log);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.ExecuteAsync(@"
            DELETE FROM ProcessingLog 
            WHERE ProcessedAt < @CutoffDate",
            new { CutoffDate = cutoffDate });
    }

    public async Task DeleteAsync(int id)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await connection.ExecuteAsync(@"
            DELETE FROM ProcessingLog 
            WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<int> DeleteByTaskExecutionIdAsync(Guid taskExecutionId)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.ExecuteAsync(@"
            DELETE FROM ProcessingLog 
            WHERE TaskExecutionId = @TaskExecutionId",
            new { TaskExecutionId = taskExecutionId });
    }
}
