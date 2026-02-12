using System.Text.Json.Serialization;

namespace A1arErpSfabGl07Gateway.Api.Models.Unit4;

/// <summary>
/// Request object for ordering a GL07 report job via Unit4 API.
/// POST /v1/report-jobs/order
/// </summary>
public class Unit4ReportJobRequest
{
    [JsonPropertyName("reportId")]
    public string ReportId { get; set; } = string.Empty;

    [JsonPropertyName("reportName")]
    public string ReportName { get; set; } = string.Empty;

    [JsonPropertyName("variant")]
    public int? Variant { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("companyId")]
    public string CompanyId { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public List<ReportJobParameter>? Parameters { get; set; }

    [JsonPropertyName("generalParameters")]
    public ReportJobGeneralParameters? GeneralParameters { get; set; }
}

/// <summary>
/// Parameter for a report job.
/// </summary>
public class ReportJobParameter
{
    [JsonPropertyName("notificationMessages")]
    public object? NotificationMessages { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

/// <summary>
/// General parameters for a report job.
/// </summary>
public class ReportJobGeneralParameters
{
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonPropertyName("emailConfirmation")]
    public bool EmailConfirmation { get; set; } = false;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "N";

    [JsonPropertyName("start")]
    public DateTime Start { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("outputType")]
    public int OutputType { get; set; } = 0;
}

/// <summary>
/// Response from Unit4 API for report job ordering.
/// </summary>
public class Unit4ReportJobResponse
{
    [JsonPropertyName("jobId")]
    public string? JobId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public List<Unit4Error>? Errors { get; set; }

    /// <summary>
    /// Location header from response (e.g., "/v1/report-jobs/order/300029f1-9227-4106-GL07-71")
    /// </summary>
    [JsonIgnore]
    public string? Location { get; set; }

    /// <summary>
    /// Task ID extracted from Location header (e.g., "300029f1-9227-4106-GL07-71")
    /// </summary>
    [JsonIgnore]
    public string? TaskId { get; set; }

    /// <summary>
    /// Order number extracted from Task ID (e.g., "71" from "300029f1-9227-4106-GL07-71")
    /// </summary>
    [JsonIgnore]
    public string? OrderNumber { get; set; }
}
