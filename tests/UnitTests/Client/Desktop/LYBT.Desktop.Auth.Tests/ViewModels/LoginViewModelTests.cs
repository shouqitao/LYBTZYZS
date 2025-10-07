using FluentAssertions;
using Xunit;

namespace LYBT.Desktop.Auth.Tests.ViewModels
{
    /// <summary>
    /// LoginViewModel 单元测试
    /// </summary>
    public class LoginViewModelTests
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
