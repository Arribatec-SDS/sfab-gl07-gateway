using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository interface for ProcessingLog operations.
/// </summary>
public interface IProcessingLogRepository
{
    Task<IEnumerable<ProcessingLog>> GetAllAsync(int? limit = 100);
    Task<IEnumerable<ProcessingLog>> GetBySourceSystemAsync(int sourceSystemId, int? limit = 100);
    Task<IEnumerable<ProcessingLog>> GetByStatusAsync(string status, int? limit = 100);
    Task<ProcessingLog?> GetByIdAsync(int id);
    Task<int> CreateAsync(ProcessingLog log);
    Task CreateBatchAsync(IEnumerable<ProcessingLog> logs);
    Task UpdateAsync(ProcessingLog log);
    Task<int> DeleteOlderThanAsync(DateTime cutoffDate);
}
