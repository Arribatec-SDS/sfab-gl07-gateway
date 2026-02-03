using Arribatec.Nexus.Client.Services;
using Dapper;
using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for AppSettings using Dapper.
/// </summary>
public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly IContextAwareDatabaseService _dbService;
    private readonly ILogger<AppSettingsRepository> _logger;

    public AppSettingsRepository(
        IContextAwareDatabaseService dbService,
        ILogger<AppSettingsRepository> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QueryAsync<AppSetting>(
            "SELECT Id, ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt FROM AppSettings ORDER BY Category, ParamName");
    }

    public async Task<AppSetting?> GetByNameAsync(string paramName)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QueryFirstOrDefaultAsync<AppSetting>(
            "SELECT Id, ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt FROM AppSettings WHERE ParamName = @ParamName",
            new { ParamName = paramName });
    }

    public async Task<IEnumerable<AppSetting>> GetByCategoryAsync(string category)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QueryAsync<AppSetting>(
            "SELECT Id, ParamName, ParamValue, Sensitive, Category, Description, CreatedAt, UpdatedAt FROM AppSettings WHERE Category = @Category ORDER BY ParamName",
            new { Category = category });
    }

    public async Task UpdateAsync(string paramName, string? paramValue)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        await connection.ExecuteAsync(
            "UPDATE AppSettings SET ParamValue = @ParamValue, UpdatedAt = GETUTCDATE() WHERE ParamName = @ParamName",
            new { ParamName = paramName, ParamValue = paramValue });
    }

    public async Task UpsertAsync(AppSetting setting)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;

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
}
