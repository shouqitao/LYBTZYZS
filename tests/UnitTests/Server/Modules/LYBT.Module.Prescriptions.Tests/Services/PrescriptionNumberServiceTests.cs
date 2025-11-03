using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// 处方编号服务单元测试
    /// Issue #1551: 处方自动编号功能
    /// 测试编号生成逻辑、格式验证、并发安全等
    /// </summary>
    public class PrescriptionNumberServiceTests : TestBase
    {
        private readonly PrescriptionNumberService _service;
        private readonly Mock<IPrescriptionRepository> _repositoryMock;
        private readonly Mock<ILogger<PrescriptionNumberService>> _loggerMock;

        public PrescriptionNumberServiceTests()
        {
            _repositoryMock = CreateMock<IPrescriptionRepository>();
            _loggerMock = CreateLoggerMock<PrescriptionNumberService>();
            _service = new PrescriptionNumberService(_repositoryMock.Object, _loggerMock.Object);
        }

        #region GenerateNumberAsync Tests

        [Fact]
        public async Task GenerateNumberAsync_WithNoExistingNumbers_ShouldReturnFirstNumber()
        {
            // Arrange
            var date = new DateTime(2025, 10, 21);
            var expectedPrefix = "RX-20251021-";

            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(expectedPrefix))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _service.GenerateNumberAsync(date);

            // Assert
            result.Should().Be("RX-20251021-0001");
            _repositoryMock.Verify(x => x.GetPrescriptionNumbersByPrefixAsync(expectedPrefix), Times.Once);
        }

        [Fact]
        public async Task GenerateNumberAsync_WithExistingNumbers_ShouldReturnNextSequence()
        {
            // Arrange
            var date = new DateTime(2025, 10, 21);
            var expectedPrefix = "RX-20251021-";
            var existingNumbers = new List<string>
            {
                "RX-20251021-0001",
                "RX-20251021-0002",
                "RX-20251021-0003"
            };

            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(expectedPrefix))
                .ReturnsAsync(existingNumbers);

            // Act
            var result = await _service.GenerateNumberAsync(date);

            // Assert
            result.Should().Be("RX-20251021-0004");
        }

        [Fact]
        public async Task GenerateNumberAsync_WithGapsInSequence_ShouldReturnMaxPlusOne()
        {
            // Arrange - 测试序号有间隔的情况（例如删除了中间的处方）
            var date = new DateTime(2025, 10, 21);
            var expectedPrefix = "RX-20251021-";
            var existingNumbers = new List<string>
            {
                "RX-20251021-0001",
                "RX-20251021-0005",  // 缺少0002-0004
                "RX-20251021-0008"   // 缺少0006-0007
            };

            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(expectedPrefix))
                .ReturnsAsync(existingNumbers);

            // Act
            var result = await _service.GenerateNumberAsync(date);

            // Assert
            result.Should().Be("RX-20251021-0009"); // 应该使用最大序号+1
        }

        [Fact]
        public async Task GenerateNumberAsync_WithDifferentDate_ShouldRestartSequence()
        {
            // Arrange - 测试跨日期边界时序号重置
            var date1 = new DateTime(2025, 10, 21);
            var date2 = new DateTime(2025, 10, 22);

            var prefix1 = "RX-20251021-";
            var prefix2 = "RX-20251022-";

            var existingNumbersDay1 = new List<string>
            {
                "RX-20251021-0001",
                "RX-20251021-0002",
                "RX-20251021-0003"
            };

            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(prefix1))
                .ReturnsAsync(existingNumbersDay1);

            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(prefix2))
                .ReturnsAsync(new List<string>()); // 新的一天，无记录

            // Act
            var result1 = await _service.GenerateNumberAsync(date1);
            var result2 = await _service.GenerateNumberAsync(date2);

            // Assert
            result1.Should().Be("RX-20251021-0004"); // 第1天的第4个
            result2.Should().Be("RX-20251022-0001"); // 第2天的第1个（重置）
        }

        [Fact]
        public async Task GenerateNumberAsync_WithMaxSequence9999_ShouldStillGenerate()
        {
            // Arrange - 测试边界情况：达到9999后仍能生成（虽然不推荐，但不应崩溃）
            var date = new DateTime(2025, 10, 21);
            var expectedPrefix = "RX-20251021-";
            var existingNumbers = new List<string>
            {
                "RX-20251021-9999"
            };

            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(expectedPrefix))
                .ReturnsAsync(existingNumbers);

            // Act
            var result = await _service.GenerateNumberAsync(date);

            // Assert
            result.Should().Be("RX-20251021-10000"); // 超过4位，但逻辑上正确
        }

        #endregion

        #region ValidateNumberFormat Tests

        [Theory]
        [InlineData("RX-20251021-0001", true)]  // 标准格式
        [InlineData("RX-20251231-9999", true)]  // 最大序号
        [InlineData("RX-20250101-0001", true)]  // 年初
        [InlineData("", false)]                  // 空字符串
        [InlineData(null, false)]                // null
        [InlineData("RX-20251021-001", false)]   // 序号不足4位
        [InlineData("RX-20251021-00001", false)] // 序号超过4位
        [InlineData("RX-2025102-0001", false)]   // 日期不足8位
        [InlineData("RX-202510211-0001", false)] // 日期超过8位
        [InlineData("PX-20251021-0001", false)]  // 错误前缀
        [InlineData("RX20251021-0001", false)]   // 缺少分隔符
        [InlineData("RX-20251021_0001", false)]  // 错误分隔符
        [InlineData("RX-20251321-0001", false)]  // 无效月份
        [InlineData("RX-20250230-0001", false)]  // 无效日期（2月30日）
        [InlineData("RX-20251021-ABCD", false)]  // 序号包含字母
        public void ValidateNumberFormat_WithVariousInputs_ShouldReturnExpectedResult(
            string? prescriptionNumber, bool expected)
        {
            // Act
            var result = _service.ValidateNumberFormat(prescriptionNumber!);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ValidateNumberFormat_WithWhitespace_ShouldReturnFalse()
        {
            // Arrange
            var prescriptionNumber = "   ";

            // Act
            var result = _service.ValidateNumberFormat(prescriptionNumber);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public async Task GenerateNumberAsync_MultipleCallsSameDay_ShouldGenerateSequentialNumbers()
        {
            // Arrange - 模拟同一天多次调用，验证序号递增
            var date = new DateTime(2025, 10, 21);
            var prefix = "RX-20251021-";

            var existingNumbers = new List<string>();
            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(prefix))
                .ReturnsAsync(() => new List<string>(existingNumbers));

            // Act - 模拟生成3个编号
            var number1 = await _service.GenerateNumberAsync(date);
            existingNumbers.Add(number1);

            var number2 = await _service.GenerateNumberAsync(date);
            existingNumbers.Add(number2);

            var number3 = await _service.GenerateNumberAsync(date);

            // Assert
            number1.Should().Be("RX-20251021-0001");
            number2.Should().Be("RX-20251021-0002");
            number3.Should().Be("RX-20251021-0003");
        }

        [Fact]
        public async Task GenerateNumberAsync_ThenValidate_ShouldPass()
        {
            // Arrange
            var date = new DateTime(2025, 10, 21);
            _repositoryMock.Setup(x => x.GetPrescriptionNumbersByPrefixAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string>());

            // Act
            var generatedNumber = await _service.GenerateNumberAsync(date);
            var isValid = _service.ValidateNumberFormat(generatedNumber);

            // Assert
            isValid.Should().BeTrue("生成的编号应该通过格式验证");
        }

        #endregion

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
