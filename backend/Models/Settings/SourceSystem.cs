using System.ComponentModel.DataAnnotations;

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
    public string Provider { get; set; } = "Local"; // Local or AzureBlob
    public string FolderPath { get; set; } = string.Empty;
    public string TransformerType { get; set; } = "ABWTransaction";
    public string FilePattern { get; set; } = "*.xml";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // GL07 Report Setup configuration (mandatory)
    public int Gl07ReportSetupId { get; set; }

    /// <summary>
    /// Override for batchInformation.interface in Unit4 API.
    /// If null/empty, use value from source file (XML Interface element).
    /// </summary>
    public string? Interface { get; set; }

    /// <summary>
    /// Override for transactionType. If null/empty, use value from source file (transformer-specific).
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Batch ID prefix (max 10 chars). If set, generates batchId as {prefix}-{yyMMddHHmmssff}.
    /// If null/empty, uses batchId from source file.
    /// </summary>
    [MaxLength(10)]
    public string? BatchId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to GL07 report setup
    /// </summary>
    public Gl07ReportSetup? Gl07ReportSetup { get; set; }
}

/// <summary>
/// DTO for source system operations.
/// </summary>
public class SourceSystemDto
{
    public int Id { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string Provider { get; set; } = "Local"; // Local or AzureBlob
    public string FolderPath { get; set; } = string.Empty;
    public string TransformerType { get; set; } = "ABWTransaction";
    public string FilePattern { get; set; } = "*.xml";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // GL07 Report Setup configuration
    public int Gl07ReportSetupId { get; set; }
    public Gl07ReportSetupSummaryDto? Gl07ReportSetup { get; set; }
    public string? Interface { get; set; }
    public string? TransactionType { get; set; }
    public string? BatchId { get; set; }

    public static SourceSystemDto FromEntity(SourceSystem entity)
    {
        return new SourceSystemDto
        {
            Id = entity.Id,
            SystemCode = entity.SystemCode,
            SystemName = entity.SystemName,
            Provider = entity.Provider,
            FolderPath = entity.FolderPath,
            TransformerType = entity.TransformerType,
            FilePattern = entity.FilePattern,
            IsActive = entity.IsActive,
            Description = entity.Description,
            Gl07ReportSetupId = entity.Gl07ReportSetupId,
            Gl07ReportSetup = entity.Gl07ReportSetup != null && entity.Gl07ReportSetup.Id > 0
                ? new Gl07ReportSetupSummaryDto
                {
                    Id = entity.Gl07ReportSetup.Id,
                    SetupCode = entity.Gl07ReportSetup.SetupCode,
                    SetupName = entity.Gl07ReportSetup.SetupName,
                    ReportName = entity.Gl07ReportSetup.ReportName
                }
                : null,
            Interface = entity.Interface,
            TransactionType = entity.TransactionType,
            BatchId = entity.BatchId
        };
    }

    public SourceSystem ToEntity()
    {
        return new SourceSystem
        {
            Id = Id,
            SystemCode = SystemCode,
            SystemName = SystemName,
            Provider = Provider,
            FolderPath = FolderPath,
            TransformerType = TransformerType,
            FilePattern = FilePattern,
            IsActive = IsActive,
            Description = Description,
            Gl07ReportSetupId = Gl07ReportSetupId,
            Interface = Interface,
            TransactionType = TransactionType,
            BatchId = BatchId
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
    public string Provider { get; set; } = "Local"; // Local or AzureBlob
    public string FolderPath { get; set; } = string.Empty;
    public string TransformerType { get; set; } = "ABWTransaction";
    public string FilePattern { get; set; } = "*.xml";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // GL07 Report Setup configuration (mandatory)
    [Required]
    public int Gl07ReportSetupId { get; set; }

    /// <summary>
    /// Override for batchInformation.interface. If null/empty, use from XML.
    /// </summary>
    public string? Interface { get; set; }

    /// <summary>
    /// Override for transactionType. If null/empty, use from XML.
    /// </summary>
    public string? TransactionType { get; set; }

    /// <summary>
    /// Batch ID prefix (max 10 chars). If set, generates batchId as {prefix}-{yyMMddHHmmssff}.
    /// If null/empty, uses batchId from source file.
    /// </summary>
    [MaxLength(10)]
    public string? BatchId { get; set; }
}

/// <summary>
/// Summary DTO for GL07 report setup (used in nested responses).
/// </summary>
public class Gl07ReportSetupSummaryDto
{
    public int Id { get; set; }
    public string SetupCode { get; set; } = string.Empty;
    public string SetupName { get; set; } = string.Empty;
    public string? ReportName { get; set; }
}
