namespace RegressionPlatform.Application.Features.Runs;

/// <summary>Outcome of storing one executed response for a run.</summary>
public enum ResponseInsertResult
{
    /// <summary>Stored.</summary>
    Inserted,

    /// <summary>The run already has a response for this request; nothing stored.</summary>
    Duplicate,

    /// <summary>This instance no longer owns the Running run (stopped, re-run or recovered); nothing stored.</summary>
    NotOwner,
}
