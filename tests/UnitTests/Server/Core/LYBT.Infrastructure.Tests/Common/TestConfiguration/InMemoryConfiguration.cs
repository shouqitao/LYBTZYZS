using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace LYBT.Infrastructure.Tests.Common.TestConfiguration
{
    /// <summary>
    /// 内存配置实现，用于单元测试
    /// 解决ConfigurationBinder.GetValue扩展方法无法mock的问题
    /// 这是JWT测试修复成功验证的核心模式
    /// </summary>
    public class InMemoryConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string> _data;
        private readonly object _lock = new object();

        public InMemoryConfiguration(Dictionary<string, string>? data = null)
        {
            _data = data ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取或设置配置值
        /// 支持大小写不敏感的键名
        /// </summary>
        public string? this[string key]
        {
            get
            {
                lock (_lock)
                {
                    return _data.TryGetValue(key, out var value) ? value : null;
                }
            }
            set
            {
                lock (_lock)
                {
                    if (value == null)
                    {
                        _data.Remove(key);
                    }
                    else
                    {
                        _data[key] = value;
                    }
                }
            }
        }

        /// <summary>
        /// 获取配置节
        /// </summary>
        public IConfigurationSection GetSection(string key)
        {
            return new InMemoryConfigurationSection(key, this[key], this);
        }

        /// <summary>
        /// 获取子配置节
        /// </summary>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            lock (_lock)
            {
                var result = new List<IConfigurationSection>();
                var visitedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in _data)
                {
                    var keyParts = kvp.Key.Split(':');
                    if (keyParts.Length > 1 && !visitedKeys.Contains(keyParts[0]))
                    {
                        visitedKeys.Add(keyParts[0]);
                        var sectionKey = keyParts[0];
                        result.Add(new InMemoryConfigurationSection(sectionKey, this[sectionKey], this));
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// 获取重载令牌
        /// </summary>
        public IChangeToken GetReloadToken()
        {
            return new ConfigurationReloadToken();
        }

        /// <summary>
        /// 批量设置配置值
        /// </summary>
        public void SetValues(Dictionary<string, string> values)
        {
            lock (_lock)
            {
                foreach (var kvp in values)
                {
                    _data[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// 清除所有配置
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _data.Clear();
            }
        }

        /// <summary>
        /// 获取所有配置键
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            lock (_lock)
            {
                return _data.Keys.ToList();
            }
        }
    }

    /// <summary>
    /// 内存配置节实现
    /// </summary>
    public class InMemoryConfigurationSection : IConfigurationSection
    {
        private readonly IConfiguration _root;
        private readonly string _key;
        private string? _value;

        public string Key => _key;
        public string Path => _key;
        public string? Value
        {
            get => _value;
            set => _value = value;
        }

        public InMemoryConfigurationSection(string key, string? value, IConfiguration root)
        {
            _key = key;
            _value = value;
            _root = root;
        }

        public IConfigurationSection GetSection(string key)
        {
            var fullKey = string.IsNullOrEmpty(Path) ? key : $"{Path}:{key}";
            return _root.GetSection(fullKey);
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            var prefix = string.IsNullOrEmpty(Path) ? "" : $"{Path}:";
            return _root.GetChildren()
                .Where(section => section.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(section => new InMemoryConfigurationSection(
                    section.Key.Substring(prefix.Length),
                    section.Value,
                    _root));
        }

        public IChangeToken GetReloadToken()
        {
            return _root.GetReloadToken();
        }

        public string? this[string key]
        {
            get => GetSection(key).Value;
            set { /* InMemoryConfiguration 不支持嵌套设置 */ }
        }
    }

    /// <summary>
    /// 简单的变更令牌实现
    /// </summary>
    public class ConfigurationReloadToken : IChangeToken
    {
        private bool _hasChanged;

        public bool ActiveChangeCallbacks => false;
        public bool HasChanged => _hasChanged;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return new EmptyDisposable();
        }

        /// <summary>
        /// 触发变更通知
        /// </summary>
        public void OnReload()
        {
            _hasChanged = true;
        }

        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}