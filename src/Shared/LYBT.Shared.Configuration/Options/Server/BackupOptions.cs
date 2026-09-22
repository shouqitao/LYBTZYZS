using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 备份配置（B-06 / US-SHELL-013）。
/// 双宿主共用：远程 WebAPI（服务端 SQL Server）与 LocalWebAPI（本机 LocalDB）。
/// </summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    /// <summary>
    /// 备份目录（绝对路径）。留空时使用各宿主默认目录：
    /// 桌面内嵌宿主为 <c>%LOCALAPPDATA%\LYBT\Desktop\Backup</c>，独立 WebAPI 为 <c>{内容根}/backup</c>。
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>备份文件保留天数（超期由清理任务删除；默认 7 天，对齐 NFR-AVAIL-001）</summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 7;

    /// <summary>手动备份是否默认启用 SQL Server 备份压缩（<c>WITH COMPRESSION</c>）</summary>
    public bool CompressByDefault { get; set; } = true;

    /// <summary>手动备份是否默认启用文件级 AES-256 加密</summary>
    public bool EncryptByDefault { get; set; }

    /// <summary>默认加密口令（手动备份未提供口令时回退本值；未配置且启用加密则报错）</summary>
    public string? EncryptionPassword { get; set; }

    /// <summary>计划调度自动备份</summary>
    public AutoBackupOptions AutoBackup { get; set; } = new();
}

/// <summary>
/// 计划调度自动备份（B-06：备份计划调度）
/// </summary>
public sealed class AutoBackupOptions
{
    /// <summary>是否启用（默认关闭——需显式开启，避免未预期占用磁盘/影响测试宿主）</summary>
    public bool Enabled { get; set; }

    /// <summary>备份间隔（小时）；距上次备份超过该间隔即触发</summary>
    [Range(1, 720)]
    public int IntervalHours { get; set; } = 24;

    /// <summary>自动备份类型（默认全量；差异备份依赖已有全量基准）</summary>
    public BackupKind Kind { get; set; } = BackupKind.Full;

    /// <summary>自动备份是否加密</summary>
    public bool Encrypt { get; set; }

    /// <summary>宿主启动后首次检查的延迟（分钟）</summary>
    [Range(0, 1440)]
    public int InitialDelayMinutes { get; set; } = 2;
}
