using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services;

namespace LYBT.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// SearchService 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class SearchServiceTests : IDisposable
{
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        _service = new SearchService();
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Assert
        _service.SearchText.Should().BeEmpty();
        _service.IsSearching.Should().BeFalse();
        _service.DebounceDelay.Should().Be(300);
    }

    #endregion

    #region ExecuteSearchImmediateAsync Tests

    [Fact]
    public async Task ExecuteSearchImmediateAsync_ShouldSetIsSearchingDuringExecution()
    {
        // Arrange
        var wasSearchingDuringExecution = false;
        _service.SearchText = "test";

        // Act
        await _service.ExecuteSearchImmediateAsync(async text =>
        {
            wasSearchingDuringExecution = _service.IsSearching;
            await Task.Delay(10);
        });

        // Assert
        wasSearchingDuringExecution.Should().BeTrue();
        _service.IsSearching.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteSearchImmediateAsync_ShouldPassSearchText()
    {
        // Arrange
        string? receivedText = null;
        _service.SearchText = "hello world";

        // Act
        await _service.ExecuteSearchImmediateAsync(async text =>
        {
            receivedText = text;
            await Task.CompletedTask;
        });

        // Assert
        receivedText.Should().Be("hello world");
    }

    [Fact]
    public async Task ExecuteSearchImmediateAsync_ShouldRaiseSearchRequestedEvent()
    {
        // Arrange
        _service.SearchText = "test query";
        SearchRequestedEventArgs? eventArgs = null;
        _service.SearchRequested += (_, e) => eventArgs = e;

        // Act
        await _service.ExecuteSearchImmediateAsync(async _ => await Task.CompletedTask);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.SearchText.Should().Be("test query");
    }

    [Fact]
    public async Task ExecuteSearchImmediateAsync_WithException_ShouldStillResetIsSearching()
    {
        // Arrange
        _service.SearchText = "test";

        // Act
        var act = async () =>
        {
            await _service.ExecuteSearchImmediateAsync(async _ =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test exception");
            });
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _service.IsSearching.Should().BeFalse();
    }

    #endregion

    #region ExecuteSearchAsync Tests

    [Fact]
    public async Task ExecuteSearchAsync_ShouldDebounce()
    {
        // Arrange
        _service.DebounceDelay = 50;
        _service.SearchText = "test";
        var searchCount = 0;

        // Act - 快速调用多次
        var task1 = _service.ExecuteSearchAsync(async _ =>
        {
            Interlocked.Increment(ref searchCount);
            await Task.CompletedTask;
        });

        var task2 = _service.ExecuteSearchAsync(async _ =>
        {
            Interlocked.Increment(ref searchCount);
            await Task.CompletedTask;
        });

        await Task.WhenAll(task1, task2);
        await Task.Delay(100); // 等待最后一个执行完成

        // Assert - 只有最后一个应该执行
        searchCount.Should().Be(1);
    }

    #endregion

    #region ClearSearch Tests

    [Fact]
    public void ClearSearch_ShouldClearSearchText()
    {
        // Arrange
        _service.SearchText = "some text";

        // Act
        _service.ClearSearch();

        // Assert
        _service.SearchText.Should().BeEmpty();
    }

    [Fact]
    public void ClearSearch_ShouldResetIsSearching()
    {
        // Arrange - 模拟搜索中状态
        _service.SearchText = "test";

        // Act
        _service.ClearSearch();

        // Assert
        _service.IsSearching.Should().BeFalse();
    }

    #endregion

    #region CancelSearch Tests

    [Fact]
    public void CancelSearch_ShouldSetIsSearchingToFalse()
    {
        // Arrange
        _service.SearchText = "test";

        // Act
        _service.CancelSearch();

        // Assert
        _service.IsSearching.Should().BeFalse();
    }

    #endregion

    #region SearchText Property Tests

    [Fact]
    public void SearchText_WhenSet_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChangedCount = 0;
        _service.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_service.SearchText))
                propertyChangedCount++;
        };

        // Act
        _service.SearchText = "new value";

        // Assert
        propertyChangedCount.Should().Be(1);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var service = new SearchService();

        // Act
        var act = () => service.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = new SearchService();

        // Act
        var act = () =>
        {
            service.Dispose();
            service.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
