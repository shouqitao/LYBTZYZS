using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Primitives;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 本地模式数据库配置服务（B-07 初始化向导 Step 2）。
/// </summary>
/// <remarks>
/// <para>持久化：<c>%LOCALAPPDATA%\LYBT\Desktop\local-database.json</c>（安装目录之外，见
/// <see cref="AppDataPaths.DesktopDataDirectory"/>）；未配置/文件损坏时回退
/// <see cref="LocalDatabaseProfile.Default"/>（= 嵌入式 LocalWebAPI 的历史硬编码目标，行为与既有安装一致）。</para>
/// <para>口令安全：SQL Server 口令绝不落明文——写入前经 DPAPI
/// （<c>ProtectedData.Protect</c>，<see cref="DataProtectionScope.CurrentUser"/>）加密并 Base64 编码，存于 JSON 的
/// <c>PasswordProtected</c> 字段；仅当前 Windows 用户可解密。解密失败（换用户/换机器/文件被篡改）
/// 记警告并按「未保存口令」处理，不阻断读取。</para>
/// <para>不注入 <c>ICredentialVault</c>：DPAPI 直接封装在本服务的持久化格式内，口令随配置一次读写；
/// <see cref="Current"/> 是同步属性，若经保险库取回则需同步等待异步 IO（死锁风险），故不采用。</para>
/// <para>线程安全：<see cref="Current"/> 首次访问惰性加载（构造期无 IO），<see cref="Reload"/> 与惰性加载
/// 共用私有锁；<see cref="SaveAsync"/> 成功后同步刷新缓存。</para>
/// <para>测试接缝：<see cref="DataDirectory"/> 为 <c>protected virtual</c>，单元测试可派生指向临时目录，
/// 避免污染真实用户配置（<b>不属于</b>冻结的 <see cref="ILocalDatabaseSettingsService"/> 契约）。</para>
/// </remarks>
public class LocalDatabaseSettingsService : ILocalDatabaseSettingsService
{
    /// <summary>配置文件名（相对 <see cref="DataDirectory"/>）</summary>
    private const string SettingsFileName = "local-database.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ILogger<LocalDatabaseSettingsService> _logger;
    private readonly object _sync = new();
    private LocalDatabaseProfile? _current;

    /// <summary>
    /// 构造服务——不执行文件 IO；磁盘配置在首次访问 <see cref="Current"/> 时惰性加载（失败不抛出）。
    /// </summary>
    public LocalDatabaseSettingsService(ILogger<LocalDatabaseSettingsService> logger)
    {
        _logger = logger;
    }

    /// <summary>数据目录（生产 = <see cref="AppDataPaths.DesktopDataDirectory"/>；测试可覆写指向临时目录）</summary>
    protected virtual string DataDirectory => AppDataPaths.DesktopDataDirectory;

    /// <inheritdoc />
    public string SettingsFilePath => Path.Combine(DataDirectory, SettingsFileName);

    /// <inheritdoc />
    public LocalDatabaseProfile Current
    {
        get
        {
            var cached = _current;
            if (cached is not null)
                return cached;

            lock (_sync)
            {
                return _current ??= LoadFromDisk();
            }
        }
    }

    /// <inheritdoc />
    public string BuildConnectionString() => Current.BuildConnectionString();

