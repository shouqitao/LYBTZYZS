using System;
using Xunit;
using FluentAssertions;

namespace LYBT.Tests.Basic
{
    /// <summary>
    /// 基础测试框架验证
    /// 验证测试环境配置正确，为后续测试扩展奠定基础
    /// </summary>
    public class BasicTestRunner
    {
        [Fact]
        public void TestFramework_Should_Work_Correctly()
        {
            // Arrange
            var expected = "Hello, Testing!";
            
            // Act
            var actual = "Hello, Testing!";
            
            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public void FluentAssertions_Should_Work_Correctly()
        {
            // Arrange
            var numbers = new[] { 1, 2, 3, 4, 5 };
            
            // Act & Assert
            numbers.Should().HaveCount(5);
            numbers.Should().Contain(3);
            numbers.Should().BeInAscendingOrder();
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2, 3, 5)]
        [InlineData(-1, 1, 0)]
        public void ParameterizedTests_Should_Work_Correctly(int a, int b, int expected)
        {
            // Act
            var result = a + b;
            
            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExceptionHandling_Should_Work_Correctly()
        {
            // Act & Assert
            Action act = () => throw new InvalidOperationException("Test exception");
            
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Test exception");
        }
    }
}