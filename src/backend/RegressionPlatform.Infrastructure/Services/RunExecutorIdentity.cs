using Microsoft.Extensions.Configuration;
using RegressionPlatform.Application.Features.Runs;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>Process-wide executor identity (singleton): node name + a per-process instance id.</summary>
public sealed class RunExecutorIdentity : IRunExecutorIdentity
{
    public RunExecutorIdentity(IConfiguration configuration)
    {
        var node = configuration["RunExecution:NodeName"];
        node = string.IsNullOrWhiteSpace(node) ? Environment.MachineName : node.Trim();
        // Run.ExecutorInstance is nvarchar(200): keep the node part short enough for pid + guid.
        NodeName = node.Length > 120 ? node[..120] : node;
        InstanceId = $"{NodeName}|{Environment.ProcessId}|{Guid.NewGuid():N}";
    }

    public string NodeName { get; }

    public string InstanceId { get; }
}
