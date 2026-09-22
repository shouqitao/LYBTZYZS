namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 恢复请求（B-06）
/// </summary>
public sealed class RestoreRequestDto
{
    /// <summary>目标备份 Id（取自备份列表）</summary>
    public string BackupId { get; set; } = string.Empty;

    /// <summary>恢复模式（整库/选择性）</summary>
    public RestoreMode Mode { get; set; } = RestoreMode.Full;

    /// <summary>选择性恢复的表清单（<see cref="Mode"/> 为 Selective 时必填）</summary>
    public List<SelectiveRestoreTableDto> Tables { get; set; } = new();

    /// <summary>恢复前是否自动备份当前数据（默认开启；失败不阻断恢复但会在结果中给出警告）</summary>
    public bool CreatePreRestoreBackup { get; set; } = true;

    /// <summary>备份加密口令（目标备份已加密时必填）</summary>
    public string? Password { get; set; }
}