    /// <inheritdoc />
    public void Reload()
    {
        lock (_sync)
        {
            _current = LoadFromDisk();
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> SaveAsync(LocalDatabaseProfile profile, CancellationToken ct = default)
    {
        if (profile is null)
            return CommandResult<bool>.Failed("数据库配置为空");

        try
        {
            var dto = new SettingsJson
            {
                Provider = profile.Provider,
                Server = profile.Server,
                Database = profile.Database,
                UseWindowsAuthentication = profile.UseWindowsAuthentication,
                UserId = profile.UserId,
                PasswordProtected = ProtectPassword(profile.Password),
                HasPassword = !string.IsNullOrEmpty(profile.Password)
            };

            var directory = DataDirectory;
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(
                SettingsFilePath,
                JsonSerializer.Serialize(dto, JsonOptions),
                ct);

            lock (_sync)
            {
                _current = profile;
            }

            _logger.LogInformation(
                "[LOCAL-DB] 本地数据库配置已保存：Provider={Provider}, Server={Server}, Database={Database}",
                profile.Provider, profile.Server, profile.Database);
            return CommandResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOCAL-DB] 保存本地数据库配置失败");
            return CommandResult<bool>.Failed(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<bool>> TestAsync(LocalDatabaseProfile profile, CancellationToken ct = default)
    {
        if (profile is null)
            return CommandResult<bool>.Failed("数据库配置为空");

        try
        {
            await using var connection = new SqlConnection(profile.BuildConnectionString());
            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            _ = await command.ExecuteScalarAsync(ct);

            // CommandResult<T> 无成功消息槽位——成功文案写入日志，结果仅承载布尔语义。
            _logger.LogInformation(
                "[LOCAL-DB] 数据库连接测试成功：Provider={Provider}, Server={Server}, Database={Database}",
                profile.Provider, profile.Server, profile.Database);
            return CommandResult<bool>.Succeeded(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LOCAL-DB] 数据库连接测试失败");
            return CommandResult<bool>.Failed(ex.Message);
        }
    }

    /// <summary>从磁盘读取配置；文件缺失/损坏/无法解密时回退默认值并记警告（不抛出）</summary>
    private LocalDatabaseProfile LoadFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return LocalDatabaseProfile.Default;

            var dto = JsonSerializer.Deserialize<SettingsJson>(File.ReadAllText(SettingsFilePath), JsonOptions);
            if (dto is null)
            {
                _logger.LogWarning("[LOCAL-DB] 配置文件为空，回退默认配置（{Path}）", SettingsFilePath);
                return LocalDatabaseProfile.Default;
            }

            return new LocalDatabaseProfile
            {
                Provider = dto.Provider,
                Server = string.IsNullOrWhiteSpace(dto.Server)
                    ? LocalDatabaseProfile.DefaultLocalDbServer
                    : dto.Server,
                Database = string.IsNullOrWhiteSpace(dto.Database)
                    ? LocalDatabaseProfile.DefaultDatabase
                    : dto.Database,
                UseWindowsAuthentication = dto.UseWindowsAuthentication,
                UserId = dto.UserId,
                Password = UnprotectPassword(dto.PasswordProtected)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LOCAL-DB] 本地数据库配置读取失败，回退默认配置（{Path}）", SettingsFilePath);
            return LocalDatabaseProfile.Default;
        }
    }

    /// <summary>DPAPI(CurrentUser) 加密 + Base64；空口令返回 null（不写字段）</summary>
    private static string? ProtectPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return null;

        var cipher = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(password),
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(cipher);
    }

    /// <summary>DPAPI(CurrentUser) 解密；空值/解密失败返回 null（按未保存口令处理）</summary>
    private string? UnprotectPassword(string? protectedBase64)
    {
        if (string.IsNullOrEmpty(protectedBase64))
            return null;

        try
        {
            var plain = ProtectedData.Unprotect(
                Convert.FromBase64String(protectedBase64),
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[LOCAL-DB] 本地数据库口令解密失败（可能换用户/换机器），按未保存口令处理");
            return null;
        }
    }

    /// <summary>
    /// <c>local-database.json</c> 的持久化形状。
    /// </summary>
    /// <remarks>口令仅以 <see cref="PasswordProtected"/>（DPAPI 密文）形式出现；<see cref="HasPassword"/> 供 UI
    /// 显示「已保存口令」而无需解密。</remarks>
    private sealed class SettingsJson
    {
        /// <summary>提供程序（LocalDb / SqlServer，序列化为字符串）</summary>
        public LocalDatabaseProvider Provider { get; set; }

        /// <summary>实例地址</summary>
        public string Server { get; set; } = LocalDatabaseProfile.DefaultLocalDbServer;

        /// <summary>数据库名</summary>
        public string Database { get; set; } = LocalDatabaseProfile.DefaultDatabase;

        /// <summary>是否 Windows 集成认证</summary>
        public bool UseWindowsAuthentication { get; set; } = true;

        /// <summary>SQL Server 登录名</summary>
        public string? UserId { get; set; }

        /// <summary>DPAPI(CurrentUser) 加密 + Base64 的口令密文（空 = 未保存口令）</summary>
        public string? PasswordProtected { get; set; }

        /// <summary>是否已保存口令</summary>
        public bool HasPassword { get; set; }
    }
}
