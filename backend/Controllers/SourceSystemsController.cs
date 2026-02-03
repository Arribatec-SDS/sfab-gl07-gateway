using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SfabGl07Gateway.Api.Models.Settings;
using SfabGl07Gateway.Api.Repositories;
using SfabGl07Gateway.Api.Services;

namespace SfabGl07Gateway.Api.Controllers;

/// <summary>
/// API controller for managing source systems.
/// All endpoints require authentication.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SourceSystemsController : ControllerBase
{
    private readonly ISourceSystemRepository _repository;
    private readonly IGl07ReportSetupRepository _reportSetupRepository;
    private readonly ITransformationServiceFactory _transformerFactory;
    private readonly ILogger<SourceSystemsController> _logger;

    public SourceSystemsController(
        ISourceSystemRepository repository,
        IGl07ReportSetupRepository reportSetupRepository,
        ITransformationServiceFactory transformerFactory,
        ILogger<SourceSystemsController> logger)
    {
        _repository = repository;
        _reportSetupRepository = reportSetupRepository;
        _transformerFactory = transformerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get all source systems.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SourceSystemDto>>> GetAll()
    {
        _logger.LogInformation("Getting all source systems");
        var systems = await _repository.GetAllAsync();
        return Ok(systems.Select(SourceSystemDto.FromEntity));
    }

    /// <summary>
    /// Get active source systems only.
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<SourceSystemDto>>> GetActive()
    {
        _logger.LogInformation("Getting active source systems");
        var systems = await _repository.GetActiveAsync();
        return Ok(systems.Select(SourceSystemDto.FromEntity));
    }

    /// <summary>
    /// Get a single source system by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SourceSystemDto>> GetById(int id)
    {
        var system = await _repository.GetByIdAsync(id);
        if (system == null)
        {
            return NotFound($"Source system with ID {id} not found");
        }
        return Ok(SourceSystemDto.FromEntity(system));
    }

    /// <summary>
    /// Create a new source system.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SourceSystemDto>> Create([FromBody] CreateSourceSystemRequest request)
    {
        _logger.LogInformation("Creating source system: {SystemCode}", request.SystemCode);

        // Validate transformer type exists
        var availableTransformers = _transformerFactory.GetAvailableTransformerTypes().ToList();
        if (!availableTransformers.Contains(request.TransformerType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = $"Invalid transformer type '{request.TransformerType}'. Available: {string.Join(", ", availableTransformers)}" });
        }

        // Check for duplicate system code
        var existing = await _repository.GetByCodeAsync(request.SystemCode);
        if (existing != null)
        {
            return Conflict(new { message = $"Source system with code '{request.SystemCode}' already exists" });
        }

        // Validate GL07 report setup exists
        var reportSetup = await _reportSetupRepository.GetByIdAsync(request.Gl07ReportSetupId);
        if (reportSetup == null)
        {
            return BadRequest(new { message = $"GL07 report setup with ID {request.Gl07ReportSetupId} not found" });
        }

        var sourceSystem = new SourceSystem
        {
            SystemCode = request.SystemCode,
            SystemName = request.SystemName,
            Provider = request.Provider,
            FolderPath = request.FolderPath,
            TransformerType = request.TransformerType,
            FilePattern = request.FilePattern,
            IsActive = request.IsActive,
            Description = request.Description,
            Gl07ReportSetupId = request.Gl07ReportSetupId,
            Interface = request.Interface,
            TransactionType = request.TransactionType,
            BatchId = request.BatchId
        };

        var id = await _repository.CreateAsync(sourceSystem);
        sourceSystem.Id = id;
        sourceSystem.Gl07ReportSetup = reportSetup;

        _logger.LogInformation("Created source system: {SystemCode} (ID: {Id})", request.SystemCode, id);

        return CreatedAtAction(nameof(GetById), new { id }, SourceSystemDto.FromEntity(sourceSystem));
    }

    /// <summary>
    /// Update an existing source system.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateSourceSystemRequest request)
    {
        _logger.LogInformation("Updating source system: {Id}", id);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Source system with ID {id} not found" });
        }

        // Validate transformer type exists
        var availableTransformers = _transformerFactory.GetAvailableTransformerTypes().ToList();
        if (!availableTransformers.Contains(request.TransformerType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = $"Invalid transformer type '{request.TransformerType}'. Available: {string.Join(", ", availableTransformers)}" });
        }

        // Check for duplicate system code (if changed)
        if (!existing.SystemCode.Equals(request.SystemCode, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _repository.GetByCodeAsync(request.SystemCode);
            if (duplicate != null)
            {
                return Conflict(new { message = $"Source system with code '{request.SystemCode}' already exists" });
            }
        }

        // Validate GL07 report setup exists
        var reportSetup = await _reportSetupRepository.GetByIdAsync(request.Gl07ReportSetupId);
        if (reportSetup == null)
        {
            return BadRequest(new { message = $"GL07 report setup with ID {request.Gl07ReportSetupId} not found" });
        }

        existing.SystemCode = request.SystemCode;
        existing.SystemName = request.SystemName;
        existing.Provider = request.Provider;
        existing.FolderPath = request.FolderPath;
        existing.TransformerType = request.TransformerType;
        existing.FilePattern = request.FilePattern;
        existing.IsActive = request.IsActive;
        existing.Description = request.Description;
        existing.Gl07ReportSetupId = request.Gl07ReportSetupId;
        existing.Interface = request.Interface;
        existing.TransactionType = request.TransactionType;
        existing.BatchId = request.BatchId;

        await _repository.UpdateAsync(existing);

        _logger.LogInformation("Updated source system: {SystemCode}", request.SystemCode);

        return NoContent();
    }

    /// <summary>
    /// Delete a source system.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Deleting source system: {Id}", id);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound($"Source system with ID {id} not found");
        }

        await _repository.DeleteAsync(id);

        _logger.LogInformation("Deleted source system: {SystemCode}", existing.SystemCode);

        return NoContent();
    }

    /// <summary>
    /// Get available transformer types.
    /// </summary>
    [HttpGet("transformers")]
    public ActionResult<IEnumerable<string>> GetTransformers()
    {
        var transformers = _transformerFactory.GetAvailableTransformerTypes();
        return Ok(transformers);
    }
}
