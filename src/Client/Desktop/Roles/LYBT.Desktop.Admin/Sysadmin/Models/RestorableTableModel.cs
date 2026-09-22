using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Desktop.Admin.Sysadmin.Models;

/// <summary>
/// 选择性恢复的可选表展示模型（B-06：指定表/记录）。
/// </summary>
public partial class RestorableTableModel : ObservableObject
{
    /// <summary>表名</summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>当前库记录数</summary>
    public int RowCount { get; init; }

    /// <summary>是否支持记录级选择（具备 Id 列）</summary>
    public bool SupportsRecordSelection { get; init; }

    /// <summary>是否勾选该表参与恢复</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>记录级过滤（逗号/分号/换行分隔的 Guid；留空 = 恢复该表全部记录）</summary>
    [ObservableProperty]
    private string _recordIdsText = string.Empty;

    /// <summary>记录级过滤提示</summary>
    public string RecordSelectionHint => SupportsRecordSelection ? "可填记录 Id" : "仅支持整表";

    /// <summary>由契约 DTO 构造展示模型</summary>
    public static RestorableTableModel FromDto(BackupTableDto dto) => new()
    {
        TableName = dto.TableName,
        RowCount = dto.RowCount,
        SupportsRecordSelection = dto.SupportsRecordSelection
    };

    /// <summary>解析记录 Id 文本（忽略空白与非法项）</summary>
    public List<Guid>? ParseRecordIds()
    {
        if (string.IsNullOrWhiteSpace(RecordIdsText))
            return null;

        var ids = RecordIdsText
            .Split([',', ';', '，', '；', '\n', '\r', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(text => Guid.TryParse(text.Trim(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        return ids.Count == 0 ? null : ids;
    }
}
