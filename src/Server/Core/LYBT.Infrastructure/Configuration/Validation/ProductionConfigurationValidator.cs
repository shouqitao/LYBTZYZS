using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace LYBT.Infrastructure.Configuration.Validation;

/// <summary>
/// Production 环境配置验证器
/// </summary>
public class ProductionConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly List<ConfigurationError> _errors = new();

    // 必需配置项定义
    private static readonly ConfigurationItem[] RequiredItems = new[]
    {
        new ConfigurationItem
        {
            Key = "ConnectionStrings:DefaultConnection",
            EnvVarName = "ConnectionStrings__DefaultConnection",
            Severity = Severity.Critical,
            Description = "数据库连接字符串",
            Example = "Server=localhost;Database=LYBTDB;User Id=sa;Password=***;TrustServerCertificate=True"
        },
        new ConfigurationItem
        {
            Key = "Jwt:SecretKey",
            EnvVarName = "Jwt__SecretKey",
            Severity = Severity.Critical,
            Description = "JWT 签名密钥",
            MinLength = 32,
            Example = "[自动生成的 Base64 字符串，至少 32 字符]"
        },
        new ConfigurationItem
        {
            Key = "DefaultPasswords:SysAdminPassword",
            EnvVarName = "DefaultPasswords__SysAdminPassword",
            Severity = Severity.Important,
            Description = "系统管理员默认密码",
            Example = "Admin@123456"
        },
        new ConfigurationItem
        {
            Key = "DefaultPasswords:NewUserPassword",
            EnvVarName = "DefaultPasswords__NewUserPassword",
            Severity = Severity.Important,
            Description = "新用户默认密码",
            Example = "User@123456"
        },
        new ConfigurationItem
        {
            Key = "SystemAdmin:UserName",
            EnvVarName = "SystemAdmin__UserName",
            Severity = Severity.Important,
            Description = "系统管理员用户名",
            Example = "admin"
        },
        new ConfigurationItem
        {
            Key = "SystemAdmin:Email",
            EnvVarName = "SystemAdmin__Email",
            Severity = Severity.Important,
            Description = "系统管理员邮箱",
            Pattern = @"^[^@]+@[^@]+\.[^@]+$",
            Example = "admin@example.com"
        },
        new ConfigurationItem
        {
            Key = "Jwt:Issuer",
            EnvVarName = "Jwt__Issuer",
            Severity = Severity.Important,
            Description = "JWT 发行者标识",
            Example = "LYBT-WebAPI"
        },
        new ConfigurationItem
        {
            Key = "Jwt:Audience",
            EnvVarName = "Jwt__Audience",
            Severity = Severity.Important,
            Description = "JWT 受众标识",
            Example = "LYBT-Client"
        },
        new ConfigurationItem
        {
            Key = "SystemAdmin:DisplayName",
            EnvVarName = "SystemAdmin__DisplayName",
            Severity = Severity.Important,
            Description = "系统管理员显示名称",
            Example = "系统管理员"
        },
        new ConfigurationItem
        {
            Key = "AllowedHosts",
            EnvVarName = "AllowedHosts",
            Severity = Severity.Optional,
            Description = "允许的主机名（多个用分号分隔）",
            Example = "example.com;*.example.com"
        },
        new ConfigurationItem
        {
            Key = "SystemAdmin:AllowAutoCreateInProduction",
            EnvVarName = "SystemAdmin__AllowAutoCreateInProduction",
            Severity = Severity.Optional,
            Description = "是否允许在生产环境自动创建系统管理员",
            Example = "false"
        },
        new ConfigurationItem
        {
            Key = "SystemAdmin:InitialSetupToken",
            EnvVarName = "SystemAdmin__InitialSetupToken",
            Severity = Severity.Optional,
            Description = "初始化安全令牌（AllowAutoCreateInProduction=true 时必须提供）",
            MinLength = 32,
            Example = "[至少 32 字符的随机安全令牌]"
        }
    };

    public ProductionConfigurationValidator(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 验证所有必需配置，如果发现错误则抛出异常
    /// </summary>
    public void ValidateOrThrow()
    {
        ValidateAllItems();

        if (_errors.Any())
        {
            throw new ProductionConfigurationException(GetDetailedErrorMessage());
        }
    }

    /// <summary>
    /// 验证 Critical 配置项（所有环境通用）
    /// T5-P3-01: 非生产环境降级为 Warning 日志，不终止启动
    /// </summary>
    /// <returns>缺失的 Critical 配置项描述列表（空列表表示全部通过）</returns>
    public List<string> ValidateCriticalItems()
    {
        var missing = new List<string>();
        foreach (var item in RequiredItems.Where(i => i.Severity == Severity.Critical))
        {
            var value = _configuration[item.Key];
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add($"{item.Description} ({item.Key}) -- 环境变量: {item.EnvVarName}");
            }
            else if (item.MinLength.HasValue && value.Length < item.MinLength.Value)
            {
                missing.Add($"{item.Description} ({item.Key}) -- 长度不足（需要至少 {item.MinLength} 字符）");
            }
        }
        return missing;
    }

    /// <summary>
    /// 验证 Important 配置项（非生产环境降级为 Warning）
    /// </summary>
    /// <returns>有问题的 Important 配置项描述列表（空列表表示全部通过）</returns>
    public List<string> ValidateImportantItems()
    {
        var issues = new List<string>();
        foreach (var item in RequiredItems.Where(i => i.Severity == Severity.Important))
        {
            var value = _configuration[item.Key];
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add($"{item.Description} ({item.Key}) -- 环境变量: {item.EnvVarName}");
            }
            else if (!string.IsNullOrEmpty(item.Pattern) &&
                     !Regex.IsMatch(value, item.Pattern))
            {
                issues.Add($"{item.Description} ({item.Key}) -- 格式验证失败");
            }
        }
        return issues;
    }

    private void ValidateAllItems()
    {
        foreach (var item in RequiredItems)
        {
            ValidateItem(item);
        }

        ValidateCrossFieldRules();
    }

    private void ValidateItem(ConfigurationItem item)
    {
        var value = _configuration[item.Key];

        // 检查 1: 值是否存在
        if (string.IsNullOrWhiteSpace(value))
        {
            // Optional 配置可以为空
            if (item.Severity == Severity.Optional)
            {
                return;
            }

            _errors.Add(new ConfigurationError
            {
                Item = item,
                ErrorType = ErrorType.Missing,
                Message = "配置值未设置"
            });
            return;
        }

        // Issue #1932: 占位符检查已移除
        // 现在使用环境变量直接覆盖配置文件中的默认值，不再使用#{VAR}#占位符格式

        // 检查 3: 长度验证
        if (item.MinLength.HasValue && value.Length < item.MinLength.Value)
        {
            _errors.Add(new ConfigurationError
            {
                Item = item,
                ErrorType = ErrorType.InvalidFormat,
                Message = $"长度不足（需要至少 {item.MinLength} 字符，当前 {value.Length}）"
            });
        }

        // 检查 4: 格式验证
        if (!string.IsNullOrEmpty(item.Pattern) &&
            !Regex.IsMatch(value, item.Pattern))
        {
            _errors.Add(new ConfigurationError
            {
                Item = item,
                ErrorType = ErrorType.InvalidFormat,
                Message = "格式验证失败"
            });
        }
    }

    private void ValidateCrossFieldRules()
    {
        var allowAutoCreate = _configuration["SystemAdmin:AllowAutoCreateInProduction"];

        // 仅当明确启用生产环境自动创建时才检查
        if (!string.Equals(allowAutoCreate, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = _configuration["SystemAdmin:InitialSetupToken"];
        var tokenItem = RequiredItems.First(i => i.Key == "SystemAdmin:InitialSetupToken");

        // 检查 Token 是否存在
        if (string.IsNullOrWhiteSpace(token))
        {
            _errors.Add(new ConfigurationError
            {
                Item = tokenItem,
                ErrorType = ErrorType.CrossFieldDependency,
                Message = "AllowAutoCreateInProduction=true 时必须提供 InitialSetupToken"
            });
            return;
        }

        // 检查是否为未展开的环境变量占位符
        if (token.StartsWith("${") && token.EndsWith("}"))
        {
            _errors.Add(new ConfigurationError
            {
                Item = tokenItem,
                ErrorType = ErrorType.CrossFieldDependency,
                Message = "InitialSetupToken 仍为未展开的环境变量占位符"
            });
            return;
        }

        // 检查最小长度
        if (token.Length < 32)
        {
            _errors.Add(new ConfigurationError
            {
                Item = tokenItem,
                ErrorType = ErrorType.CrossFieldDependency,
                Message = $"InitialSetupToken 长度不足（需要至少 32 字符，当前 {token.Length}）"
            });
        }
    }

    private string GetDetailedErrorMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("╔═══════════════════════════════════════════════════════════╗");
        sb.AppendLine("║   Production 配置验证失败                               ║");
        sb.AppendLine("╚═══════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        var criticalErrors = _errors.Where(e => e.Item.Severity == Severity.Critical).ToList();
        var importantErrors = _errors.Where(e => e.Item.Severity == Severity.Important).ToList();

        sb.AppendLine($"发现 {_errors.Count} 个配置错误：");
        sb.AppendLine();

        if (criticalErrors.Any())
        {
            sb.AppendLine(" CRITICAL 错误（必须修复）:");
            sb.AppendLine();
            foreach (var error in criticalErrors)
            {
                AppendErrorDetail(sb, error);
            }
        }

        if (importantErrors.Any())
        {
            sb.AppendLine(" IMPORTANT 错误（建议修复）:");
            sb.AppendLine();
            foreach (var error in importantErrors)
            {
                AppendErrorDetail(sb, error);
            }
        }

        var crossFieldErrors = _errors.Where(e => e.ErrorType == ErrorType.CrossFieldDependency).ToList();
        if (crossFieldErrors.Any())
        {
            sb.AppendLine("⚠ 跨字段安全验证错误（必须修复）:");
            sb.AppendLine();
            foreach (var error in crossFieldErrors)
            {
                AppendErrorDetail(sb, error);
            }
        }

        sb.AppendLine("───────────────────────────────────────────────────────────");
        sb.AppendLine("📖 详细配置指南: docs/deployment/production-setup.md");
        sb.AppendLine(" 验证脚本: .\\scripts\\validate-production-config.ps1");
        sb.AppendLine();

        return sb.ToString();
    }

    private void AppendErrorDetail(StringBuilder sb, ConfigurationError error)
    {
        sb.AppendLine($"  [{_errors.IndexOf(error) + 1}] {error.Item.Description}");
        sb.AppendLine($"      配置路径: {error.Item.Key}");
        sb.AppendLine($"      环境变量: {error.Item.EnvVarName}");
        sb.AppendLine($"      问题: {error.Message}");

        if (!string.IsNullOrEmpty(error.Item.Example))
        {
            sb.AppendLine($"      示例: {error.Item.Example}");
        }

        sb.AppendLine("      修复方法（Windows）:");
        sb.AppendLine($"      setx {error.Item.EnvVarName} \"<your-value>\"");
        sb.AppendLine("      修复方法（Linux）:");
        sb.AppendLine($"      export {error.Item.EnvVarName}=\"<your-value>\"");
        sb.AppendLine();
    }
}

/// <summary>
/// 配置项定义
/// </summary>
public class ConfigurationItem
{
    public required string Key { get; init; }
    public required string EnvVarName { get; init; }
    public required Severity Severity { get; init; }
    public required string Description { get; init; }
    public string? Example { get; init; }
    public int? MinLength { get; init; }
    public string? Pattern { get; init; }
}

/// <summary>
/// 配置严重性等级
/// </summary>
public enum Severity
{
    Critical,   // 关键配置，必须设置
    Important,  // 重要配置，建议设置
    Optional    // 可选配置
}

/// <summary>
/// 错误类型
/// </summary>
public enum ErrorType
{
    Missing,            // 配置缺失
    Placeholder,        // 仍为占位符
    InvalidFormat,      // 格式不正确
    CrossFieldDependency // 跨字段依赖不满足
}

/// <summary>
/// 配置错误
/// </summary>
public class ConfigurationError
{
    public required ConfigurationItem Item { get; init; }
    public required ErrorType ErrorType { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Production 配置异常
/// </summary>
public class ProductionConfigurationException : Exception
{
    public ProductionConfigurationException(string message) : base(message)
    {
    }
}
