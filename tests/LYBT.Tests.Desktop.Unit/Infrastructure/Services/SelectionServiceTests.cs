using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services;

namespace LYBT.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// SelectionService 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class SelectionServiceTests
{
    private class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var service = new SelectionService<TestItem>();

        // Assert
        service.SelectedItem.Should().BeNull();
        service.SelectedItems.Should().BeEmpty();
        service.HasSelection.Should().BeFalse();
        service.SelectionCount.Should().Be(0);
        service.IsMultiSelectMode.Should().BeFalse();
    }

    #endregion

    #region Select Tests

    [Fact]
    public void Select_WithItem_ShouldSetSelectedItem()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };

        // Act
        service.Select(item);

        // Assert
        service.SelectedItem.Should().Be(item);
        service.HasSelection.Should().BeTrue();
        service.SelectionCount.Should().Be(1);
    }

    [Fact]
    public void Select_WithNull_ShouldClearSelection()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };
        service.Select(item);

        // Act
        service.Select(null);

        // Assert
        service.SelectedItem.Should().BeNull();
        service.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Select_ShouldAddToSelectedItems()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };

        // Act
        service.Select(item);

        // Assert
        service.SelectedItems.Should().Contain(item);
    }

    [Fact]
    public void Select_InSingleMode_ShouldReplaceSelectedItems()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item1 = new TestItem { Id = 1, Name = "Test1" };
        var item2 = new TestItem { Id = 2, Name = "Test2" };
        service.Select(item1);

        // Act
        service.Select(item2);

        // Assert
        service.SelectedItem.Should().Be(item2);
        service.SelectedItems.Should().HaveCount(1);
        service.SelectedItems.Should().Contain(item2);
        service.SelectedItems.Should().NotContain(item1);
    }

    #endregion

    #region SelectMultiple Tests

    [Fact]
    public void SelectMultiple_ShouldSelectAllItems()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var items = new[]
        {
            new TestItem { Id = 1, Name = "Test1" },
            new TestItem { Id = 2, Name = "Test2" },
            new TestItem { Id = 3, Name = "Test3" }
        };

        // Act
        service.SelectMultiple(items);

        // Assert
        service.SelectedItems.Should().HaveCount(3);
        service.SelectedItem.Should().Be(items[0]);
    }

    [Fact]
    public void SelectMultiple_ShouldClearPreviousSelection()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item1 = new TestItem { Id = 1, Name = "Test1" };
        service.Select(item1);

        var newItems = new[]
        {
            new TestItem { Id = 2, Name = "Test2" },
            new TestItem { Id = 3, Name = "Test3" }
        };

        // Act
        service.SelectMultiple(newItems);

        // Assert
        service.SelectedItems.Should().HaveCount(2);
        service.SelectedItems.Should().NotContain(item1);
    }

    #endregion

    #region ClearSelection Tests

    [Fact]
    public void ClearSelection_ShouldClearAllSelections()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        service.SelectMultiple(new[]
        {
            new TestItem { Id = 1, Name = "Test1" },
            new TestItem { Id = 2, Name = "Test2" }
        });

        // Act
        service.ClearSelection();

        // Assert
        service.SelectedItem.Should().BeNull();
        service.SelectedItems.Should().BeEmpty();
        service.HasSelection.Should().BeFalse();
        service.SelectionCount.Should().Be(0);
    }

    #endregion

    #region ToggleSelection Tests

    [Fact]
    public void ToggleSelection_InSingleMode_ShouldSelectItem()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };

        // Act
        service.ToggleSelection(item);

        // Assert
        service.SelectedItem.Should().Be(item);
    }

    [Fact]
    public void ToggleSelection_InSingleMode_ShouldDeselectIfAlreadySelected()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };
        service.Select(item);

        // Act
        service.ToggleSelection(item);

        // Assert
        service.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void ToggleSelection_InMultiSelectMode_ShouldAddToSelection()
    {
        // Arrange
        var service = new SelectionService<TestItem> { IsMultiSelectMode = true };
        var item1 = new TestItem { Id = 1, Name = "Test1" };
        var item2 = new TestItem { Id = 2, Name = "Test2" };
        // 多选模式下使用 ToggleSelection 添加第一个元素
        service.ToggleSelection(item1);

        // Act - 再添加第二个元素
        service.ToggleSelection(item2);

        // Assert
        service.SelectedItems.Should().HaveCount(2);
        service.SelectedItems.Should().Contain(item1);
        service.SelectedItems.Should().Contain(item2);
    }

    [Fact]
    public void ToggleSelection_InMultiSelectMode_ShouldRemoveIfExists()
    {
        // Arrange
        var service = new SelectionService<TestItem> { IsMultiSelectMode = true };
        var item1 = new TestItem { Id = 1, Name = "Test1" };
        var item2 = new TestItem { Id = 2, Name = "Test2" };
        service.SelectMultiple(new[] { item1, item2 });

        // Act
        service.ToggleSelection(item1);

        // Assert
        service.SelectedItems.Should().HaveCount(1);
        service.SelectedItems.Should().NotContain(item1);
        service.SelectedItems.Should().Contain(item2);
    }

    #endregion

    #region SelectionChanged Event Tests

    [Fact]
    public void Select_ShouldRaiseSelectionChangedEvent()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };
        SelectionChangedEventArgs<TestItem>? eventArgs = null;
        service.SelectionChanged += (_, e) => eventArgs = e;

        // Act
        service.Select(item);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewSelection.Should().Be(item);
        eventArgs.OldSelection.Should().BeNull();
    }

    [Fact]
    public void ClearSelection_ShouldRaiseSelectionChangedEvent()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        var item = new TestItem { Id = 1, Name = "Test" };
        service.Select(item);
        SelectionChangedEventArgs<TestItem>? eventArgs = null;
        service.SelectionChanged += (_, e) => eventArgs = e;

        // Act
        service.ClearSelection();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewSelection.Should().BeNull();
        eventArgs.OldSelection.Should().Be(item);
    }

    #endregion

    #region SelectionCount Tests

    [Fact]
    public void SelectionCount_InSingleMode_ShouldReturnOneWhenSelected()
    {
        // Arrange
        var service = new SelectionService<TestItem>();
        service.Select(new TestItem { Id = 1, Name = "Test" });

        // Assert
        service.SelectionCount.Should().Be(1);
    }

    [Fact]
    public void SelectionCount_InMultiSelectMode_ShouldReturnItemsCount()
    {
        // Arrange
        var service = new SelectionService<TestItem> { IsMultiSelectMode = true };
        service.SelectMultiple(new[]
        {
            new TestItem { Id = 1, Name = "Test1" },
            new TestItem { Id = 2, Name = "Test2" },
            new TestItem { Id = 3, Name = "Test3" }
        });

        // Assert
        service.SelectionCount.Should().Be(3);
    }

    #endregion
}
