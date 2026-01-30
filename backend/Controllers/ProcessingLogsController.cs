using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SfabGl07Gateway.Api.Models.Settings;
using SfabGl07Gateway.Api.Repositories;

namespace SfabGl07Gateway.Api.Controllers;

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
}
