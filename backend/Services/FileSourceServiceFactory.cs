using A1arErpSfabGl07Gateway.Api.Models.Settings;

namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Factory for creating file source services based on the source system's provider.
/// </summary>
public interface IFileSourceServiceFactory
{
    /// <summary>
    /// Get the appropriate file source service for a source system.
    /// </summary>
    IFileSourceService GetFileSourceService(SourceSystem sourceSystem);
}

/// <summary>
/// Implementation of the file source service factory.
/// Creates LocalFileSourceService, AzureBlobFileSourceService, or AzureFileShareFileSourceService based on the source system's Provider.
/// </summary>
public class FileSourceServiceFactory : IFileSourceServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileSourceServiceFactory> _logger;

    public FileSourceServiceFactory(
        IServiceProvider serviceProvider,
        ILogger<FileSourceServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IFileSourceService GetFileSourceService(SourceSystem sourceSystem)
    {
        var provider = sourceSystem.Provider ?? "Local";

        _logger.LogDebug("Creating file source service for provider: {Provider}", provider);

        return provider.ToUpperInvariant() switch
        {
            "AZUREFILESHARE" => _serviceProvider.GetRequiredService<AzureFileShareFileSourceService>(),
            "AZUREBLOB" => _serviceProvider.GetRequiredService<AzureBlobFileSourceService>(),
            "LOCAL" => _serviceProvider.GetRequiredService<LocalFileSourceService>(),
            _ => throw new InvalidOperationException($"Unknown file source provider: {provider}. Valid options are: Local, AzureBlob, AzureFileShare")
        };
    }
}
