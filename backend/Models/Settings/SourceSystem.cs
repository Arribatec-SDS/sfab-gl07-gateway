namespace SfabGl07Gateway.Api.Models.Settings;

/// <summary>
/// Represents a source system that provides files for processing.
/// Each source system has its own folder structure and transformer type.
/// </summary>
public class SourceSystem
{
    public int Id { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string TransformerType { get; set; } = "ABWTransaction";
    public string FilePattern { get; set; } = "*.xml";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for source system operations.
/// </summary>
public class SourceSystemDto
{
    public int Id { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string TransformerType { get; set; } = "ABWTransaction";
    public string FilePattern { get; set; } = "*.xml";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public static SourceSystemDto FromEntity(SourceSystem entity)
    {
        return new SourceSystemDto
        {
            Id = entity.Id,
            SystemCode = entity.SystemCode,
            SystemName = entity.SystemName,
            FolderPath = entity.FolderPath,
            TransformerType = entity.TransformerType,
            FilePattern = entity.FilePattern,
            IsActive = entity.IsActive,
            Description = entity.Description
        };
    }

    public SourceSystem ToEntity()
    {
        return new SourceSystem
        {
            Id = Id,
            SystemCode = SystemCode,
            SystemName = SystemName,
            FolderPath = FolderPath,
            TransformerType = TransformerType,
            FilePattern = FilePattern,
            IsActive = IsActive,
            Description = Description
        };
    }
}

/// <summary>
/// Request model for creating/updating a source system.
/// </summary>
public class CreateSourceSystemRequest
{
    public string SystemCode { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string TransformerType { get; set; } = "ABWTransaction";
    public string FilePattern { get; set; } = "*.xml";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
