using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services;

namespace LYBT.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// LoadingStateManager 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class LoadingStateManagerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var manager = new LoadingStateManager();

        // Assert
        manager.IsLoading.Should().BeFalse();
        manager.IsBusy.Should().BeFalse();
        manager.BusyMessage.Should().BeEmpty();
        manager.LoadingCount.Should().Be(0);
    }

    #endregion

    #region BeginLoading/EndLoading Tests

    [Fact]
    public void BeginLoading_ShouldSetIsLoadingToTrue()
    {
        // Arrange
        var manager = new LoadingStateManager();

        // Act
        manager.BeginLoading();

        // Assert
        manager.IsLoading.Should().BeTrue();
        manager.LoadingCount.Should().Be(1);
    }

    [Fact]
    public void BeginLoading_WithMessage_ShouldSetBusyMessage()
    {
        // Arrange
        var manager = new LoadingStateManager();

        // Act
        manager.BeginLoading("Loading data...");

        // Assert
        manager.BusyMessage.Should().Be("Loading data...");
    }

    [Fact]
    public void EndLoading_AfterSingleBegin_ShouldSetIsLoadingToFalse()
    {
        // Arrange
        var manager = new LoadingStateManager();
        manager.BeginLoading();

        // Act
        manager.EndLoading();

        // Assert
        manager.IsLoading.Should().BeFalse();
        manager.LoadingCount.Should().Be(0);
    }

    [Fact]
    public void EndLoading_ShouldClearBusyMessage()
    {
        // Arrange
        var manager = new LoadingStateManager();
        manager.BeginLoading("Loading...");

        // Act
        manager.EndLoading();

        // Assert
        manager.BusyMessage.Should().BeEmpty();
    }

    [Fact]
    public void NestedLoading_ShouldTrackCorrectly()
    {
        // Arrange
        var manager = new LoadingStateManager();

        // Act
        manager.BeginLoading();
        manager.BeginLoading();
        manager.LoadingCount.Should().Be(2);
        manager.IsLoading.Should().BeTrue();

        manager.EndLoading();
        manager.LoadingCount.Should().Be(1);
        manager.IsLoading.Should().BeTrue();

        manager.EndLoading();

        // Assert
        manager.LoadingCount.Should().Be(0);
        manager.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void EndLoading_WhenNotLoading_ShouldNotGoNegative()
    {
        // Arrange
        var manager = new LoadingStateManager();

        // Act
        manager.EndLoading();

        // Assert
        manager.LoadingCount.Should().Be(0);
    }

    #endregion

    #region ExecuteWithLoadingAsync Tests

    [Fact]
    public async Task ExecuteWithLoadingAsync_ShouldSetLoadingDuringExecution()
    {
        // Arrange
        var manager = new LoadingStateManager();
        var wasLoadingDuringExecution = false;

        // Act
        await manager.ExecuteWithLoadingAsync(async () =>
        {
            wasLoadingDuringExecution = manager.IsLoading;
            await Task.Delay(10);
        });

        // Assert
        wasLoadingDuringExecution.Should().BeTrue();
        manager.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWithLoadingAsync_WithBusyFlag_ShouldSetIsBusy()
    {
        // Arrange
        var manager = new LoadingStateManager();
        var wasBusyDuringExecution = false;

        // Act
        await manager.ExecuteWithLoadingAsync(async () =>
        {
            wasBusyDuringExecution = manager.IsBusy;
            await Task.Delay(10);
        }, isBusy: true);

        // Assert
        wasBusyDuringExecution.Should().BeTrue();
        manager.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWithLoadingAsync_WithException_ShouldStillEndLoading()
    {
        // Arrange
        var manager = new LoadingStateManager();

        // Act
        var act = async () =>
        {
            await manager.ExecuteWithLoadingAsync(async () =>
            {
                await Task.Delay(10);
                throw new InvalidOperationException("Test exception");
            });
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        manager.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteWithLoadingAsync_WithResult_ShouldReturnValue()
    {
        // Arrange
        var manager = new LoadingStateManager();

        // Act
        var result = await manager.ExecuteWithLoadingAsync(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        // Assert
        result.Should().Be(42);
        manager.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ShouldClearAllState()
    {
        // Arrange
        var manager = new LoadingStateManager();
        manager.BeginLoading("Loading...");
        manager.BeginLoading();

        // Act
        manager.Reset();

        // Assert
        manager.IsLoading.Should().BeFalse();
        manager.IsBusy.Should().BeFalse();
        manager.BusyMessage.Should().BeEmpty();
        manager.LoadingCount.Should().Be(0);
    }

    #endregion
}
