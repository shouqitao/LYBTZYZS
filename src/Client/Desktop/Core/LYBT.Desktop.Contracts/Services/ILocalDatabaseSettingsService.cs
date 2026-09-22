using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Results;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 本地模式数据库配置服务（B-07 初始化向导 Step 2 本地分支）。
/// </summary>
/// <remarks>
/// <para>持久化位置：<c>%LOCALAPPDATA%\LYBT\Desktop\local-database.json</c>（安装目录之外，见
/// <c>AppDataPaths.DesktopDataDirectory</c>）；口令经 DPAPI 加密后存储，不落明文。</para>
/// <para>消费方：嵌入式 LocalWebAPI 宿主（<c>EmbeddedLocalWebApiService</c> 用 <see cref="Current"/>
/// 构造连接串）与初始化向导（读写配置 + 测试连接）。</para>
/// </remarks>
public interface ILocalDatabaseSettingsService
{
    /// <summary>当前配置（未配置过时返回 <see cref="LocalDatabaseProfile.Default"/>）</summary>
    LocalDatabaseProfile Current { get; }

    /// <summary>配置文件路径（供诊断/展示）</summary>
    string SettingsFilePath { get; }

    /// <summary>构造连接字符串（等价 <c>Current.BuildConnectionString()</c>，供宿主直接消费）</summary>
    string BuildConnectionString();

    /// <summary>保存配置（口令经 DPAPI 落盘），成功后刷新 <see cref="Current"/></summary>
    Task<CommandResult<bool>> SaveAsync(LocalDatabaseProfile profile, CancellationToken ct = default);

    /// <summary>测试连接（打开 <c>SqlConnection</c> 并执行 <c>SELECT 1</c>）</summary>
    Task<CommandResult<bool>> TestAsync(LocalDatabaseProfile profile, CancellationToken ct = default);

    /// <summary>从磁盘重新加载（配置被外部修改后调用）</summary>
    void Reload();
}
