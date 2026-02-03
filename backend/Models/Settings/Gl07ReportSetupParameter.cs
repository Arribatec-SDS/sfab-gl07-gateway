using System.ComponentModel.DataAnnotations;

namespace SfabGl07Gateway.Api.Models.Settings;

/// <summary>
/// Represents a parameter for a GL07 report setup.
/// </summary>
public class Gl07ReportSetupParameter
{
    public int Id { get; set; }

    public int Gl07ReportSetupId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ParameterId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ParameterValue { get; set; }

    /// <summary>
    /// Navigation property to parent setup
    /// </summary>
    public Gl07ReportSetup? Gl07ReportSetup { get; set; }
}

/// <summary>
/// DTO for GL07 report setup parameter operations.
/// </summary>
public class Gl07ReportSetupParameterDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string ParameterId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ParameterValue { get; set; }

    public static Gl07ReportSetupParameterDto FromEntity(Gl07ReportSetupParameter entity)
    {
        return new Gl07ReportSetupParameterDto
        {
            Id = entity.Id,
            ParameterId = entity.ParameterId,
            ParameterValue = entity.ParameterValue
        };
    }

    public Gl07ReportSetupParameter ToEntity()
    {
        return new Gl07ReportSetupParameter
        {
            Id = Id,
            ParameterId = ParameterId,
            ParameterValue = ParameterValue
        };
    }
}
