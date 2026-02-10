using FluentAssertions;
using LYBT.Entities.Prescriptions;
using Xunit;

namespace LYBT.UnitTests.Core.Entities
{
    /// <summary>
    /// Prescription实体单元测试 - 包含打印版本管理功能
    /// Prescription继承BaseEntity
    /// 属性：MedicalCaseId, PrescriptionNumber, DosageCount, Discount,
    ///       Usage, Advice, ReferencedFormulas, Remark,
    ///       PrintVersion, LastPrintedAt, PrintCount, IsPrinted
    /// 导航属性：Items (PrescriptionItem), PrintLogs (PrescriptionPrintLog)
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
            prescription.Items.Should().NotBeNull("Items集合应初始化为空列表");
            prescription.Items.Should().BeEmpty();
            prescription.PrintLogs.Should().NotBeNull("PrintLogs集合应初始化为空列表");
            prescription.PrintLogs.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_ShouldInitializeBusinessFields()
        {
            // Arrange & Act
            var prescription = new Prescription();

            // Assert
            prescription.DosageCount.Should().Be(7, "默认帖数为7");
            prescription.Discount.Should().Be(1.0m, "默认折扣为1（不打折）");
            prescription.PrescriptionNumber.Should().BeNull();
            prescription.Usage.Should().BeNull();
            prescription.Advice.Should().BeNull();
            prescription.ReferencedFormulas.Should().BeNull();
            prescription.Remark.Should().BeNull();
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

        #endregion

        #region Prescription Content Tests

        [Fact]
        public void PrescriptionContent_ShouldBeStoredCorrectly()
        {
            // Arrange
            var prescription = new Prescription();

            // Act
            prescription.PrescriptionNumber = $"RX-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            prescription.DosageCount = 7;
            prescription.Discount = 0.8m;
            prescription.Usage = "水煎服，每日一剂，分两次温服";
            prescription.Advice = "忌辛辣生冷";
            prescription.ReferencedFormulas = "逍遥散,六味地黄丸";

            // Assert
            prescription.PrescriptionNumber.Should().StartWith("RX-");
            prescription.DosageCount.Should().Be(7);
            prescription.Discount.Should().Be(0.8m);
            prescription.Usage.Should().Contain("水煎服");
            prescription.Advice.Should().Contain("忌辛辣");
            prescription.ReferencedFormulas.Should().Contain("逍遥散");
        }

        #endregion

        #region Items Navigation Property Tests

        [Fact]
        public void Items_ShouldBeInitializedAsEmptyCollection()
        {
            // Arrange & Act
            var prescription = new Prescription();

            // Assert
            prescription.Items.Should().NotBeNull("Items集合应初始化为空列表");
            prescription.Items.Should().BeEmpty();
        }

        [Fact]
        public void Items_ShouldSupportAddingPrescriptionItems()
        {
            // Arrange
            var prescription = new Prescription { Id = Guid.NewGuid() };
            var item = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                HerbId = Guid.NewGuid(),
                HerbName = "当归",
                Dosage = 12,
                Unit = "g",
                UnitPrice = 2.5m
            };

            // Act
            prescription.Items.Add(item);

            // Assert
            prescription.Items.Should().HaveCount(1);
            prescription.Items.First().HerbName.Should().Be("当归");
            prescription.Items.First().PrescriptionId.Should().Be(prescription.Id);
        }

        #endregion

        #region MedicalCaseId Tests

        [Fact]
        public void MedicalCaseId_ShouldBeRequired()
        {
            // Arrange
            var prescription = new Prescription();

            // Assert - 默认为空Guid
            prescription.MedicalCaseId.Should().Be(Guid.Empty, "MedicalCaseId必须由外部设置");

            // Act
            var caseId = Guid.NewGuid();
            prescription.MedicalCaseId = caseId;

            // Assert
            prescription.MedicalCaseId.Should().Be(caseId);
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
            prescription.CreatedBy.Should().NotBe(prescription.UpdatedBy!.Value);
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
