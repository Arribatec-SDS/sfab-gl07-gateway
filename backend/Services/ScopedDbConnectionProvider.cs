using System.Data;
using Arribatec.Nexus.Client.Services;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Provides a scoped database connection that is cached for the lifetime of the request/task scope.
/// This minimizes Master API calls by reusing the same connection across multiple repository operations.
/// </summary>
public interface IScopedDbConnectionProvider
{
    /// <summary>
    /// Gets or creates a database connection for the current scope.
    /// The connection is cached and reused for subsequent calls within the same scope.
    /// </summary>
    Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Scoped implementation that caches the database connection for the lifetime of the DI scope.
/// Reduces Master API calls from N (one per repository method) to 1 (one per request/task).
/// </summary>
public class ScopedDbConnectionProvider : IScopedDbConnectionProvider, IAsyncDisposable, IDisposable
{
    private readonly IContextAwareDatabaseService _dbService;
    private readonly ILogger<ScopedDbConnectionProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IDbConnection? _cachedConnection;
    private bool _disposed;

    public ScopedDbConnectionProvider(
        IContextAwareDatabaseService dbService,
        ILogger<ScopedDbConnectionProvider> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScopedDbConnectionProvider));
        }

        // Fast path: return cached connection if available
        if (_cachedConnection != null)
        {
            return _cachedConnection;
        }

        // Slow path: acquire lock and create connection
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedConnection != null)
            {
                return _cachedConnection;
            }

            _logger.LogDebug("Creating new scoped database connection");
            _cachedConnection = (await _dbService.CreateProductConnectionAsync())!;

            return _cachedConnection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_cachedConnection != null)
        {
            _logger.LogDebug("Disposing scoped database connection");

            if (_cachedConnection is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                _cachedConnection.Dispose();
            }

            _cachedConnection = null;
        }

        _lock.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cachedConnection?.Dispose();
        _cachedConnection = null;
        _lock.Dispose();
    }
}
