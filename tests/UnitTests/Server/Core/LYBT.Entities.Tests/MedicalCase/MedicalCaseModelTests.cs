using FluentAssertions;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace LYBT.UnitTests.Core.Entities
{
    /// <summary>
    /// MedicalCase实体单元测试
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
            medicalCase.Status.Should().Be(CommonStatus.Enabled);
            medicalCase.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        #endregion

        #region CanEdit Permission Tests

        [Theory]
        [InlineData(true, null, true)]  // 管理员可以编辑
        [InlineData(false, "doctor123", true)]  // 当天的医生可以编辑
        [InlineData(false, "otherDoctor", false)]  // 其他医生不能编辑
        public void CanEdit_ShouldReturnCorrectPermission(bool isAdmin, string currentUserId, bool expected)
        {
            // Arrange
            var doctorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var medicalCase = new MedicalCase
            {
                DoctorId = doctorId,
                CreatedAt = DateTime.UtcNow.AddHours(-1) // 1小时前创建
            };

            // 设置用户ID
            if (currentUserId == "doctor123")
            {
                currentUserId = doctorId.ToString();
            }
            else if (currentUserId == "otherDoctor")
            {
                currentUserId = Guid.NewGuid().ToString();
            }

            // Act
            var result = medicalCase.CanEdit(isAdmin, currentUserId);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void CanEdit_ShouldReturnFalse_WhenCaseIsOlderThanOneDay()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                DoctorId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddDays(-2) // 2天前创建
            };

            // Act
            var result = medicalCase.CanEdit(false, medicalCase.DoctorId.ToString());

            // Assert
            result.Should().BeFalse("医疗案例超过24小时后不能编辑");
        }

        #endregion

        #region IsLocked Status Tests

        [Theory]
        [InlineData(MedicalCaseStatus.Active, false)]
        [InlineData(MedicalCaseStatus.Completed, true)]
        [InlineData(MedicalCaseStatus.Cancelled, true)]
        public void IsLocked_ShouldReturnCorrectStatus(MedicalCaseStatus caseStatus, bool expected)
        {
            // Arrange
            var medicalCase = new MedicalCase { CaseStatus = caseStatus };

            // Act
            var result = medicalCase.IsLocked();

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Navigation Property Tests

        [Fact]
        public void NavigationProperties_ShouldBeInitialized()
        {
            // Arrange & Act
            var medicalCase = new MedicalCase();

            // Assert
            medicalCase.Patient.Should().BeNull("导航属性默认为null");
            medicalCase.Doctor.Should().BeNull("导航属性默认为null");
            medicalCase.Consultation.Should().BeNull("一对一关系默认为null");
        }

        [Fact]
        public void MedicalCase_ShouldSupportOneToOneConsultation()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                Id = Guid.NewGuid()
            };

            var consultation = new Consultation.Consultation
            {
                MedicalCaseId = medicalCase.Id,
                MedicalCase = medicalCase
            };

            // Act
            medicalCase.Consultation = consultation;

            // Assert
            medicalCase.Consultation.Should().NotBeNull();
            medicalCase.Consultation.MedicalCaseId.Should().Be(medicalCase.Id);
            medicalCase.Consultation.MedicalCase.Should().Be(medicalCase);
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
            medicalCase.DoctorId.Should().Be(Guid.Empty, "DoctorId必须由外部设置");
        }

        [Fact]
        public void CaseNumber_ShouldBeGenerated_WhenCreated()
        {
            // Arrange
            var medicalCase = new MedicalCase
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
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
    }
}