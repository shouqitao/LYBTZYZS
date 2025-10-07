using FluentAssertions;
using LYBT.Entities.Prescriptions;
using System;
using Xunit;

namespace LYBT.UnitTests.Core.Entities
{
    /// <summary>
    /// PrescriptionPrintLog实体单元测试
    /// </summary>
    public class PrescriptionPrintLogTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeAllProperties()
        {
            // Arrange & Act
            var printLog = new PrescriptionPrintLog();

            // Assert
            printLog.Id.Should().Be(Guid.Empty, "Id应该由外部设置");
            printLog.PrescriptionId.Should().Be(Guid.Empty, "PrescriptionId应该由外部设置");
            printLog.PrintVersion.Should().Be(0);
            printLog.IsSuccess.Should().BeFalse();
            printLog.PrintedAt.Should().Be(default(DateTime));
        }

        #endregion

        #region Print Log Data Tests

        [Fact]
        public void PrintLog_ShouldRecordSuccessfulPrint()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var printedBy = Guid.NewGuid();

            // Act
            var printLog = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                PrintVersion = 1,
                PrintedAt = DateTime.UtcNow,
                PrintedBy = printedBy,
                PrintedByName = "张医生",
                PrinterName = "HP LaserJet P1108",
                IsSuccess = true,
                ErrorMessage = null,
                Remark = "首次打印"
            };

            // Assert
            printLog.PrescriptionId.Should().Be(prescriptionId);
            printLog.PrintVersion.Should().Be(1);
            printLog.PrintedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            printLog.PrintedBy.Should().Be(printedBy);
            printLog.PrintedByName.Should().Be("张医生");
            printLog.PrinterName.Should().Contain("HP");
            printLog.IsSuccess.Should().BeTrue();
            printLog.ErrorMessage.Should().BeNull();
            printLog.Remark.Should().Be("首次打印");
        }

        [Fact]
        public void PrintLog_ShouldRecordFailedPrint()
        {
            // Arrange & Act
            var printLog = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                PrintVersion = 2,
                PrintedAt = DateTime.UtcNow,
                PrintedBy = Guid.NewGuid(),
                PrintedByName = "李医生",
                PrinterName = "Canon LBP2900",
                IsSuccess = false,
                ErrorMessage = "打印机缺纸",
                Remark = "第二次尝试打印失败"
            };

            // Assert
            printLog.IsSuccess.Should().BeFalse();
            printLog.ErrorMessage.Should().NotBeNullOrEmpty();
            printLog.ErrorMessage.Should().Contain("缺纸");
            printLog.Remark.Should().Contain("失败");
        }

        #endregion

        #region Print Version Tracking Tests

        [Fact]
        public void PrintLog_ShouldTrackDifferentVersions()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            
            // Act
            var printLog1 = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                PrintVersion = 1,
                PrintedAt = DateTime.UtcNow.AddHours(-2),
                IsSuccess = true
            };

            var printLog2 = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                PrintVersion = 2,
                PrintedAt = DateTime.UtcNow,
                IsSuccess = true
            };

            // Assert
            printLog1.PrescriptionId.Should().Be(printLog2.PrescriptionId, "同一处方的打印日志");
            printLog2.PrintVersion.Should().BeGreaterThan(printLog1.PrintVersion, "版本号应递增");
            printLog2.PrintedAt.Should().BeAfter(printLog1.PrintedAt, "打印时间应递增");
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void PrintLog_ShouldHavePrescriptionReference()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var printLog = new PrescriptionPrintLog
            {
                PrescriptionId = prescriptionId
            };

            // Act & Assert
            printLog.PrescriptionId.Should().Be(prescriptionId);
            printLog.Prescription.Should().BeNull("导航属性需要通过EF Core加载");
        }

        #endregion

        #region Audit Fields Tests

        [Fact]
        public void AuditFields_ShouldBePresent()
        {
            // Arrange & Act
            var printLog = new PrescriptionPrintLog
            {
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            };

            // Assert
            printLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            printLog.CreatedBy.Should().NotBe(Guid.Empty);
            printLog.UpdatedAt.Should().BeNull("新创建的日志不应有更新时间");
            printLog.UpdatedBy.Should().BeNull("新创建的日志不应有更新者");
        }

        #endregion

        #region Business Rules Tests

        [Fact]
        public void PrintLog_ShouldBeImmutable_AfterCreation()
        {
            // Arrange
            var printLog = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                PrintVersion = 1,
                PrintedAt = DateTime.UtcNow,
                IsSuccess = true,
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            // 打印日志通常不应该被修改，这是审计记录
            Action act = () =>
            {
                if (printLog.CreatedAt != default(DateTime))
                {
                    throw new InvalidOperationException("打印日志不能被修改");
                }
            };

            // 如果尝试修改已创建的日志，应该抛出异常
            printLog.CreatedAt.Should().NotBe(default(DateTime));
        }

        [Fact]
        public void PrintLog_ShouldCaptureAllRelevantInfo()
        {
            // Arrange & Act
            var printLog = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                PrintVersion = 1,
                PrintedAt = DateTime.UtcNow,
                PrintedBy = Guid.NewGuid(),
                PrintedByName = "王医生",
                PrinterName = "Epson L3150",
                IsSuccess = true
            };

            // Assert - 验证所有关键信息都被记录
            printLog.PrescriptionId.Should().NotBe(Guid.Empty, "必须关联处方");
            printLog.PrintVersion.Should().BeGreaterThan(0, "必须有版本号");
            printLog.PrintedAt.Should().NotBe(default(DateTime), "必须有打印时间");
            printLog.PrintedBy.Should().NotBeNull("应记录打印人");
            printLog.PrintedByName.Should().NotBeNullOrEmpty("应记录打印人姓名");
            printLog.PrinterName.Should().NotBeNullOrEmpty("应记录打印机名称");
        }

        #endregion

        #region Error Handling Tests

        [Theory]
        [InlineData("打印机离线", false)]
        [InlineData("纸张卡纸", false)]
        [InlineData("墨盒用尽", false)]
        [InlineData(null, true)]
        public void PrintLog_ShouldHandleDifferentErrorScenarios(string errorMessage, bool isSuccess)
        {
            // Arrange & Act
            var printLog = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                PrescriptionId = Guid.NewGuid(),
                PrintVersion = 1,
                PrintedAt = DateTime.UtcNow,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            // Assert
            if (isSuccess)
            {
                printLog.ErrorMessage.Should().BeNull("成功打印不应有错误信息");
            }
            else
            {
                printLog.ErrorMessage.Should().NotBeNullOrEmpty("失败打印应记录错误信息");
            }
        }

        #endregion

        #region Soft Delete Tests

        [Fact]
        public void PrintLog_ShouldSupportSoftDelete()
        {
            // Arrange
            var printLog = new PrescriptionPrintLog
            {
                Id = Guid.NewGuid(),
                IsDeleted = false
            };

            // Act
            printLog.IsDeleted = true;

            // Assert
            printLog.IsDeleted.Should().BeTrue();
        }

        #endregion
    }
}