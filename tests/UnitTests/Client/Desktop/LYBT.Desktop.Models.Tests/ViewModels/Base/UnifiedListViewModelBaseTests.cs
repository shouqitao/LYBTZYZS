using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Desktop.Models.Tests.ViewModels.Base;

/// <summary>
/// UnifiedListViewModelBase批量删除功能测试
/// Issue #2155 - Task 2.2: ViewModel基类单元测试
/// </summary>
public class UnifiedListViewModelBaseTests
{
    #region Test Data Models

    /// <summary>
    /// 测试用数据模型
    /// </summary>
    public class TestDataItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 测试用具体ViewModel类（继承UnifiedListViewModelBase）
    /// </summary>
    public class TestListViewModel : UnifiedListViewModelBase<TestDataItem>
    {
        // Mock回调标志
        public int OnExecuteBatchDeleteAsyncCallCount { get; private set; }
        public List<TestDataItem>? LastDeletedItems { get; private set; }
        public int LoadDataAsyncCallCount { get; private set; }

        // 可配置的确认对话框返回值
        public bool ConfirmationResult { get; set; } = true;

        public TestListViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager)
            : base(eventAggregator, loggerFactory, regionManager)
        {
        }

        protected override async Task<IEnumerable<TestDataItem>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            LoadDataAsyncCallCount++;
            await Task.CompletedTask;

