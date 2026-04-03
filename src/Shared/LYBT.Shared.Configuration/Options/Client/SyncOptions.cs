namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 数据同步配置
/// </summary>
public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    /// <summary>
    /// 冲突时是否覆盖服务器数据，默认 false
    /// </summary>
    public bool OverwriteConflicts { get; set; } = false;
}
