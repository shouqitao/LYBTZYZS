using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace LYBT.Module.Auth.Services {

    /// <summary>
    /// 登录尝试跟踪服务 - 用于防暴力破解
    /// </summary>
    public interface ILoginAttemptService {
        /// <summary>
        /// 记录失败的登录尝试
        /// </summary>
        void RecordFailedAttempt(string username);

        /// <summary>
        /// 检查账户是否被锁定
        /// </summary>
        bool IsAccountLocked(string username);

        /// <summary>
        /// 清除登录尝试记录（登录成功后调用）
        /// </summary>
        void ClearAttempts(string username);

        /// <summary>
        /// 获取剩余锁定时间（秒）
        /// </summary>
        int GetRemainingLockTime(string username);
    }

    public class LoginAttemptService : ILoginAttemptService {
        private readonly IMemoryCache _cache;
        private readonly int _maxAttempts = 3;
        private readonly int _lockoutDurationMinutes = 15;
        
        // 用于跟踪失败次数
        private readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();

        public LoginAttemptService(IMemoryCache cache) {
            _cache = cache;
        }

        public void RecordFailedAttempt(string username) {
            var key = GetNormalizedKey(username);
            var now = DateTime.UtcNow;

            _attempts.AddOrUpdate(key, 
                new LoginAttemptInfo { 
                    FailedAttempts = 1, 
                    FirstAttemptTime = now,
                    LastAttemptTime = now 
                },
                (k, existing) => {
                    existing.FailedAttempts++;
                    existing.LastAttemptTime = now;
                    
                    // 如果达到最大尝试次数，设置锁定时间
                    if (existing.FailedAttempts >= _maxAttempts) {
                        existing.LockedUntil = now.AddMinutes(_lockoutDurationMinutes);
                        
                        // 同时在缓存中设置锁定标记
                        var cacheKey = $"login_lockout_{key}";
                        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(_lockoutDurationMinutes));
                    }
                    
                    return existing;
                });
        }

        public bool IsAccountLocked(string username) {
            var key = GetNormalizedKey(username);
            
            // 先检查缓存
            var cacheKey = $"login_lockout_{key}";
            if (_cache.TryGetValue<bool>(cacheKey, out _)) {
                return true;
            }

            // 再检查内存字典
            if (_attempts.TryGetValue(key, out var attemptInfo)) {
                if (attemptInfo.LockedUntil.HasValue && attemptInfo.LockedUntil > DateTime.UtcNow) {
                    return true;
                }
                
                // 如果锁定已过期，清理记录
                if (attemptInfo.LockedUntil.HasValue && attemptInfo.LockedUntil <= DateTime.UtcNow) {
                    _attempts.TryRemove(key, out _);
                }
            }

            return false;
        }

        public void ClearAttempts(string username) {
            var key = GetNormalizedKey(username);
            _attempts.TryRemove(key, out _);
            
            // 同时清除缓存中的锁定标记
            var cacheKey = $"login_lockout_{key}";
            _cache.Remove(cacheKey);
        }

        public int GetRemainingLockTime(string username) {
            var key = GetNormalizedKey(username);
            
            if (_attempts.TryGetValue(key, out var attemptInfo)) {
                if (attemptInfo.LockedUntil.HasValue && attemptInfo.LockedUntil > DateTime.UtcNow) {
                    return (int)(attemptInfo.LockedUntil.Value - DateTime.UtcNow).TotalSeconds;
                }
            }

            return 0;
        }

        private string GetNormalizedKey(string username) {
            return username?.ToLowerInvariant() ?? string.Empty;
        }

        private class LoginAttemptInfo {
            public int FailedAttempts { get; set; }
            public DateTime FirstAttemptTime { get; set; }
            public DateTime LastAttemptTime { get; set; }
            public DateTime? LockedUntil { get; set; }
        }
    }
}