            // 返回模拟数据
            return new List<TestDataItem>
            {
                new() { Id = 1, Name = "Item 1" },
                new() { Id = 2, Name = "Item 2" },
                new() { Id = 3, Name = "Item 3" }
            };
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<TestDataItem> items)
        {
            OnExecuteBatchDeleteAsyncCallCount++;
            LastDeletedItems = items;
            await Task.CompletedTask;
        }

        protected override async Task<bool> ShowConfirmationAsync(string message, string title)
        {
            await Task.CompletedTask;
            return ConfirmationResult;
        }

        protected override async Task ShowSuccessMessageAsync(string message)
        {
            await Task.CompletedTask;
        }

        protected override async Task ShowWarningMessageAsync(string message)
        {
            await Task.CompletedTask;
        }
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// 创建测试用ViewModel实例
    /// </summary>
    private TestListViewModel CreateViewModel()
    {
        var eventAggregator = Substitute.For<IEventAggregator>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger<TestListViewModel>>();
        loggerFactory.CreateLogger<TestListViewModel>().Returns(logger);

        var regionManager = Substitute.For<IRegionManager>();

        return new TestListViewModel(eventAggregator, loggerFactory, regionManager);
    }

    #endregion

    #region BatchDeleteCommand CanExecute Tests

    /// <summary>
    /// 测试：SelectedItems为空时，BatchDeleteCommand.CanExecute返回false
    /// BR-005: 空选择处理
    /// </summary>
    [Fact]
    public void BatchDeleteCommand_WhenSelectedItemsIsEmpty_CanExecuteShouldReturnFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        var canExecute = viewModel.BatchDeleteCommand.CanExecute();

        // Assert
        canExecute.Should().BeFalse("因为SelectedItems为空时不允许批量删除");
        viewModel.SelectedItems.Should().BeEmpty("因为未添加任何选中项");
    }

    /// <summary>
    /// 测试：SelectedItems有数据时，BatchDeleteCommand.CanExecute返回true
    /// </summary>
    [Fact]
    public void BatchDeleteCommand_WhenSelectedItemsHasData_CanExecuteShouldReturnTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var testItem = new TestDataItem { Id = 1, Name = "Test" };

        // Act
        viewModel.SelectedItems.Add(testItem);
        var canExecute = viewModel.BatchDeleteCommand.CanExecute();

        // Assert
        canExecute.Should().BeTrue("因为SelectedItems有数据时允许批量删除");
        viewModel.SelectedItems.Should().HaveCount(1, "因为添加了1个选中项");
    }

    #endregion

    #region ExecuteBatchDeleteAsync Tests

    /// <summary>
    /// 测试：ExecuteBatchDeleteAsync调用OnExecuteBatchDeleteAsync
    /// </summary>
    [Fact]
    public async Task ExecuteBatchDeleteAsync_WhenConfirmed_ShouldCallOnExecuteBatchDeleteAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ConfirmationResult = true; // 用户确认删除

        var testItem1 = new TestDataItem { Id = 1, Name = "Item 1" };
        var testItem2 = new TestDataItem { Id = 2, Name = "Item 2" };
        viewModel.SelectedItems.Add(testItem1);
        viewModel.SelectedItems.Add(testItem2);

        // Act
        viewModel.BatchDeleteCommand.Execute();
        // 等待异步操作完成
        await Task.Delay(100);

        // Assert
        viewModel.OnExecuteBatchDeleteAsyncCallCount.Should().Be(1, "因为应该调用一次OnExecuteBatchDeleteAsync");
        viewModel.LastDeletedItems.Should().NotBeNull("因为应该传递删除项列表");
        viewModel.LastDeletedItems.Should().HaveCount(2, "因为选中了2个项目");
        viewModel.LastDeletedItems.Should().Contain(testItem1, "因为testItem1在选中列表中");
        viewModel.LastDeletedItems.Should().Contain(testItem2, "因为testItem2在选中列表中");
    }

    /// <summary>
    /// 测试：ExecuteBatchDeleteAsync完成后清空SelectedItems
    /// </summary>
    [Fact]
    public async Task ExecuteBatchDeleteAsync_WhenCompleted_ShouldClearSelectedItems()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ConfirmationResult = true;

        viewModel.SelectedItems.Add(new TestDataItem { Id = 1, Name = "Item 1" });
        viewModel.SelectedItems.Add(new TestDataItem { Id = 2, Name = "Item 2" });

        var initialCount = viewModel.SelectedItems.Count;

        // Act
        viewModel.BatchDeleteCommand.Execute();
        // 等待异步操作完成
        await Task.Delay(100);

        // Assert
        initialCount.Should().Be(2, "因为添加了2个选中项");
        viewModel.SelectedItems.Should().BeEmpty("因为批量删除完成后应该清空SelectedItems");
    }

    /// <summary>
    /// 测试：ExecuteBatchDeleteAsync完成后调用LoadDataAsync（通过RefreshAsync）
    /// </summary>
    [Fact]
    public async Task ExecuteBatchDeleteAsync_WhenCompleted_ShouldCallLoadDataAsync()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ConfirmationResult = true;

        viewModel.SelectedItems.Add(new TestDataItem { Id = 1, Name = "Item 1" });

        var initialCallCount = viewModel.LoadDataAsyncCallCount;

        // Act
        viewModel.BatchDeleteCommand.Execute();
        // 等待异步操作完成
        await Task.Delay(100);

        // Assert
        viewModel.LoadDataAsyncCallCount.Should().BeGreaterThan(initialCallCount,
            "因为批量删除完成后应该调用RefreshAsync从而调用LoadDataAsync");
    }

    /// <summary>
    /// 测试：用户取消确认时不执行删除
    /// BR-002: 删除前必须确认
    /// </summary>
    [Fact]
    public async Task ExecuteBatchDeleteAsync_WhenUserCancels_ShouldNotExecuteDelete()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ConfirmationResult = false; // 用户取消确认

        viewModel.SelectedItems.Add(new TestDataItem { Id = 1, Name = "Item 1" });
        var initialSelectedCount = viewModel.SelectedItems.Count;

        // Act
        viewModel.BatchDeleteCommand.Execute();
        // 等待异步操作完成
        await Task.Delay(100);

        // Assert
        viewModel.OnExecuteBatchDeleteAsyncCallCount.Should().Be(0, "因为用户取消确认时不应该调用OnExecuteBatchDeleteAsync");
        viewModel.SelectedItems.Should().HaveCount(initialSelectedCount, "因为用户取消确认时不应该清空SelectedItems");
    }

    #endregion
}
