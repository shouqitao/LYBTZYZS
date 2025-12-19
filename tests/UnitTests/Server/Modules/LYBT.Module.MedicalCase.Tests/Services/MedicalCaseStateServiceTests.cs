using AutoMapper;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
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
    /// - 三步流程验证（辨证→开方标记→处方）
    /// - LIFECYCLE-010: 暂存医案
    /// - LIFECYCLE-011: 取消医案
    /// </summary>
    public class MedicalCaseStateServiceTests : TestBase
    {
        private readonly MedicalCaseStateService _service;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMedicalCaseAuditService> _auditServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MedicalCaseStateService>> _loggerMock;

        public MedicalCaseStateServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _userRepositoryMock = CreateMock<IUserRepository>();
            _auditServiceMock = CreateMock<IMedicalCaseAuditService>();
            _mapperMock = CreateMock<IMapper>();
            _loggerMock = CreateLoggerMock<MedicalCaseStateService>();

            _service = new MedicalCaseStateService(
                _repositoryMock.Object,
                _userRepositoryMock.Object,
                _auditServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidStatus_ShouldUpdateStatus()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var newStatus = MedicalCaseStatus.Completed;

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.UpdateStatusAsync(medicalCaseId, newStatus);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(newStatus);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            MedicalCaseEntity? nullCase = null;

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(nullCase!);

            // Act
            var result = await _service.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Completed);

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
                    TCMDiagnosis = "风寒感冒"
                },
                Prescription = new PrescriptionEntity
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = false
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CompleteAsync(medicalCaseId);

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
                    TCMDiagnosis = "风寒感冒"
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CompleteAsync(medicalCaseId);

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
                    TCMDiagnosis = "风寒感冒"
                },
                Prescription = null
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CompleteAsync(medicalCaseId));
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

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CloseCaseAsync_WhenNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            MedicalCaseEntity? nullCase = null;

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(nullCase!);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region SaveDraftAsync Tests

        [Fact]
        public async Task SaveDraftAsync_WithValidRequest_ShouldSaveDraft()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            // OpenSpec: simplify-medicalcase-dataflow - ChiefComplaint已移除，使用PresentIllness
            var request = new ConsultationInputDto
            {
                PresentIllness = "头痛",
                TCMDiagnosis = "风寒感冒"
            };

            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map(request, medicalCase.Consultation));

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.SaveDraftAsync(medicalCaseId, request, operatorId, isAdmin: false);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Draft);
        }

        [Fact]
        public async Task SaveDraftAsync_WithoutRequest_ShouldOnlyUpdateStatus()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.SaveDraftAsync(medicalCaseId, null, operatorId, isAdmin: false);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Draft);
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

            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CancelAsync(medicalCaseId, operatorId, isAdmin: false, reason);

            // Assert
            result.Should().NotBeNull();
            result!.CaseStatus.Should().Be(MedicalCaseStatus.Cancelled);
        }

        [Fact]
        public async Task CancelAsync_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = operatorId,
                CaseStatus = MedicalCaseStatus.Completed,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            // 使用isAdmin=true绕过权限检查，专注测试业务规则（已完成医案不可取消）
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CancelAsync(medicalCaseId, operatorId, isAdmin: true, "取消原因"));
        }

        [Fact]
        public async Task CancelAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            MedicalCaseEntity? nullCase = null;

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(nullCase!);

            // Act
            var result = await _service.CancelAsync(medicalCaseId, operatorId, isAdmin: false, "取消原因");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
