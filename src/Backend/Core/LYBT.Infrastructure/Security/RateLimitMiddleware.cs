using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// API限流中间件 - UltraThink重构安全防护
    /// 实现令牌桶算法，防止API滥用和DDoS攻击
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly RateLimitOptions _options;

        // 令牌桶存储
        private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets 
            = new ConcurrentDictionary<string, TokenBucket>();

        public RateLimitMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitMiddleware> logger,
            RateLimitOptions options)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 获取客户端标识
            var clientId = GetClientIdentifier(context);
            
            // 检查是否在白名单中
            if (_options.WhitelistedIPs.Contains(GetClientIP(context)))
            {
                await _next(context);
                return;
            }

            // 获取对应的限制配置
            var limitConfig = GetRateLimitConfig(context);
            if (limitConfig == null)
            {
                await _next(context);
                return;
            }

            // 检查限流
            var allowed = await CheckRateLimitAsync(clientId, limitConfig);
            
            if (!allowed)
            {
                await HandleRateLimitExceeded(context, clientId);
                return;
            }

            // 添加限流响应头
            AddRateLimitHeaders(context, clientId, limitConfig);

            await _next(context);
        }

        /// <summary>
        /// 获取客户端标识符
        /// </summary>
        private string GetClientIdentifier(HttpContext context)
        {
            // 优先使用认证用户ID
            var userId = context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            // 否则使用IP地址
            return $"ip:{GetClientIP(context)}";
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        private string GetClientIP(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }

            var realIP = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIP))
            {
                return realIP;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 获取限流配置
        /// </summary>
        private RateLimitConfig? GetRateLimitConfig(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // 登录接口特殊限制
            if (path.Contains("/auth/login"))
            {
                return _options.LoginEndpointLimit;
            }

            // API接口限制
            if (path.StartsWith("/api/"))
            {
                return _options.ApiEndpointLimit;
            }

            // 默认限制
            return _options.DefaultLimit;
        }

        /// <summary>
        /// 检查限流（令牌桶算法）
        /// </summary>
        private async Task<bool> CheckRateLimitAsync(string clientId, RateLimitConfig config)
        {
            var bucket = _tokenBuckets.GetOrAdd(clientId, _ => new TokenBucket(config));
            
            return await Task.Run(() => bucket.TryConsume(1));
        }

        /// <summary>
        /// 处理限流超出情况
        /// </summary>
        private async Task HandleRateLimitExceeded(HttpContext context, string clientId)
        {
            var clientIP = GetClientIP(context);
            var path = context.Request.Path;
            
            _logger.LogWarning("限流触发: 客户端 {ClientId} (IP: {ClientIP}) 访问 {Path} 超出限制", 
                clientId, clientIP, path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                error = "请求过于频繁",
                message = "您的请求频率超出限制，请稍后再试",
                code = "RATE_LIMIT_EXCEEDED",
                retryAfter = _options.DefaultLimit.WindowSeconds
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        /// <summary>
        /// 添加限流响应头
        /// </summary>
        private void AddRateLimitHeaders(HttpContext context, string clientId, RateLimitConfig config)
        {
            var bucket = _tokenBuckets.GetOrAdd(clientId, _ => new TokenBucket(config));
            
            context.Response.Headers.Add("X-RateLimit-Limit", config.RequestsPerWindow.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", bucket.AvailableTokens.ToString());
            context.Response.Headers.Add("X-RateLimit-Window", config.WindowSeconds.ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", 
                DateTimeOffset.UtcNow.AddSeconds(config.WindowSeconds).ToUnixTimeSeconds().ToString());
        }
    }

    /// <summary>
    /// 令牌桶实现
    /// </summary>
    public class TokenBucket
    {
        private readonly object _lock = new object();
        private readonly int _capacity;
        private readonly int _refillRate; // 每秒补充的令牌数
        private readonly TimeSpan _windowSize;
        private int _availableTokens;
        private DateTime _lastRefill;

        public int AvailableTokens => _availableTokens;

        public TokenBucket(RateLimitConfig config)
        {
            _capacity = config.RequestsPerWindow;
            _windowSize = TimeSpan.FromSeconds(config.WindowSeconds);
            _refillRate = (int)Math.Ceiling((double)config.RequestsPerWindow / config.WindowSeconds);
            _availableTokens = _capacity;
            _lastRefill = DateTime.UtcNow;
        }

        /// <summary>
        /// 尝试消费令牌
        /// </summary>
        public bool TryConsume(int tokens)
        {
            lock (_lock)
            {
                RefillTokens();
                
                if (_availableTokens >= tokens)
                {
                    _availableTokens -= tokens;
                    return true;
                }
                
                return false;
            }
        }

        /// <summary>
        /// 补充令牌
        /// </summary>
        private void RefillTokens()
        {
            var now = DateTime.UtcNow;
            var timePassed = now - _lastRefill;
            
            if (timePassed.TotalSeconds >= 1)
            {
                var tokensToAdd = (int)(timePassed.TotalSeconds * _refillRate);
                _availableTokens = Math.Min(_capacity, _availableTokens + tokensToAdd);
                _lastRefill = now;
            }
        }
    }

    /// <summary>
    /// 限流配置
    /// </summary>
    public class RateLimitConfig
    {
        public int RequestsPerWindow { get; set; }
        public int WindowSeconds { get; set; }
    }

    /// <summary>
    /// 限流选项配置
    /// </summary>
    public class RateLimitOptions
    {
        public RateLimitConfig DefaultLimit { get; set; } = new() 
        { 
            RequestsPerWindow = 100, 
            WindowSeconds = 60 
        };

        public RateLimitConfig ApiEndpointLimit { get; set; } = new() 
        { 
            RequestsPerWindow = 300, 
            WindowSeconds = 60 
        };

        public RateLimitConfig LoginEndpointLimit { get; set; } = new() 
        { 
            RequestsPerWindow = 5, 
            WindowSeconds = 60 
        };

        public List<string> WhitelistedIPs { get; set; } = new() 
        { 
            "127.0.0.1", 
            "::1" 
        };
    }
}