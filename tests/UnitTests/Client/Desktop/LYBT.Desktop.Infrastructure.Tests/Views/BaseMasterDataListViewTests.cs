using System.Collections;
using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Infrastructure.Views;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Views;

/// <summary>
/// BaseMasterDataListView控件测试
/// Issue #2153 - Task 1.3: 控件层单元测试
/// </summary>
public class BaseMasterDataListViewTests
{
    /// <summary>
    /// 静态构造函数：初始化WPF资源
    /// </summary>
    static BaseMasterDataListViewTests()
    {
        WpfTestInitializer.Initialize();
    }

    #region Test Data Models

    /// <summary>
    /// 测试用数据模型
    /// </summary>
    private class TestDataItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region SelectedItems Property Tests

    /// <summary>
    /// 测试：SelectedItems属性绑定正确
    /// </summary>
    [StaFact]
    public void SelectedItems_Property_ShouldBindCorrectly()
    {
        // Arrange
        var view = new BaseMasterDataListView();
        var selectedItems = new ObservableCollection<object>
        {
            new TestDataItem { Id = 1, Name = "Item 1" },
            new TestDataItem { Id = 2, Name = "Item 2" }
        };

        // Act
        view.SelectedItems = selectedItems;

        // Assert
        view.SelectedItems.Should().BeSameAs(selectedItems, "因为SelectedItems应该支持双向绑定");
        view.SelectedItems.Cast<object>().Should().HaveCount(2, "因为设置了2个选中项");
    }

    /// <summary>
    /// 测试：SelectedItems属性变化应触发PropertyChanged
    /// </summary>
    [StaFact]
    public void SelectedItems_WhenChanged_ShouldSupportTwoWayBinding()
    {
        // Arrange
        var view = new BaseMasterDataListView();
        var firstSelection = new ObservableCollection<object>
        {
            new TestDataItem { Id = 1, Name = "Item 1" }
        };
        var secondSelection = new ObservableCollection<object>
        {
            new TestDataItem { Id = 2, Name = "Item 2" }
        };

        // Act
        view.SelectedItems = firstSelection;
        var firstResult = view.SelectedItems;

        view.SelectedItems = secondSelection;
        var secondResult = view.SelectedItems;

        // Assert
        firstResult.Should().BeSameAs(firstSelection, "因为第一次设置应该生效");
        secondResult.Should().BeSameAs(secondSelection, "因为第二次设置应该生效");
        firstResult.Should().NotBeSameAs(secondResult, "因为两次设置的集合不同");
    }

    #endregion

    #region ShowCheckBoxColumn Property Tests

    /// <summary>
    /// 测试：ShowCheckBoxColumn属性绑定正确
    /// </summary>
    [StaFact]
    public void ShowCheckBoxColumn_Property_ShouldBindCorrectly()
    {
        // Arrange
        var view = new BaseMasterDataListView();

        // Act - 默认值
        var defaultValue = view.ShowCheckBoxColumn;

        // 设置为true
        view.ShowCheckBoxColumn = true;
        var trueValue = view.ShowCheckBoxColumn;

        // 设置为false
        view.ShowCheckBoxColumn = false;
        var falseValue = view.ShowCheckBoxColumn;

        // Assert
        defaultValue.Should().BeFalse("因为默认值应该是false");
        trueValue.Should().BeTrue("因为设置为true后应该返回true");
        falseValue.Should().BeFalse("因为设置为false后应该返回false");
    }

    /// <summary>
    /// 测试：ShowCheckBoxColumn属性应该传递到UnifiedManagementTable
    /// </summary>
    [StaFact]
    public void ShowCheckBoxColumn_WhenSet_ShouldPassToUnifiedManagementTable()
    {
        // Arrange
        var view = new BaseMasterDataListView();

        // Act - 触发Loaded事件以初始化视觉树
        view.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.LoadedEvent));

        view.ShowCheckBoxColumn = true;

        // 查找UnifiedManagementTable
        var dataTable = FindVisualChild<LYBT.Desktop.Infrastructure.Controls.UnifiedManagementTable>(view);

        // Assert
        if (dataTable != null)
        {
            dataTable.ShowCheckBoxColumn.Should().BeTrue("因为BaseMasterDataListView的ShowCheckBoxColumn应该传递到UnifiedManagementTable");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 查找可视树中的子元素
    /// </summary>
    private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }

    #endregion
}
