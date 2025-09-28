using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LYBT.Infrastructure.Caching
{
    /// <summary>
    /// 缓存键构建器 - 提供一致、安全的缓存键生成策略
    /// 防止缓存键冲突，支持分层命名空间和参数化键
    /// </summary>
    public class CacheKeyBuilder
    {
        private readonly string _prefix;
        private readonly List<string> _segments;
        private readonly Dictionary<string, object> _parameters;
        private TimeSpan? _expiration;
        private bool _useHash;

        /// <summary>
        /// 创建缓存键构建器实例
        /// </summary>
        /// <param name="prefix">键前缀，通常是模块名</param>
        public CacheKeyBuilder(string prefix = "LYBT")
        {
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            _segments = new List<string>();
            _parameters = new Dictionary<string, object>();
            _useHash = false;
        }

        /// <summary>
        /// 添加键段
        /// </summary>
        public CacheKeyBuilder AddSegment(string segment)
        {
            if (!string.IsNullOrWhiteSpace(segment))
            {
                _segments.Add(segment);
            }
            return this;
        }

        /// <summary>
        /// 添加多个键段
        /// </summary>
        public CacheKeyBuilder AddSegments(params string[] segments)
        {
            foreach (var segment in segments.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                _segments.Add(segment);
            }
            return this;
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        public CacheKeyBuilder AddParameter(string name, object value)
        {
            if (!string.IsNullOrWhiteSpace(name) && value != null)
            {
                _parameters[name] = value;
            }
            return this;
        }

        /// <summary>
        /// 添加多个参数
        /// </summary>
        public CacheKeyBuilder AddParameters(object parameters)
        {
            if (parameters == null) return this;

            var properties = parameters.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(parameters);
                if (value != null)
                {
                    _parameters[prop.Name] = value;
                }
            }
            return this;
        }

        /// <summary>
        /// 设置过期时间
        /// </summary>
        public CacheKeyBuilder WithExpiration(TimeSpan expiration)
        {
            _expiration = expiration;
            return this;
        }

        /// <summary>
        /// 启用哈希键（用于超长键）
        /// </summary>
        public CacheKeyBuilder UseHash()
        {
            _useHash = true;
            return this;
        }

        /// <summary>
        /// 构建最终的缓存键
        /// </summary>
        public string Build()
        {
            var keyBuilder = new StringBuilder();

            // 添加前缀
            keyBuilder.Append(_prefix);

            // 添加段
            foreach (var segment in _segments)
            {
                keyBuilder.Append(':').Append(segment);
            }

            // 添加参数
            if (_parameters.Count > 0)
            {
                var sortedParams = _parameters.OrderBy(p => p.Key);
                foreach (var param in sortedParams)
                {
                    keyBuilder.Append(':').Append(param.Key).Append('=').Append(SerializeValue(param.Value));
                }
            }

            var key = keyBuilder.ToString();

            // 如果启用哈希或键太长，则使用哈希
            if (_useHash || key.Length > 250)
            {
                key = $"{_prefix}:hash:{ComputeHash(key)}";
            }

            return key;
        }

        /// <summary>
        /// 获取配置的过期时间
        /// </summary>
        public TimeSpan? GetExpiration()
        {
            return _expiration;
        }

        /// <summary>
        /// 序列化参数值
        /// </summary>
        private string SerializeValue(object value)
        {
            if (value == null) return "null";

            return value switch
            {
                string s => s,
                Guid g => g.ToString("N"),
                DateTime dt => dt.ToString("yyyyMMddHHmmss"),
                DateOnly d => d.ToString("yyyyMMdd"),
                bool b => b ? "1" : "0",
                _ => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false })
            };
        }

        /// <summary>
        /// 计算字符串哈希
        /// </summary>
        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        /// <summary>
        /// 创建用户相关的缓存键
        /// </summary>
        public static CacheKeyBuilder ForUser(Guid userId, string? operation = null)
        {
            var builder = new CacheKeyBuilder("LYBT")
                .AddSegments("User", userId.ToString("N"));

            if (!string.IsNullOrWhiteSpace(operation))
            {
                builder.AddSegment(operation);
            }

            return builder;
        }

        /// <summary>
        /// 创建患者相关的缓存键
        /// </summary>
        public static CacheKeyBuilder ForPatient(Guid patientId, string? operation = null)
        {
            var builder = new CacheKeyBuilder("LYBT")
                .AddSegments("Patient", patientId.ToString("N"));

            if (!string.IsNullOrWhiteSpace(operation))
            {
                builder.AddSegment(operation);
            }

            return builder;
        }

        /// <summary>
        /// 创建处方相关的缓存键
        /// </summary>
        public static CacheKeyBuilder ForPrescription(Guid prescriptionId, string? operation = null)
        {
            var builder = new CacheKeyBuilder("LYBT")
                .AddSegments("Prescription", prescriptionId.ToString("N"));

            if (!string.IsNullOrWhiteSpace(operation))
            {
                builder.AddSegment(operation);
            }

            return builder;
        }

        /// <summary>
        /// 创建查询相关的缓存键
        /// </summary>
        public static CacheKeyBuilder ForQuery(string entityType, object? queryParams = null)
        {
            var builder = new CacheKeyBuilder("LYBT")
                .AddSegments("Query", entityType);

            if (queryParams != null)
            {
                builder.AddParameters(queryParams);
            }

            return builder;
        }

        /// <summary>
        /// 创建列表相关的缓存键
        /// </summary>
        public static CacheKeyBuilder ForList(string entityType, int page = 1, int pageSize = 20, string? sortBy = null)
        {
            var builder = new CacheKeyBuilder("LYBT")
                .AddSegments("List", entityType)
                .AddParameter("page", page)
                .AddParameter("size", pageSize);

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                builder.AddParameter("sort", sortBy);
            }

            return builder;
        }

        /// <summary>
        /// 创建统计相关的缓存键
        /// </summary>
        public static CacheKeyBuilder ForStatistics(string statType, DateTime? date = null)
        {
            var builder = new CacheKeyBuilder("LYBT")
                .AddSegments("Stats", statType);

            if (date.HasValue)
            {
                builder.AddParameter("date", date.Value.ToString("yyyyMMdd"));
            }

            return builder;
        }

        /// <summary>
        /// 重置构建器状态
        /// </summary>
        public CacheKeyBuilder Reset()
        {
            _segments.Clear();
            _parameters.Clear();
            _expiration = null;
            _useHash = false;
            return this;
        }
    }
}