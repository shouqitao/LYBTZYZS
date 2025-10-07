using System.Collections.Concurrent;
using LYBT.Infrastructure.Caching.Adapters;
using LYBT.Infrastructure.Caching.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Caching.Adapters
{
    public class MemoryCacheAdapterTests : IDisposable
    {
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<ILogger<MemoryCacheAdapter>> _mockLogger;
        private readonly MemoryCacheAdapter _adapter;
        private readonly ConcurrentDictionary<object, object> _cacheStore;

        public MemoryCacheAdapterTests()
        {
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<MemoryCacheAdapter>>();
            _cacheStore = new ConcurrentDictionary<object, object>();

            // Setup memory cache behavior
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                .Returns((object key, out object value) =>
                {
                    return _cacheStore.TryGetValue(key, out value);
                });

            _mockMemoryCache.Setup(x => x.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<MemoryCacheEntryOptions>()))
                .Callback<object, object, MemoryCacheEntryOptions>((key, val, options) =>
                {
                    _cacheStore[key] = val;
                });

            _mockMemoryCache.Setup(x => x.Remove(It.IsAny<object>()))
                .Callback<object>(key => _cacheStore.TryRemove(key, out _));

            _adapter = new MemoryCacheAdapter(_mockMemoryCache.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_MemoryCacheIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MemoryCacheAdapter(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MemoryCacheAdapter(_mockMemoryCache.Object, null));
        }

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _adapter.Should().NotBeNull();
            _adapter.Should().BeAssignableTo<ICacheService>();
        }

        #endregion

        #region Sync Get Tests

        [Fact]
        public void Get_Should_ReturnValue_When_KeyExists()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            _cacheStore[key] = value;

            // Act
            var result = _adapter.Get<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void Get_Should_ReturnDefault_When_KeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var result = _adapter.Get<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Get_Should_ThrowArgumentException_When_KeyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _adapter.Get<string>(null));
        }

        [Fact]
        public void Get_Should_ThrowArgumentException_When_KeyIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _adapter.Get<string>(""));
        }

        [Fact]
        public void Get_Should_ReturnDefault_When_ValueIsWrongType()
        {
            // Arrange
            var key = "test-key";
            var value = 123; // int value
            _cacheStore[key] = value;

            // Act
            var result = _adapter.Get<string>(key); // requesting as string

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Get_Should_ReturnDefault_When_ExceptionOccurs()
        {
            // Arrange
            var key = "test-key";
            _mockMemoryCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            var result = _adapter.Get<string>(key);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Sync Set Tests

        [Fact]
        public void Set_Should_StoreValue_When_ValidParametersProvided()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Act
            _adapter.Set(key, value);

            // Assert
            _mockMemoryCache.Verify(x => x.Set(key, value, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public void Set_Should_ThrowArgumentException_When_KeyIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _adapter.Set(null, "value"));
        }

        [Fact]
        public void Set_Should_ThrowArgumentException_When_KeyIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _adapter.Set("", "value"));
        }

        [Fact]
        public void Set_Should_UseDefaultExpiration_When_ExpirationIsNull()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Act
            _adapter.Set(key, value, null);

            // Assert
            _mockMemoryCache.Verify(x => x.Set(key, value, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public void Set_Should_UseProvidedExpiration_When_ExpirationIsProvided()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            _adapter.Set(key, value, expiration);

            // Assert
            _mockMemoryCache.Verify(x => x.Set(key, value, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public void Set_Should_NotThrow_When_ExceptionOccurs()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            _mockMemoryCache.Setup(x => x.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<MemoryCacheEntryOptions>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act & Assert
            _adapter.Invoking(a => a.Set(key, value)).Should().NotThrow();
        }

        #endregion

        #region Sync Remove Tests

        [Fact]
        public void Remove_Should_ReturnTrue_When_KeyExists()
        {
            // Arrange
            var key = "test-key";
            _cacheStore[key] = "test-value";

            // Act
            var result = _adapter.Remove(key);

            // Assert
            result.Should().BeTrue();
            _mockMemoryCache.Verify(x => x.Remove(key), Times.Once);
        }

        [Fact]
        public void Remove_Should_ReturnFalse_When_KeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var result = _adapter.Remove(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Remove_Should_ReturnFalse_When_KeyIsNull()
        {
            // Act
            var result = _adapter.Remove(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Remove_Should_ReturnFalse_When_KeyIsEmpty()
        {
            // Act
            var result = _adapter.Remove("");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Remove_Should_ReturnFalse_When_ExceptionOccurs()
        {
            // Arrange
            var key = "test-key";
            _mockMemoryCache.Setup(x => x.Remove(It.IsAny<object>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            var result = _adapter.Remove(key);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Sync Clear Tests

        [Fact]
        public void Clear_Should_RemoveAllKeys_When_Called()
        {
            // Arrange
            _cacheStore["key1"] = "value1";
            _cacheStore["key2"] = "value2";

            // Act
            _adapter.Clear();

            // Assert
            _mockMemoryCache.Verify(x => x.Remove(It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Clear_Should_NotThrow_When_ExceptionOccurs()
        {
            // Arrange
            _cacheStore["key1"] = "value1";
            _mockMemoryCache.Setup(x => x.Remove(It.IsAny<object>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act & Assert
            _adapter.Invoking(a => a.Clear()).Should().NotThrow();
        }

        #endregion

        #region Sync Exists Tests

        [Fact]
        public void Exists_Should_ReturnTrue_When_KeyExists()
        {
            // Arrange
            var key = "test-key";
            _cacheStore[key] = "test-value";

            // Act
            var result = _adapter.Exists(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Exists_Should_ReturnFalse_When_KeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var result = _adapter.Exists(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Exists_Should_ReturnFalse_When_KeyIsNull()
        {
            // Act
            var result = _adapter.Exists(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Exists_Should_ReturnFalse_When_KeyIsEmpty()
        {
            // Act
            var result = _adapter.Exists("");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Async Get Tests

        [Fact]
        public async Task GetAsync_Should_ReturnValue_When_KeyExists()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            _cacheStore[key] = value;

            // Act
            var result = await _adapter.GetAsync<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task GetAsync_Should_ReturnDefault_When_KeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var result = await _adapter.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_Should_SupportCancellation_When_CancellationTokenProvided()
        {
            // Arrange
            var key = "test-key";
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _adapter.GetAsync<string>(key, cancellationToken);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Async Set Tests

        [Fact]
        public async Task SetAsync_Should_StoreValue_When_ValidParametersProvided()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Act
            await _adapter.SetAsync(key, value);

            // Assert
            _mockMemoryCache.Verify(x => x.Set(key, value, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public async Task SetAsync_Should_SupportCancellation_When_CancellationTokenProvided()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var cancellationToken = new CancellationToken();

            // Act
            await _adapter.SetAsync(key, value, cancellationToken: cancellationToken);

            // Assert
            _mockMemoryCache.Verify(x => x.Set(key, value, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        #endregion

        #region Async Remove Tests

        [Fact]
        public async Task RemoveAsync_Should_ReturnTrue_When_KeyExists()
        {
            // Arrange
            var key = "test-key";
            _cacheStore[key] = "test-value";

            // Act
            var result = await _adapter.RemoveAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveAsync_Should_ReturnFalse_When_KeyDoesNotExist()
        {
            // Arrange
            var key = "non-existent-key";

            // Act
            var result = await _adapter.RemoveAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetOrSet Tests

        [Fact]
        public async Task GetOrSetAsync_Should_ReturnCachedValue_When_KeyExists()
        {
            // Arrange
            var key = "test-key";
            var cachedValue = "cached-value";
            _cacheStore[key] = cachedValue;
            var factoryCalled = false;

            // Act
            var result = await _adapter.GetOrSetAsync(key, () =>
            {
                factoryCalled = true;
                return Task.FromResult("factory-value");
            });

            // Assert
            result.Should().Be(cachedValue);
            factoryCalled.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_Should_CallFactoryAndCache_When_KeyDoesNotExist()
        {
            // Arrange
            var key = "test-key";
            var factoryValue = "factory-value";
            var factoryCalled = false;

            // Act
            var result = await _adapter.GetOrSetAsync(key, () =>
            {
                factoryCalled = true;
                return Task.FromResult(factoryValue);
            });

            // Assert
            result.Should().Be(factoryValue);
            factoryCalled.Should().BeTrue();
            _mockMemoryCache.Verify(x => x.Set(key, factoryValue, It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetOrSetAsync_Should_ThrowArgumentException_When_KeyIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _adapter.GetOrSetAsync<string>(null, () => Task.FromResult("value")));
        }

        [Fact]
        public async Task GetOrSetAsync_Should_ThrowArgumentException_When_KeyIsEmpty()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _adapter.GetOrSetAsync("", () => Task.FromResult("value")));
        }

        [Fact]
        public async Task GetOrSetAsync_Should_ThrowArgumentNullException_When_FactoryIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _adapter.GetOrSetAsync<string>("key", null));
        }

        #endregion

        #region Batch Operations Tests

        [Fact]
        public async Task GetManyAsync_Should_ReturnExistingValues_When_KeysProvided()
        {
            // Arrange
            var keys = new[] { "key1", "key2", "key3" };
            _cacheStore["key1"] = "value1";
            _cacheStore["key3"] = "value3";

            // Act
            var result = await _adapter.GetManyAsync<string>(keys);

            // Assert
            result.Should().HaveCount(2);
            result["key1"].Should().Be("value1");
            result["key3"].Should().Be("value3");
            result.Should().NotContainKey("key2");
        }

        [Fact]
        public async Task GetManyAsync_Should_SupportCancellation_When_CancellationTokenProvided()
        {
            // Arrange
            var keys = new[] { "key1", "key2" };
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act
            var result = await _adapter.GetManyAsync<string>(keys, cancellationTokenSource.Token);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SetManyAsync_Should_StoreAllValues_When_ItemsProvided()
        {
            // Arrange
            var items = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            // Act
            await _adapter.SetManyAsync(items);

            // Assert
            _mockMemoryCache.Verify(x => x.Set("key1", "value1", It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
            _mockMemoryCache.Verify(x => x.Set("key2", "value2", It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public async Task RemoveManyAsync_Should_RemoveExistingKeys_When_KeysProvided()
        {
            // Arrange
            var keys = new[] { "key1", "key2", "key3" };
            _cacheStore["key1"] = "value1";
            _cacheStore["key2"] = "value2";

            // Act
            var result = await _adapter.RemoveManyAsync(keys);

            // Assert
            result.Should().Be(2);
            _mockMemoryCache.Verify(x => x.Remove("key1"), Times.Once);
            _mockMemoryCache.Verify(x => x.Remove("key2"), Times.Once);
            _mockMemoryCache.Verify(x => x.Remove("key3"), Times.Once);
        }

        #endregion

        #region Pattern Operations Tests

        [Fact]
        public async Task RemoveByPatternAsync_Should_RemoveMatchingKeys_When_PatternProvided()
        {
            // Arrange
            var pattern = "user:*";
            _cacheStore["user:123"] = "value1";
            _cacheStore["user:456"] = "value2";
            _cacheStore["product:789"] = "value3";

            // Act
            var result = await _adapter.RemoveByPatternAsync(pattern);

            // Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task RemoveByPatternAsync_Should_ReturnZero_When_EmptyPatternProvided()
        {
            // Act
            var result = await _adapter.RemoveByPatternAsync("");

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task RemoveByPatternAsync_Should_ReturnZero_When_NullPatternProvided()
        {
            // Act
            var result = await _adapter.RemoveByPatternAsync(null);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task RemoveByPrefixAsync_Should_RemoveMatchingKeys_When_PrefixProvided()
        {
            // Arrange
            var prefix = "user:";
            _cacheStore["user:123"] = "value1";
            _cacheStore["user:456"] = "value2";
            _cacheStore["product:789"] = "value3";

            // Act
            var result = await _adapter.RemoveByPrefixAsync(prefix);

            // Assert
            result.Should().Be(2);
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public async Task GetStatisticsAsync_Should_ReturnStatistics_When_Called()
        {
            // Arrange
            _cacheStore["key1"] = "value1";
            _cacheStore["key2"] = "value2";

            // Act
            var result = await _adapter.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalKeys.Should().BeGreaterOrEqualTo(0);
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetStatisticsAsync_Should_SupportCancellation_When_CancellationTokenProvided()
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _adapter.GetStatisticsAsync(cancellationToken);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetStatisticsAsync_Should_TrackHitAndMissCount_When_AccessOperationsPerformed()
        {
            // Arrange
            var key = "test-key";
            _cacheStore[key] = "test-value";

            // Act
            _adapter.Get<string>(key); // Hit
            _adapter.Get<string>("non-existent"); // Miss

            var statistics = await _adapter.GetStatisticsAsync();

            // Assert
            statistics.HitCount.Should().BeGreaterThan(0);
            statistics.MissCount.Should().BeGreaterThan(0);
        }

        #endregion

        public void Dispose()
        {
            // No resources to dispose in this test
        }
    }
}