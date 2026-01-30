using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Repositories;

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
}
