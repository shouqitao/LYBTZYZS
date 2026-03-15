using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Sync.ViewModels;

namespace LYBT.Desktop.Sync.Services;

/// <summary>
/// 同步决议构建器 - 从用户选择构建同步决议
/// </summary>
public static class SyncResolutionBuilder
{
    /// <summary>
    /// 根据用户选择构建同步决议
    /// </summary>
    public static SyncResolution Build(
        ObservableCollection<SyncItemViewModel> localOnlyItems,
        ObservableCollection<SyncItemViewModel> serverOnlyItems,
        ObservableCollection<SyncItemViewModel> conflictItems)
    {
        var resolution = new SyncResolution();

        resolution.ToUpload.AddRange(
            localOnlyItems.Where(x => x.IsSelected).Select(x => x.EntityId));

        resolution.ToDownload.AddRange(
            serverOnlyItems.Where(x => x.IsSelected).Select(x => x.EntityId));

        foreach (var conflict in conflictItems.Where(x => x.IsSelected && x.ResolutionDecision.HasValue))
            resolution.ConflictResolutions[conflict.EntityId] = conflict.ResolutionDecision!.Value;

        resolution.Skipped.AddRange(
            conflictItems.Where(x => !x.IsSelected).Select(x => x.EntityId));

        return resolution;
    }

    /// <summary>
    /// 检查是否有数据需要同步
    /// </summary>
    public static bool HasDataToSync(
        ObservableCollection<SyncItemViewModel> localOnlyItems,
        ObservableCollection<SyncItemViewModel> serverOnlyItems,
        ObservableCollection<SyncItemViewModel> conflictItems)
    {
        return localOnlyItems.Any(x => x.IsSelected) ||
               serverOnlyItems.Any(x => x.IsSelected) ||
               conflictItems.Any(x => x.IsSelected);
    }

    /// <summary>
    /// 获取同步计数统计
    /// </summary>
    public static (int UploadCount, int DownloadCount, int ConflictCount, int TotalCount) GetCounts(
        ObservableCollection<SyncItemViewModel> localOnlyItems,
        ObservableCollection<SyncItemViewModel> serverOnlyItems,
        ObservableCollection<SyncItemViewModel> conflictItems)
    {
        var uploadCount = localOnlyItems.Count(x => x.IsSelected);
        var downloadCount = serverOnlyItems.Count(x => x.IsSelected);
        var conflictCount = conflictItems.Count;
        var totalCount = localOnlyItems.Count + serverOnlyItems.Count + conflictItems.Count;

        return (uploadCount, downloadCount, conflictCount, totalCount);
    }
}
