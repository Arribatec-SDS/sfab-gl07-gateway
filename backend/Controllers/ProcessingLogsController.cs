using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using A1arErpSfabGl07Gateway.Api.Models.Settings;
using A1arErpSfabGl07Gateway.Api.Repositories;

namespace A1arErpSfabGl07Gateway.Api.Controllers;

/// <summary>
/// API controller for viewing processing logs.
/// All endpoints require authentication.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProcessingLogsController : ControllerBase
{
    private readonly IProcessingLogRepository _repository;
    private readonly ILogger<ProcessingLogsController> _logger;

    public ProcessingLogsController(
        IProcessingLogRepository repository,
        ILogger<ProcessingLogsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all processing logs (most recent first).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcessingLogDto>>> GetAll([FromQuery] int limit = 100)
    {
        _logger.LogInformation("Getting processing logs (limit: {Limit})", limit);
        var logs = await _repository.GetAllAsync(limit);
        return Ok(logs.Select(ProcessingLogDto.FromEntity));
    }

    /// <summary>
    /// Get processing logs by source system.
    /// </summary>
    [HttpGet("source/{sourceSystemId:int}")]
    public async Task<ActionResult<IEnumerable<ProcessingLogDto>>> GetBySourceSystem(
        int sourceSystemId,
        [FromQuery] int limit = 100)
    {
        _logger.LogInformation("Getting processing logs for source system: {SourceSystemId}", sourceSystemId);
        var logs = await _repository.GetBySourceSystemAsync(sourceSystemId, limit);
        return Ok(logs.Select(ProcessingLogDto.FromEntity));
    }

    /// <summary>
    /// Get processing logs by status.
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<ProcessingLogDto>>> GetByStatus(
        string status,
        [FromQuery] int limit = 100)
    {
        _logger.LogInformation("Getting processing logs with status: {Status}", status);
        var logs = await _repository.GetByStatusAsync(status, limit);
        return Ok(logs.Select(ProcessingLogDto.FromEntity));
    }

    /// <summary>
    /// Get a single processing log entry.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcessingLogDto>> GetById(int id)
    {
        var log = await _repository.GetByIdAsync(id);
        if (log == null)
        {
            return NotFound($"Processing log with ID {id} not found");
        }
        return Ok(ProcessingLogDto.FromEntity(log));
    }

    /// <summary>
    /// Get processing logs grouped by TaskExecutionId.
    /// Returns one entry per execution with nested source system details.
    /// </summary>
    [HttpGet("grouped")]
    public async Task<ActionResult<IEnumerable<ExecutionLogDto>>> GetGrouped([FromQuery] int limit = 50)
    {
        _logger.LogInformation("Getting grouped processing logs (limit: {Limit})", limit);
        var logs = await _repository.GetAllAsync(limit * 10); // Get more to ensure we have enough groups
        
        // Group by TaskExecutionId
        var grouped = logs
            .GroupBy(l => l.TaskExecutionId)
            .Take(limit)
            .Select(g => new ExecutionLogDto
            {
                TaskExecutionId = g.Key,
                ProcessedAt = g.Max(l => l.ProcessedAt),
                Status = g.Any(l => l.Status == "Error") ? "Error" 
                       : g.Any(l => l.Status == "Warning") ? "Warning" 
                       : "Success",
                TotalVouchers = g.Sum(l => l.VoucherCount ?? 0),
                TotalTransactions = g.Sum(l => l.TransactionCount ?? 0),
                TotalDurationMs = g.Sum(l => l.DurationMs ?? 0),
                SourceSystemCount = g.Count(),
                SourceSystems = g.Select(l => new SourceSystemLogDto
                {
                    Id = l.Id,
                    SourceSystemId = l.SourceSystemId,
                    SourceSystemName = l.SourceSystem?.SystemName,
                    FileName = l.FileName,
                    Status = l.Status,
                    VoucherCount = l.VoucherCount,
                    TransactionCount = l.TransactionCount,
                    ErrorMessage = l.ErrorMessage,
                    DurationMs = l.DurationMs
                }).OrderBy(s => s.SourceSystemName).ToList()
            })
            .OrderByDescending(e => e.ProcessedAt)
            .ToList();
        
        return Ok(grouped);
    }

    /// <summary>
    /// Delete all processing logs for a task execution.
    /// </summary>
    [HttpDelete("execution/{taskExecutionId:guid}")]
    public async Task<IActionResult> DeleteByExecution(Guid taskExecutionId)
    {
        _logger.LogInformation("Deleting processing logs for execution: {TaskExecutionId}", taskExecutionId);
        
        var deleted = await _repository.DeleteByTaskExecutionIdAsync(taskExecutionId);
        _logger.LogInformation("Deleted {Count} processing logs for execution: {TaskExecutionId}", deleted, taskExecutionId);
        
        return Ok(new { deleted });
    }
}
