namespace RegressionPlatform.Application.Features.Runs;

/// <summary>
/// Identity of this API process as a run executor. Several instances can share one database (a
/// developer's local API and the deployed server), so each run records which instance owns it.
/// </summary>
public interface IRunExecutorIdentity
{
    /// <summary>Stable name of the machine / deployment (config <c>RunExecution:NodeName</c>, default machine name).</summary>
    string NodeName { get; }

    /// <summary>Unique id of this process: <c>"&lt;node&gt;|&lt;pid&gt;|&lt;random&gt;"</c>. Changes on every restart.</summary>
    string InstanceId { get; }
}
