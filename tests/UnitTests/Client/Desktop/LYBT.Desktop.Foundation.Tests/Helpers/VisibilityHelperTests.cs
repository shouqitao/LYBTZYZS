using System.Windows;
using LYBT.Desktop.Foundation.Helpers;
using Xunit;

namespace LYBT.Desktop.Foundation.Tests.Helpers
{
    /// <summary>
    /// VisibilityHelper单元测试
    /// Issue #2148: 验证VisibilityHelper的正确性
    /// </summary>
    public class VisibilityHelperTests
    {
        #region ToVisibility Tests

        [Fact(DisplayName = "ToVisibility_WhenTrue_ShouldReturnVisible")]
        public void ToVisibility_WhenTrue_ShouldReturnVisible()
        {
            // Arrange
            var isVisible = true;

            // Act
            var result = VisibilityHelper.ToVisibility(isVisible);

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact(DisplayName = "ToVisibility_WhenFalse_ShouldReturnCollapsed")]
        public void ToVisibility_WhenFalse_ShouldReturnCollapsed()
        {
            // Arrange
            var isVisible = false;

            // Act
            var result = VisibilityHelper.ToVisibility(isVisible);

            // Assert
            Assert.Equal(Visibility.Collapsed, result);
        }

        #endregion

        #region ToVisibilityHidden Tests

        [Fact(DisplayName = "ToVisibilityHidden_WhenTrue_ShouldReturnVisible")]
        public void ToVisibilityHidden_WhenTrue_ShouldReturnVisible()
        {
            // Arrange
            var isVisible = true;

            // Act
            var result = VisibilityHelper.ToVisibilityHidden(isVisible);

            // Assert
            Assert.Equal(Visibility.Visible, result);
        }

        [Fact(DisplayName = "ToVisibilityHidden_WhenFalse_ShouldReturnHidden")]
        public void ToVisibilityHidden_WhenFalse_ShouldReturnHidden()
        {
            // Arrange
            var isVisible = false;

            // Act
            var result = VisibilityHelper.ToVisibilityHidden(isVisible);

            // Assert
            Assert.Equal(Visibility.Hidden, result);
        }

        #endregion

        #region Integration Tests

        [Theory(DisplayName = "ToVisibility_ShouldConvertBooleanCorrectly")]
        [InlineData(true, Visibility.Visible)]
        [InlineData(false, Visibility.Collapsed)]
        public void ToVisibility_ShouldConvertBooleanCorrectly(bool input, Visibility expected)
        {
            // Act
            var result = VisibilityHelper.ToVisibility(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory(DisplayName = "ToVisibilityHidden_ShouldConvertBooleanCorrectly")]
        [InlineData(true, Visibility.Visible)]
        [InlineData(false, Visibility.Hidden)]
        public void ToVisibilityHidden_ShouldConvertBooleanCorrectly(bool input, Visibility expected)
        {
            // Act
            var result = VisibilityHelper.ToVisibilityHidden(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
