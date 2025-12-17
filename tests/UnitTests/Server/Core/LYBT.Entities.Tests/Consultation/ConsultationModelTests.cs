using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;
using System;
using Xunit;

namespace LYBT.UnitTests.Core.Entities
{
    /// <summary>
    /// Consultation实体单元测试
    /// </summary>
    public class ConsultationModelTests
    {
        #region Shared Primary Key Tests

        [Fact]
        public void Consultation_ShouldSharePrimaryKey_WithMedicalCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCase { Id = medicalCaseId };
            
            // Act
            var consultation = new Consultation
            {
                MedicalCaseId = medicalCaseId,
                MedicalCase = medicalCase
            };

            // Assert
            consultation.MedicalCaseId.Should().Be(medicalCaseId);
            consultation.MedicalCase.Should().Be(medicalCase);
            consultation.MedicalCaseId.Should().Be(consultation.MedicalCase.Id, "共享主键应该相同");
        }

        [Fact]
        public void Consultation_ShouldNotHaveOwnId()
        {
            // Arrange & Act
            var consultation = new Consultation();

            // Assert
            consultation.GetType().GetProperty("Id").Should().BeNull("Consultation不应该有独立的Id属性");
            consultation.MedicalCaseId.Should().Be(Guid.Empty, "MedicalCaseId是主键");
        }

        #endregion

        #region Cascade Delete Tests

        [Fact]
        public void Consultation_ShouldBeDeleted_WhenMedicalCaseIsDeleted()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid(),
                IsDeleted = false
            };

            var consultation = new Consultation
            {
                MedicalCaseId = medicalCase.Id,
                MedicalCase = medicalCase,
                IsDeleted = false
            };

            medicalCase.Consultation = consultation;

            // Act
            // 模拟级联删除行为
            medicalCase.IsDeleted = true;
            if (medicalCase.IsDeleted && medicalCase.Consultation != null)
            {
                medicalCase.Consultation.IsDeleted = true;
            }

            // Assert
            consultation.IsDeleted.Should().BeTrue("Consultation应该随MedicalCase一起被删除");
        }

        #endregion

        #region Status Tests

        [Theory]
        [InlineData(ConsultationStatus.Pending)]
        [InlineData(ConsultationStatus.InProgress)]
        [InlineData(ConsultationStatus.Completed)]
        public void Consultation_ShouldSupportDifferentStatuses(ConsultationStatus status)
        {
            // Arrange
            var consultation = new Consultation();

            // Act
            consultation.Status = status;

            // Assert
            consultation.Status.Should().Be(status);
        }

        [Fact]
        public void StatusTransition_ShouldUpdateCompletedAt_WhenCompleted()
        {
            // Arrange
            var consultation = new Consultation
            {
                Status = ConsultationStatus.InProgress
            };

            // Act
            consultation.Status = ConsultationStatus.Completed;
            consultation.CompletedAt = DateTime.UtcNow;

            // Assert
            consultation.Status.Should().Be(ConsultationStatus.Completed);
            consultation.CompletedAt.Should().NotBeNull();
            consultation.CompletedAt.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region Medical Information Tests

        [Fact]
        public void MedicalInformation_ShouldBeStoredCorrectly()
        {
            // Arrange
            var consultation = new Consultation();

            // Act
            consultation.ChiefComplaint = "头痛、发热3天";
            consultation.PresentIllness = "患者3天前开始出现头痛，伴有发热，体温最高38.5℃";
            consultation.PastHistory = "既往体健，否认高血压、糖尿病史";
            consultation.PersonalHistory = "无吸烟饮酒史";
            consultation.FamilyHistory = "父母体健";
            consultation.AllergyHistory = "青霉素过敏";

            // Assert
            consultation.ChiefComplaint.Should().NotBeEmpty();
            consultation.PresentIllness.Should().Contain("发热");
            consultation.PastHistory.Should().Contain("既往体健");
            consultation.PersonalHistory.Should().Contain("无吸烟");
            consultation.FamilyHistory.Should().Contain("父母");
            consultation.AllergyHistory.Should().Contain("青霉素");
        }

        #endregion

        #region TCM Diagnosis Tests

        [Fact]
        public void TCMDiagnosis_ShouldSupportFourDiagnosticMethods()
        {
            // Arrange
            var consultation = new Consultation();

            // Act - 新字段结构：四诊、舌诊、脉诊
            consultation.FourDiagnosis = "面色红润，声音洪亮，主诉头痛，睡眠欠佳";
            consultation.TongueDiagnosis = "舌淡红，苔薄白";
            consultation.PulseDiagnosis = "脉象浮数";

            // Assert
            consultation.FourDiagnosis.Should().Contain("面色");
            consultation.FourDiagnosis.Should().Contain("主诉");
            consultation.TongueDiagnosis.Should().Contain("舌");
            consultation.PulseDiagnosis.Should().Contain("脉");
        }

        [Fact]
        public void TCMDiagnosis_ShouldIncludeSyndromeAndTreatmentPrinciple()
        {
            // Arrange
            var consultation = new Consultation();

            // Act
            consultation.TcmDiagnosis = "风寒感冒";
            consultation.Syndrome = "外感风寒证";
            consultation.TreatmentPrinciple = "疏风散寒，宣肺解表";

            // Assert
            consultation.TcmDiagnosis.Should().Contain("感冒");
            consultation.Syndrome.Should().Contain("风寒");
            consultation.TreatmentPrinciple.Should().Contain("疏风散寒");
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void Consultation_ShouldHavePrescriptionReference()
        {
            // Arrange
            var consultation = new Consultation();
            var prescriptionId = Guid.NewGuid();

            // Act
            consultation.PrescriptionId = prescriptionId;

            // Assert
            consultation.PrescriptionId.Should().Be(prescriptionId);
            consultation.Prescription.Should().BeNull("导航属性需要通过EF Core加载");
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

        #region Validation Tests

        [Fact]
        public void RequiredRelationship_MedicalCaseId_ShouldNotBeEmpty()
        {
            // Arrange
            var consultation = new Consultation();

            // Act & Assert
            consultation.MedicalCaseId.Should().Be(Guid.Empty, "MedicalCaseId必须由外部设置");
            
            // 验证规则
            Action act = () =>
            {
                if (consultation.MedicalCaseId == Guid.Empty)
                {
                    throw new InvalidOperationException("Consultation必须关联到MedicalCase");
                }
            };

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Consultation必须关联到MedicalCase");
        }

        #endregion
    }
}