using Arribatec.Nexus.Client.Services;
using Dapper;
using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for SourceSystems using Dapper.
/// </summary>
public class SourceSystemRepository : ISourceSystemRepository
{
    private readonly IContextAwareDatabaseService _dbService;
    private readonly ILogger<SourceSystemRepository> _logger;

    public SourceSystemRepository(
        IContextAwareDatabaseService dbService,
        ILogger<SourceSystemRepository> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<IEnumerable<SourceSystem>> GetAllAsync()
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        return await connection.QueryAsync<SourceSystem>(@"
            SELECT Id, SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, IsActive, Description, CreatedAt, UpdatedAt 
            FROM SourceSystems 
            ORDER BY SystemName");
    }

    public async Task<IEnumerable<SourceSystem>> GetActiveAsync()
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        return await connection.QueryAsync<SourceSystem>(@"
            SELECT Id, SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, IsActive, Description, CreatedAt, UpdatedAt 
            FROM SourceSystems 
            WHERE IsActive = 1 
            ORDER BY SystemName");
    }

    public async Task<SourceSystem?> GetByIdAsync(int id)
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<SourceSystem>(@"
            SELECT Id, SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, IsActive, Description, CreatedAt, UpdatedAt 
            FROM SourceSystems 
            WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<SourceSystem?> GetByCodeAsync(string systemCode)
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<SourceSystem>(@"
            SELECT Id, SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, IsActive, Description, CreatedAt, UpdatedAt 
            FROM SourceSystems 
            WHERE SystemCode = @SystemCode",
            new { SystemCode = systemCode });
    }

    public async Task<int> CreateAsync(SourceSystem sourceSystem)
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        return await connection.QuerySingleAsync<int>(@"
            INSERT INTO SourceSystems (SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, IsActive, Description, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@SystemCode, @SystemName, @Provider, @FolderPath, @TransformerType, @FilePattern, @IsActive, @Description, GETUTCDATE(), GETUTCDATE())",
            sourceSystem);
    }

    public async Task UpdateAsync(SourceSystem sourceSystem)
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        await connection.ExecuteAsync(@"
            UPDATE SourceSystems 
            SET SystemCode = @SystemCode, 
                SystemName = @SystemName, 
                Provider = @Provider,
                FolderPath = @FolderPath, 
                TransformerType = @TransformerType, 
                FilePattern = @FilePattern, 
                IsActive = @IsActive, 
                Description = @Description, 
                UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id",
            sourceSystem);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = await _dbService.CreateProductConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM SourceSystems WHERE Id = @Id", new { Id = id });
    }
}
