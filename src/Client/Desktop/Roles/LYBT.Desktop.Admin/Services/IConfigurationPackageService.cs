using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Admin.Services;

/// <summary>
/// 配置导入导出服务（US-SHELL-016）——把可移植的客户端配置导出为单个 JSON 配置包，或从配置包导入并应用。
/// </summary>
/// <remarks>
/// <para><b>安全边界（US-SHELL-016 规则 3）</b>：配置包<b>永不包含任何密钥</b>——不含
/// <c>Jwt:SecretKey</c>、不含 <c>DefaultPasswords</c>、不含数据库口令（导出占位 <c>***</c>）、
/// 不含任何访问/刷新令牌。因此导入侧无需为「保留当前 Jwt:SecretKey」做任何特殊处理：
/// 密钥根本不随包传输，本机既有密钥自然保持不变。</para>
/// <para><b>角色权限</b>：角色 → 视图权限是编译期定义
/// （<c>NavigationCoordinator.ViewRoleAccess</c> + <see cref="LYBT.Desktop.Contracts.Roles.IRoleDefinition"/>），
/// 既不落盘也不可导入。配置包中的 <c>rolePermissions</c> 只是导出时的快照，
/// 导入时仅与当前程序比对一致性并报告差异，<b>不做任何应用</b>。</para>
/// <para><b>导入生效面</b>：诊所信息（即时生效）、连接设置（远程地址/模式/本地数据库，重启生效）、
/// 功能开关（热更新）。</para>
/// </remarks>
public interface IConfigurationPackageService
{
    /// <summary>
    /// 导出配置包（UTF-8、缩进、camelCase 的 JSON 单文件）。
    /// </summary>
    /// <param name="filePath">目标文件完整路径（父目录不存在时自动创建）</param>
    /// <param name="ct">取消标记</param>
    /// <returns>成功时返回包含节清单与文件大小的摘要；失败（IO/权限）时返回中文错误消息，不抛异常</returns>
    Task<CommandResult<ConfigurationPackageResult>> ExportAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// 导入配置包（导入前校验格式：JSON 解析失败 / 缺失或版本不支持的 <c>packageVersion</c> → 直接拒绝，不应用任何配置）。
    /// </summary>
    /// <param name="filePath">配置包完整路径</param>
    /// <param name="ct">取消标记</param>
    /// <returns>成功时返回应用/跳过报告与重启提示；失败时返回中文错误消息，不抛异常</returns>
    Task<CommandResult<ConfigurationPackageResult>> ImportAsync(string filePath, CancellationToken ct = default);
}

/// <summary>
/// 配置包根文档（US-SHELL-016；JSON 属性 camelCase、枚举经 <c>JsonStringEnumConverter</c> 按成员名写出）。
/// </summary>
public sealed class ConfigurationPackageDocument
{
    /// <summary>当前支持的配置包格式版本（<c>packageVersion</c> 必须精确匹配）</summary>
    public const string CurrentVersion = "1.0";

    /// <summary>敏感值占位符——口令等安全项导出时恒为此值，导入时保留当前值</summary>
    public const string MaskedSecret = "***";

    /// <summary>
    /// 配置包格式版本（缺失或与 <see cref="CurrentVersion"/> 不符 → 导入被拒绝）。
    /// <b>无默认值</b>：缺失时必须保持 null，否则模型默认值会让「无版本标识的任意 JSON」通过校验。
    /// </summary>
    public string? PackageVersion { get; set; }

    /// <summary>导出时间（含时区偏移）</summary>
    public DateTimeOffset ExportedAt { get; set; }

    /// <summary>导出端应用信息（仅供参考，导入不应用）</summary>
    public ConfigurationPackageApplicationSection? Application { get; set; }

    /// <summary>诊所信息（导入：经 <c>IClinicSettingsService</c> 应用，即时生效）</summary>
    public ConfigurationPackageClinicSection? ClinicSettings { get; set; }

    /// <summary>连接设置（导入：远程地址/模式/本地数据库，重启生效）</summary>
    public ConfigurationPackageConnectionSection? Connection { get; set; }

    /// <summary>角色权限快照（只读导出；导入不应用，仅校验一致性）</summary>
    public List<ConfigurationPackageRoleSnapshot>? RolePermissions { get; set; }

