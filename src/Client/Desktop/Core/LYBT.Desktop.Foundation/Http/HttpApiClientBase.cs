// ---------------------------------------------------------------------------
// HttpApiClientBase — shared HTTP helpers for LocalWebAPI IApiClient adapters
// ---------------------------------------------------------------------------
// Provides the HttpClient + System.Text.Json plumbing used by every
// {Domain}HttpApiClient adapter (split from the former HttpClientApiClient).
//
// LocalWebAPI returns raw DTOs; helper methods wrap them in ApiResponse<T>.
// ---------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.Json;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 本地模式 API 客户端基类 — 提供 HTTP 调用与 ApiResponse&lt;T&gt; 信封包装的公共 helper。
/// 各领域适配器（{Domain}HttpApiClient）继承本类实现对应 IApiClient 子接口。
/// </summary>
internal abstract class HttpApiClientBase
{
    /// <summary>
    /// JSON serialization options matching LocalWebAPI format:
    /// PascalCase naming, case-insensitive deserialization.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// 初始化 <see cref="HttpApiClientBase"/>。
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating named HttpClient instances.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClientFactory"/> is null.</exception>
    protected HttpApiClientBase(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    protected HttpClient CreateClient() => _httpClientFactory.CreateClient();

    protected static StringContent ToJsonContent<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct = default)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// 反序列化 LocalWebAPI 响应并解包 ApiResponse&lt;T&gt; 信封（与 Remote/Refit 契约一致）。
    /// 兼容两种情况：LocalWebAPI 返回信封时取 Data；极端情况返回裸 T 时直接反序列化。
    /// </summary>
    protected static async Task<ApiResponse<T>> DeserializeEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken ct = default)
    {
        var json = await response.Content.ReadAsStringAsync(ct);

        try
        {
            var envelope = JsonSerializer.Deserialize<ApiResponse<T>>(json, JsonOptions);
            if (envelope != null)
                return envelope;
        }
        catch (JsonException)
        {
            // 不是信封格式，回退裸反序列化
        }

        var raw = JsonSerializer.Deserialize<T>(json, JsonOptions);
        return ApiResponse<T>.CreateSuccess(raw!);
    }

    protected static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();
        var message = !string.IsNullOrWhiteSpace(errorContent)
            ? errorContent
            : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    protected static ApiResponse<T> WrapSuccess<T>(T data, string message = "操作成功")
        => ApiResponse<T>.CreateSuccess(data, message);

    protected static ApiResponse WrapSuccess(string message = "操作成功")
        => ApiResponse.CreateSuccess(null, message);

    /// <summary>Build URL with pagination + optional filter parameters.</summary>
    protected static string BuildPagedUrl(string baseUrl, int page, int pageSize, params (string Key, string? Value)[] filters)
    {
        var sb = new StringBuilder($"{baseUrl}?page={page}&pageSize={pageSize}");
        foreach (var (key, value) in filters)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            sb.Append('&');
            sb.Append(key);
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }
        return sb.ToString();
    }

    /// <summary>Build URL with conditional query parameters.</summary>
    protected static string BuildQueryString(string baseUrl, params (string Key, string? Value)[] parameters)
    {
        var sb = new StringBuilder(baseUrl);
        var first = !baseUrl.Contains('?');
        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            sb.Append(first ? '?' : '&');
            sb.Append(key);
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
            first = false;
        }
        return sb.ToString();
    }

    /// <summary>Unified HTTP request execution with response handling.</summary>
    protected async Task<HttpResponseMessage> SendAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
    {
        using var client = CreateClient();
        HttpResponseMessage response;
        if (body != null)
        {
            using var content = ToJsonContent(body);
            response = method.Method.ToUpperInvariant() switch
            {
                "POST" => await client.PostAsync(url, content, ct),
                "PUT" => await client.PutAsync(url, content, ct),
                "PATCH" => await client.PatchAsync(url, content, ct),
                _ => throw new ArgumentException($"Unsupported HTTP method: {method.Method}")
            };
        }
        else
        {
            response = method.Method.ToUpperInvariant() switch
            {
                "GET" => await client.GetAsync(url, ct),
                "POST" => await client.PostAsync(url, null, ct),
                "PUT" => await client.PutAsync(url, null, ct),
                "DELETE" => await client.DeleteAsync(url, ct),
                _ => throw new ArgumentException($"Unsupported HTTP method: {method.Method}")
            };
        }
        await EnsureSuccessOrThrowAsync(response);
        return response;
    }

    /// <summary>GET -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    protected async Task<ApiResponse<T>> GetAndWrapAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await SendAsync(url, HttpMethod.Get, ct: ct);
        return await DeserializeEnvelopeAsync<T>(response, ct);
    }

    /// <summary>GET -> deserialize -> return raw T (local-only methods).</summary>
    protected async Task<T> GetRawAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await SendAsync(url, HttpMethod.Get, ct: ct);
        var envelope = await DeserializeEnvelopeAsync<T>(response, ct);
        return envelope.Data ?? default!;
    }

    /// <summary>POST with JSON body -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    protected Task<ApiResponse<T>> PostAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)
        => SendAndWrapAsync<T>(url, HttpMethod.Post, body, ct);

    /// <summary>Unified void HTTP request -> ApiResponse.</summary>
    protected async Task<ApiResponse> SendVoidAsync(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
    {
        await SendAsync(url, method, body, ct);
        return WrapSuccess();
    }

    /// <summary>POST -> non-generic ApiResponse (void operations).</summary>
    protected Task<ApiResponse> PostVoidAsync(string url, object? body = null, CancellationToken ct = default)
        => SendVoidAsync(url, HttpMethod.Post, body, ct);

    /// <summary>POST -> return raw T (local-only methods).</summary>
    protected async Task<T> PostRawAsync<T>(string url, object? body = null, CancellationToken ct = default)
    {
        var response = await SendAsync(url, HttpMethod.Post, body, ct);
        var envelope = await DeserializeEnvelopeAsync<T>(response, ct);
        return envelope.Data ?? default!;
    }

    /// <summary>PUT with JSON body -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    protected Task<ApiResponse<T>> PutAndWrapAsync<T>(string url, object? body = null, CancellationToken ct = default)
        => SendAndWrapAsync<T>(url, HttpMethod.Put, body, ct);

    /// <summary>PUT -> non-generic ApiResponse (void operations).</summary>
    protected Task<ApiResponse> PutVoidAsync(string url, object? body = null, CancellationToken ct = default)
        => SendVoidAsync(url, HttpMethod.Put, body, ct);

    /// <summary>DELETE -> non-generic ApiResponse.</summary>
    protected Task<ApiResponse> DeleteVoidAsync(string url, CancellationToken ct = default)
        => SendVoidAsync(url, HttpMethod.Delete, ct: ct);

    /// <summary>HTTP request -> deserialize -> wrap in ApiResponse&lt;T&gt;.</summary>
    protected async Task<ApiResponse<T>> SendAndWrapAsync<T>(string url, HttpMethod method, object? body = null, CancellationToken ct = default)
    {
        var response = await SendAsync(url, method, body, ct);
        return await DeserializeEnvelopeAsync<T>(response, ct);
    }

    /// <summary>GET -> server-side pagination envelope -> wrap in ApiResponse&lt;PagedResult&lt;T&gt;&gt;.</summary>
    protected Task<ApiResponse<PagedResult<T>>> GetPagedAndWrapAsync<T>(string url, CancellationToken ct = default)
        => GetAndWrapAsync<PagedResult<T>>(url, ct);

    /// <summary>GET -> return HttpResponseMessage (file downloads). Caller disposes response.</summary>
    protected async Task<HttpResponseMessage> GetResponseAsync(string url, CancellationToken ct = default)
    {
        var client = CreateClient();
        var response = await client.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response);
        return response;
    }
}
