// ---------------------------------------------------------------------------
// CachingHttpMessageHandler — 桌面 GET 响应缓存（读写一体的失效点）
// ---------------------------------------------------------------------------
// 架构审查 L3「缓存策略」定案：既有 IMemoryCache 只有失效入口、没有生产者
// （全仓 0 处 Set/GetOrCreate），且失效调用点散落在 ViewModel，导致
// 「失效空转 + 写路径漏失效」。本处理器把缓存的**生产**与**失效**同时下沉到传输层：
//
//   读：GET 2xx → 按「方法:路径?查询#用户域」缓存（TTL 分级，条目显式 Size=1）
//   写：非 GET 2xx → 按路径域前缀失效（含跨域依赖表），覆盖全部写路径，
//       不再依赖调用方自觉
//
// 键形如 `GET:/api/v1/patients?page=1&pageSize=20#{userId}`，因此
// `RemoveByPrefix("GET:/api/v1/patients")` 仍可命中原有前缀约定；
// 用户域后缀保证换用户/换账号后不会串读他人缓存。
//
// 不缓存的路径：导出/模板下载（大体积二进制）、健康检查、下载页。
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using LYBT.Desktop.Foundation.Caching;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// GET 响应缓存 + 写路径失效的 <see cref="DelegatingHandler"/>。
/// </summary>
public sealed class CachingHttpMessageHandler : DelegatingHandler
{
    /// <summary>不参与缓存的路径片段（导出/模板/健康检查/下载页）。</summary>
    private static readonly string[] ExcludedPathFragments =
    [
        "/export",
        "/import-template",
        "/health",
        "/download",
    ];

    /// <summary>写操作成功后需要连带失效的域（跨域依赖）。</summary>
    private static readonly Dictionary<string, string[]> WriteInvalidationMap = new(StringComparer.Ordinal)
    {
        // 挂号写操作会创建/变更关联医案（StartVisit/Cancel 联动）
        ["registrations"] = ["registrations", "medicalcases"],
        ["medicalcases"] = ["medicalcases", "registrations"],
    };

    /// <summary>目录类域：读多写少，TTL 放宽。</summary>
    private static readonly string[] CatalogDomains = ["herbs", "formulas"];

    private static readonly TimeSpan CatalogTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TransactionalTtl = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly DesktopCacheKeyRegistry _registry;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ILogger<CachingHttpMessageHandler> _logger;

    /// <summary>
    /// 初始化 <see cref="CachingHttpMessageHandler"/> 类的新实例。
    /// </summary>
    /// <param name="cache">缓存实例。</param>
    /// <param name="registry">缓存键登记表。</param>
    /// <param name="tokenStorage">令牌存储（用于按用户域隔离缓存）。</param>
    /// <param name="logger">日志器。</param>
    public CachingHttpMessageHandler(
        IMemoryCache cache,
        DesktopCacheKeyRegistry registry,
        ITokenStorageService tokenStorage,
        ILogger<CachingHttpMessageHandler> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is null)
            return await base.SendAsync(request, cancellationToken);

        if (request.Method == HttpMethod.Get)
            return await SendGetAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            InvalidateForWrite(request.RequestUri);

        return response;
    }

    private async Task<HttpResponseMessage> SendGetAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        if (!IsCacheablePath(uri.AbsolutePath))
            return await base.SendAsync(request, cancellationToken);

        var key = BuildKey(request.Method, uri);

        if (_cache.TryGetValue(key, out CachedResponse? cached)
            && cached is not null
            && cached.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogDebug("[Cache] Hit {Key}", key);
            return cached.ToResponseMessage(request);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return response;

        // 显式载入缓冲区：保证调用方仍能读取正文（ReadAsByteArrayAsync 在未缓冲时会消费流）
        await response.Content.LoadIntoBufferAsync();
        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ResolveTtl(uri.AbsolutePath),
            // IMemoryCache 注册了 SizeLimit，条目必须显式给出 Size，否则 Set 抛 InvalidOperationException
            Size = 1,
        };
        _registry.Track(key, options);
        _cache.Set(key, new CachedResponse(response.StatusCode, payload, contentType), options);

        return response;
    }

    private void InvalidateForWrite(Uri uri)
    {
        var domain = ExtractDomain(uri.AbsolutePath);
        if (domain is null)
            return;

        var targets = WriteInvalidationMap.TryGetValue(domain, out var mapped)
            ? mapped
            : [domain];

        foreach (var target in targets)
            _registry.RemoveByPrefix(_cache, $"GET:/api/v1/{target}");
    }

    private static bool IsCacheablePath(string absolutePath) =>
        !ExcludedPathFragments.Any(fragment =>
            absolutePath.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static string? ExtractDomain(string absolutePath)
    {
        const string prefix = "/api/v1/";
        if (!absolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = absolutePath[prefix.Length..];
        var slash = rest.IndexOf('/');
        var domain = slash < 0 ? rest : rest[..slash];
        return string.IsNullOrEmpty(domain) ? null : domain;
    }

    private static TimeSpan ResolveTtl(string absolutePath)
    {
        var domain = ExtractDomain(absolutePath);
        return domain is not null && CatalogDomains.Contains(domain, StringComparer.Ordinal)
            ? CatalogTtl
            : TransactionalTtl;
    }

    private string BuildKey(HttpMethod method, Uri uri)
    {
        var scope = _tokenStorage.GetLoginResponse()?.User.Id.ToString("N") ?? "anonymous";
        return $"{method.Method.ToUpperInvariant()}:{uri.AbsolutePath}{uri.Query}#{scope}";
    }

    /// <summary>缓存条目载荷（状态码 + 正文 + 内容类型）。</summary>
    private sealed record CachedResponse(HttpStatusCode StatusCode, byte[] Payload, string? ContentType)
    {
        public HttpResponseMessage ToResponseMessage(HttpRequestMessage request)
        {
            var content = new ByteArrayContent(Payload);
            if (!string.IsNullOrWhiteSpace(ContentType))
                content.Headers.TryAddWithoutValidation("Content-Type", ContentType);

            return new HttpResponseMessage(StatusCode)
            {
                RequestMessage = request,
                Content = content,
            };
        }
    }
}
