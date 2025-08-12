using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.CQRS.Queries;
using LYBT.Shared.Interfaces.Caching;

namespace LYBT.Infrastructure.CQRS.Behaviors
{
    /// <summary>
    /// 缓存行为管道 - UltraThink重构架构
    /// 为查询操作提供自动缓存功能，提升系统性能
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(
            IMemoryCacheService cacheService,
            ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // 只对查询操作应用缓存
            if (!IsQuery(request))
            {
                return await next();
            }

            var cacheKey = GetCacheKey(request);
            if (string.IsNullOrEmpty(cacheKey))
            {
                // 如果无法生成缓存键，直接执行操作
                return await next();
            }

            try
            {
                // 尝试从缓存获取
                var cachedResult = await _cacheService.GetAsync<TResponse>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("缓存命中 - Key: {CacheKey}, RequestType: {RequestType}", 
                        cacheKey, typeof(TRequest).Name);
                    return cachedResult;
                }

                // 缓存未命中，执行实际操作
                _logger.LogDebug("缓存未命中 - Key: {CacheKey}, RequestType: {RequestType}", 
                    cacheKey, typeof(TRequest).Name);

                var result = await next();

                // 将结果缓存
                if (result != null && ShouldCache(request))
                {
                    var expiration = GetCacheExpiration(request);
                    await _cacheService.SetAsync(cacheKey, result, expiration);
                    
                    _logger.LogDebug("结果已缓存 - Key: {CacheKey}, Expiration: {Expiration}", 
                        cacheKey, expiration);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "缓存操作失败 - Key: {CacheKey}, RequestType: {RequestType}", 
                    cacheKey, typeof(TRequest).Name);
                
                // 缓存失败不应影响业务逻辑，直接执行操作
                return await next();
            }
        }

        /// <summary>
        /// 判断是否为查询操作
        /// </summary>
        private bool IsQuery(TRequest request)
        {
            var requestType = request.GetType();
            
            // 检查是否实现了IQuery<T>接口
            if (IsQueryInterface(requestType))
            {
                return true;
            }

            // 基于命名约定判断
            var requestName = requestType.Name;
            return requestName.EndsWith("Query") || 
                   requestName.StartsWith("Get") || 
                   requestName.StartsWith("Find") || 
                   requestName.StartsWith("Search");
        }

        /// <summary>
        /// 检查类型是否实现了IQuery接口
        /// </summary>
        private bool IsQueryInterface(Type requestType)
        {
            var interfaces = requestType.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                if (@interface.IsGenericType && 
                    @interface.GetGenericTypeDefinition() == typeof(IQuery<>))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取缓存键
        /// </summary>
        private string GetCacheKey(TRequest request)
        {
            try
            {
                // 如果请求实现了缓存键生成方法
                if (request is QueryBase<TResponse> queryBase)
                {
                    return queryBase.GenerateCacheKey();
                }

                // 基于类型和哈希生成缓存键
                var requestType = request.GetType().Name;
                var hashCode = request.GetHashCode();
                return $"cqrs:query:{requestType}:{hashCode}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成缓存键失败 - RequestType: {RequestType}", typeof(TRequest).Name);
                return null;
            }
        }

        /// <summary>
        /// 判断是否应该缓存
        /// </summary>
        private bool ShouldCache(TRequest request)
        {
            // 如果请求有明确的缓存设置
            if (request is QueryBase<TResponse> queryBase)
            {
                return queryBase.EnableCache;
            }

            // 默认缓存查询操作
            return true;
        }

        /// <summary>
        /// 获取缓存过期时间
        /// </summary>
        private TimeSpan? GetCacheExpiration(TRequest request)
        {
            // 如果请求有明确的缓存过期设置
            if (request is QueryBase<TResponse> queryBase && queryBase.CacheExpiration.HasValue)
            {
                return queryBase.CacheExpiration.Value;
            }

            // 根据操作类型设置默认过期时间
            var requestName = request.GetType().Name;
            
            if (requestName.Contains("Statistics"))
                return TimeSpan.FromMinutes(15);  // 统计数据缓存15分钟
            
            if (requestName.Contains("GetById") || requestName.Contains("ById"))
                return TimeSpan.FromMinutes(10);  // 单个实体缓存10分钟
            
            if (requestName.Contains("Search"))
                return TimeSpan.FromMinutes(5);   // 搜索结果缓存5分钟
            
            if (requestName.Contains("GetPaged") || requestName.Contains("List"))
                return TimeSpan.FromMinutes(5);   // 列表数据缓存5分钟

            // 默认缓存5分钟
            return TimeSpan.FromMinutes(5);
        }
    }

    /// <summary>
    /// 缓存配置选项
    /// </summary>
    public class CachingOptions
    {
        /// <summary>
        /// 是否启用缓存行为管道
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 默认缓存过期时间
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 最大缓存过期时间
        /// </summary>
        public TimeSpan MaxExpiration { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// 缓存键前缀
        /// </summary>
        public string KeyPrefix { get; set; } = "cqrs";

        /// <summary>
        /// 不同操作类型的缓存时间配置
        /// </summary>
        public CacheExpirationSettings ExpirationSettings { get; set; } = new();
    }

    /// <summary>
    /// 缓存过期时间设置
    /// </summary>
    public class CacheExpirationSettings
    {
        public TimeSpan GetById { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan GetList { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan Search { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan Statistics { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan Reports { get; set; } = TimeSpan.FromMinutes(30);
    }
}