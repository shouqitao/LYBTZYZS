using FluentAssertions;
using LYBT.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Server.Unit.Infrastructure;

/// <summary>
/// ServerCacheKeyRegistry 单元测试 — 去反射的前缀失效登记表。
/// 覆盖：登记/前缀移除/清空/计数、前缀隔离、未知前缀 no-op、逐出回调注销。
/// 真实 <see cref="MemoryCache"/>（无 mock 框架，Server 测试 AntiMock 约定）。
/// </summary>
public class ServerCacheKeyRegistryTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ServerCacheKeyRegistry _registry = new();

    public void Dispose() => _cache.Dispose();

    private void Track(string key)
    {
        var options = new MemoryCacheEntryOptions();
        _registry.Track(key, options);
        _cache.Set(key, key, options);
    }

    [Fact]
    public void Track_ThenRemoveByPrefix_RemovesMatchingEntriesAndUntracksThem()
    {
        Track("medicalcases:1");
        Track("medicalcases:2");
        Track("herbs:1");
        _registry.Count.Should().Be(3);

        var removed = _registry.RemoveByPrefix(_cache, "medicalcases");

        removed.Should().Be(2);
        _registry.Count.Should().Be(1);
        _cache.TryGetValue("medicalcases:1", out _).Should().BeFalse();
        _cache.TryGetValue("medicalcases:2", out _).Should().BeFalse();
        _cache.TryGetValue("herbs:1", out _).Should().BeTrue("非目标前缀的条目必须保留");
    }

    [Fact]
    public void RemoveByPrefix_UnknownPrefix_IsNoOp()
    {
        Track("herbs:1");

        var removed = _registry.RemoveByPrefix(_cache, "patients");

        removed.Should().Be(0);
        _registry.Count.Should().Be(1);
        _cache.TryGetValue("herbs:1", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RemoveByPrefix_EmptyOrNullPrefix_IsNoOp(string? prefix)
    {
        Track("herbs:1");

        var removed = _registry.RemoveByPrefix(_cache, prefix!);

        removed.Should().Be(0);
        _registry.Count.Should().Be(1);
        _cache.TryGetValue("herbs:1", out _).Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesAllTrackedEntries()
    {
        Track("medicalcases:1");
        Track("herbs:1");
        Track("patients:1");

        var removed = _registry.Clear(_cache);

        removed.Should().Be(3);
        _registry.Count.Should().Be(0);
        _cache.TryGetValue("medicalcases:1", out _).Should().BeFalse();
        _cache.TryGetValue("herbs:1", out _).Should().BeFalse();
        _cache.TryGetValue("patients:1", out _).Should().BeFalse();
    }

    [Fact]
    public async Task EvictedEntry_IsUntrackedAutomatically()
    {
        Track("medicalcases:1");
        Track("herbs:1");

        // 直接驱逐（模拟过期/容量压缩/替换）——逐出回调应注销登记
        _cache.Remove("medicalcases:1");

        // MemoryCache 的逐出回调在线程池上触发（非同步）——有界等待登记表同步
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_registry.Count > 1 && DateTime.UtcNow < deadline)
            await Task.Delay(10);

        _registry.Count.Should().Be(1);
        _registry.RemoveByPrefix(_cache, "medicalcases").Should().Be(0, "已驱逐的键不应再出现在登记表");
    }

    [Fact]
    public async Task CacheInvalidationService_InvalidateAsync_UsesRegistry()
    {
        Track("medicalcases:1");
        Track("medicalcases:2");
        Track("herbs:1");
        var service = new CacheInvalidationService(
            _cache, _registry, NullLogger<CacheInvalidationService>.Instance);

        await service.InvalidateAsync("medicalcases");

        _registry.Count.Should().Be(1);
        _cache.TryGetValue("medicalcases:1", out _).Should().BeFalse();
        _cache.TryGetValue("herbs:1", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CacheInvalidationService_InvalidateAsync_MultipleTags_ClearsAllMatching()
    {
        Track("medicalcases:1");
        Track("herbs:1");
        Track("patients:1");
        var service = new CacheInvalidationService(
            _cache, _registry, NullLogger<CacheInvalidationService>.Instance);

        await service.InvalidateAsync(new[] { "medicalcases", "herbs" });

        _registry.Count.Should().Be(1);
        _cache.TryGetValue("medicalcases:1", out _).Should().BeFalse();
        _cache.TryGetValue("herbs:1", out _).Should().BeFalse();
        _cache.TryGetValue("patients:1", out _).Should().BeTrue();
    }
}
