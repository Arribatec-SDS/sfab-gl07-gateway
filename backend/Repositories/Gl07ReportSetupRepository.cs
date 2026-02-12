using Dapper;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Services;

namespace A1arErpSfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository implementation for GL07 Report Setups using Dapper.
/// </summary>
public class Gl07ReportSetupRepository : IGl07ReportSetupRepository
{
    private readonly IScopedDbConnectionProvider _connectionProvider;
    private readonly ILogger<Gl07ReportSetupRepository> _logger;

    public Gl07ReportSetupRepository(
        IScopedDbConnectionProvider connectionProvider,
        ILogger<Gl07ReportSetupRepository> logger)
    {
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<Gl07ReportSetup>> GetAllAsync()
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<Gl07ReportSetup>(@"
            SELECT Id, SetupCode, SetupName, Description, ReportId, ReportName, Variant, UserId, CompanyId,
                   Priority, EmailConfirmation, Status, OutputType, IsActive, CreatedAt, UpdatedAt
            FROM Gl07ReportSetups
            ORDER BY SetupName");
    }

    public async Task<IEnumerable<Gl07ReportSetup>> GetAllActiveAsync()
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<Gl07ReportSetup>(@"
            SELECT Id, SetupCode, SetupName, Description, ReportId, ReportName, Variant, UserId, CompanyId,
                   Priority, EmailConfirmation, Status, OutputType, IsActive, CreatedAt, UpdatedAt
            FROM Gl07ReportSetups
            WHERE IsActive = 1
            ORDER BY SetupName");
    }

    public async Task<Gl07ReportSetup?> GetByIdAsync(int id)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Gl07ReportSetup>(@"
            SELECT Id, SetupCode, SetupName, Description, ReportId, ReportName, Variant, UserId, CompanyId,
                   Priority, EmailConfirmation, Status, OutputType, IsActive, CreatedAt, UpdatedAt
            FROM Gl07ReportSetups
            WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<Gl07ReportSetup?> GetByIdWithParametersAsync(int id)
    {
        var connection = await _connectionProvider.GetConnectionAsync();

        var setup = await connection.QueryFirstOrDefaultAsync<Gl07ReportSetup>(@"
            SELECT Id, SetupCode, SetupName, Description, ReportId, ReportName, Variant, UserId, CompanyId,
                   Priority, EmailConfirmation, Status, OutputType, IsActive, CreatedAt, UpdatedAt
            FROM Gl07ReportSetups
            WHERE Id = @Id",
            new { Id = id });

        if (setup != null)
        {
            var parameters = await connection.QueryAsync<Gl07ReportSetupParameter>(@"
                SELECT Id, Gl07ReportSetupId, ParameterId, ParameterValue
                FROM Gl07ReportSetupParameters
                WHERE Gl07ReportSetupId = @SetupId
                ORDER BY ParameterId",
                new { SetupId = id });

            setup.Parameters = parameters.ToList();
        }

        return setup;
    }

    public async Task<Gl07ReportSetup?> GetBySetupCodeAsync(string setupCode)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Gl07ReportSetup>(@"
            SELECT Id, SetupCode, SetupName, Description, ReportId, ReportName, Variant, UserId, CompanyId,
                   Priority, EmailConfirmation, Status, OutputType, IsActive, CreatedAt, UpdatedAt
            FROM Gl07ReportSetups
            WHERE SetupCode = @SetupCode",
            new { SetupCode = setupCode });
    }

    public async Task<int> CreateAsync(Gl07ReportSetup setup)
    {
        var connection = await _connectionProvider.GetConnectionAsync();

        var setupId = await connection.QuerySingleAsync<int>(@"
            INSERT INTO Gl07ReportSetups (SetupCode, SetupName, Description, ReportId, ReportName, Variant, UserId, CompanyId,
                                          Priority, EmailConfirmation, Status, OutputType, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@SetupCode, @SetupName, @Description, @ReportId, @ReportName, @Variant, @UserId, @CompanyId,
                    @Priority, @EmailConfirmation, @Status, @OutputType, @IsActive, GETUTCDATE(), GETUTCDATE())",
            setup);

        // Insert parameters if any
        if (setup.Parameters?.Any() == true)
        {
            foreach (var param in setup.Parameters)
            {
                param.Gl07ReportSetupId = setupId;
                await connection.ExecuteAsync(@"
                    INSERT INTO Gl07ReportSetupParameters (Gl07ReportSetupId, ParameterId, ParameterValue)
                    VALUES (@Gl07ReportSetupId, @ParameterId, @ParameterValue)",
                    param);
            }
        }

        return setupId;
    }

    public async Task<bool> UpdateAsync(Gl07ReportSetup setup)
    {
        var connection = await _connectionProvider.GetConnectionAsync();

        var rowsAffected = await connection.ExecuteAsync(@"
            UPDATE Gl07ReportSetups 
            SET SetupCode = @SetupCode,
                SetupName = @SetupName,
                Description = @Description,
                ReportId = @ReportId,
                ReportName = @ReportName,
                Variant = @Variant,
                UserId = @UserId,
                CompanyId = @CompanyId,
                Priority = @Priority,
                EmailConfirmation = @EmailConfirmation,
                Status = @Status,
                OutputType = @OutputType,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id",
            setup);

        // Update parameters: delete existing and insert new
        if (setup.Parameters != null)
        {
            await connection.ExecuteAsync(@"
                DELETE FROM Gl07ReportSetupParameters WHERE Gl07ReportSetupId = @SetupId",
                new { SetupId = setup.Id });

            foreach (var param in setup.Parameters)
            {
                param.Gl07ReportSetupId = setup.Id;
                await connection.ExecuteAsync(@"
                    INSERT INTO Gl07ReportSetupParameters (Gl07ReportSetupId, ParameterId, ParameterValue)
                    VALUES (@Gl07ReportSetupId, @ParameterId, @ParameterValue)",
                    param);
            }
        }

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        // Parameters are deleted via CASCADE
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM Gl07ReportSetups WHERE Id = @Id",
            new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> IsSetupCodeUniqueAsync(string setupCode, int? excludeId = null)
    {
        var connection = await _connectionProvider.GetConnectionAsync();

        var sql = @"SELECT COUNT(1) FROM Gl07ReportSetups WHERE SetupCode = @SetupCode";
        if (excludeId.HasValue)
        {
            sql += " AND Id != @ExcludeId";
        }

        var count = await connection.ExecuteScalarAsync<int>(sql, new { SetupCode = setupCode, ExcludeId = excludeId });
        return count == 0;
    }

    public async Task<int> AddParameterAsync(int setupId, Gl07ReportSetupParameter parameter)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        parameter.Gl07ReportSetupId = setupId;

        return await connection.QuerySingleAsync<int>(@"
            INSERT INTO Gl07ReportSetupParameters (Gl07ReportSetupId, ParameterId, ParameterValue)
            OUTPUT INSERTED.Id
            VALUES (@Gl07ReportSetupId, @ParameterId, @ParameterValue)",
            parameter);
    }

    public async Task<bool> UpdateParameterAsync(Gl07ReportSetupParameter parameter)
    {
        var connection = await _connectionProvider.GetConnectionAsync();

        var rowsAffected = await connection.ExecuteAsync(@"
            UPDATE Gl07ReportSetupParameters 
            SET ParameterId = @ParameterId, ParameterValue = @ParameterValue
            WHERE Id = @Id",
            parameter);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteParameterAsync(int parameterId)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM Gl07ReportSetupParameters WHERE Id = @Id",
            new { Id = parameterId });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<Gl07ReportSetupParameter>> GetParametersAsync(int setupId)
    {
        var connection = await _connectionProvider.GetConnectionAsync();
        return await connection.QueryAsync<Gl07ReportSetupParameter>(@"
            SELECT Id, Gl07ReportSetupId, ParameterId, ParameterValue
            FROM Gl07ReportSetupParameters
            WHERE Gl07ReportSetupId = @SetupId
            ORDER BY ParameterId",
            new { SetupId = setupId });
    }
}
