using FluentAssertions;
using LYBT.Entities.Prescriptions;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.Prescriptions
{
    /// <summary>
    /// Prescription实体单元测试
    /// T2-X8-09: 打印字段已迁移到 MedicalCase 层级
    /// 属性：MedicalCaseId, PrescriptionNumber, DosageCount, Discount,
    ///       Usage, Advice, ReferencedFormulas, Remark
    /// 导航属性：Items (PrescriptionItem)
    /// </summary>
    public class PrescriptionModelTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeDefaultFields()
        {
            // Arrange & Act
            var prescription = new Prescription();

            // Assert
            prescription.Items.Should().NotBeNull("Items集合应初始化为空列表");
            prescription.Items.Should().BeEmpty();
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
        public void SoftDelete_ShouldSetIsDeletedFlag()
        {
            // Arrange
            var prescription = new Prescription
            {
                IsDeleted = false
            };

            // Act
            prescription.IsDeleted = true;

            // Assert
            prescription.IsDeleted.Should().BeTrue();
        }

        #endregion
    }
}
