using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Local file system implementation of IFileSourceService for development/testing.
/// </summary>
public class LocalFileSourceService : IFileSourceService
{
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<LocalFileSourceService> _logger;
    private string? _basePath;

    public LocalFileSourceService(
        IAppSettingsService settingsService,
        ILogger<LocalFileSourceService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    private async Task<string> GetBasePathAsync()
    {
        if (_basePath == null)
        {
            _basePath = await _settingsService.GetValueAsync("FileSource:LocalBasePath")
                ?? "C:/dev/gl07-files";
        }
        return _basePath;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(SourceSystem sourceSystem)
    {
        var basePath = await GetBasePathAsync();
        var inboxPath = Path.Combine(basePath, sourceSystem.FolderPath, "inbox");

        _logger.LogDebug("Listing files in {InboxPath} with pattern {Pattern}",
            inboxPath, sourceSystem.FilePattern);

        if (!Directory.Exists(inboxPath))
        {
            _logger.LogWarning("Inbox directory does not exist: {InboxPath}", inboxPath);
            return Enumerable.Empty<string>();
        }

        var files = Directory.GetFiles(inboxPath, sourceSystem.FilePattern)
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();

        _logger.LogInformation("Found {Count} files in {InboxPath}", files.Count, inboxPath);
        return files;
    }

    public async Task<string> DownloadAsStringAsync(SourceSystem sourceSystem, string fileName)
    {
        var basePath = await GetBasePathAsync();
        var filePath = Path.Combine(basePath, sourceSystem.FolderPath, "inbox", fileName);

        _logger.LogDebug("Reading file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return await File.ReadAllTextAsync(filePath);
    }

    public async Task MoveToProcessedAsync(SourceSystem sourceSystem, string fileName)
    {
        var basePath = await GetBasePathAsync();
        var sourcePath = Path.Combine(basePath, sourceSystem.FolderPath, "inbox", fileName);
        var destFolder = Path.Combine(basePath, sourceSystem.FolderPath, "archive");

        Directory.CreateDirectory(destFolder);

        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var destPath = Path.Combine(destFolder, $"{datePrefix}_{fileName}");

        // Handle duplicate file names
        var counter = 1;
        while (File.Exists(destPath))
        {
            destPath = Path.Combine(destFolder, $"{datePrefix}_{counter}_{fileName}");
            counter++;
        }

        _logger.LogInformation("Moving file to archive: {SourcePath} -> {DestPath}", sourcePath, destPath);
        File.Move(sourcePath, destPath);
    }

    public async Task SaveJsonToArchiveAsync(SourceSystem sourceSystem, string originalFileName, string jsonContent)
    {
        var basePath = await GetBasePathAsync();
        var destFolder = Path.Combine(basePath, sourceSystem.FolderPath, "archive");

        Directory.CreateDirectory(destFolder);

        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var jsonFileName = Path.ChangeExtension(originalFileName, ".json");
        var destPath = Path.Combine(destFolder, $"{datePrefix}_{jsonFileName}");

        // Handle duplicate file names
        var counter = 1;
        while (File.Exists(destPath))
        {
            destPath = Path.Combine(destFolder, $"{datePrefix}_{counter}_{jsonFileName}");
            counter++;
        }

        _logger.LogInformation("Saving JSON to archive: {DestPath}", destPath);
        await File.WriteAllTextAsync(destPath, jsonContent);
    }

    public async Task MoveToErrorAsync(SourceSystem sourceSystem, string fileName)
    {
        var basePath = await GetBasePathAsync();
        var sourcePath = Path.Combine(basePath, sourceSystem.FolderPath, "inbox", fileName);
        var destFolder = Path.Combine(basePath, sourceSystem.FolderPath, "error");

        Directory.CreateDirectory(destFolder);

        var datePrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var destPath = Path.Combine(destFolder, $"{datePrefix}_{fileName}");

        // Handle duplicate file names
        var counter = 1;
        while (File.Exists(destPath))
        {
            destPath = Path.Combine(destFolder, $"{datePrefix}_{counter}_{fileName}");
            counter++;
        }

        _logger.LogInformation("Moving file to error: {SourcePath} -> {DestPath}", sourcePath, destPath);
        File.Move(sourcePath, destPath);
    }
}
