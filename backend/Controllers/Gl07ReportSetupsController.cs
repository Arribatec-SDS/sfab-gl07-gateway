using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SfabGl07Gateway.Api.Models.Settings;
using SfabGl07Gateway.Api.Repositories;

namespace SfabGl07Gateway.Api.Controllers;

/// <summary>
/// API controller for managing GL07 report setups.
/// All endpoints require authentication.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class Gl07ReportSetupsController : ControllerBase
{
    private readonly IGl07ReportSetupRepository _repository;
    private readonly ILogger<Gl07ReportSetupsController> _logger;

    public Gl07ReportSetupsController(
        IGl07ReportSetupRepository repository,
        ILogger<Gl07ReportSetupsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Get all GL07 report setups.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Gl07ReportSetupDto>>> GetAll()
    {
        _logger.LogInformation("Getting all GL07 report setups");
        var setups = await _repository.GetAllAsync();
        return Ok(setups.Select(Gl07ReportSetupDto.FromEntity));
    }

    /// <summary>
    /// Get active GL07 report setups only.
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Gl07ReportSetupDto>>> GetActive()
    {
        _logger.LogInformation("Getting active GL07 report setups");
        var setups = await _repository.GetAllActiveAsync();
        return Ok(setups.Select(Gl07ReportSetupDto.FromEntity));
    }

    /// <summary>
    /// Get a single GL07 report setup by ID with parameters.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Gl07ReportSetupDto>> GetById(int id)
    {
        var setup = await _repository.GetByIdWithParametersAsync(id);
        if (setup == null)
        {
            return NotFound(new { message = $"GL07 report setup with ID {id} not found" });
        }
        return Ok(Gl07ReportSetupDto.FromEntity(setup));
    }

    /// <summary>
    /// Create a new GL07 report setup.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Gl07ReportSetupDto>> Create([FromBody] CreateGl07ReportSetupRequest request)
    {
        _logger.LogInformation("Creating GL07 report setup: {SetupCode}", request.SetupCode);

        // Validate SetupCode uniqueness
        if (!await _repository.IsSetupCodeUniqueAsync(request.SetupCode))
        {
            return BadRequest(new { message = $"Setup code '{request.SetupCode}' already exists" });
        }

        var setup = new Gl07ReportSetup
        {
            SetupCode = request.SetupCode,
            SetupName = request.SetupCode, // Auto-fill with SetupCode
            Description = request.Description,
            ReportId = request.ReportId,
            ReportName = request.ReportName,
            Variant = request.Variant,
            UserId = request.UserId,
            CompanyId = request.CompanyId,
            Priority = request.Priority,
            EmailConfirmation = request.EmailConfirmation,
            Status = request.Status,
            OutputType = request.OutputType,
            IsActive = request.IsActive,
            Parameters = request.Parameters?.Select(p => p.ToEntity()).ToList() ?? new()
        };

        var id = await _repository.CreateAsync(setup);
        setup.Id = id;

        _logger.LogInformation("Created GL07 report setup with ID: {Id}", id);

        return CreatedAtAction(nameof(GetById), new { id }, Gl07ReportSetupDto.FromEntity(setup));
    }

    /// <summary>
    /// Update an existing GL07 report setup.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Gl07ReportSetupDto>> Update(int id, [FromBody] CreateGl07ReportSetupRequest request)
    {
        var existingSetup = await _repository.GetByIdAsync(id);
        if (existingSetup == null)
        {
            return NotFound(new { message = $"GL07 report setup with ID {id} not found" });
        }

        // Validate SetupCode uniqueness (excluding current record)
        if (!await _repository.IsSetupCodeUniqueAsync(request.SetupCode, id))
        {
            return BadRequest(new { message = $"Setup code '{request.SetupCode}' already exists" });
        }

        _logger.LogInformation("Updating GL07 report setup: {Id}", id);

        var setup = new Gl07ReportSetup
        {
            Id = id,
            SetupCode = request.SetupCode,
            SetupName = request.SetupCode, // Auto-fill with SetupCode
            Description = request.Description,
            ReportId = request.ReportId,
            ReportName = request.ReportName,
            Variant = request.Variant,
            UserId = request.UserId,
            CompanyId = request.CompanyId,
            Priority = request.Priority,
            EmailConfirmation = request.EmailConfirmation,
            Status = request.Status,
            OutputType = request.OutputType,
            IsActive = request.IsActive,
            Parameters = request.Parameters?.Select(p => p.ToEntity()).ToList() ?? new()
        };

        await _repository.UpdateAsync(setup);

        // Fetch updated setup with parameters
        var updatedSetup = await _repository.GetByIdWithParametersAsync(id);
        return Ok(Gl07ReportSetupDto.FromEntity(updatedSetup!));
    }

    /// <summary>
    /// Delete a GL07 report setup.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var existingSetup = await _repository.GetByIdAsync(id);
        if (existingSetup == null)
        {
            return NotFound(new { message = $"GL07 report setup with ID {id} not found" });
        }

        _logger.LogInformation("Deleting GL07 report setup: {Id}", id);

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Check if a setup code is unique.
    /// </summary>
    [HttpGet("check-code")]
    public async Task<ActionResult<object>> CheckSetupCode([FromQuery] string code, [FromQuery] int? excludeId = null)
    {
        var isUnique = await _repository.IsSetupCodeUniqueAsync(code, excludeId);
        return Ok(new { isUnique });
    }
}
