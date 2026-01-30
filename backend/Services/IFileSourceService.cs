using SfabGl07Gateway.Api.Models.Settings;

namespace SfabGl07Gateway.Api.Services;

/// <summary>
/// Service interface for accessing files from various sources (local, Azure Blob, etc.).
/// </summary>
public interface IFileSourceService
{
    /// <summary>
    /// List files in a source system's inbox folder matching the file pattern.
    /// </summary>
    Task<IEnumerable<string>> ListFilesAsync(SourceSystem sourceSystem);

    /// <summary>
    /// Download file content as string.
    /// </summary>
    Task<string> DownloadAsStringAsync(SourceSystem sourceSystem, string fileName);

    /// <summary>
    /// Move file to the source system's archive folder with date prefix.
    /// </summary>
    Task MoveToProcessedAsync(SourceSystem sourceSystem, string fileName);

    /// <summary>
    /// Save JSON content to the archive folder with the same name as the original file but .json extension.
    /// </summary>
    Task SaveJsonToArchiveAsync(SourceSystem sourceSystem, string originalFileName, string jsonContent);

    /// <summary>
    /// Move file to the source system's error folder with date prefix.
    /// </summary>
    Task MoveToErrorAsync(SourceSystem sourceSystem, string fileName);
}
