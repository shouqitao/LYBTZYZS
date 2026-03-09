namespace LYBT.Desktop.Sync.ViewModels;

/// <summary>
/// Sync workflow phase (US-SYNC-007).
/// Drives command CanExecute, status text, retry visibility.
/// </summary>
public enum SyncPhase
{
    Idle = 0,
    CheckingDifferences = 1,
    ReviewingDifferences = 2,
    ExecutingSync = 3,
    Completed = 4,
    Failed = 5
}

/// <summary>
/// Classified error category for workflow-level retry decisions.
/// Polly handles transport-level retry; this drives workflow-level resume.
/// </summary>
public enum SyncErrorCategory
{
    TransientNetwork = 0,
    AuthExpired = 1,
    BusinessReject = 2,
    ConflictChanged = 3,
    Unknown = 4
}
