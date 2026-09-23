using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Navigation;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Logging.Masking;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.Services;

/// <summary>
/// 配置导入导出服务实现（US-SHELL-016）。
/// </summary>
/// <remarks>
/// <para><b>导出面</b>：诊所信息、连接设置（模式/远程地址/本地数据库元数据）、角色权限快照、功能开关。</para>
/// <para><b>安全边界</b>：包内不含任何密钥——<c>Jwt:SecretKey</c>、<c>DefaultPasswords</c>、
/// 数据库口令（写占位 <c>***</c>）、令牌一律不导出；因此导入侧不需要「保留当前 Jwt:SecretKey」的
/// 特殊处理（见 <see cref="IConfigurationPackageService"/> 的 XML 文档）。</para>
/// <para><b>导入面</b>：诊所信息（即时生效）→ 连接设置（重启生效，<c>RequiresRestart=true</c>）→
/// 功能开关（热更新）；角色权限快照只做一致性校验，绝不应用。</para>
/// <para><b>健壮性</b>：所有失败路径均返回 <see cref="CommandResult{T}"/>.Failed（中文消息），从不抛出。</para>
/// </remarks>
public class ConfigurationPackageService : IConfigurationPackageService
{
    /// <summary>导出摘要中的节清单（与配置包 JSON 顶层节一一对应）</summary>
    private static readonly string[] ExportedSections =
    {
        "clinicSettings", "connection", "featureToggles", "rolePermissions"
    };

