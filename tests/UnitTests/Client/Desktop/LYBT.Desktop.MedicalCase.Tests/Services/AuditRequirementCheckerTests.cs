using FluentAssertions;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Services
{
    /// <summary>
    /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010)
    /// AuditRequirementChecker单元测试 - 验证审计需求判断逻辑
    /// </summary>
    public class AuditRequirementCheckerTests
    {
        private readonly Mock<ILogger<AuditRequirementChecker>> _mockLogger;
        private readonly AuditRequirementChecker _sut;

        public AuditRequirementCheckerTests()
        {
            _mockLogger = new Mock<ILogger<AuditRequirementChecker>>();
            _sut = new AuditRequirementChecker(_mockLogger.Object);
        }

        #region Completed Status Tests

        [Fact]
        public void IsAuditRequired_WhenStatusIsCompleted_ReturnsTrue()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = currentUserId,
                CaseStatus = MedicalCaseStatus.Completed,
                CreatedAt = DateTime.Now  // 今天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeTrue("Completed医案修改必须审计");
        }

        [Fact]
        public void IsAuditRequired_WhenStatusIsDraft_ReturnsFalse()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = currentUserId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Now  // 今天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeFalse("Draft状态、同一用户、同一天修改不需要审计");
        }

        #endregion

        #region Non-Owner Modification Tests

        [Fact]
        public void IsAuditRequired_WhenNonOwnerModifies_ReturnsTrue()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();  // 不同用户
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = ownerId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Now  // 今天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeTrue("非创建者修改医案必须审计");
        }

        [Fact]
        public void IsAuditRequired_WhenOwnerModifies_ReturnsFalse()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = ownerId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Now  // 今天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, ownerId);

            // Assert
            result.Should().BeFalse("创建者同一天修改Draft医案不需要审计");
        }

        #endregion

        #region Different Day Modification Tests

        [Fact]
        public void IsAuditRequired_WhenModifyingPreviousDayCase_ReturnsTrue()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = currentUserId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Today.AddDays(-1)  // 昨天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeTrue("跨日修改医案必须审计");
        }

        [Fact]
        public void IsAuditRequired_WhenModifyingSameDayCase_ReturnsFalse()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = currentUserId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Today  // 今天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeFalse("同一天创建者修改Draft医案不需要审计");
        }

        [Fact]
        public void IsAuditRequired_WhenModifyingOldCase_ReturnsTrue()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = currentUserId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Today.AddDays(-30)  // 一个月前创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeTrue("修改历史医案必须审计");
        }

        #endregion

        #region Combined Scenarios Tests

        [Fact]
        public void IsAuditRequired_CompletedStatus_AlwaysReturnsTrue_EvenForOwnerSameDay()
        {
            // Arrange - Completed状态优先级最高
            var currentUserId = Guid.NewGuid();
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = currentUserId,  // 同一用户
                CaseStatus = MedicalCaseStatus.Completed,
                CreatedAt = DateTime.Now  // 今天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeTrue("即使是创建者同一天修改Completed医案也必须审计");
        }

        [Fact]
        public void IsAuditRequired_NonOwnerPreviousDay_ReturnsTrue()
        {
            // Arrange - 多重条件都触发
            var ownerId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();  // 不同用户
            var medicalCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                DoctorId = ownerId,
                CaseStatus = MedicalCaseStatus.Draft,
                CreatedAt = DateTime.Today.AddDays(-1)  // 昨天创建
            };

            // Act
            var result = _sut.IsAuditRequired(medicalCase, currentUserId);

            // Assert
            result.Should().BeTrue("非创建者跨日修改必须审计");
        }

        #endregion
    }
}
