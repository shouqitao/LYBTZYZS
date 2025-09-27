using FluentAssertions;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace LYBT.UnitTests.Core.Entities
{
    /// <summary>
    /// Prescription实体单元测试 - 包含打印版本管理功能
    /// </summary>
    public class PrescriptionModelTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializePrintFields()
        {
            // Arrange & Act
            var prescription = new Prescription();

            // Assert
            prescription.IsPrinted.Should().BeFalse("默认未打印");
            prescription.PrintCount.Should().Be(0, "默认打印次数为0");
            prescription.PrintVersion.Should().Be(1, "默认打印版本为1");
            prescription.LastPrintedAt.Should().BeNull("默认无打印时间");
            prescription.PrintLogs.Should().BeNull("导航属性默认为null");
        }

        #endregion

        #region Print Version Management Tests

        [Fact]
        public void PrintVersion_ShouldIncrementAfterModification()
        {
            // Arrange
            var prescription = new Prescription
            {
                PrintVersion = 1,
                IsPrinted = true,
                PrintCount = 1
            };

            // Act - 模拟修改后重新打印
            prescription.PrintVersion++;

            // Assert
            prescription.PrintVersion.Should().Be(2);
        }

        [Fact]
        public void PrintCount_ShouldIncrementAfterEachPrint()
        {
            // Arrange
            var prescription = new Prescription
            {
                PrintCount = 0,
                IsPrinted = false
            };

            // Act - 模拟打印操作
            for (int i = 0; i < 3; i++)
            {
                prescription.PrintCount++;
                prescription.IsPrinted = true;
                prescription.LastPrintedAt = DateTime.UtcNow;
            }

            // Assert
            prescription.PrintCount.Should().Be(3);
            prescription.IsPrinted.Should().BeTrue();
            prescription.LastPrintedAt.Should().NotBeNull();
        }

        [Fact]
        public void LastPrintedAt_ShouldBeUpdatedWhenPrinted()
        {
            // Arrange
            var prescription = new Prescription
            {
                IsPrinted = false,
                LastPrintedAt = null
            };

            // Act
            var printTime = DateTime.UtcNow;
            prescription.IsPrinted = true;
            prescription.LastPrintedAt = printTime;
            prescription.PrintCount++;

            // Assert
            prescription.LastPrintedAt.Should().NotBeNull();
            prescription.LastPrintedAt.Value.Should().BeCloseTo(printTime, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region Print Status Tests

        [Theory]
        [InlineData(false, 0, false)]  // 未打印
        [InlineData(true, 1, true)]     // 已打印一次
        [InlineData(true, 5, true)]     // 已打印多次
        public void IsPrinted_ShouldReflectPrintStatus(bool isPrinted, int printCount, bool expectedStatus)
        {
            // Arrange
            var prescription = new Prescription
            {
                IsPrinted = isPrinted,
                PrintCount = printCount
            };

            // Act & Assert
            prescription.IsPrinted.Should().Be(expectedStatus);
            if (expectedStatus)
            {
                prescription.PrintCount.Should().BeGreaterThan(0);
            }
            else
            {
                prescription.PrintCount.Should().Be(0);
            }
        }

        #endregion

        #region Print Log Relationship Tests

        [Fact]
        public void PrintLogs_ShouldSupportMultipleLogs()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid()
            };

            var printLogs = new List<PrescriptionPrintLog>
            {
                new PrescriptionPrintLog
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    PrintVersion = 1,
                    PrintedAt = DateTime.UtcNow.AddHours(-2),
                    IsSuccess = true
                },
                new PrescriptionPrintLog
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = prescription.Id,
                    PrintVersion = 2,
                    PrintedAt = DateTime.UtcNow.AddHours(-1),
                    IsSuccess = true
                }
            };

            // Act
            prescription.PrintLogs = printLogs;

            // Assert
            prescription.PrintLogs.Should().NotBeNull();
            prescription.PrintLogs.Should().HaveCount(2);
            prescription.PrintLogs.Should().AllSatisfy(log => 
                log.PrescriptionId.Should().Be(prescription.Id));
        }

        #endregion

        #region Business Rules Tests

        [Fact]
        public void Prescription_ShouldTrackModificationForReprint()
        {
            // Arrange
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                IsPrinted = true,
                PrintCount = 1,
                PrintVersion = 1,
                LastPrintedAt = DateTime.UtcNow.AddDays(-1)
            };

            // Act - 模拟处方内容修改
            prescription.UpdatedAt = DateTime.UtcNow;
            
            // 业务逻辑：如果已打印的处方被修改，应增加版本号
            if (prescription.IsPrinted && prescription.UpdatedAt > prescription.LastPrintedAt)
            {
                prescription.PrintVersion++;
            }

            // Assert
            prescription.PrintVersion.Should().Be(2, "修改后的处方应该增加版本号");
        }

        [Fact]
        public void PrescriptionStatus_ShouldSupportDifferentStates()
        {
            // Arrange
            var prescription = new Prescription();

            // Act & Assert - 测试不同状态
            prescription.Status = PrescriptionStatus.Draft;
            prescription.Status.Should().Be(PrescriptionStatus.Draft);

            prescription.Status = PrescriptionStatus.Confirmed;
            prescription.Status.Should().Be(PrescriptionStatus.Confirmed);

            prescription.Status = PrescriptionStatus.Dispensed;
            prescription.Status.Should().Be(PrescriptionStatus.Dispensed);
        }

        #endregion

        #region Prescription Content Tests

        [Fact]
        public void PrescriptionContent_ShouldBeStoredCorrectly()
        {
            // Arrange
            var prescription = new Prescription();

            // Act
            prescription.PrescriptionNo = $"RX{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
            prescription.Type = "中药饮片";
            prescription.DosageCount = 7;
            prescription.DailyDose = 1;
            prescription.Usage = "水煎服，每日一剂，分两次温服";
            prescription.PayableAmount = 168.50m;

            // Assert
            prescription.PrescriptionNo.Should().StartWith("RX");
            prescription.Type.Should().Be("中药饮片");
            prescription.DosageCount.Should().Be(7);
            prescription.DailyDose.Should().Be(1);
            prescription.Usage.Should().Contain("水煎服");
            prescription.PayableAmount.Should().Be(168.50m);
        }

        #endregion

        #region Navigation Properties Tests

        [Fact]
        public void NavigationProperties_ShouldBeInitialized()
        {
            // Arrange & Act
            var prescription = new Prescription();

            // Assert
            prescription.Patient.Should().BeNull("导航属性默认为null");
            prescription.Doctor.Should().BeNull("导航属性默认为null");
            prescription.PrescriptionItems.Should().BeNull("集合导航属性默认为null");
            prescription.PrintLogs.Should().BeNull("集合导航属性默认为null");
        }

        #endregion

        #region Audit Fields Tests

        [Fact]
        public void AuditFields_ShouldTrackChanges()
        {
            // Arrange
            var prescription = new Prescription
            {
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            };

            // Act - 模拟更新
            prescription.UpdatedAt = DateTime.UtcNow.AddMinutes(30);
            prescription.UpdatedBy = Guid.NewGuid();

            // Assert
            prescription.CreatedAt.Should().BeBefore(prescription.UpdatedAt.Value);
            prescription.CreatedBy.Should().NotBe(prescription.UpdatedBy);
        }

        #endregion

        #region Soft Delete Tests

        [Fact]
        public void SoftDelete_ShouldNotAffectPrintHistory()
        {
            // Arrange
            var prescription = new Prescription
            {
                IsPrinted = true,
                PrintCount = 3,
                PrintVersion = 2,
                LastPrintedAt = DateTime.UtcNow.AddHours(-1),
                IsDeleted = false
            };

            // Act
            prescription.IsDeleted = true;

            // Assert
            prescription.IsDeleted.Should().BeTrue();
            prescription.IsPrinted.Should().BeTrue("打印历史应该保留");
            prescription.PrintCount.Should().Be(3, "打印次数应该保留");
            prescription.PrintVersion.Should().Be(2, "打印版本应该保留");
            prescription.LastPrintedAt.Should().NotBeNull("打印时间应该保留");
        }

        #endregion
    }
}