namespace LYBT.Shared.Configuration.Constants;

/// <summary>
/// 本地开发环境常用端点地址
/// </summary>
public static class LocalEndpoints
{
    /// <summary>
    /// LocalWebAPI 本地地址
    /// </summary>
    public const string LocalWebApiBaseUrl = "http://localhost:5300";

    /// <summary>
    /// 远程 WebAPI HTTP 地址
    /// </summary>
    public const string RemoteWebApiBaseUrl = "http://localhost:5000";

    /// <summary>
    /// 远程 WebAPI HTTPS 地址
    /// </summary>
    public const string RemoteWebApiHttpsUrl = "https://localhost:5001";
}
