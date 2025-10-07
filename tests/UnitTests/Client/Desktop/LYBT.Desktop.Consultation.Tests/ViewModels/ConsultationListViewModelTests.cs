using FluentAssertions;
using Xunit;

namespace LYBT.Desktop.Consultation.Tests.ViewModels
{
    /// <summary>
    /// ConsultationListViewModel 单元测试
    /// </summary>
    public class ConsultationListViewModelTests
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
