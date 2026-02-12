using A1arErpSfabGl07Gateway.Api.Models.Settings;

namespace A1arErpSfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository interface for AppSettings operations.
/// </summary>
public interface IAppSettingsRepository
{
    Task<IEnumerable<AppSetting>> GetAllAsync();
    Task<AppSetting?> GetByNameAsync(string paramName);
    Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category);
    Task UpdateAsync(string paramName, string? paramValue);
    Task UpsertAsync(AppSetting setting);
    Task DeleteAsync(string paramName);
}
