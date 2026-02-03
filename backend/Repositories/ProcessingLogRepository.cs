using Arribatec.Nexus.Client.Services;
using Dapper;
using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for ProcessingLog using Dapper.
/// </summary>
public class ProcessingLogRepository : IProcessingLogRepository
{
    private readonly IContextAwareDatabaseService _dbService;
    private readonly ILogger<ProcessingLogRepository> _logger;

    public ProcessingLogRepository(
        IContextAwareDatabaseService dbService,
        ILogger<ProcessingLogRepository> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<IEnumerable<ProcessingLog>> GetAllAsync(int? limit = 100)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        var sql = @"
            SELECT TOP (@Limit) 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs,
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
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        var sql = @"
            SELECT TOP (@Limit) 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs,
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
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        var sql = @"
            SELECT TOP (@Limit) 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs,
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
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        var sql = @"
            SELECT 
                p.Id, p.SourceSystemId, p.FileName, p.Status, p.VoucherCount, p.TransactionCount, 
                p.ErrorMessage, p.ProcessedAt, p.DurationMs,
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
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QuerySingleAsync<int>(@"
            INSERT INTO ProcessingLog (SourceSystemId, FileName, Status, VoucherCount, TransactionCount, ErrorMessage, ProcessedAt, DurationMs)
            OUTPUT INSERTED.Id
            VALUES (@SourceSystemId, @FileName, @Status, @VoucherCount, @TransactionCount, @ErrorMessage, GETUTCDATE(), @DurationMs)",
            log);
    }

    public async Task CreateBatchAsync(IEnumerable<ProcessingLog> logs)
    {
        var logList = logs.ToList();
        if (!logList.Any()) return;

        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        await connection.ExecuteAsync(@"
            INSERT INTO ProcessingLog (SourceSystemId, FileName, Status, VoucherCount, TransactionCount, ErrorMessage, ProcessedAt, DurationMs)
            VALUES (@SourceSystemId, @FileName, @Status, @VoucherCount, @TransactionCount, @ErrorMessage, GETUTCDATE(), @DurationMs)",
            logList);
    }

    public async Task UpdateAsync(ProcessingLog log)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
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
}
