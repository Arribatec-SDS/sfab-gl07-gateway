namespace A1arErpSfabGl07Gateway.Api.Models.Settings;

/// <summary>
/// Represents a configuration setting stored in the database.
/// </summary>
public class AppSetting
{
    public int Id { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public string? ParamValue { get; set; }
    public bool Sensitive { get; set; }
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for returning settings to the frontend (with optional masking of sensitive values).
/// </summary>
public class AppSettingDto
{
    public int Id { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public string? ParamValue { get; set; }
    public bool Sensitive { get; set; }
    public string Category { get; set; } = "General";
    public string? Description { get; set; }

    // Aliases for frontend compatibility
    public string Key => ParamName;
    public string Value => ParamValue ?? string.Empty;

    public static AppSettingDto FromEntity(AppSetting entity, bool maskSensitive = true)
    {
        return new AppSettingDto
        {
            Id = entity.Id,
            ParamName = entity.ParamName,
            ParamValue = maskSensitive && entity.Sensitive && !string.IsNullOrEmpty(entity.ParamValue)
                ? "********"
                : entity.ParamValue,
            Sensitive = entity.Sensitive,
            Category = entity.Category,
            Description = entity.Description
        };
    }
}

/// <summary>
/// Request model for updating a setting value.
/// </summary>
public class UpdateSettingRequest
{
    public string? Value { get; set; }
}
