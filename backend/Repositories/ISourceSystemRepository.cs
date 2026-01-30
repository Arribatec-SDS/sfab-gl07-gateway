using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository interface for SourceSystem operations.
/// </summary>
public interface ISourceSystemRepository
{
    Task<IEnumerable<SourceSystem>> GetAllAsync();
    Task<IEnumerable<SourceSystem>> GetActiveAsync();
    Task<SourceSystem?> GetByIdAsync(int id);
    Task<SourceSystem?> GetByCodeAsync(string systemCode);
    Task<int> CreateAsync(SourceSystem sourceSystem);
    Task UpdateAsync(SourceSystem sourceSystem);
    Task DeleteAsync(int id);
}
