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

    private const string SelectColumns = @"
        s.Id, s.SystemCode, s.SystemName, s.Provider, s.FolderPath, s.TransformerType, s.FilePattern, 
        s.IsActive, s.Description, s.Gl07ReportSetupId, s.Interface, s.TransactionType, 
        s.BatchId, s.CreatedAt, s.UpdatedAt";

    public SourceSystemRepository(
        IContextAwareDatabaseService dbService,
        ILogger<SourceSystemRepository> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<IEnumerable<SourceSystem>> GetAllAsync()
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   g.Id, g.SetupCode, g.SetupName
            FROM SourceSystems s
            LEFT JOIN Gl07ReportSetups g ON s.Gl07ReportSetupId = g.Id
            ORDER BY s.SystemName",
            (sourceSystem, reportSetup) =>
            {
                sourceSystem.Gl07ReportSetup = reportSetup;
                return sourceSystem;
            },
            splitOn: "Id");
    }

    public async Task<IEnumerable<SourceSystem>> GetActiveAsync()
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   g.Id, g.SetupCode, g.SetupName
            FROM SourceSystems s
            LEFT JOIN Gl07ReportSetups g ON s.Gl07ReportSetupId = g.Id
            WHERE s.IsActive = 1 
            ORDER BY s.SystemName",
            (sourceSystem, reportSetup) =>
            {
                sourceSystem.Gl07ReportSetup = reportSetup;
                return sourceSystem;
            },
            splitOn: "Id");
    }

    public async Task<SourceSystem?> GetByIdAsync(int id)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        var results = await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   g.Id, g.SetupCode, g.SetupName
            FROM SourceSystems s
            LEFT JOIN Gl07ReportSetups g ON s.Gl07ReportSetupId = g.Id
            WHERE s.Id = @Id",
            (sourceSystem, reportSetup) =>
            {
                sourceSystem.Gl07ReportSetup = reportSetup;
                return sourceSystem;
            },
            new { Id = id },
            splitOn: "Id");

        return results.FirstOrDefault();
    }

    public async Task<SourceSystem?> GetByCodeAsync(string systemCode)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        var results = await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   g.Id, g.SetupCode, g.SetupName
            FROM SourceSystems s
            LEFT JOIN Gl07ReportSetups g ON s.Gl07ReportSetupId = g.Id
            WHERE s.SystemCode = @SystemCode",
            (sourceSystem, reportSetup) =>
            {
                sourceSystem.Gl07ReportSetup = reportSetup;
                return sourceSystem;
            },
            new { SystemCode = systemCode },
            splitOn: "Id");

        return results.FirstOrDefault();
    }

    public async Task<int> CreateAsync(SourceSystem sourceSystem)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        return await connection.QuerySingleAsync<int>(@"
            INSERT INTO SourceSystems (SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, 
                                       IsActive, Description, Gl07ReportSetupId, Interface, TransactionType, 
                                       BatchId, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@SystemCode, @SystemName, @Provider, @FolderPath, @TransformerType, @FilePattern, 
                    @IsActive, @Description, @Gl07ReportSetupId, @Interface, @TransactionType,
                    @BatchId, GETUTCDATE(), GETUTCDATE())",
            sourceSystem);
    }

    public async Task UpdateAsync(SourceSystem sourceSystem)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
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
                Gl07ReportSetupId = @Gl07ReportSetupId,
                Interface = @Interface,
                TransactionType = @TransactionType,
                BatchId = @BatchId,
                UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id",
            sourceSystem);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = (await _dbService.CreateProductConnectionAsync())!;
        await connection.ExecuteAsync("DELETE FROM SourceSystems WHERE Id = @Id", new { Id = id });
    }
}
