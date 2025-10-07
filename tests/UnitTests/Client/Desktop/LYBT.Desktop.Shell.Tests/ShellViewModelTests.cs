using FluentAssertions;
using Xunit;

namespace LYBT.Desktop.Shell.Tests
{
    /// <summary>
    /// ShellViewModel 单元测试
    /// </summary>
    public class ShellViewModelTests
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