    /// <summary>
    /// 功能开关（键为 camelCase，如 <c>overwriteConflicts</c>；导入：经 <c>IClientConfigurationStore</c>
    /// 应用，热更新即时生效）。值按 JSON 原生类型承载（布尔/字符串）。
    /// </summary>
    public Dictionary<string, object?>? FeatureToggles { get; set; }
}

/// <summary>应用信息节（导出端标识，仅作参考）</summary>
public sealed class ConfigurationPackageApplicationSection
{
    /// <summary>应用名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>应用版本</summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>诊所信息节</summary>
public sealed class ConfigurationPackageClinicSection
{
    /// <summary>诊所名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>科室</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>地址</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>电话</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>电子邮箱</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>执业许可证号</summary>
    public string LicenseNumber { get; set; } = string.Empty;
}

/// <summary>连接设置节</summary>
public sealed class ConfigurationPackageConnectionSection
{
    /// <summary>首选连接模式（Local/Remote）</summary>
    public ConnectionMode Mode { get; set; } = ConnectionMode.Remote;

    /// <summary>
    /// 远程服务器地址。导出时经 <c>SensitiveDataMasker.MaskUri</c> 脱敏——
    /// 若地址内嵌凭据（查询参数形式）会被替换为 <c>***</c>，此类地址导入时会被跳过。
    /// </summary>
    public string RemoteUrl { get; set; } = string.Empty;

    /// <summary>本地数据库设置（本地模式）</summary>
    public ConfigurationPackageLocalDatabaseSection? LocalDatabase { get; set; }
}

/// <summary>本地数据库节（<c>password</c> 永不导出，恒为 <c>***</c>）</summary>
public sealed class ConfigurationPackageLocalDatabaseSection
{
    /// <summary>提供程序（LocalDb / SqlServer）</summary>
    public LocalDatabaseProvider Provider { get; set; } = LocalDatabaseProvider.LocalDb;

    /// <summary>实例地址</summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>数据库名</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>是否使用 Windows 集成认证</summary>
    public bool UseWindowsAuthentication { get; set; } = true;

    /// <summary>SQL Server 登录名（集成认证时为空）</summary>
    public string? UserId { get; set; }

    /// <summary>口令占位——导出恒为 <see cref="ConfigurationPackageDocument.MaskedSecret"/>，导入时保留当前口令</summary>
    public string Password { get; set; } = ConfigurationPackageDocument.MaskedSecret;
}

/// <summary>单个角色的权限快照（编译期权限基线的导出投影，导入仅用于一致性校验）</summary>
public sealed class ConfigurationPackageRoleSnapshot
{
    /// <summary>角色</summary>
    public UserRole Role { get; set; }

    /// <summary>该角色主页视图名</summary>
    public string HomeView { get; set; } = string.Empty;

    /// <summary>该角色可导航的视图名（按名称排序，保证快照可比对）</summary>
    public List<string> AllowedViews { get; set; } = new();
}

/// <summary>导入时被跳过的一项（节名 + 中文原因）</summary>
/// <param name="Section">节/键名（如 <c>connection.remoteUrl</c>）</param>
/// <param name="Reason">跳过原因</param>
public sealed record ConfigurationPackageSkip(string Section, string Reason);

/// <summary>导出摘要 / 导入报告（US-SHELL-016）</summary>
public sealed record ConfigurationPackageResult
{
    /// <summary>配置包文件路径</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>配置包文件大小（字节）</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>导出：包内包含的节清单</summary>
    public IReadOnlyList<string> Sections { get; init; } = Array.Empty<string>();

    /// <summary>导入：已应用的节清单</summary>
    public IReadOnlyList<string> AppliedSections { get; init; } = Array.Empty<string>();

    /// <summary>导入：被跳过的项及原因</summary>
    public IReadOnlyList<ConfigurationPackageSkip> SkippedItems { get; init; } = Array.Empty<ConfigurationPackageSkip>();

    /// <summary>导入：附加说明（口令保留、角色权限一致性结论等）</summary>
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();

    /// <summary>导入：是否需要重启应用才能生效（连接设置/本地数据库变更）</summary>
    public bool RequiresRestart { get; init; }
}
