using Microsoft.AspNetCore.Mvc;
using Arribatec.Nexus.Client.Services;
using Microsoft.AspNetCore.Authorization;
using SfabGl07Gateway.Api.Services;
using System.Security.Claims;

namespace SfabGl07Gateway.Api.Controllers;

[ApiController]
[Route("api")]
public class SfabGl07GatewayController : ControllerBase
{
    private readonly ILogger<SfabGl07GatewayController> _logger;
    private readonly IContextProvider _contextProvider;
    private readonly IDatabaseInitializer _dbInitializer;

    public SfabGl07GatewayController(
        ILogger<SfabGl07GatewayController> logger,
        IContextProvider contextProvider,
        IDatabaseInitializer dbInitializer)
    {
        _logger = logger;
        _contextProvider = contextProvider;
        _dbInitializer = dbInitializer;
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser([FromQuery] string? tenantShortName = null)
    {
        try
        {
            // Initialize database on first authenticated request
            await _dbInitializer.InitializeAsync();

            // Get user validation information from context provider
            var validation = _contextProvider.GetUserValidation();

            if (validation == null)
            {
                return BadRequest(new { message = "User validation context not available" });
            }

            var data = new
            {
                message = $"Hello, {validation.User?.Username ?? "User"}!",
                user = new
                {
                    id = validation.User?.Id,
                    username = validation.User?.Username,
                    email = validation.User?.Email,
                    isGlobalAdmin = validation.User?.IsGlobalAdmin ?? false,
                    createdAt = validation.User?.CreatedAt,
                    updatedAt = validation.User?.UpdatedAt
                },
                context = new
                {
                    // Tenant information from validation
                    tenantId = validation.TenantInfo?.TenantShortName,
                    currentTenant = validation.CurrentTenant,
                    tenantInfo = validation.TenantInfo != null ? new
                    {
                        tenantShortName = validation.TenantInfo.TenantShortName,
                        tenantName = validation.TenantInfo.TenantName,
                        isGlobalAdmin = validation.TenantInfo.IsGlobalAdmin
                    } : null,

                    // Product information from validation
                    productId = validation.ProductId,
                    productShortName = validation.ProductShortName,
                    productInfo = validation.ProductInfo,

                    // Global admin status
                    isGlobalAdmin = validation.IsGlobalAdmin,

                    // Available tenants
                    availableTenants = validation.AvailableTenants,

                    // Host information
                    host = Request.Host.Host,
                    subdomain = Request.Host.Host.Split('.').FirstOrDefault()
                },
                validation = new
                {
                    success = validation.Success,
                    message = validation.Message,
                    tenantAccess = validation.TenantAccess
                },
                allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                timestamp = DateTime.UtcNow,
                authenticated = true
            };

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user data");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
