using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Events;
using LYBT.Shared.Utilities.Extensions.ServiceCollection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Caching;

/// <summary>
/// Desktop 缓存管理器 -- 统一管理 ApiService GET 缓存 + 发布缓存失效事件
/// </summary>
/// <remarks>
/// 职责:
/// 1. 清理 IMemoryCache (ApiService 的 HTTP GET 响应缓存)
/// 2. 发布 CacheEvents.InvalidatedEvent 通知各模块缓存订阅者
/// </remarks>
public sealed class DesktopCacheManager : IDesktopCacheManager
{
    private readonly IMemoryCache _memoryCache;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<DesktopCacheManager> _logger;

    public DesktopCacheManager(
        IMemoryCache memoryCache,
        IEventAggregator eventAggregator,
        ILogger<DesktopCacheManager> logger)
    {
        _memoryCache = memoryCache;
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    public void InvalidatePatientCaches()
    {
        _logger.LogDebug("[Cache] Desktop invalidating patient caches");
        _memoryCache.RemoveByPrefix("GET:/api/v1/patients");
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.Patients,
                Reason = "PatientCRUD"
            });
    }

    public void InvalidateMedicalCaseCaches()
    {
        _logger.LogDebug("[Cache] Desktop invalidating medical case caches");
        _memoryCache.RemoveByPrefix("GET:/api/v1/medicalcases");
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.MedicalCases,
                Reason = "MedicalCaseStateChange"
            });
    }

    public void InvalidateHerbCaches()
    {
        _logger.LogDebug("[Cache] Desktop invalidating herb caches");
        _memoryCache.RemoveByPrefix("GET:/api/v1/herbs");
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.Herbs,
                Reason = "HerbCRUD"
            });
    }

    public void InvalidateFormulaCaches()
    {
        _logger.LogDebug("[Cache] Desktop invalidating formula caches");
        _memoryCache.RemoveByPrefix("GET:/api/v1/formulas");
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.Formulas,
                Reason = "FormulaCRUD"
            });
    }

    public void InvalidateUserCaches()
    {
        _logger.LogDebug("[Cache] Desktop invalidating user caches");
        _memoryCache.RemoveByPrefix("GET:/api/v1/users");
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.Users,
                Reason = "UserCRUD"
            });
    }

    public void InvalidateAll()
    {
        _logger.LogInformation("[Cache] Desktop invalidating ALL caches (post-sync)");
        _memoryCache.Clear();
        _eventAggregator.GetEvent<CacheEvents.InvalidatedEvent>().Publish(
            new CacheInvalidatedPayload
            {
                Domain = CacheDomain.All,
                Reason = "SyncCompleted"
            });
    }
}
