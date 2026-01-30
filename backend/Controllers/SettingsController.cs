using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SfabGl07Gateway.Api.Models.Settings;
using SfabGl07Gateway.Api.Services;

namespace SfabGl07Gateway.Api.Controllers;

/// <summary>
/// API controller for managing application settings.
/// All endpoints require authentication.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IAppSettingsService settingsService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all settings (sensitive values are masked).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppSettingDto>>> GetAll()
    {
        _logger.LogInformation("Getting all settings");
        var settings = await _settingsService.GetAllAsync(maskSensitive: true);
        return Ok(settings);
    }

    /// <summary>
    /// Get settings by category.
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<AppSettingDto>>> GetByCategory(string category)
    {
        _logger.LogInformation("Getting settings for category: {Category}", category);
        var settings = await _settingsService.GetByCategoryAsync(category, maskSensitive: true);
        return Ok(settings);
    }

    /// <summary>
    /// Get a single setting value.
    /// </summary>
    [HttpGet("{name}")]
    public async Task<ActionResult<AppSettingDto>> GetByName(string name)
    {
        var settings = await _settingsService.GetAllAsync(maskSensitive: true);
        var setting = settings.FirstOrDefault(s => s.ParamName == name);

        if (setting == null)
        {
            return NotFound($"Setting '{name}' not found");
        }

        return Ok(setting);
    }

    /// <summary>
    /// Update a setting value.
    /// </summary>
    [HttpPut("{name}")]
    public async Task<IActionResult> Update(string name, [FromBody] UpdateSettingRequest request)
    {
        _logger.LogInformation("Updating setting: {Name}", name);

        try
        {
            await _settingsService.SetValueAsync(name, request.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update setting: {Name}", name);
            return BadRequest($"Failed to update setting: {ex.Message}");
        }
    }

    /// <summary>
    /// Test Unit4 API connection.
    /// </summary>
    [HttpPost("test-unit4")]
    public async Task<ActionResult<object>> TestUnit4Connection([FromServices] IUnit4ApiClient unit4Client)
    {
        _logger.LogInformation("Testing Unit4 API connection");

        try
        {
            var success = await unit4Client.TestConnectionAsync();
            return Ok(new { success, message = success ? "Connection successful" : "Connection failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unit4 connection test failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }
}
