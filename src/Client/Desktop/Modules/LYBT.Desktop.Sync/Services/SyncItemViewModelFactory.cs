using System.ComponentModel;
using LYBT.Desktop.Sync.ViewModels;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Desktop.Sync.Services;

/// <summary>
/// 同步项 ViewModel 工厂 - 负责创建和配置 SyncItemViewModel 实例
/// </summary>
public class SyncItemViewModelFactory
{
    private Action? _onSelectionChanged;

    /// <summary>
    /// 设置选择变更回调
    /// </summary>
    public void SetSelectionChangedCallback(Action? callback)
    {
        _onSelectionChanged = callback;
    }

    /// <summary>
    /// 从差异 DTO 创建同步项 ViewModel
    /// </summary>
    public SyncItemViewModel Create(SyncDiffDto diff, bool isSelected)
    {
        var item = new SyncItemViewModel
        {
            EntityId = diff.EntityId,
            EntityType = diff.EntityType,
            EntityName = diff.EntityName ?? diff.EntityId.ToString(),
            DiffType = diff.DiffType,
            LocalChecksum = diff.LocalChecksum,
            ServerChecksum = diff.ServerChecksum,
            LocalChangedAt = diff.LocalChangedAt,
            ServerChangedAt = diff.ServerChangedAt,
            ChangedFields = diff.ChangedFields,
            IsSelected = isSelected
        };

        if (_onSelectionChanged != null)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SyncItemViewModel.IsSelected))
                    _onSelectionChanged();
            };
        }

        return item;
    }
}
