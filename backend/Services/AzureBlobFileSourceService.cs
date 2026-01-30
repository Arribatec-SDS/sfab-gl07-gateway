using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Azure Blob Storage implementation of IFileSourceService for production use.
/// </summary>
public class AzureBlobFileSourceService : IFileSourceService
{
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<AzureBlobFileSourceService> _logger;
    private BlobContainerClient? _containerClient;

    public AzureBlobFileSourceService(
        IAppSettingsService settingsService,
        ILogger<AzureBlobFileSourceService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    private async Task<BlobContainerClient> GetContainerClientAsync()
    {
        if (_containerClient == null)
        {
            var connectionString = await _settingsService.GetValueAsync("AzureStorage:ConnectionString");
            var containerName = await _settingsService.GetValueAsync("AzureStorage:ContainerName") ?? "gl07-files";

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Storage connection string is not configured");
            }

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure container exists
            await _containerClient.CreateIfNotExistsAsync();
        }

        return _containerClient;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(SourceSystem sourceSystem)
    {
        var container = await GetContainerClientAsync();
        var inboxPrefix = $"{sourceSystem.FolderPath}/inbox/";

        _logger.LogDebug("Listing blobs with prefix {Prefix} and pattern {Pattern}",
            inboxPrefix, sourceSystem.FilePattern);

        var files = new List<string>();
        var pattern = sourceSystem.FilePattern.Replace("*", "");

        await foreach (var blobItem in container.GetBlobsAsync(prefix: inboxPrefix))
        {
            var fileName = Path.GetFileName(blobItem.Name);

            // Simple pattern matching (*.xml, *.csv, etc.)
            if (string.IsNullOrEmpty(pattern) || fileName.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                files.Add(fileName);
            }
        }

        _logger.LogInformation("Found {Count} files in {Prefix}", files.Count, inboxPrefix);
        return files;
    }

    public async Task<string> DownloadAsStringAsync(SourceSystem sourceSystem, string fileName)
    {
        var container = await GetContainerClientAsync();
        var blobPath = $"{sourceSystem.FolderPath}/inbox/{fileName}";

        _logger.LogDebug("Downloading blob: {BlobPath}", blobPath);

        var blobClient = container.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}");
        }

        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }

    public async Task MoveToProcessedAsync(SourceSystem sourceSystem, string fileName)
    {
        await MoveBlobAsync(sourceSystem, fileName, "archive");
    }

    public async Task SaveJsonToArchiveAsync(SourceSystem sourceSystem, string originalFileName, string jsonContent)
    {
        var container = await GetContainerClientAsync();
        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var jsonFileName = Path.ChangeExtension(originalFileName, ".json");
        var destPath = $"{sourceSystem.FolderPath}/archive/{datePrefix}_{jsonFileName}";

        var destBlob = container.GetBlobClient(destPath);

        // Check for existing file and add counter if needed
        var counter = 1;
        while (await destBlob.ExistsAsync())
        {
            destPath = $"{sourceSystem.FolderPath}/archive/{datePrefix}_{counter}_{jsonFileName}";
            destBlob = container.GetBlobClient(destPath);
            counter++;
        }

        _logger.LogInformation("Saving JSON to archive blob: {DestPath}", destPath);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        await destBlob.UploadAsync(stream, overwrite: false);
    }

    public async Task MoveToErrorAsync(SourceSystem sourceSystem, string fileName)
    {
        await MoveBlobAsync(sourceSystem, fileName, "error");
    }

    private async Task MoveBlobAsync(SourceSystem sourceSystem, string fileName, string destFolder)
    {
        var container = await GetContainerClientAsync();
        var sourcePath = $"{sourceSystem.FolderPath}/inbox/{fileName}";
        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var destPath = $"{sourceSystem.FolderPath}/{destFolder}/{datePrefix}_{fileName}";

        var sourceBlob = container.GetBlobClient(sourcePath);
        var destBlob = container.GetBlobClient(destPath);

        // Check for existing file and add counter if needed
        var counter = 1;
        while (await destBlob.ExistsAsync())
        {
            destPath = $"{sourceSystem.FolderPath}/{destFolder}/{datePrefix}_{counter}_{fileName}";
            destBlob = container.GetBlobClient(destPath);
            counter++;
        }

        _logger.LogInformation("Moving blob: {SourcePath} -> {DestPath}", sourcePath, destPath);

        // Copy then delete (Azure Blob doesn't have native move)
        await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);

        // Wait for copy to complete
        var properties = await destBlob.GetPropertiesAsync();
        while (properties.Value.CopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(100);
            properties = await destBlob.GetPropertiesAsync();
        }

        if (properties.Value.CopyStatus == CopyStatus.Success)
        {
            await sourceBlob.DeleteAsync();
            _logger.LogDebug("Blob move completed successfully");
        }
        else
        {
            throw new InvalidOperationException($"Blob copy failed with status: {properties.Value.CopyStatus}");
        }
    }
}
