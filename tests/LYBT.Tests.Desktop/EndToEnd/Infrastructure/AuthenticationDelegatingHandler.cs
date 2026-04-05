using System.Net.Http;
using System.Net.Http.Headers;

namespace LYBT.Tests.Desktop.EndToEnd.Infrastructure;

/// <summary>
/// Token 持有者 - 在 DI 容器中共享 Token 状态
/// </summary>
public class TokenHolder
{
    public string? AccessToken { get; set; }
}

/// <summary>
/// HTTP 认证拦截器 - 自动添加 Bearer Token
/// </summary>
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly TokenHolder _tokenHolder;

    public AuthenticationDelegatingHandler(TokenHolder tokenHolder)
    {
        _tokenHolder = tokenHolder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // 跳过登录和注册请求
        var path = request.RequestUri?.AbsolutePath ?? "";
        if (path.Contains("/auth/login") || path.Contains("/auth/register"))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // 添加认证 Header
        if (!string.IsNullOrEmpty(_tokenHolder.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenHolder.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
