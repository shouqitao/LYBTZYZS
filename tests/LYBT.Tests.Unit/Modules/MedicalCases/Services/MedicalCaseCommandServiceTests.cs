using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultations.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.DTOs.Users;

namespace LYBT.Tests.Unit.Modules.MedicalCases.Services
{
    /// <summary>
    /// Phase 3: MedicalCaseCommandService单元测试
    /// 测试范围：Command Service（写操作）
    /// 业务规则：
    /// - BR-001: 单患者仅一条未完成病案
    /// - AR-003: 一诊一方约束
    /// </summary>
    public class MedicalCaseCommandServiceTests : TestBase
    {
        private readonly MedicalCaseCommandService _service;
        private readonly IMedicalCaseRepository _repositoryMock;
        private readonly IPatientCrossModuleService _patientCrossModuleMock;
        private readonly IUserCrossModuleService _userCrossModuleMock;
        private readonly IHerbCrossModuleService _herbCrossModuleMock;
        private readonly IMedicalCaseAuditService _auditServiceMock;
        private readonly IMedicalCasePermissionService _permissionServiceMock;
        private readonly ILogger<MedicalCaseCommandService> _loggerMock;
        private readonly ICacheInvalidationService _cacheInvalidationMock;

        public MedicalCaseCommandServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _patientCrossModuleMock = CreateMock<IPatientCrossModuleService>();
            _userCrossModuleMock = CreateMock<IUserCrossModuleService>();
            _herbCrossModuleMock = CreateMock<IHerbCrossModuleService>();
            _auditServiceMock = CreateMock<IMedicalCaseAuditService>();
            _permissionServiceMock = CreateMock<IMedicalCasePermissionService>();
            _loggerMock = CreateLoggerMock<MedicalCaseCommandService>();
            _cacheInvalidationMock = CreateMock<ICacheInvalidationService>();

            // 默认: 权限检查通过
            _permissionServiceMock.CanEdit(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<MedicalCaseEntity>())
                .Returns(true);
            _permissionServiceMock.CanDelete(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<MedicalCaseEntity>())
                .Returns(true);

            // 默认: 药材价格查询返回空
            _herbCrossModuleMock.GetHerbPricesAsync(Arg.Any<IEnumerable<Guid>>())
                .Returns(new Dictionary<Guid, decimal>());

            _service = new MedicalCaseCommandService(
                _repositoryMock,
                _patientCrossModuleMock,
                _userCrossModuleMock,
                _herbCrossModuleMock,
                _auditServiceMock,
                _permissionServiceMock,
                _loggerMock,
                _cacheInvalidationMock);
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidPatient_ShouldCreateMedicalCaseAndConsultation()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            var patient = new PatientBasicDto { Id = patientId, Name = "张三" };
            var doctor = new UserBasicDto { Id = doctorId, RealName = "李医生" };

            _patientCrossModuleMock.GetPatientBasicInfoAsync(patientId)
                .Returns(patient);

            _userCrossModuleMock.GetUserBasicInfoAsync(doctorId)
                .Returns(doctor);

            _repositoryMock.GetByPatientIdAsync(patientId)
                .Returns(new List<MedicalCaseEntity>());

            var createdMedicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "张三",
                UserId = doctorId,
                DoctorName = "李医生",
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = Guid.NewGuid() }
            };

            _repositoryMock.AddAsync(Arg.Any<MedicalCaseEntity>())
                .Returns(createdMedicalCase);

            // Act
            var result = await _service.CreateAsync(patientId, visitDate, doctorId);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(patientId);
            result.UserId.Should().Be(doctorId);
            result.PatientName.Should().Be("张三");
            result.DoctorName.Should().Be("李医生");
            result.CaseStatus.Should().Be(MedicalCaseStatus.Active);
        }

        [Fact]
        public async Task CreateAsync_WhenPatientHasActiveCase_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var visitDate = DateTime.Now;
            var existingActiveCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                CaseStatus = MedicalCaseStatus.Active
            };

            _repositoryMock.GetByPatientIdAsync(patientId)
                .Returns(new List<MedicalCaseEntity> { existingActiveCase });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(patientId, visitDate, doctorId));
        }

        [Fact]
        public async Task CreateAsync_WhenDoctorIdIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var visitDate = DateTime.Now;
            var emptyDoctorId = Guid.Empty;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(patientId, visitDate, emptyDoctorId));

            exception.Message.Should().Contain("不能为空");
        }

        #endregion

        #region UpdateConsultationAsync Tests

        [Fact]
        public async Task UpdateConsultationAsync_WithValidRequest_ShouldUpdateConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new ConsultationInputDto
            {
                PresentIllness = "头痛",
                TcmDiagnosis = "风寒感冒"
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId
                }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.UpdateConsultationAsync(medicalCaseId, request, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateConsultationAsync_WhenStatusNotActive_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var request = new ConsultationInputDto();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = doctorId,
                CaseStatus = MedicalCaseStatus.Completed,
                CreatedAt = DateTime.Today.AddDays(-1),
                CompletedAt = DateTime.Today.AddDays(-1),
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            // 覆盖默认 mock: 非管理员不能编辑已完成(跨日锁定)的医案
            _permissionServiceMock.CanEdit(doctorId, false, medicalCase)
                .Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.UpdateConsultationAsync(medicalCaseId, request, doctorId, isAdmin: false));
        }

        #endregion

        #region SetPrescriptionFlagAsync Tests

        [Fact]
        public async Task SetPrescriptionFlagAsync_WithValidRequest_ShouldUpdateFlag()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var needsPrescription = true;

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = false,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId
                }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.SetPrescriptionFlagAsync(medicalCaseId, needsPrescription, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().NotBeNull();
            result!.NeedsPrescription.Should().BeTrue();
        }

        #endregion

        #region CreatePrescriptionAsync Tests

        [Fact]
        public async Task CreatePrescriptionAsync_WithValidRequest_ShouldCreatePrescription()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var request = new PrescriptionInputDto
            {
                Items = new List<PrescriptionItemInputDto>()
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = patientId,
                UserId = doctorId,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    TcmDiagnosis = "风寒感冒"
                }
            };

            _repositoryMock.GetByIdWithDetailsFreshAsync(medicalCaseId)
                .Returns(medicalCase);

            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.CreatePrescriptionAsync(medicalCaseId, request);

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCaseId.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task CreatePrescriptionAsync_WhenPrescriptionAlreadyExists_ShouldThrowBusinessException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new PrescriptionInputDto();

            var existingPrescription = new PrescriptionEntity
            {
                Id = Guid.NewGuid(),
                IsDeleted = false
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                CaseStatus = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Prescription = existingPrescription,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    TcmDiagnosis = "风寒感冒"
                }
            };

            _repositoryMock.GetByIdWithDetailsFreshAsync(medicalCaseId)
                .Returns(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<BusinessException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity { Id = medicalCaseId, UserId = operatorId };

            _repositoryMock.GetByIdAsync(medicalCaseId)
                .Returns(medicalCase);
            _repositoryMock.DeleteAsync(medicalCaseId)
                .Returns(true);

            // Act
            var result = await _service.DeleteAsync(medicalCaseId, operatorId, isAdmin: false);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();

            _repositoryMock.GetByIdAsync(medicalCaseId)
                .ReturnsNull();

            // Act
            var result = await _service.DeleteAsync(medicalCaseId, operatorId, isAdmin: false);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
