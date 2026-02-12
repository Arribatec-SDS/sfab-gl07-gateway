using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Repositories;

namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Service implementation for managing application settings with Data Protection API encryption.
/// </summary>
public class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _repository;
    private readonly IDataProtector _protector;
    private readonly ILogger<AppSettingsService> _logger;

    private const string EncryptedPrefix = "ENC:";

    public AppSettingsService(
        IAppSettingsRepository repository,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AppSettingsService> logger)
    {
        _repository = repository;
        _protector = dataProtectionProvider.CreateProtector("GL07Gateway.Settings");
        _logger = logger;
    }

    public async Task<IEnumerable<AppSettingDto>> GetAllAsync(bool maskSensitive = true)
    {
        var settings = await _repository.GetAllAsync();
        return settings.Select(s => AppSettingDto.FromEntity(s, maskSensitive));
    }

    public async Task<IEnumerable<AppSettingDto>> GetByCategoryAsync(string category, bool maskSensitive = true)
    {
        var settings = await _repository.GetByCategoryAsync(category);
        return settings.Select(s => AppSettingDto.FromEntity(s, maskSensitive));
    }

    public async Task<string?> GetValueAsync(string paramName, bool decrypt = true)
    {
        var setting = await _repository.GetByNameAsync(paramName);
        if (setting == null)
        {
            return null;
        }

        var value = setting.ParamValue;

        // Decrypt if needed and value is encrypted
        if (decrypt && setting.Sensitive && !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix))
        {
            try
            {
                var encryptedValue = value.Substring(EncryptedPrefix.Length);
                value = _protector.Unprotect(encryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt setting {ParamName}", paramName);
                return null;
            }
        }

        return value;
    }

    public async Task SetValueAsync(string paramName, string? value)
    {
        var setting = await _repository.GetByNameAsync(paramName);

        var valueToStore = value;

        // Determine if this should be encrypted (based on existing setting or key name pattern)
        var isSensitive = setting?.Sensitive ??
            paramName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
            paramName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            paramName.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) ||
            paramName.Contains(":Token:", StringComparison.OrdinalIgnoreCase);

        // Encrypt if sensitive and has a value
        if (isSensitive && !string.IsNullOrEmpty(value))
        {
            // Don't re-encrypt if already encrypted
            if (!value.StartsWith(EncryptedPrefix))
            {
                var encrypted = _protector.Protect(value);
                valueToStore = $"{EncryptedPrefix}{encrypted}";
            }
        }

        if (setting == null)
        {
            // Create new setting via upsert
            var category = paramName.Contains(':') ? paramName.Split(':')[0] : "General";
            await _repository.UpsertAsync(new AppSetting
            {
                Category = category,
                ParamName = paramName,
                ParamValue = valueToStore,
                Description = null,
                Sensitive = isSensitive
            });
            _logger.LogInformation("Created setting: {ParamName}", paramName);
        }
        else
        {
            // Update existing setting
            await _repository.UpdateAsync(paramName, valueToStore);
            _logger.LogInformation("Updated setting: {ParamName}", paramName);
        }
    }

    public async Task DeleteAsync(string paramName)
    {
        await _repository.DeleteAsync(paramName);
        _logger.LogInformation("Deleted setting: {ParamName}", paramName);
    }

    public async Task<T> GetSettingsGroupAsync<T>(string category) where T : new()
    {
        var settings = await _repository.GetByCategoryAsync(category);
        var result = new T();
        var type = typeof(T);

        foreach (var setting in settings)
        {
            // Extract property name from ParamName (e.g., "Unit4:BaseUrl" -> "BaseUrl")
            var propertyName = setting.ParamName;
            if (propertyName.Contains(':'))
            {
                propertyName = propertyName.Split(':').Last();
            }

            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null && property.CanWrite)
            {
                var value = setting.ParamValue;

                // Decrypt if sensitive
                if (setting.Sensitive && !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix))
                {
                    try
                    {
                        var encryptedValue = value.Substring(EncryptedPrefix.Length);
                        value = _protector.Unprotect(encryptedValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to decrypt setting {ParamName} for settings group", setting.ParamName);
                        continue;
                    }
                }

                try
                {
                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(result, convertedValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set property {PropertyName} from setting {ParamName}",
                        propertyName, setting.ParamName);
                }
            }
        }

        return result;
    }
}

/// <summary>
/// Strongly-typed Unit4 settings.
/// </summary>
public class Unit4Settings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "api";
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint path for financial transaction batches (default: /v1/financial-transaction-batch)
    /// </summary>
    public string BatchEndpoint { get; set; } = "/v1/financial-transaction-batch";
}

/// <summary>
/// Strongly-typed Azure Storage settings.
/// </summary>
public class AzureStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "gl07-files";
}

/// <summary>
/// Strongly-typed File Source settings.
/// </summary>
public class FileSourceSettings
{
    public string Provider { get; set; } = "Local";
    public string LocalBasePath { get; set; } = "C:/dev/gl07-files";
}
