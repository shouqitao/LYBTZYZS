namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 创建备份请求（B-06）
/// </summary>
public sealed class BackupCreateRequestDto
{
    /// <summary>备份类型（全量/差异）；PreRestore 由系统内部使用</summary>
    public BackupKind Kind { get; set; } = BackupKind.Full;

    /// <summary>是否启用 SQL Server 备份压缩（<c>WITH COMPRESSION</c>）</summary>
    public bool Compress { get; set; } = true;

    /// <summary>是否启用文件级 AES-256 加密</summary>
    public bool Encrypt { get; set; }

    /// <summary>加密口令（<see cref="Encrypt"/> 为 true 时必填；未提供时回退配置 <c>Backup:EncryptionPassword</c>）</summary>
    public string? Password { get; set; }
}
