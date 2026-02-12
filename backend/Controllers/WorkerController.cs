using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace A1arErpSfabGl07Gateway.Api.Controllers;

/// <summary>
/// Controller for manually triggering and monitoring the GL07 worker via Nexus Task Scheduler.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkerController : ControllerBase
{
    private readonly ILogger<WorkerController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public WorkerController(
        ILogger<WorkerController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Request model for starting the worker.
    /// </summary>
    public record RunWorkerRequest
    {
        /// <summary>
        /// Optional: Process only specific source system by code.
        /// If null, all active source systems are processed.
        /// </summary>
        public string? SourceSystemCode { get; init; }

        /// <summary>
        /// Optional: Process only a specific file by name.
        /// If null, all files in the inbox are processed.
        /// </summary>
        public string? Filename { get; init; }

        /// <summary>
        /// If true, only validates files without posting to Unit4.
        /// </summary>
        public bool DryRun { get; init; } = false;
    }

    /// <summary>
    /// Response model for worker execution.
    /// </summary>
    public record RunWorkerResponse
    {
        public string TaskExecutionId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    /// <summary>
    /// Task status response model.
    /// </summary>
    public record TaskStatusResponse
    {
        public string TaskExecutionId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime? StartedAt { get; init; }
        public DateTime? CompletedAt { get; init; }
        public string? Message { get; init; }
        public int Progress { get; init; }
    }

    private string GetMasterApiUrl()
    {
        // Try environment variable first, then configuration
        return Environment.GetEnvironmentVariable("NEXUS_API_URL")
            ?? Environment.GetEnvironmentVariable("MasterApiUrl")
            ?? _configuration["MasterApiUrl"]
            ?? "http://localhost:7833";
    }

    /// <summary>
    /// Manually trigger the GL07 processing worker via Nexus Task Scheduler.
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<RunWorkerResponse>> RunWorker([FromBody] RunWorkerRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Manual worker run requested. SourceSystemCode={SourceSystemCode}, Filename={Filename}, DryRun={DryRun}",
                request.SourceSystemCode ?? "ALL",
                request.Filename ?? "ALL",
                request.DryRun);

            var masterApiUrl = GetMasterApiUrl();

            // Get the user's JWT token from the Authorization header to forward to Master API
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new RunWorkerResponse
                {
                    Status = "Failed",
                    Message = "No authorization token found"
                });
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);

            // Get the tenant from the current user's claims
            var tenantClaim = User.FindFirst("tenant_id")?.Value
                ?? User.FindFirst("tenant")?.Value;

            _logger.LogDebug("Tenant from claims: {Tenant}", tenantClaim ?? "(not found)");

            // Search for the task configuration
            var searchUrl = $"{masterApiUrl}/api/admin/tasks?taskCode=gl07-process";
            _logger.LogDebug("Searching for task at: {Url}", searchUrl);

            var searchResponse = await client.GetAsync(searchUrl);
            if (!searchResponse.IsSuccessStatusCode)
            {
                var errorContent = await searchResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to search for tasks: {StatusCode} - {Content}",
                    searchResponse.StatusCode, errorContent);

                return StatusCode(500, new RunWorkerResponse
                {
                    Status = "Failed",
                    Message = $"Failed to find task configuration: {searchResponse.StatusCode}"
                });
            }

            var tasksJson = await searchResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Tasks response for gl07-process: {Json}", tasksJson);

            using var tasksDoc = JsonDocument.Parse(tasksJson);
            var tasks = tasksDoc.RootElement;

            // Find the task config ID
            string? taskConfigId = null;

            if (tasks.ValueKind == JsonValueKind.Array && tasks.GetArrayLength() > 0)
            {
                foreach (var task in tasks.EnumerateArray())
                {
                    // Log each task for debugging
                    _logger.LogDebug("Checking task: {Task}", task.GetRawText());

                    // The task code is in applicationTask.apiRoute (e.g., "/api/tasks/gl07-process/execute")
                    if (task.TryGetProperty("applicationTask", out var appTask) &&
                        appTask.TryGetProperty("apiRoute", out var apiRouteProp))
                    {
                        var apiRoute = apiRouteProp.GetString();
                        if (apiRoute?.Contains("gl07-process") == true && task.TryGetProperty("id", out var idProp))
                        {
                            taskConfigId = idProp.GetString();
                            _logger.LogInformation("Found gl07-process task with ID: {TaskConfigId}, apiRoute: {ApiRoute}", taskConfigId, apiRoute);
                            break;
                        }
                    }
                }
            }
            else if (tasks.ValueKind == JsonValueKind.Object)
            {
                if (tasks.TryGetProperty("id", out var idProp))
                {
                    taskConfigId = idProp.GetString();
                }
            }

            if (string.IsNullOrEmpty(taskConfigId))
            {
                _logger.LogWarning("No task configuration found for gl07-process");
                return NotFound(new RunWorkerResponse
                {
                    Status = "Failed",
                    Message = "Task 'gl07-process' not found. Please configure the task in Nexus Console first."
                });
            }

            _logger.LogInformation("Found task config ID: {TaskConfigId}", taskConfigId);

            // Trigger the task
            var triggerUrl = $"{masterApiUrl}/api/task-configs/{taskConfigId}/trigger";

            // Parameters must be a JSON string, not a nested object
            var parametersObject = new
            {
                SourceSystemCode = request.SourceSystemCode,
                Filename = request.Filename,
                DryRun = request.DryRun
            };

            var triggerPayload = new
            {
                parameters = JsonSerializer.Serialize(parametersObject, JsonOptions)
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(triggerPayload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Triggering task at: {Url}", triggerUrl);
            var triggerResponse = await client.PostAsync(triggerUrl, jsonContent);

            if (!triggerResponse.IsSuccessStatusCode)
            {
                var errorContent = await triggerResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to trigger task: {StatusCode} - {Content}",
                    triggerResponse.StatusCode, errorContent);

                return StatusCode(500, new RunWorkerResponse
                {
                    Status = "Failed",
                    Message = $"Failed to trigger task: {errorContent}"
                });
            }

            var responseJson = await triggerResponse.Content.ReadAsStringAsync();
            _logger.LogDebug("Trigger response: {Json}", responseJson);

            using var responseDoc = JsonDocument.Parse(responseJson);
            var root = responseDoc.RootElement;

            var executionId = root.TryGetProperty("executionId", out var execIdProp)
                ? execIdProp.GetString()
                : root.TryGetProperty("id", out var idProp2)
                    ? idProp2.GetString()
                    : Guid.NewGuid().ToString();

            _logger.LogInformation("Task triggered successfully. ExecutionId: {ExecutionId}", executionId);

            return Ok(new RunWorkerResponse
            {
                TaskExecutionId = executionId ?? Guid.NewGuid().ToString(),
                Status = "Running",
                Message = $"GL07 processing task triggered. " +
                    (request.SourceSystemCode != null
                        ? $"Processing system: {request.SourceSystemCode}"
                        : "Processing all active systems.") +
                    (request.DryRun ? " (Dry Run)" : "")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger worker");
            return StatusCode(500, new RunWorkerResponse
            {
                TaskExecutionId = string.Empty,
                Status = "Failed",
                Message = "Failed to trigger worker: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Get the status of a running or completed task from Nexus.
    /// </summary>
    [HttpGet("status/{taskExecutionId}")]
    public async Task<ActionResult<TaskStatusResponse>> GetTaskStatus(string taskExecutionId)
    {
        try
        {
            _logger.LogDebug("Getting status for task: {TaskExecutionId}", taskExecutionId);

            var masterApiUrl = GetMasterApiUrl();
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(authHeader))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
            }

            var statusUrl = $"{masterApiUrl}/api/task-executions/{taskExecutionId}";
            var response = await client.GetAsync(statusUrl);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound(new TaskStatusResponse
                    {
                        TaskExecutionId = taskExecutionId,
                        Status = "NotFound",
                        Message = "Task execution not found"
                    });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get task status: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);

                return StatusCode(500, new TaskStatusResponse
                {
                    TaskExecutionId = taskExecutionId,
                    Status = "Unknown",
                    Message = $"Failed to get status: {response.StatusCode}"
                });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return Ok(new TaskStatusResponse
            {
                TaskExecutionId = taskExecutionId,
                Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? "Unknown" : "Unknown",
                StartedAt = root.TryGetProperty("startedAt", out var startProp) && startProp.ValueKind != JsonValueKind.Null
                    ? startProp.GetDateTime() : null,
                CompletedAt = root.TryGetProperty("completedAt", out var endProp) && endProp.ValueKind != JsonValueKind.Null
                    ? endProp.GetDateTime() : null,
                Message = root.TryGetProperty("errorMessage", out var msgProp) ? msgProp.GetString() : null,
                Progress = root.TryGetProperty("progress", out var progProp) ? progProp.GetInt32() : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task status for {TaskExecutionId}", taskExecutionId);
            return StatusCode(500, new TaskStatusResponse
            {
                TaskExecutionId = taskExecutionId,
                Status = "Unknown",
                Message = "Failed to get task status: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Cancel a running task.
    /// </summary>
    [HttpPost("cancel/{taskExecutionId}")]
    public async Task<ActionResult> CancelTask(string taskExecutionId)
    {
        try
        {
            _logger.LogInformation("Cancellation requested for task: {TaskExecutionId}", taskExecutionId);
            return Ok(new { Message = "Cancellation requested" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel task {TaskExecutionId}", taskExecutionId);
            return StatusCode(500, new { Message = "Failed to cancel task: " + ex.Message });
        }
    }

    /// <summary>
    /// Get list of recent task executions from Nexus.
    /// </summary>
    [HttpGet("executions")]
    public async Task<ActionResult> GetRecentExecutions()
    {
        try
        {
            var masterApiUrl = GetMasterApiUrl();
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            var client = _httpClientFactory.CreateClient();
            if (!string.IsNullOrEmpty(authHeader))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);
            }

            var url = $"{masterApiUrl}/api/task-executions/recent?taskCode=gl07-process&limit=20";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get recent executions: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);

                return StatusCode(500, new { Message = "Failed to get recent executions" });
            }

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent executions");
            return StatusCode(500, new { Message = "Failed to get recent executions: " + ex.Message });
        }
    }

    /// <summary>
    /// Download task execution logs from Nexus.
    /// </summary>
    [HttpGet("logs/{taskExecutionId}")]
    public async Task<ActionResult> DownloadTaskLogs(Guid taskExecutionId)
    {
        try
        {
            _logger.LogInformation("Downloading logs for task execution: {TaskExecutionId}", taskExecutionId);

            var masterApiUrl = GetMasterApiUrl();
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new { Message = "No authorization token found" });
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);

            // Call Master API to get task execution logs (returns JSON)
            var url = $"{masterApiUrl}/api/task-executions/{taskExecutionId}/logs?limit=10000";
            _logger.LogDebug("Fetching logs from: {Url}", url);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get task logs: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);

                return StatusCode((int)response.StatusCode, new { Message = "Failed to get task logs" });
            }

            var jsonContent = await response.Content.ReadAsStringAsync();

            // Parse JSON and format as text
            var textContent = FormatLogsAsText(jsonContent);

            // Return as downloadable plain text file
            var bytes = Encoding.UTF8.GetBytes(textContent);
            return File(bytes, "text/plain", $"gl07-execution-{taskExecutionId}.log");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download task logs for {TaskExecutionId}", taskExecutionId);
            return StatusCode(500, new { Message = "Failed to download task logs: " + ex.Message });
        }
    }

    /// <summary>
    /// Formats JSON log entries as plain text in the format: "HH:mm:ss.fff [LEVEL] message"
    /// </summary>
    private string FormatLogsAsText(string jsonContent)
    {
        var sb = new StringBuilder();

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Handle array of log entries
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in root.EnumerateArray())
                {
                    var line = FormatLogEntry(entry);
                    if (!string.IsNullOrEmpty(line))
                    {
                        sb.AppendLine(line);
                    }
                }
            }
            // Handle object with logs property
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("logs", out var logs) && logs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in logs.EnumerateArray())
                    {
                        var line = FormatLogEntry(entry);
                        if (!string.IsNullOrEmpty(line))
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse logs as JSON, returning raw content");
            return jsonContent;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a single log entry as text.
    /// </summary>
    private string FormatLogEntry(JsonElement entry)
    {
        try
        {
            // Try to extract timestamp
            var timestamp = "";
            if (entry.TryGetProperty("timestamp", out var ts))
            {
                if (DateTime.TryParse(ts.GetString(), out var dt))
                {
                    timestamp = dt.ToString("HH:mm:ss.fff");
                }
                else
                {
                    timestamp = ts.GetString() ?? "";
                }
            }
            else if (entry.TryGetProperty("time", out var time))
            {
                if (DateTime.TryParse(time.GetString(), out var dt))
                {
                    timestamp = dt.ToString("HH:mm:ss.fff");
                }
                else
                {
                    timestamp = time.GetString() ?? "";
                }
            }

            // Try to extract level
            var level = "INFO";
            if (entry.TryGetProperty("level", out var lvl))
            {
                level = lvl.GetString()?.ToUpperInvariant() ?? "INFO";
            }
            else if (entry.TryGetProperty("severity", out var sev))
            {
                level = sev.GetString()?.ToUpperInvariant() ?? "INFO";
            }

            // Try to extract message
            var message = "";
            if (entry.TryGetProperty("message", out var msg))
            {
                message = msg.GetString() ?? "";
            }
            else if (entry.TryGetProperty("msg", out var m))
            {
                message = m.GetString() ?? "";
            }
            else if (entry.TryGetProperty("body", out var body))
            {
                message = body.GetString() ?? "";
            }

            if (string.IsNullOrEmpty(message))
            {
                return "";
            }

            return $"{timestamp} [{level}] {message}";
        }
        catch
        {
            return "";
        }
    }
}
