using FluentAssertions;
using LYBT.Entities.Consultations;
using Xunit;

namespace LYBT.UnitTests.Core.Entities
{
    /// <summary>
    /// Consultation实体单元测试
    /// Consultation继承BaseEntity，使用共享主键（Id与MedicalCase相同）
    /// 当前字段：PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis
    /// 审计字段继承自BaseEntity
    /// </summary>
    public class ConsultationModelTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var consultation = new Consultation();

            // Assert - BaseEntity defaults
            consultation.Id.Should().NotBe(Guid.Empty, "Id由BaseEntity自动生成");
            consultation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            consultation.IsDeleted.Should().BeFalse();

            // Assert - Consultation-specific fields default to null
            consultation.PresentIllness.Should().BeNull();
            consultation.TongueDiagnosis.Should().BeNull();
            consultation.PulseDiagnosis.Should().BeNull();
            consultation.TcmDiagnosis.Should().BeNull();
        }

        [Fact]
        public void Consultation_ShouldInheritBaseEntityId()
        {
            // Arrange
            var sharedId = Guid.NewGuid();

            // Act
            var consultation = new Consultation { Id = sharedId };

            // Assert
            consultation.Id.Should().Be(sharedId, "Consultation的Id与MedicalCase共享主键");
        }

        #endregion

        #region Cascade Delete Tests

        [Fact]
        public void Consultation_SoftDelete_ShouldSetIsDeletedFlag()
        {
            // Arrange
            var consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                IsDeleted = false
            };

            // Act
            consultation.IsDeleted = true;

            // Assert
            consultation.IsDeleted.Should().BeTrue("Consultation应该支持软删除");
        }

        #endregion

        #region TCM Diagnosis Tests

        [Fact]
        public void TcmDiagnosis_ShouldSupportFourDiagnosticMethods()
        {
            // Arrange
            var consultation = new Consultation();

            // Act - 舌诊、脉诊
            consultation.TongueDiagnosis = "舌淡红，苔薄白";
            consultation.PulseDiagnosis = "脉象浮数";

            // Assert
            consultation.TongueDiagnosis.Should().Contain("舌");
            consultation.PulseDiagnosis.Should().Contain("脉");
        }

        [Fact]
        public void TcmDiagnosis_ShouldStoreCorrectly()
        {
            // Arrange
            var consultation = new Consultation();

            // Act
            consultation.TcmDiagnosis = "风寒感冒";

            // Assert
            consultation.TcmDiagnosis.Should().Contain("感冒");
        }

        #endregion

        #region Medical Information Tests

        [Fact]
        public void PresentIllness_ShouldBeStoredCorrectly()
        {
            // Arrange
            var consultation = new Consultation();

            // Act
            consultation.PresentIllness = "患者3天前开始出现头痛，伴有发热，体温最高38.5\u2103";

            // Assert
            consultation.PresentIllness.Should().Contain("发热");
        }

        [Fact]
        public void AllFields_ShouldBeIndependent()
        {
            // Arrange & Act
            var consultation = new Consultation
            {
                PresentIllness = "现病史内容",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "脉弦数",
                TcmDiagnosis = "肝火上炎"
            };

            // Assert
            consultation.PresentIllness.Should().Be("现病史内容");
            consultation.TongueDiagnosis.Should().Be("舌红苔黄");
            consultation.PulseDiagnosis.Should().Be("脉弦数");
            consultation.TcmDiagnosis.Should().Be("肝火上炎");
        }

        #endregion

        #region Audit Fields Tests

        [Fact]
        public void AuditFields_ShouldBePresent()
        {
            // Arrange & Act
            var consultation = new Consultation
            {
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            };

            // Assert
            consultation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            consultation.CreatedBy.Should().NotBe(Guid.Empty);
            consultation.UpdatedAt.Should().BeNull("UpdatedAt应该在更新时设置");
            consultation.UpdatedBy.Should().BeNull("UpdatedBy应该在更新时设置");
        }

        #endregion

        #region Nullable Properties Tests

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var consultation = new Consultation
            {
                PresentIllness = "有内容",
                TongueDiagnosis = "有内容",
                PulseDiagnosis = "有内容",
                TcmDiagnosis = "有内容"
            };

            // Act
            consultation.PresentIllness = null;
            consultation.TongueDiagnosis = null;
            consultation.PulseDiagnosis = null;
            consultation.TcmDiagnosis = null;

            // Assert
            consultation.PresentIllness.Should().BeNull();
            consultation.TongueDiagnosis.Should().BeNull();
            consultation.PulseDiagnosis.Should().BeNull();
            consultation.TcmDiagnosis.Should().BeNull();
        }

        #endregion

        #region Shared Primary Key Tests

        [Fact]
        public void Consultation_Id_CanBeSetToMatchMedicalCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            // Act
            var consultation = new Consultation { Id = medicalCaseId };

            // Assert
            consultation.Id.Should().Be(medicalCaseId, "Consultation使用与MedicalCase相同的Id作为共享主键");
        }

        #endregion
    }
}
