using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces.Services;

namespace LYBT.Desktop.Services.Handlers
{

/// <summary>
/// HTTP请求认证头处理器
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
private readonly ITokenManager _tokenManager;

public AuthHeaderHandler(ITokenManager tokenManager)
{
_tokenManager = tokenManager;
}

/// <inheritdoc/>
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
var token = _tokenManager.GetToken();

System.Diagnostics.Debug.WriteLine($"🔐 AuthHeaderHandler: URL={request.RequestUri}, Token={(!string.IsNullOrEmpty(token) ? "存在" : "空")}");

if (!string.IsNullOrEmpty(token))
{
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
// 安全加固：不输出任何 token 内容，只记录状态
System.Diagnostics.Debug.WriteLine($"🔐 已添加认证头: Bearer [REDACTED-{token.Length}chars]");
}
else
{
System.Diagnostics.Debug.WriteLine($" 没有Token，发送未认证请求");
}

var response = await base.SendAsync(request, cancellationToken);

System.Diagnostics.Debug.WriteLine($"🔐 API响应: {response.StatusCode} - {request.RequestUri}");

return response;
}
}
}
