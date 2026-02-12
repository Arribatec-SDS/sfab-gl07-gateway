using Dapper;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Services;

namespace A1arErpSfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for AppSettings using Dapper.
/// </summary>
public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly IScopedDbConnectionProvider _connectionProvider;
    private readonly ILogger<AppSettingsRepository> _logger;

    public AppSettingsRepository(
        IScopedDbConnectionProvider connectionProvider,
        ILogger<AppSettingsRepository> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<AppSetting>(
            "SELECT Id, ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt FROM AppSettings ORDER BY Category, ParamName");
    }

    public async Task<AppSetting?> GetByNameAsync(string paramName)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<AppSetting>(
            "SELECT Id, ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt FROM AppSettings WHERE ParamName = @ParamName",
            new { ParamName = paramName });
    }

    public async Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<AppSetting>(
            "SELECT Id, ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt FROM AppSettings WHERE Category = @Category ORDER BY ParamName",
            new { Category = category });
    }

    public async Task UpdateAsync(string paramName, string? paramValue)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await connection.ExecuteAsync(
            "UPDATE AppSettings SET ParamValue = @ParamValue, UpdatedAt = GETUTCDATE() WHERE ParamName = @ParamName",
            new { ParamName = paramName, ParamValue = paramValue });
    }

    public async Task UpsertAsync(AppSetting setting)
    {
        var connection = await _connectionProvider.GetConnectionAsync();

        var existing = await GetByNameAsync(setting.ParamName);
        if (existing != null)
        {
            await connection.ExecuteAsync(@"
                UPDATE AppSettings 
                SET ParamValue = @ParamValue, 
                    Sensitive = @Sensitive, 
                    Category = @Category, 
                    Description = @Description, 
                    UpdatedAt = GETUTCDATE() 
                WHERE ParamName = @ParamName",
                setting);
        }
        else
        {
            await connection.ExecuteAsync(@"
                INSERT INTO AppSettings (ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt)
                VALUES (@ParamName, @ParamValue, @Sensitive, @Category, @Description, GETUTCDATE(), GETUTCDATE())",
                setting);
        }
    }

    public async Task DeleteAsync(string paramName)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await connection.ExecuteAsync(
            "DELETE FROM AppSettings WHERE ParamName = @ParamName",
            new { ParamName = paramName });
    }
}
