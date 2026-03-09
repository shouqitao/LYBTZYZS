namespace LYBT.Desktop.Sync.ViewModels;

/// <summary>
/// Per-entity-type sync result summary for card-style display (US-SYNC-007 D.2).
/// </summary>
public sealed record SyncResultSummary(
    string EntityType,
    int UploadedCount,
    int DownloadedCount,
    int DeletedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> Rejections)
{
    public int TotalProcessed => UploadedCount + DownloadedCount + DeletedCount + SkippedCount;
    public bool HasRejections => Rejections.Count > 0;
}
