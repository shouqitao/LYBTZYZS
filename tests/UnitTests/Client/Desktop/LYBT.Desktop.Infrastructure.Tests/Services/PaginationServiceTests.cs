using FluentAssertions;
using LYBT.Desktop.Infrastructure.Services;

namespace LYBT.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// PaginationService 单元测试
/// Phase 4.4: Infrastructure P2 测试
/// </summary>
public class PaginationServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var service = new PaginationService();

        // Assert
        service.CurrentPage.Should().Be(1);
        service.PageSize.Should().Be(20);
        service.TotalCount.Should().Be(0);
        service.TotalPages.Should().Be(0);
    }

    #endregion

    #region TotalPages Tests

    [Theory]
    [InlineData(100, 20, 5)]
    [InlineData(101, 20, 6)]
    [InlineData(99, 20, 5)]
    [InlineData(50, 10, 5)]
    [InlineData(0, 20, 0)]
    public void TotalPages_ShouldCalculateCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var service = new PaginationService
        {
            TotalCount = totalCount,
            PageSize = pageSize
        };

        // Act & Assert
        service.TotalPages.Should().Be(expectedPages);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public void GoToPage_WithValidPage_ShouldNavigate()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Act
        service.GoToPage(3);

        // Assert
        service.CurrentPage.Should().Be(3);
    }

    [Fact]
    public void GoToPage_WithPageLessThanOne_ShouldGoToFirstPage()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Act
        service.GoToPage(0);

        // Assert
        service.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void GoToPage_WithPageGreaterThanTotal_ShouldGoToLastPage()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Act
        service.GoToPage(10);

        // Assert
        service.CurrentPage.Should().Be(5); // 100/20 = 5 pages
    }

    [Fact]
    public void GoToNextPage_WhenNotOnLastPage_ShouldIncrement()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(3);

        // Act
        service.GoToNextPage();

        // Assert
        service.CurrentPage.Should().Be(4);
    }

    [Fact]
    public void GoToNextPage_WhenOnLastPage_ShouldNotChange()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(5);

        // Act
        service.GoToNextPage();

        // Assert
        service.CurrentPage.Should().Be(5);
    }

    [Fact]
    public void GoToPreviousPage_WhenNotOnFirstPage_ShouldDecrement()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(3);

        // Act
        service.GoToPreviousPage();

        // Assert
        service.CurrentPage.Should().Be(2);
    }

    [Fact]
    public void GoToPreviousPage_WhenOnFirstPage_ShouldNotChange()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Act
        service.GoToPreviousPage();

        // Assert
        service.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void GoToFirstPage_ShouldNavigateToPageOne()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(4);

        // Act
        service.GoToFirstPage();

        // Assert
        service.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void GoToLastPage_ShouldNavigateToLastPage()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Act
        service.GoToLastPage();

        // Assert
        service.CurrentPage.Should().Be(5);
    }

    #endregion

    #region CanNavigate Tests

    [Fact]
    public void CanGoToFirstPage_WhenOnFirstPage_ShouldBeFalse()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Assert
        service.CanGoToFirstPage.Should().BeFalse();
    }

    [Fact]
    public void CanGoToFirstPage_WhenNotOnFirstPage_ShouldBeTrue()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(3);

        // Assert
        service.CanGoToFirstPage.Should().BeTrue();
    }

    [Fact]
    public void CanGoToLastPage_WhenOnLastPage_ShouldBeFalse()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(5);

        // Assert
        service.CanGoToLastPage.Should().BeFalse();
    }

    [Fact]
    public void CanGoToLastPage_WhenNotOnLastPage_ShouldBeTrue()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };

        // Assert
        service.CanGoToLastPage.Should().BeTrue();
    }

    #endregion

    #region PageChanged Event Tests

    [Fact]
    public void GoToPage_ShouldRaisePageChangedEvent()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        PageChangedEventArgs? eventArgs = null;
        service.PageChanged += (_, e) => eventArgs = e;

        // Act
        service.GoToPage(3);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.OldPage.Should().Be(1);
        eventArgs.NewPage.Should().Be(3);
        eventArgs.PageSize.Should().Be(20);
    }

    [Fact]
    public void GoToPage_WhenSamePage_ShouldNotRaiseEvent()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        var eventRaised = false;
        service.PageChanged += (_, _) => eventRaised = true;

        // Act
        service.GoToPage(1); // Already on page 1

        // Assert
        eventRaised.Should().BeFalse();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ShouldResetToDefaults()
    {
        // Arrange
        var service = new PaginationService { TotalCount = 100, PageSize = 20 };
        service.GoToPage(3);

        // Act
        service.Reset();

        // Assert
        service.CurrentPage.Should().Be(1);
        service.TotalCount.Should().Be(0);
    }

    #endregion

    #region PageSizes Tests

    [Fact]
    public void PageSizes_ShouldContainDefaultValues()
    {
        // Arrange
        var service = new PaginationService();

        // Assert
        service.PageSizes.Should().BeEquivalentTo(new[] { 10, 20, 50, 100 });
    }

    #endregion
}
