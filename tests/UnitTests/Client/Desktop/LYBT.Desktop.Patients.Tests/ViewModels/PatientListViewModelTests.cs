using FluentAssertions;

namespace LYBT.Desktop.Patients.Tests.ViewModels
{
    /// <summary>
    /// PatientListViewModel 单元测试
    /// </summary>
    public class PatientListViewModelTests
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
