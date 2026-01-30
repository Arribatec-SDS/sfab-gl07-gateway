using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Service interface for managing application settings with encryption support.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Get all settings, optionally masking sensitive values.
    /// </summary>
    Task<IEnumerable<AppSettingDto>> GetAllAsync(bool maskSensitive = true);
    
    /// <summary>
    /// Get settings by category.
    /// </summary>
    Task<IEnumerable<AppSettingDto>> GetByCategoryAsync(string category, bool maskSensitive = true);
    
    /// <summary>
    /// Get a single setting value, optionally decrypting if sensitive.
    /// </summary>
    Task<string?> GetValueAsync(string paramName, bool decrypt = true);
    
    /// <summary>
    /// Set a setting value, automatically encrypting if marked as sensitive.
    /// </summary>
    Task SetValueAsync(string paramName, string? value);
    
    /// <summary>
    /// Get a strongly-typed settings group by category.
    /// </summary>
    Task<T> GetSettingsGroupAsync<T>(string category) where T : new();
}
