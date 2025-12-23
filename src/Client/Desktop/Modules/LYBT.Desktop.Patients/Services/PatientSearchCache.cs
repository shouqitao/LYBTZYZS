using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// 患者搜索缓存服务实现
    /// 使用LRU策略，缓存最近搜索结果
    /// </summary>
    public class PatientSearchCache : IPatientSearchCache
    {
        private readonly int _maxCacheSize = 10;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        private readonly LinkedList<CacheEntry> _cache = new();
        private readonly object _lock = new();
        private readonly ILogger<PatientSearchCache> _logger;

        public PatientSearchCache(ILogger<PatientSearchCache> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public PagedResult<PatientListDto>? Get(string keyword, int page)
        {
            var key = GenerateKey(keyword, page);

            lock (_lock)
            {
                var node = _cache.First;
                while (node != null)
                {
                    if (node.Value.Key == key)
                    {
                        // 检查是否过期
                        if (DateTime.UtcNow - node.Value.CreatedAt > _cacheExpiration)
                        {
                            _cache.Remove(node);
                            _logger.LogDebug("缓存已过期：{Key}", key);
                            return null;
                        }

                        // LRU: 移动到链表头部
                        _cache.Remove(node);
                        _cache.AddFirst(node);

                        _logger.LogDebug("缓存命中：{Key}", key);
                        return node.Value.Result;
                    }
                    node = node.Next;
                }
            }

            _logger.LogDebug("缓存未命中：{Key}", key);
            return null;
        }

        /// <inheritdoc/>
        public void Set(string keyword, int page, PagedResult<PatientListDto> result)
        {
            var key = GenerateKey(keyword, page);

            lock (_lock)
            {
                // 检查是否已存在，如果存在则更新
                var node = _cache.First;
                while (node != null)
                {
                    if (node.Value.Key == key)
                    {
                        _cache.Remove(node);
                        break;
                    }
                    node = node.Next;
                }

                // 添加到链表头部
                var entry = new CacheEntry(key, keyword, page, result, DateTime.UtcNow);
                _cache.AddFirst(entry);

                // 如果超过最大容量，移除最后一个（最少使用的）
                while (_cache.Count > _maxCacheSize)
                {
                    var lastNode = _cache.Last;
                    if (lastNode != null)
                    {
                        _logger.LogDebug("缓存已满，移除最少使用的条目：{Key}", lastNode.Value.Key);
                        _cache.RemoveLast();
                    }
                }

                _logger.LogDebug("已缓存搜索结果：{Key}，当前缓存数量：{Count}", key, _cache.Count);
            }
        }

        /// <inheritdoc/>
        public void Invalidate(string? keyword = null)
        {
            lock (_lock)
            {
                if (keyword == null)
                {
                    var count = _cache.Count;
                    _cache.Clear();
                    _logger.LogInformation("已清空所有缓存，共{Count}条", count);
                }
                else
                {
                    var nodesToRemove = new List<LinkedListNode<CacheEntry>>();
                    var node = _cache.First;
                    while (node != null)
                    {
                        if (node.Value.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            nodesToRemove.Add(node);
                        }
                        node = node.Next;
                    }

                    foreach (var n in nodesToRemove)
                    {
                        _cache.Remove(n);
                    }

                    if (nodesToRemove.Count > 0)
                    {
                        _logger.LogInformation("已清除关键字'{Keyword}'的缓存，共{Count}条", keyword, nodesToRemove.Count);
                    }
                }
            }
        }

        /// <summary>
        /// 生成缓存Key
        /// </summary>
        private static string GenerateKey(string keyword, int page)
        {
            return $"{keyword?.ToLowerInvariant() ?? string.Empty}:{page}";
        }

        /// <summary>
        /// 缓存条目
        /// </summary>
        private sealed record CacheEntry(
            string Key,
            string Keyword,
            int Page,
            PagedResult<PatientListDto> Result,
            DateTime CreatedAt);
    }
}
