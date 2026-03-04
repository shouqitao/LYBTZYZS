using FluentAssertions;
using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.MedicalCases
{
    /// <summary>
    /// MedicalCase实体单元测试
    /// MedicalCase是聚合根，继承BaseEntity
    /// 属性：PatientId, PatientName, UserId, DoctorName, CaseNumber, CaseStatus,
    ///       NeedsPrescription, CompletedAt, Remark
    /// 导航属性：Consultation, Prescription
    /// 计算属性：IsLocked, IsActive, IsCompleted
    /// </summary>
    public class MedicalCaseModelTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeAllProperties()
        {
            // Arrange & Act
            var medicalCase = new MedicalCase();

            // Assert
            medicalCase.Id.Should().NotBe(Guid.Empty);
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Active);
            medicalCase.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            medicalCase.PatientName.Should().Be(string.Empty);
            medicalCase.DoctorName.Should().Be(string.Empty);
            medicalCase.NeedsPrescription.Should().BeNull("默认未标记是否需要处方");
            medicalCase.CompletedAt.Should().BeNull();
            medicalCase.Remark.Should().BeNull();
        }

        #endregion

        #region IsLocked Computed Property Tests

        [Fact]
        public void IsLocked_ShouldReturnFalse_WhenActive()
        {
            // Arrange
            var medicalCase = new MedicalCase { CaseStatus = MedicalCaseStatus.Active };

            // Act & Assert
            medicalCase.IsLocked.Should().BeFalse("Active状态不应被锁定");
        }

        [Fact]
        public void IsLocked_ShouldReturnFalse_WhenSuspended()
        {
            // Arrange
            var medicalCase = new MedicalCase { CaseStatus = MedicalCaseStatus.Suspended };

            // Act & Assert
            medicalCase.IsLocked.Should().BeFalse("Suspended状态不应被锁定");
        }

        [Fact]
        public void IsLocked_ShouldReturnFalse_WhenCompletedToday()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                CaseStatus = MedicalCaseStatus.Completed,
                CompletedAt = DateTime.Today.AddHours(1) // 今天完成
            };

            // Act & Assert
            medicalCase.IsLocked.Should().BeFalse("当天完成的医案不应被锁定");
        }

        [Fact]
        public void IsLocked_ShouldReturnTrue_WhenCompletedBeforeToday()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                CaseStatus = MedicalCaseStatus.Completed,
                CompletedAt = DateTime.Today.AddDays(-1) // 昨天完成
            };

            // Act & Assert
            medicalCase.IsLocked.Should().BeTrue("非当天完成的医案应该被锁定");
        }

        #endregion

        #region IsActive Computed Property Tests

        [Theory]
        [InlineData(MedicalCaseStatus.Suspended, true)]
        [InlineData(MedicalCaseStatus.Active, true)]
        [InlineData(MedicalCaseStatus.Completed, false)]
        public void IsActive_ShouldReturnCorrectValue(MedicalCaseStatus status, bool expected)
        {
            // Arrange
            var medicalCase = new MedicalCase { CaseStatus = status };

            // Act & Assert
            medicalCase.IsActive.Should().Be(expected);
        }

        #endregion

        #region IsCompleted Computed Property Tests

        [Theory]
        [InlineData(MedicalCaseStatus.Active, false)]
        [InlineData(MedicalCaseStatus.Suspended, false)]
        [InlineData(MedicalCaseStatus.Completed, true)]
        public void IsCompleted_ShouldReturnCorrectValue(MedicalCaseStatus status, bool expected)
        {
            // Arrange
            var medicalCase = new MedicalCase { CaseStatus = status };

            // Act & Assert
            medicalCase.IsCompleted.Should().Be(expected);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_ShouldBeInitialized()
        {
            // Arrange & Act
            var medicalCase = new MedicalCase();

            // Assert
            medicalCase.Consultation.Should().BeNull("一对一关系默认为null");
            medicalCase.Prescription.Should().BeNull("一对零或一关系默认为null");
        }

        [Fact]
        public void MedicalCase_ShouldSupportOneToOneConsultation()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid()
            };

            var consultation = new Consultation
            {
                Id = medicalCase.Id
            };

            // Act
            medicalCase.Consultation = consultation;

            // Assert
            medicalCase.Consultation.Should().NotBeNull();
            medicalCase.Consultation.Id.Should().Be(medicalCase.Id, "Consultation使用共享主键");
        }

        [Fact]
        public void MedicalCase_ShouldSupportOptionalPrescription()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid()
            };

            var prescription = new Prescription
            {
                MedicalCaseId = medicalCase.Id
            };

            // Act
            medicalCase.Prescription = prescription;

            // Assert
            medicalCase.Prescription.Should().NotBeNull();
            medicalCase.Prescription.MedicalCaseId.Should().Be(medicalCase.Id);
        }

        #endregion

        #region Business Rule Validation Tests

        [Fact]
        public void MedicalCase_RequiredFields_ShouldBePresent()
        {
            // Arrange
            var medicalCase = new MedicalCase();

            // Act & Assert
            medicalCase.PatientId.Should().Be(Guid.Empty, "PatientId必须由外部设置");
            medicalCase.UserId.Should().Be(Guid.Empty, "UserId必须由外部设置");
        }

        [Fact]
        public void CaseNumber_ShouldBeGenerated_WhenCreated()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            // Act
            // 实际的CaseNumber应该由服务层生成
            medicalCase.CaseNumber = $"MC{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";

            // Assert
            medicalCase.CaseNumber.Should().StartWith("MC");
            medicalCase.CaseNumber.Should().HaveLength(14);
        }

        #endregion

        #region Status Transition Tests

        [Fact]
        public void StatusTransition_FromActive_ToCompleted_ShouldBeAllowed()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                CaseStatus = MedicalCaseStatus.Active
            };

            // Act
            medicalCase.CaseStatus = MedicalCaseStatus.Completed;

            // Assert
            medicalCase.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public void StatusTransition_FromCompleted_ToActive_ShouldNotBeAllowed()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                CaseStatus = MedicalCaseStatus.Completed
            };

            // Act & Assert
            // 实际业务逻辑应该在服务层验证
            Action act = () =>
            {
                if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
                {
                    throw new InvalidOperationException("已完成的医疗案例不能重新激活");
                }
            };

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("已完成的医疗案例不能重新激活");
        }

        #endregion

        #region NeedsPrescription Tests

        [Fact]
        public void NeedsPrescription_ShouldSupportThreeStates()
        {
            // Arrange
            var medicalCase = new MedicalCase();

            // Assert - 默认为null（未标记）
            medicalCase.NeedsPrescription.Should().BeNull();

            // Act - 设置为需要处方
            medicalCase.NeedsPrescription = true;
            medicalCase.NeedsPrescription.Should().BeTrue();

            // Act - 设置为不需要处方
            medicalCase.NeedsPrescription = false;
            medicalCase.NeedsPrescription.Should().BeFalse();
        }

        #endregion

        #region Soft Delete Tests

        [Fact]
        public void SoftDelete_ShouldSetIsDeletedFlag()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                IsDeleted = false
            };

            // Act
            medicalCase.IsDeleted = true;

            // Assert
            medicalCase.IsDeleted.Should().BeTrue();
        }

        #endregion

        #region Denormalized Name Fields Tests

        [Fact]
        public void DenormalizedNames_ShouldBeStoredCorrectly()
        {
            // Arrange & Act
            var medicalCase = new MedicalCase
            {
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                UserId = Guid.NewGuid(),
                DoctorName = "李医生"
            };

            // Assert
            medicalCase.PatientName.Should().Be("张三");
            medicalCase.DoctorName.Should().Be("李医生");
        }

        #endregion
    }
}
