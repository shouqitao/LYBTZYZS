using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Events;
using LYBT.Desktop.Foundation.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Caching;

/// <summary>
/// Desktop 缓存管理器 -- 统一管理 HTTP GET 响应缓存 + 发布缓存失效事件
/// </summary>
/// <remarks>
/// <para>职责:</para>
/// <para>1. 按域清理 <see cref="IMemoryCache"/> 中的 GET 响应缓存（经
/// <see cref="DesktopCacheKeyRegistry"/> 前缀失效，**不再反射** <c>MemoryCache</c> 私有集合）</para>
/// <para>2. 发布 <see cref="CacheEvents.InvalidatedEvent"/> 通知各模块缓存订阅者</para>
/// <para>说明：写路径的自动失效已下沉到 <c>CachingHttpMessageHandler</c>（传输层），
/// 本类提供的是「业务事件驱动」的补充失效入口。</para>
/// </remarks>
public sealed class DesktopCacheManager : IDesktopCacheManager
{
    private readonly IMemoryCache _memoryCache;
    private readonly DesktopCacheKeyRegistry _registry;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<DesktopCacheManager> _logger;

    public DesktopCacheManager(
        IMemoryCache memoryCache,
        DesktopCacheKeyRegistry registry,
        IEventAggregator eventAggregator,
        ILogger<DesktopCacheManager> logger)
    {
        _memoryCache = memoryCache;
        _registry = registry;
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    public void InvalidatePatientCaches() => Invalidate(CacheDomain.Patients, "patients", "PatientCRUD");

    public void InvalidateMedicalCaseCaches() => Invalidate(CacheDomain.MedicalCases, "medicalcases", "MedicalCaseStateChange");

    public void InvalidateHerbCaches() => Invalidate(CacheDomain.Herbs, "herbs", "HerbCRUD");

    public void InvalidateFormulaCaches() => Invalidate(CacheDomain.Formulas, "formulas", "FormulaCRUD");

    public void InvalidateUserCaches() => Invalidate(CacheDomain.Users, "users", "UserCRUD");

    public void InvalidateRegistrationCaches() =>
        Invalidate(CacheDomain.Registrations, "registrations", "RegistrationStateChange", "medicalcases");

    public void InvalidateReportCaches() => Invalidate(CacheDomain.Reports, "reports", "ReportRefresh");

    /// <inheritdoc />
    public void InvalidateAll()
    {
        var removed = _registry.Clear(_memoryCache);
        _logger.LogInformation("[Cache] InvalidateAll - removed {Count} entries", removed);
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.All,
                Reason = "InvalidateAll"
            });
    }

    /// <summary>
    /// 失效指定域（可含跨域连带域）并发布域失效事件。
    /// </summary>
    /// <param name="domain">对外发布的失效域。</param>
    /// <param name="pathDomains">需要清理缓存键的 URL 域（第一个为主动域）。</param>
    /// <param name="reason">失效原因（事件载荷）。</param>
    private void Invalidate(CacheDomain domain, string pathDomains, string reason, params string[] extraPathDomains)
    {
        var removed = _registry.RemoveByPrefix(_memoryCache, $"GET:/api/v1/{pathDomains}");
        foreach (var extra in extraPathDomains)
            removed += _registry.RemoveByPrefix(_memoryCache, $"GET:/api/v1/{extra}");

        _logger.LogDebug("[Cache] Domain {Domain} invalidated - removed {Count} entries", domain, removed);

        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = domain,
                Reason = reason
            });
    }
}
