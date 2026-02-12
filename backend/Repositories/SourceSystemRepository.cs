using Dapper;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Services;

namespace A1arErpSfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for SourceSystems using Dapper.
/// </summary>
public class SourceSystemRepository : ISourceSystemRepository
{
    private readonly IScopedDbConnectionProvider _connectionProvider;
    private readonly ILogger<SourceSystemRepository> _logger;

    private const string SelectColumns = @"
        s.Id, s.SystemCode, s.SystemName, s.Provider, s.FolderPath, s.TransformerType, s.FilePattern, 
        s.IsActive, s.Description, s.Gl07ReportSetupId, s.Interface, s.TransactionType, 
        s.BatchId, s.DefaultCurrency, s.AzureFileShareConnectionName, s.CreatedAt, s.UpdatedAt";

    private const string Gl07SetupColumns = @"
        g.Id, g.SetupCode, g.SetupName, g.Description, g.ReportId, g.ReportName, g.Variant,
        g.UserId, g.CompanyId, g.Priority, g.EmailConfirmation, g.Status, g.OutputType, g.IsActive";

    public SourceSystemRepository(
        IScopedDbConnectionProvider connectionProvider,
        ILogger<SourceSystemRepository> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<SourceSystem>> GetAllAsync()
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   {Gl07SetupColumns}
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
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   {Gl07SetupColumns}
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
        var connection = await _connectionProvider.GetConnectionAsync();
        var results = await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   {Gl07SetupColumns}
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
        var connection = await _connectionProvider.GetConnectionAsync();
        var results = await connection.QueryAsync<SourceSystem, Gl07ReportSetup, SourceSystem>($@"
            SELECT {SelectColumns},
                   {Gl07SetupColumns}
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
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(@"
            INSERT INTO SourceSystems (SystemCode, SystemName, Provider, FolderPath, TransformerType, FilePattern, 
                                       IsActive, Description, Gl07ReportSetupId, Interface, TransactionType, 
                                       BatchId, DefaultCurrency, AzureFileShareConnectionName, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@SystemCode, @SystemName, @Provider, @FolderPath, @TransformerType, @FilePattern, 
                    @IsActive, @Description, @Gl07ReportSetupId, @Interface, @TransactionType,
                    @BatchId, @DefaultCurrency, @AzureFileShareConnectionName, GETUTCDATE(), GETUTCDATE())",
            sourceSystem);
    }

    public async Task UpdateAsync(SourceSystem sourceSystem)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
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
                DefaultCurrency = @DefaultCurrency,
                AzureFileShareConnectionName = @AzureFileShareConnectionName,
                UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id",
            sourceSystem);
    }

    public async Task DeleteAsync(int id)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM SourceSystems WHERE Id = @Id", new { Id = id });
    }
}
