using FluentAssertions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Infrastructure.Caching;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultations.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.MedicalCases.Tests.Services
{
    /// <summary>
    /// Phase 3: MedicalCaseStateService单元测试
    /// 测试范围：State Service（状态管理操作）
    /// 业务规则：
    /// - 三步流程验证（辨证->开方标记->处方）
    /// - LIFECYCLE-010: 暂存医案
    /// - LIFECYCLE-011: 取消医案
    /// </summary>
    public class MedicalCaseStateServiceTests : TestBase
    {
        private readonly MedicalCaseStateService _service;
        private readonly IMedicalCaseRepository _repositoryMock;
        private readonly IUserCrossModuleService _userCrossModuleMock;
        private readonly IMedicalCaseAuditService _auditServiceMock;
        private readonly IMedicalCasePermissionService _permissionServiceMock;
        private readonly ILogger<MedicalCaseStateService> _loggerMock;
        private readonly ICacheInvalidationService _cacheInvalidationMock;

        public MedicalCaseStateServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _userCrossModuleMock = CreateMock<IUserCrossModuleService>();
            _auditServiceMock = CreateMock<IMedicalCaseAuditService>();
            _permissionServiceMock = CreateMock<IMedicalCasePermissionService>();
            _loggerMock = CreateLoggerMock<MedicalCaseStateService>();
            _cacheInvalidationMock = CreateMock<ICacheInvalidationService>();

            // 默认: 权限检查通过
            _permissionServiceMock.CanEdit(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<MedicalCaseEntity>())
                .Returns(true);

            _service = new MedicalCaseStateService(
                _repositoryMock,
                _userCrossModuleMock,
                _auditServiceMock,
                _permissionServiceMock,
                _loggerMock,
                _cacheInvalidationMock);
        }

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidStatus_ShouldUpdateStatus()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var newStatus = MedicalCaseStatus.Active;

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Suspended,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.UpdateStatusAsync(medicalCaseId, newStatus);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(newStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithCompletedStatus_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Completed));
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns((MedicalCaseEntity?)null);

            // Act
            var result = await _service.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Active);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CompleteAsync Tests

        [Fact]
        public async Task CompleteAsync_WithAllConditionsMet_ShouldComplete()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    TcmDiagnosis = "风寒感冒"
                },
                Prescription = new PrescriptionEntity
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = false
                }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.CompleteAsync(medicalCaseId, Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task CompleteAsync_WithNoPrescriptionNeeded_ShouldComplete()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = false,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    TcmDiagnosis = "风寒感冒"
                }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.CompleteAsync(medicalCaseId, Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task CompleteAsync_WhenPrescriptionNeededButMissing_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    TcmDiagnosis = "风寒感冒"
                },
                Prescription = null
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CompleteAsync(medicalCaseId, Guid.NewGuid()));
        }

        #endregion

        #region CloseCaseAsync Tests

        [Fact]
        public async Task CloseCaseAsync_WithValidId_ShouldCloseCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task CloseCaseAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns((MedicalCaseEntity?)null);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region SuspendAsync Tests

        [Fact]
        public async Task SuspendAsync_WithValidRequest_ShouldSuspend()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var request = new ConsultationInputDto
            {
                PresentIllness = "头痛",
                TcmDiagnosis = "风寒感冒"
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.SuspendAsync(medicalCaseId, request, operatorId, isAdmin: false);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Suspended);
        }

        [Fact]
        public async Task SuspendAsync_WithoutRequest_ShouldOnlyUpdateStatus()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.SuspendAsync(medicalCaseId, null, operatorId, isAdmin: false);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Suspended);
        }

        #endregion

        #region CancelAsync Tests

        [Fact]
        public async Task CancelAsync_WithValidIdAndReason_ShouldCancelCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var reason = "患者取消就诊";

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.CancelAsync(medicalCaseId, operatorId, isAdmin: false, reason);

            // Assert
            result.Should().NotBeNull();
            result!.IsDeleted.Should().BeTrue("取消操作统一为软删除");
        }

        [Fact]
        public async Task CancelAsync_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Completed,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CancelAsync(medicalCaseId, operatorId, isAdmin: true, "取消原因"));
        }

        [Fact]
        public async Task CancelAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns((MedicalCaseEntity?)null);

            // Act
            var result = await _service.CancelAsync(medicalCaseId, operatorId, isAdmin: false, "取消原因");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
