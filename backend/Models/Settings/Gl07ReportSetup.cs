using System.ComponentModel.DataAnnotations;

namespace SfabGl07Gateway.Api.Models.Settings;

/// <summary>
/// Represents a GL07 report setup configuration for Unit4 API posting.
/// </summary>
public class Gl07ReportSetup
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SetupCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name - auto-filled from SetupCode.
    /// </summary>
    [MaxLength(100)]
    public string SetupName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReportId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ReportName { get; set; } = string.Empty;

    public int? Variant { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CompanyId { get; set; } = string.Empty;

    /// <summary>
    /// General parameter: priority (always 0)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// General parameter: emailConfirmation
    /// </summary>
    public bool EmailConfirmation { get; set; } = false;

    /// <summary>
    /// General parameter: status (default 'N')
    /// </summary>
    [MaxLength(1)]
    public string Status { get; set; } = "N";

    /// <summary>
    /// General parameter: outputType (default 0)
    /// </summary>
    public int OutputType { get; set; } = 0;

    /// <summary>
    /// General parameter: start - always returns current UTC time when accessed.
    /// This is injected dynamically and not stored in the database.
    /// </summary>
    public DateTime StartDate => DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property for parameters
    /// </summary>
    public List<Gl07ReportSetupParameter> Parameters { get; set; } = new();
}

/// <summary>
/// DTO for GL07 report setup operations.
/// </summary>
public class Gl07ReportSetupDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SetupCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SetupName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReportId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ReportName { get; set; } = string.Empty;

    public int? Variant { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CompanyId { get; set; } = string.Empty;

    [Range(0, 9)]
    public int Priority { get; set; } = 5;
    public bool EmailConfirmation { get; set; } = false;

    [MaxLength(1)]
    public string Status { get; set; } = "N";

    public int OutputType { get; set; } = 0;

    /// <summary>
    /// General parameter: start - always returns current UTC time when accessed.
    /// This is injected dynamically and not stored in the database.
    /// </summary>
    public DateTime StartDate => DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public List<Gl07ReportSetupParameterDto> Parameters { get; set; } = new();

    public static Gl07ReportSetupDto FromEntity(Gl07ReportSetup entity)
    {
        return new Gl07ReportSetupDto
        {
            Id = entity.Id,
            SetupCode = entity.SetupCode,
            SetupName = entity.SetupName,
            Description = entity.Description,
            ReportId = entity.ReportId,
            ReportName = entity.ReportName,
            Variant = entity.Variant,
            UserId = entity.UserId,
            CompanyId = entity.CompanyId,
            Priority = entity.Priority,
            EmailConfirmation = entity.EmailConfirmation,
            Status = entity.Status,
            OutputType = entity.OutputType,
            IsActive = entity.IsActive,
            Parameters = entity.Parameters?.Select(Gl07ReportSetupParameterDto.FromEntity).ToList() ?? new()
        };
    }

    public Gl07ReportSetup ToEntity()
    {
        return new Gl07ReportSetup
        {
            Id = Id,
            SetupCode = SetupCode,
            SetupName = SetupName,
            Description = Description,
            ReportId = ReportId,
            ReportName = ReportName,
            Variant = Variant,
            UserId = UserId,
            CompanyId = CompanyId,
            Priority = Priority,
            EmailConfirmation = EmailConfirmation,
            Status = Status,
            OutputType = OutputType,
            IsActive = IsActive,
            Parameters = Parameters?.Select(p => p.ToEntity()).ToList() ?? new()
        };
    }
}

/// <summary>
/// Request model for creating/updating a GL07 report setup.
/// </summary>
public class CreateGl07ReportSetupRequest
{
    [Required]
    [MaxLength(50)]
    public string SetupCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string ReportId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ReportName { get; set; } = string.Empty;

    public int? Variant { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CompanyId { get; set; } = string.Empty;

    [Range(0, 9)]
    public int Priority { get; set; } = 5;
    public bool EmailConfirmation { get; set; } = false;

    [MaxLength(1)]
    public string Status { get; set; } = "N";

    public int OutputType { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public List<Gl07ReportSetupParameterDto> Parameters { get; set; } = new();
}
