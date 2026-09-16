using System.Net;
using System.Net.Http;

namespace LYBT.Desktop.Foundation.ExceptionHandling;

/// <summary>
/// 桌面 API 客户端统一领域异常 —— 本地（HttpClientApiClient）与远程（RefitApiClient）两条传输路径
/// 的非 2xx 响应统一抛出本类型，使上层（Service / ViewModel / 错误映射器）只需处理一种异常形状。
/// </summary>
/// <remarks>
/// <para>取代此前「本地抛 <see cref="HttpRequestException"/>、远程抛 Refit <c>ApiException</c>」的双形状：
/// 后者迫使 <see cref="ClientErrorMessageMapper"/> 用类型全名反射解析 Content，且本地路径会丢失服务端业务消息。</para>
/// <para><b>继承 <see cref="HttpRequestException"/> 是有意为之</b>：既有调用方大量以
/// <c>catch (HttpRequestException)</c> + <c>ex.StatusCode</c> 处理传输失败
/// （<c>LogoutService</c> / <c>ApiHealthCheckService</c> / <c>ConnectionModeService</c> /
/// <c>FormulaImportDialogViewModel</c> / E2E 断言助手），继承该基类可在引入统一形状的同时**不破坏现有功能**，
/// 并叠加 <see cref="ErrorCode"/> / <see cref="ServerMessage"/> 两个新信息。</para>
/// <para><see cref="Exception.Message"/> 始终保留原始响应体（诊断用）；面向用户的文案由
/// <see cref="ClientErrorMessageMapper"/> 依据 <see cref="ServerMessage"/> / 状态码生成。</para>
/// </remarks>
public sealed class ApiClientException : HttpRequestException
{
    /// <summary>
    /// 初始化 <see cref="ApiClientException"/> 类的新实例。
    /// </summary>
    /// <param name="message">原始诊断消息（通常为响应体）。</param>
    /// <param name="statusCode">HTTP 状态码。</param>
    /// <param name="errorCode">服务端错误码（信封 <c>errors.code</c>，可能为空）。</param>
    /// <param name="serverMessage">服务端面向用户的消息（信封 <c>message</c>，可能为空）。</param>
    /// <param name="innerException">内部异常。</param>
    public ApiClientException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        string? serverMessage = null,
        Exception? innerException = null)
        : base(message, innerException, statusCode)
    {
        ErrorCode = errorCode;
        ServerMessage = serverMessage;
    }

    /// <summary>服务端错误码（形如 <c>LYBT-XXX-NNN</c>）；不可用时为 null。</summary>
    public string? ErrorCode { get; }

    /// <summary>服务端面向用户的消息；不可用时为 null（此时由状态码映射兜底）。</summary>
    public string? ServerMessage { get; }
}
