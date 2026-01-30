using Arribatec.Nexus.Client.TaskExecution;

namespace SfabGl07Gateway.Api.Workers;

/// <summary>
/// Parameters for the sample worker task.
/// These are passed when scheduling the background job.
/// </summary>
public record WorkerParameters
{
    /// <summary>
    /// A message to process.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// An identifier for tracking purposes.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// A number to use in processing.
    /// </summary>
    public int Number { get; init; } = 1;
}

/// <summary>
/// Sample background job handler for the Stena Fastigheter – GL07 Gateway.
/// This demonstrates how to create a Nexus task handler with logging,
/// progress tracking, and cancellation support.
/// </summary>
[TaskHandler("sfab-gl07-gateway-worker",
    Name = "Stena Fastigheter – GL07 Gateway Worker",
    Description = "A sample background job that demonstrates task execution"
    )]
public class SfabGl07GatewayWorker : ITaskHandler<WorkerParameters>
{
    private readonly ILogger<SfabGl07GatewayWorker> _logger;
    private readonly ITaskContext _context;

    public SfabGl07GatewayWorker(ILogger<SfabGl07GatewayWorker> logger, ITaskContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task ExecuteAsync(WorkerParameters parameters, CancellationToken cancellationToken = default)
    {
        // Log task context for debugging
        _logger.LogInformation("╔════════════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║              SAMPLE WORKER TASK CONTEXT                        ║");
        _logger.LogInformation("╠════════════════════════════════════════════════════════════════╣");
        _logger.LogInformation("║ TaskExecutionId  : {TaskExecutionId}", _context.TaskExecutionId);
        _logger.LogInformation("║ TaskCode         : {TaskCode}", _context.TaskCode);
        _logger.LogInformation("║ CorrelationId    : {CorrelationId}", _context.CorrelationId);
        _logger.LogInformation("║ ApplicationId    : {ApplicationId}", _context.ApplicationId);
        _logger.LogInformation("║ ApplicationTaskId: {ApplicationTaskId}", _context.ApplicationTaskId);
        _logger.LogInformation("╠════════════════════════════════════════════════════════════════╣");
        _logger.LogInformation("║ TENANT:");
        _logger.LogInformation("║   - TenantId     : {TenantId}", _context.Tenant.TenantId);
        _logger.LogInformation("║   - ShortName    : {ShortName}", _context.Tenant.ShortName);
        _logger.LogInformation("║   - Name         : {Name}", _context.Tenant.Name);
        _logger.LogInformation("║   - IsActive     : {IsActive}", _context.Tenant.IsActive);
        _logger.LogInformation("╚════════════════════════════════════════════════════════════════╝");

        _logger.LogInformation("Starting sample worker for tenant {TenantId}", _context.Tenant.TenantId);
        _logger.LogInformation("Parameters - Id: {Id}, Message: {Message}, Number: {Number}", 
            parameters.Id, parameters.Message, parameters.Number);

        // Simulate processing work
        for (int i = 1; i <= parameters.Number; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            _logger.LogInformation("Processing iteration {Current}/{Total}...", i, parameters.Number);
            await Task.Delay(1000, cancellationToken);
            _logger.LogInformation("Iteration {Current} completed", i);
        }

        _logger.LogInformation("Sample worker completed - processed {Count} iterations", parameters.Number);
    }
}
