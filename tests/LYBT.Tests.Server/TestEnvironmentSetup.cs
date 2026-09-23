using System;
using System.Runtime.CompilerServices;

namespace LYBT.Tests.Server;

/// <summary>
/// Server 测试宿主环境前置（程序集加载时执行一次）。
/// </summary>
/// <remarks>
/// <para><b>Security__AesKey</b>：患者敏感字段（电话/身份证等）由 <c>AesGcmValueConverter</c> 透明加解密，
/// 其密钥解析 <b>仅读环境变量</b>（<c>Security__AesKey</c> / <c>Security:AesKey</c>），且非 Development 环境
/// 缺失即 fail-fast（R-5：防止生产静默使用内置测试密钥）。测试宿主环境名为 <c>Test</c> → 必须显式提供测试密钥，
/// 否则触碰加密列的读写会以「敏感数据加密/解密失败」（ERR-00013 / HTTP 422）失败。</para>
/// <para>注入方式与生产部署一致（环境变量）；密钥值与 <c>AesGcmValueConverter</c> 的 Development 回退测试密钥同值。
/// 已由外部显式提供密钥时（如 CI 注入真实密钥）不覆盖。</para>
/// </remarks>
internal static class TestEnvironmentSetup
{
    /// <summary>Development 环境回退测试密钥（32 字节 Base64，与 AesGcmValueConverter.FallbackKey 同值）</summary>
    private const string TestAesKey = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=";

    [ModuleInitializer]
    internal static void Initialize()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Security__AesKey")))
        {
            Environment.SetEnvironmentVariable("Security__AesKey", TestAesKey);
        }
    }
}
