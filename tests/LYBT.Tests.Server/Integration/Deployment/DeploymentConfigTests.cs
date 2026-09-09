using System.Net;
using LYBT.Tests.Server.Integration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Xunit;

namespace LYBT.Tests.Server.Integration.Deployment;

/// <summary>
/// L4 部署层测试（P2 批次——方案 §五 L4）：Production 配置契约守护——
/// Program.Main 构建前校验（密码/JWT/token）读原始配置链（含环境变量）——
/// 测试用**环境变量注入**（真实部署方式——拦截上线坑 #4 #5 #7）
/// 注意：进程级环境变量——本类串行（EnvIsolated collection）避免并行污染
/// </summary>
[Collection("EnvIsolated")]
public class DeploymentConfigTests
{
    private const string ValidJwt = "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2";

    private static readonly (string Key, string? Value)[] ValidEnv =
    {
        ("Jwt__SecretKey", ValidJwt),
        ("DefaultPasswords__SysAdminPassword", "Admin@Lybt2026"),
        ("DefaultPasswords__NewUserPassword", "User@Lybt2026"),
        ("SystemAdmin__InitialSetupToken", "test-setup-token-2026-abcdefghijklmnopqrstuvwxyz"),
        ("SystemAdmin__AllowAutoCreateInProduction", "false"),
        ("ConnectionStrings__DefaultConnection", "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True")
    };

    /// <summary>设置环境变量（记录原值供还原）</summary>
    private static Dictionary<string, string?> SetEnv(params (string Key, string? Value)[] pairs)
    {
        var original = new Dictionary<string, string?>();
        foreach (var (key, value) in pairs)
        {
            original[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
        return original;
    }

    private static void RestoreEnv(Dictionary<string, string?> original)
    {
        foreach (var (key, value) in original)
            Environment.SetEnvironmentVariable(key, value);
    }

    private static LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationValidator CreateValidator(
        params (string Key, string Value)[] overrides)
    {
        // ADR-0019: appsettings 在 config/ 子目录。真实部署链 = JSON + 环境变量
        // （appsettings.Production.json 的 ${DefaultPasswords__...} 等占位符由环境变量展开——
        // 与 Program 配置链一致，见 ConfigurationPostProcessor / EnvironmentVariablesExtensions）
        var configDir = Path.Combine(AppContext.BaseDirectory, "config");
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonFile(Path.Combine(configDir, "appsettings.json"), optional: true)
            .AddJsonFile(Path.Combine(configDir, "appsettings.Production.json"), optional: true)
            .AddEnvironmentVariables();
        var dict = new Dictionary<string, string?>();
        foreach (var (key, value) in overrides)
            dict[key] = value;
        if (dict.Count > 0)
            builder.AddInMemoryCollection(dict);
        return new LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationValidator(builder.Build());
    }

    [Fact]
    public void Production_InvalidJwtSecret_IsRejectedByValidator()
    {
        // #4: 非 Base64 JWT → 校验器拦截（纯逻辑——无 host 崩溃风险）
        var validator = CreateValidator(
            ("Jwt:SecretKey", "not-a-base64-key!"),
            ("Jwt:Issuer", "Test"),
            ("Jwt:Audience", "Test"));

        var act = () => validator.ValidateOrThrow();

        act.Should().Throw<Exception>().WithMessage("*SecretKey*");
    }

    [Fact]
    public void Production_UnexpandedPlaceholder_IsRejectedByValidator()
    {
        // #7: 未展开 ${} 占位符（AllowAutoCreate=true 触发 token 校验）
        var validator = CreateValidator(
            ("Jwt:SecretKey", ValidJwt),
            ("Jwt:Issuer", "Test"),
            ("Jwt:Audience", "Test"),
            ("SystemAdmin:AllowAutoCreateInProduction", "true"),
            ("SystemAdmin:InitialSetupToken", "${LYBT_INITIAL_SETUP_TOKEN}"));

        var act = () => validator.ValidateOrThrow();

        act.Should().Throw<Exception>().WithMessage("*占位符*");
    }

    [Fact]
    public void Production_ValidConfig_PassesValidator()
    {
        // #4 #7 通过路径：合规配置 → 校验通过（无异常）
        // 真实部署语义：appsettings.Production.json 占位符由进程环境变量展开——
        // 显式注入全套 ValidEnv（还原原值）后校验（JSON 中的 ${...} 不再泄漏）
        var original = SetEnv(ValidEnv);
        try
        {
            var validator = CreateValidator(
                ("Jwt:Issuer", "Test"),
                ("Jwt:Audience", "Test"));

            var act = () => validator.ValidateOrThrow();

            act.Should().NotThrow();
        }
        finally
        {
            RestoreEnv(original);
        }
    }
}
