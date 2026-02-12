using Azure.Storage.Files.Shares;
using A1arErpSfabGl07Gateway.Api.Models.Settings;

namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Azure File Share implementation of IFileSourceService.
/// Uses named connection from settings (URL + SAS token pair).
/// Folder structure: {FolderPath}/inbox, {FolderPath}/archive, {FolderPath}/error
/// </summary>
public class AzureFileShareFileSourceService : IFileSourceService
{
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<AzureFileShareFileSourceService> _logger;

    public AzureFileShareFileSourceService(
        IAppSettingsService settingsService,
        ILogger<AzureFileShareFileSourceService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get the connection (URL + Token) by connection name from settings.
    /// </summary>
    private async Task<(string Url, string Token)> GetConnectionAsync(string connectionName)
    {
        var urlKey = $"AzureFileShare:{connectionName}:Url";
        var tokenKey = $"AzureFileShare:{connectionName}:Token";

        var url = await _settingsService.GetValueAsync(urlKey);
        var token = await _settingsService.GetValueAsync(tokenKey);

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException(
                $"Azure File Share connection '{connectionName}' URL not found in settings. " +
                $"Please add setting '{urlKey}'.");
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException(
                $"Azure File Share connection '{connectionName}' token not found in settings. " +
                $"Please add setting '{tokenKey}'.");
        }

        return (url, token);
    }

