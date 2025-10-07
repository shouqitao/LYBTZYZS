using FluentAssertions;
using Xunit;

namespace LYBT.Desktop.Prescriptions.Tests.ViewModels
{
    /// <summary>
    /// PrescriptionListViewModel 单元测试
    /// </summary>
    public class PrescriptionListViewModelTests
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
