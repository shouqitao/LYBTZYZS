using FluentAssertions;

namespace LYBT.Desktop.Users.Tests.ViewModels
{
    /// <summary>
    /// UserListViewModel 单元测试
    /// </summary>
    public class UserListViewModelTests
    {
        [Fact]
        public void Placeholder_Test_ShouldPass()
        {
            // Arrange
            var expected = true;

            // Act
            var actual = true;

            // Assert
            actual.Should().Be(expected);
        }
    }
}