    /// <summary>
    /// Build the full SAS URL by combining the connection URL with its token.
    /// </summary>
    private async Task<string> BuildSasUrlAsync(SourceSystem sourceSystem)
    {
        if (string.IsNullOrEmpty(sourceSystem.AzureFileShareConnectionName))
        {
            throw new InvalidOperationException(
                $"Source system '{sourceSystem.SystemCode}' is configured to use AzureFileShare but has no connection name configured");
        }

        var (url, token) = await GetConnectionAsync(sourceSystem.AzureFileShareConnectionName);

        // Combine URL + token
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}{token}";
    }

    /// <summary>
    /// Get the base URL for the connection (used for logging).
    /// </summary>
    private async Task<string> GetBaseUrlAsync(SourceSystem sourceSystem)
    {
        if (string.IsNullOrEmpty(sourceSystem.AzureFileShareConnectionName))
            return "(no connection configured)";

        try
        {
            var (url, _) = await GetConnectionAsync(sourceSystem.AzureFileShareConnectionName);
            return url;
        }
        catch
        {
            return "(connection not found)";
        }
    }

    /// <summary>
    /// Get a ShareClient for the source system using its connection from settings.
    /// </summary>
    private async Task<ShareClient> GetShareClientAsync(SourceSystem sourceSystem)
    {
        var sasUrl = await BuildSasUrlAsync(sourceSystem);
        return new ShareClient(new Uri(sasUrl));
    }

    /// <summary>
    /// Get a directory client for the specified subfolder (inbox, archive, error).
    /// Creates the directory if it doesn't exist.
    /// </summary>
    private async Task<ShareDirectoryClient> GetDirectoryClientAsync(SourceSystem sourceSystem, string subFolder)
    {
        var shareClient = await GetShareClientAsync(sourceSystem);

        // Navigate to the folder path (e.g., "GL07")
        var rootDir = string.IsNullOrEmpty(sourceSystem.FolderPath)
            ? shareClient.GetRootDirectoryClient()
            : shareClient.GetDirectoryClient(sourceSystem.FolderPath);

        // Get the subfolder (inbox, archive, error)
        var targetDir = rootDir.GetSubdirectoryClient(subFolder);

        // Ensure directory exists
        await targetDir.CreateIfNotExistsAsync();

        return targetDir;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(SourceSystem sourceSystem)
    {
        var inboxDir = await GetDirectoryClientAsync(sourceSystem, "inbox");
        var pattern = sourceSystem.FilePattern.Replace("*", "");

        var baseUrl = await GetBaseUrlAsync(sourceSystem);
        _logger.LogDebug("Listing files in {ShareUrl}/{FolderPath}/inbox with pattern {Pattern}",
            baseUrl, sourceSystem.FolderPath, sourceSystem.FilePattern);

        var files = new List<string>();

        await foreach (var item in inboxDir.GetFilesAndDirectoriesAsync())
        {
            // Skip directories
            if (item.IsDirectory)
                continue;

            // Simple pattern matching (*.xml, *.csv, etc.)
            if (string.IsNullOrEmpty(pattern) || item.Name.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                files.Add(item.Name);
            }
        }

        _logger.LogInformation("Found {Count} files in {FolderPath}/inbox for source system {SystemCode}",
            files.Count, sourceSystem.FolderPath, sourceSystem.SystemCode);

        return files;
    }

    public async Task<string> DownloadAsStringAsync(SourceSystem sourceSystem, string fileName)
    {
        var inboxDir = await GetDirectoryClientAsync(sourceSystem, "inbox");
        var fileClient = inboxDir.GetFileClient(fileName);

        _logger.LogDebug("Downloading file: {FolderPath}/inbox/{FileName}", sourceSystem.FolderPath, fileName);

        if (!await fileClient.ExistsAsync())
        {
            throw new FileNotFoundException($"File not found: {sourceSystem.FolderPath}/inbox/{fileName}");
        }

        var download = await fileClient.DownloadAsync();
        using var reader = new StreamReader(download.Value.Content);
        return await reader.ReadToEndAsync();
    }

    public async Task MoveToProcessedAsync(SourceSystem sourceSystem, string fileName)
    {
        await MoveFileAsync(sourceSystem, fileName, "archive");
    }

    public async Task SaveJsonToArchiveAsync(SourceSystem sourceSystem, string originalFileName, string jsonContent)
    {
        var archiveDir = await GetDirectoryClientAsync(sourceSystem, "archive");
        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var jsonFileName = Path.ChangeExtension(originalFileName, ".json");
        var destFileName = $"{datePrefix}_{jsonFileName}";

        // Check for existing file and add counter if needed
        var fileClient = archiveDir.GetFileClient(destFileName);
        var counter = 1;
        while (await fileClient.ExistsAsync())
        {
            destFileName = $"{datePrefix}_{counter}_{jsonFileName}";
            fileClient = archiveDir.GetFileClient(destFileName);
            counter++;
        }

        _logger.LogInformation("Saving JSON to archive: {FolderPath}/archive/{FileName}",
            sourceSystem.FolderPath, destFileName);

        // Upload the JSON content
        var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
        await fileClient.CreateAsync(bytes.Length);
        using var stream = new MemoryStream(bytes);
        await fileClient.UploadAsync(stream);
    }

    public async Task MoveToErrorAsync(SourceSystem sourceSystem, string fileName)
    {
        await MoveFileAsync(sourceSystem, fileName, "error");
    }

    /// <summary>
    /// Move a file from inbox to the specified destination folder (archive or error).
    /// Azure File Share doesn't have native move, so we copy then delete.
    /// </summary>
    private async Task MoveFileAsync(SourceSystem sourceSystem, string fileName, string destFolder)
    {
        var inboxDir = await GetDirectoryClientAsync(sourceSystem, "inbox");
        var destDir = await GetDirectoryClientAsync(sourceSystem, destFolder);

        var sourceFile = inboxDir.GetFileClient(fileName);
        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var destFileName = $"{datePrefix}_{fileName}";

        // Check for existing file and add counter if needed
        var destFile = destDir.GetFileClient(destFileName);
        var counter = 1;
        while (await destFile.ExistsAsync())
        {
            destFileName = $"{datePrefix}_{counter}_{fileName}";
            destFile = destDir.GetFileClient(destFileName);
            counter++;
        }

        _logger.LogInformation("Moving file: {FolderPath}/inbox/{SourceFile} -> {FolderPath}/{DestFolder}/{DestFile}",
            sourceSystem.FolderPath, fileName, sourceSystem.FolderPath, destFolder, destFileName);

        // Download source file
        var download = await sourceFile.DownloadAsync();
        var contentLength = download.Value.ContentLength;

        // Create destination file and upload
        await destFile.CreateAsync(contentLength);

        // Copy content
        using var stream = new MemoryStream();
        await download.Value.Content.CopyToAsync(stream);
        stream.Position = 0;
        await destFile.UploadAsync(stream);

        // Delete source file
        await sourceFile.DeleteAsync();

        _logger.LogDebug("Successfully moved file to {DestFolder}: {FileName}", destFolder, destFileName);
    }
}
