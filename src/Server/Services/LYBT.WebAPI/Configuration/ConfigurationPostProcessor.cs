using Microsoft.Extensions.Configuration;

namespace LYBT.WebAPI.Configuration;

/// <summary>
/// 配置后处理器（CFG-BATCH2 边界决策 2/3：占位符未展开/空串 → 视为无效 → 回退下一级）
/// 已知键清单遍历——取 providers 链中第一个有效值（非空串、非 ${...} 占位）；
/// 最高 provider 无效但低层有效 → 内存覆盖层写入有效值（等效回退）。
/// 只处理已知键，不全量遍历（避免误删合法配置）。
/// </summary>
public static class ConfigurationPostProcessor
{
    /// <summary>已知键清单（配置校验/注入敏感键——占位或空串视为无效）</summary>
    public static readonly string[] KnownKeys =
    {
        "Jwt:SecretKey",
        "ConnectionStrings:DefaultConnection",
        "DefaultPasswords:SysAdminPassword",
        "DefaultPasswords:NewUserPassword",
        "SystemAdmin:InitialSetupToken",
        "DesktopUpdate:FeedUrl"
    };

    /// <summary>值是否有效（非空串、非未展开占位符 ${...}）</summary>
    public static bool IsValidValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith("}", StringComparison.Ordinal))
            return false;
        return true;
    }

    /// <summary>
    /// 后处理：已知键若最高 provider 值无效（空串/占位），回退到下一级有效值（内存覆盖层写入）。
    /// 全部无效时保持原样——由配置校验器拦截提示。
    /// </summary>
    public static void Process(IConfigurationRoot root)
    {
        var fallbacks = new Dictionary<string, string?>();
        var providers = root.Providers.ToList(); // 从低到高顺序

        foreach (var key in KnownKeys)
        {
            // 从高到低取第一个有效值
            string? firstValid = null;
            for (var i = providers.Count - 1; i >= 0; i--)
            {
                if (providers[i].TryGet(key, out var value) && IsValidValue(value))
                {
                    firstValid = value;
                    break;
                }
            }

            var topValue = root[key];
            // 最高 provider 值无效（空/占位）且低层有有效值 → 覆盖为有效值（回退生效）
            if (!IsValidValue(topValue) && firstValid is not null)
                fallbacks[key] = firstValid;
        }

        if (fallbacks.Count > 0)
        {
            var builder = new ConfigurationBuilder().AddInMemoryCollection(fallbacks);
            var overlay = builder.Build();
            // 注入到 root（ConfigurationManager 作为 IConfigurationBuilder 支持追加）
            if (root is IConfigurationBuilder configBuilder)
                configBuilder.AddConfiguration(overlay);
        }
    }
}