    /// <summary>功能开关键映射（包内 camelCase → 配置文件 PascalCase；SSOT：FeatureToggles 节）</summary>
    private static readonly IReadOnlyDictionary<string, string> FeatureToggleKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["overwriteConflicts"] = "OverwriteConflicts",
            ["duplicateHerbMergeStrategy"] = "DuplicateHerbMergeStrategy"
        };

    /// <summary>包格式固定为 camelCase + 缩进 + 枚举成员名原样（如 <c>LocalDb</c>/<c>SuperAdmin</c>）</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        // 不传命名策略：枚举按成员名原样写出（PropertyNamingPolicy 不影响枚举值）
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IClinicSettingsService _clinicSettings;
    private readonly IConnectionSettingsService _connectionSettings;
    private readonly ILocalDatabaseSettingsService _localDatabaseSettings;
    private readonly IClientConfigurationStore _configurationStore;
    private readonly IFeatureToggleService _featureToggles;
    private readonly IRoleRegistry _roleRegistry;
    private readonly ILogger<ConfigurationPackageService> _logger;

    /// <summary>构造配置包服务</summary>
    public ConfigurationPackageService(
        IClinicSettingsService clinicSettings,
        IConnectionSettingsService connectionSettings,
        ILocalDatabaseSettingsService localDatabaseSettings,
        IClientConfigurationStore configurationStore,
        IFeatureToggleService featureToggles,
        IRoleRegistry roleRegistry,
        ILogger<ConfigurationPackageService> logger)
    {
        _clinicSettings = clinicSettings ?? throw new ArgumentNullException(nameof(clinicSettings));
        _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
        _localDatabaseSettings = localDatabaseSettings ?? throw new ArgumentNullException(nameof(localDatabaseSettings));
        _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
        _featureToggles = featureToggles ?? throw new ArgumentNullException(nameof(featureToggles));
        _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 导出

    /// <inheritdoc />
    public async Task<CommandResult<ConfigurationPackageResult>> ExportAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return CommandResult<ConfigurationPackageResult>.Failed("未指定导出路径，无法导出配置包");

            var document = BuildDocument();
            var json = JsonSerializer.Serialize(document, JsonOptions);

            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(fullPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), ct)
                .ConfigureAwait(false);

            var size = new FileInfo(fullPath).Length;
            _logger.LogInformation("[CFG-PKG] 配置包已导出：{FilePath}（{Size} 字节）", fullPath, size);

            return CommandResult<ConfigurationPackageResult>.Succeeded(new ConfigurationPackageResult
            {
                FilePath = fullPath,
                FileSizeBytes = size,
                Sections = ExportedSections
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CFG-PKG] 导出配置包失败：{FilePath}", filePath);
            return CommandResult<ConfigurationPackageResult>.Failed($"导出配置包失败：{ex.Message}");
        }
    }

    /// <summary>按当前运行配置构建配置包文档（密钥一律不写入）</summary>
    private ConfigurationPackageDocument BuildDocument()
    {
        var clinic = _clinicSettings.GetSettings() ?? new ClinicSettingsOptions();
        var localDatabase = _localDatabaseSettings.Current ?? LocalDatabaseProfile.Default;

        return new ConfigurationPackageDocument
        {
            PackageVersion = ConfigurationPackageDocument.CurrentVersion,
            ExportedAt = DateTimeOffset.Now,
            Application = new ConfigurationPackageApplicationSection
            {
                Name = SystemConstants.ApplicationName,
                Version = SystemConstants.ApplicationVersion
            },
            ClinicSettings = new ConfigurationPackageClinicSection
            {
                Name = clinic.Name ?? string.Empty,
                Department = clinic.Department ?? string.Empty,
                Address = clinic.Address ?? string.Empty,
                Phone = clinic.Phone ?? string.Empty,
                Email = clinic.Email ?? string.Empty,
                LicenseNumber = clinic.LicenseNumber ?? string.Empty
            },
            Connection = new ConfigurationPackageConnectionSection
            {
                Mode = ParseMode(_connectionSettings.PreferredMode),
                // 内嵌凭据（查询参数形式）脱敏后才入包——安全项不落包
                RemoteUrl = SensitiveDataMasker.MaskUri(_connectionSettings.RemoteUrl),
                LocalDatabase = new ConfigurationPackageLocalDatabaseSection
                {
                    Provider = localDatabase.Provider,
                    Server = localDatabase.Server ?? string.Empty,
                    Database = localDatabase.Database ?? string.Empty,
                    UseWindowsAuthentication = localDatabase.UseWindowsAuthentication,
                    UserId = localDatabase.UserId,
                    // 口令永不导出：仅写占位符，导入侧保留本机当前口令
                    Password = ConfigurationPackageDocument.MaskedSecret
                }
            },
            RolePermissions = BuildRoleSnapshot(),
            FeatureToggles = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["overwriteConflicts"] = _featureToggles.IsEnabled("OverwriteConflicts"),
                ["duplicateHerbMergeStrategy"] = _featureToggles.GetValue("DuplicateHerbMergeStrategy") ?? "Max"
            }
        };
    }

    /// <summary>构建角色权限快照（编译期权限基线的投影：<c>IRoleDefinition</c> + <c>ViewRoleAccess</c>）</summary>
    private List<ConfigurationPackageRoleSnapshot> BuildRoleSnapshot()
    {
        var snapshot = new List<ConfigurationPackageRoleSnapshot>();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            snapshot.Add(new ConfigurationPackageRoleSnapshot
            {
                Role = role,
                HomeView = _roleRegistry.GetHomeViewName(role),
                AllowedViews = NavigationCoordinator.RoleAccessSnapshot
                    .Where(entry => entry.Value.Contains(role))
                    .Select(entry => entry.Key)
                    .OrderBy(view => view, StringComparer.Ordinal)
                    .ToList()
            });
        }

        return snapshot;
    }

    #endregion

    #region 导入

    /// <inheritdoc />
    public async Task<CommandResult<ConfigurationPackageResult>> ImportAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return CommandResult<ConfigurationPackageResult>.Failed("未指定配置文件路径，无法导入");
            if (!File.Exists(filePath))
                return CommandResult<ConfigurationPackageResult>.Failed("配置文件不存在，无法导入");

            ConfigurationPackageDocument? document;
            try
            {
                var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
                document = JsonSerializer.Deserialize<ConfigurationPackageDocument>(json, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[CFG-PKG] 配置包解析失败：{FilePath}", filePath);
                return CommandResult<ConfigurationPackageResult>.Failed("配置文件格式错误（JSON 解析失败），已拒绝导入");
            }

            if (document == null)
                return CommandResult<ConfigurationPackageResult>.Failed("配置文件内容为空，已拒绝导入");

            if (string.IsNullOrWhiteSpace(document.PackageVersion))
                return CommandResult<ConfigurationPackageResult>.Failed("配置文件缺少 packageVersion 标识，已拒绝导入");

            if (!string.Equals(document.PackageVersion.Trim(), ConfigurationPackageDocument.CurrentVersion, StringComparison.Ordinal))
            {
                return CommandResult<ConfigurationPackageResult>.Failed(
                    $"不支持的配置包版本：{document.PackageVersion}（当前支持 {ConfigurationPackageDocument.CurrentVersion}），已拒绝导入");
            }

            var applied = new List<string>();
            var skipped = new List<ConfigurationPackageSkip>();
            var notes = new List<string>();

            await ApplyClinicSettingsAsync(document, applied, skipped).ConfigureAwait(false);
            var requiresRestart = await ApplyConnectionAsync(document, applied, skipped, notes, ct).ConfigureAwait(false);
            await ApplyFeatureTogglesAsync(document, applied, skipped).ConfigureAwait(false);
            ReportRolePermissionConsistency(document, skipped, notes);

            var result = new ConfigurationPackageResult
            {
                FilePath = filePath,
                FileSizeBytes = new FileInfo(filePath).Length,
                AppliedSections = applied,
                SkippedItems = skipped,
                Notes = notes,
                RequiresRestart = requiresRestart
            };

            _logger.LogInformation(
                "[CFG-PKG] 配置包已导入：{FilePath}（应用 {Applied} 节，跳过 {Skipped} 项，需重启={Restart}）",
                filePath, applied.Count, skipped.Count, requiresRestart);

            return CommandResult<ConfigurationPackageResult>.Succeeded(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CFG-PKG] 导入配置包失败：{FilePath}", filePath);
            return CommandResult<ConfigurationPackageResult>.Failed($"导入配置包失败：{ex.Message}");
        }
    }

    /// <summary>应用诊所信息（即时生效；时区不在包内，必须沿用当前值以免节级覆盖时静默丢失）</summary>
    private async Task ApplyClinicSettingsAsync(
        ConfigurationPackageDocument document,
        List<string> applied,
        List<ConfigurationPackageSkip> skipped)
    {
        if (document.ClinicSettings == null)
        {
            skipped.Add(new ConfigurationPackageSkip("clinicSettings", "配置包未包含诊所信息，保留当前设置"));
            return;
        }

        var section = document.ClinicSettings;
        var saved = await _clinicSettings.SaveSettingsAsync(new ClinicSettingsOptions
        {
            Name = section.Name ?? string.Empty,
            Department = section.Department ?? string.Empty,
            Address = section.Address ?? string.Empty,
            Phone = section.Phone ?? string.Empty,
            Email = section.Email ?? string.Empty,
            LicenseNumber = section.LicenseNumber ?? string.Empty,
            Timezone = _clinicSettings.GetSettings().Timezone
        }).ConfigureAwait(false);

        if (saved)
            applied.Add("clinicSettings");
        else
            skipped.Add(new ConfigurationPackageSkip("clinicSettings", "诊所信息保存失败（请检查配置文件权限或磁盘状态）"));
    }

    /// <summary>应用连接设置（远程地址/模式/本地数据库）；返回是否需要重启生效</summary>
    private async Task<bool> ApplyConnectionAsync(
        ConfigurationPackageDocument document,
        List<string> applied,
        List<ConfigurationPackageSkip> skipped,
        List<string> notes,
        CancellationToken ct)
    {
        if (document.Connection == null)
        {
            skipped.Add(new ConfigurationPackageSkip("connection", "配置包未包含连接设置，保留当前设置"));
            return false;
        }

        var requiresRestart = false;
        var connection = document.Connection;

        // 远程地址
        var remoteUrl = connection.RemoteUrl?.Trim();
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            skipped.Add(new ConfigurationPackageSkip("connection.remoteUrl", "远程地址为空，保留当前设置"));
        }
        else if (remoteUrl.Contains(ConfigurationPackageDocument.MaskedSecret, StringComparison.Ordinal))
        {
            skipped.Add(new ConfigurationPackageSkip("connection.remoteUrl",
                "远程地址含脱敏占位符（***），为避免写入无效地址已跳过（安全项不落包）"));
        }
        else if (!_connectionSettings.IsValidUrl(remoteUrl))
        {
            skipped.Add(new ConfigurationPackageSkip("connection.remoteUrl",
                $"远程地址不是合法的 http/https URL：{SensitiveDataMasker.MaskUri(remoteUrl)}"));
        }
        else if (string.Equals(_connectionSettings.RemoteUrl, remoteUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            skipped.Add(new ConfigurationPackageSkip("connection.remoteUrl", "远程地址与当前一致，无需变更"));
        }
        else
        {
            await _connectionSettings.SaveRemoteUrlAsync(remoteUrl).ConfigureAwait(false);
            applied.Add("connection.remoteUrl");
            requiresRestart = true;
        }

        // 连接模式
        var targetMode = connection.Mode.ToString();
        if (string.Equals(_connectionSettings.PreferredMode, targetMode, StringComparison.OrdinalIgnoreCase))
        {
            skipped.Add(new ConfigurationPackageSkip("connection.mode", $"连接模式与当前一致（{targetMode}），无需变更"));
        }
        else
        {
            await _connectionSettings.SavePreferredModeAsync(targetMode).ConfigureAwait(false);
            applied.Add("connection.mode");
            requiresRestart = true;
        }

        // 本地数据库（口令永不导出 → 保留本机当前口令）
        if (connection.LocalDatabase == null)
        {
            skipped.Add(new ConfigurationPackageSkip("connection.localDatabase", "配置包未包含本地数据库设置，保留当前设置"));
        }
        else
        {
            var current = _localDatabaseSettings.Current ?? LocalDatabaseProfile.Default;
            var profile = new LocalDatabaseProfile
            {
                Provider = connection.LocalDatabase.Provider,
                Server = connection.LocalDatabase.Server ?? string.Empty,
                Database = connection.LocalDatabase.Database ?? string.Empty,
                UseWindowsAuthentication = connection.LocalDatabase.UseWindowsAuthentication,
                UserId = connection.LocalDatabase.UserId,
                Password = current.Password
            };

            var saveResult = await _localDatabaseSettings.SaveAsync(profile, ct).ConfigureAwait(false);
            if (saveResult.Success)
            {
                applied.Add("connection.localDatabase");
                requiresRestart = true;
                notes.Add("本地数据库口令不在配置包内（安全项），已保留当前口令");
            }
            else
            {
                skipped.Add(new ConfigurationPackageSkip("connection.localDatabase",
                    saveResult.Error ?? "本地数据库配置保存失败"));
            }
        }

        return requiresRestart;
    }

    /// <summary>应用功能开关（热更新即时生效）；未知键与敏感键名一律跳过</summary>
    private async Task ApplyFeatureTogglesAsync(
        ConfigurationPackageDocument document,
        List<string> applied,
        List<ConfigurationPackageSkip> skipped)
    {
        if (document.FeatureToggles is not { Count: > 0 })
        {
            skipped.Add(new ConfigurationPackageSkip("featureToggles", "配置包未包含功能开关，保留当前设置"));
            return;
        }

        var values = new Dictionary<string, object>();
        foreach (var (key, rawValue) in document.FeatureToggles)
        {
            if (SensitiveDataMasker.IsSensitiveFieldName(key))
            {
                skipped.Add(new ConfigurationPackageSkip($"featureToggles.{key}", "敏感字段名，已跳过（安全项不导入）"));
                continue;
            }

            if (!FeatureToggleKeys.TryGetValue(key, out var canonicalKey))
            {
                skipped.Add(new ConfigurationPackageSkip($"featureToggles.{key}", "未知功能开关，已跳过"));
                continue;
            }

            var value = ToClrValue(rawValue);
            if (value == null)
            {
                skipped.Add(new ConfigurationPackageSkip($"featureToggles.{key}", "取值为空，已跳过"));
                continue;
            }

            values[canonicalKey] = value;
        }

        if (values.Count == 0)
        {
            skipped.Add(new ConfigurationPackageSkip("featureToggles", "无有效功能开关可应用，保留当前设置"));
            return;
        }

        var saved = await _configurationStore.SaveSectionAsync("FeatureToggles", values).ConfigureAwait(false);
        if (saved)
            applied.Add("featureToggles");
        else
            skipped.Add(new ConfigurationPackageSkip("featureToggles", "功能开关保存失败（请检查配置文件权限或磁盘状态）"));
    }

    /// <summary>
    /// 角色权限快照一致性校验（US-SHELL-016：角色权限由代码定义，导入不应用，仅报告差异）。
    /// </summary>
    private void ReportRolePermissionConsistency(
        ConfigurationPackageDocument document,
        List<ConfigurationPackageSkip> skipped,
        List<string> notes)
    {
        if (document.RolePermissions is not { Count: > 0 })
            return;

        skipped.Add(new ConfigurationPackageSkip("rolePermissions",
            "角色权限由代码定义（NavigationCoordinator + IRoleDefinition），导入不应用，仅校验一致性"));

        var mismatches = CompareRoleSnapshot(document.RolePermissions);
        if (mismatches.Count == 0)
        {
            notes.Add("角色权限快照与当前程序一致");
            return;
        }

        foreach (var mismatch in mismatches)
            notes.Add($"角色权限不一致：{mismatch}");
    }

    /// <summary>把包内角色权限快照与当前程序的编译期权限基线逐角色比对</summary>
    private List<string> CompareRoleSnapshot(IReadOnlyList<ConfigurationPackageRoleSnapshot> imported)
    {
        var running = BuildRoleSnapshot();
        var mismatches = new List<string>();

        foreach (var item in imported)
        {
            var current = running.FirstOrDefault(entry => entry.Role == item.Role);
            if (current == null)
            {
                mismatches.Add($"包内角色 {item.Role} 不在当前程序中");
                continue;
            }

            if (!string.Equals(current.HomeView, item.HomeView, StringComparison.Ordinal))
                mismatches.Add($"角色 {item.Role} 主页视图：包内 {item.HomeView} / 当前 {current.HomeView}");

            var importedViews = (item.AllowedViews ?? new List<string>())
                .OrderBy(view => view, StringComparer.Ordinal)
                .ToList();
            if (!importedViews.SequenceEqual(current.AllowedViews, StringComparer.Ordinal))
            {
                mismatches.Add(
                    $"角色 {item.Role} 可访问视图：包内 [{string.Join(", ", importedViews)}] / 当前 [{string.Join(", ", current.AllowedViews)}]");
            }
        }

        foreach (var missing in running.Where(entry => imported.All(importedEntry => importedEntry.Role != entry.Role)))
            mismatches.Add($"当前程序角色 {missing.Role} 在配置包快照中缺失");

        return mismatches;
    }

    #endregion

    #region 辅助

    /// <summary>持久化的模式文本 → 枚举（无法识别时回退 Remote，与 <c>IConnectionModeService</c> 的解析语义一致）</summary>
    private static ConnectionMode ParseMode(string? mode)
        => string.Equals(mode, nameof(ConnectionMode.Local), StringComparison.OrdinalIgnoreCase)
            ? ConnectionMode.Local
            : ConnectionMode.Remote;

    /// <summary>反序列化得到的 JSON 值 → 可直接写入配置节的 CLR 值（布尔/字符串/数值）</summary>
    private static object? ToClrValue(object? value) => value switch
    {
        null => null,
        JsonElement element => element.ValueKind switch
        {
            JsonValueKind.True => (object)true,
            JsonValueKind.False => (object)false,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        },
        _ => value
    };

    #endregion
}
