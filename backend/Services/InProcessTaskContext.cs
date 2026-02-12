using Arribatec.Nexus.Client.Models;
using Arribatec.Nexus.Client.TaskExecution;

namespace A1arErpSfabGl07Gateway.Api.Services;

/// <summary>
/// Simple in-process task context for development/testing.
/// Provides minimal implementation of ITaskContext for running workers locally.
/// In production, the Nexus platform provides the real ITaskContext.
/// </summary>
public class InProcessTaskContext : ITaskContext
{
    public InProcessTaskContext(string taskExecutionId)
    {
        TaskExecutionId = Guid.TryParse(taskExecutionId, out var guid) ? guid : Guid.NewGuid();
        CorrelationId = Guid.NewGuid().ToString();
        Tenant = new ActivatedTenantInfo
        {
            TenantId = Guid.Empty,
            Name = "Local Development",
            ShortName = "local"
        };
    }

    public Guid TaskExecutionId { get; }

    public string TaskCode => "gl07-process";

    public string CorrelationId { get; }

    public Guid ApplicationId => Guid.Empty;

    public Guid ApplicationTaskId => Guid.Empty;

    public Guid TenantId => Guid.Empty;

    public string TenantShortName => "local";

    public ActivatedTenantInfo Tenant { get; }

    public Task<string> GetDatabaseConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        // In-process execution doesn't use this - repositories get connections directly
        throw new NotSupportedException("Use IContextAwareDatabaseService instead for in-process execution");
    }
}
