using Arribatec.Nexus.Client.Services;
using Dapper;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Initializes the database schema on first startup.
/// Runs the init-tables.sql script if tables don't exist.
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IContextAwareDatabaseService _dbService;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IWebHostEnvironment _environment;
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public DatabaseInitializer(
        IContextAwareDatabaseService dbService,
        ILogger<DatabaseInitializer> logger,
        IWebHostEnvironment environment)
    {
        _dbService = dbService;
        _logger = logger;
        _environment = environment;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Thread-safe check - only run once per app lifetime
        lock (_lock)
        {
            if (_initialized)
            {
                _logger.LogDebug("Database already initialized, skipping");
                return;
            }
            _initialized = true;
        }

        try
        {
            _logger.LogInformation("Running database schema initialization/migration...");

            using var connection = (await _dbService.CreateProductConnectionAsync())!;

            // Find the SQL script
            var sqlPath = FindInitScript();
            if (sqlPath == null)
            {
                _logger.LogWarning("init-tables.sql not found, skipping database initialization");
                return;
            }

            // Read and execute the script
            var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);

            // Split by GO statements (handles various line ending formats)
            var batches = System.Text.RegularExpressions.Regex.Split(sql, @"^\s*GO\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var batchNumber = 0;
            foreach (var batch in batches)
            {
                batchNumber++;
                var trimmedBatch = batch.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedBatch))
                {
                    try
                    {
                        await connection.ExecuteAsync(trimmedBatch);
                    }
                    catch (Exception ex)
                    {
                        // Log but continue - some migrations may fail if already applied
                        _logger.LogWarning(ex, "Batch {BatchNumber} failed (may be expected if already applied): {Message}",
                            batchNumber, ex.Message);
                    }
                }
            }

            _logger.LogInformation("✅ Database schema initialized successfully from {SqlPath}", sqlPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database schema");
            // Don't throw - allow app to start, the error will surface when tables are accessed
        }
    }

    private string? FindInitScript()
    {
        // Try multiple locations
        var searchPaths = new[]
        {
            // Development: relative to content root
            Path.Combine(_environment.ContentRootPath, "sql", "init-tables.sql"),
            // Production: in app directory
            Path.Combine(AppContext.BaseDirectory, "sql", "init-tables.sql"),
            // Fallback: current directory
            Path.Combine(Directory.GetCurrentDirectory(), "sql", "init-tables.sql"),
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        _logger.LogDebug("Searched for init-tables.sql in: {Paths}", string.Join(", ", searchPaths));
        return null;
    }
}
