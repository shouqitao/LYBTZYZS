using AutoMapper;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultations.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;

namespace LYBT.Module.MedicalCases.Tests.Services
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
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly Mock<IPatientRepository> _patientRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMedicalCaseAuditService> _auditServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MedicalCaseCommandService>> _loggerMock;

        public MedicalCaseCommandServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _patientRepositoryMock = CreateMock<IPatientRepository>();
            _userRepositoryMock = CreateMock<IUserRepository>();
            _auditServiceMock = CreateMock<IMedicalCaseAuditService>();
            _mapperMock = CreateMock<IMapper>();
            _loggerMock = CreateLoggerMock<MedicalCaseCommandService>();

            _service = new MedicalCaseCommandService(
                _repositoryMock.Object,
                _patientRepositoryMock.Object,
                _userRepositoryMock.Object,
                _auditServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidPatient_ShouldCreateMedicalCaseAndConsultation()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            var patient = new Patient { Id = patientId, Name = "张三" };
            var doctor = new User { Id = doctorId, RealName = "李医生" };

            _patientRepositoryMock.Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _userRepositoryMock.Setup(x => x.GetByIdAsync(doctorId))
                .ReturnsAsync(doctor);

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());

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

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(createdMedicalCase);

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

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity> { existingActiveCase });

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

            exception.Message.Should().Contain("DoctorId不能为空");
            exception.ParamName.Should().Be("doctorId");
        }

        #endregion

        #region UpdateConsultationAsync Tests

        [Fact]
        public async Task UpdateConsultationAsync_WithValidRequest_ShouldUpdateConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            // OpenSpec: simplify-medicalcase-dataflow - ChiefComplaint已移除，使用PresentIllness
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

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map(request, medicalCase.Consultation));

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

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

            // OpenSpec: simplify-medicalcase-dataflow - 测试非管理员不能编辑已完成医案
            // UserId设为当前用户，确保不会因为"非创建者"而被拒绝
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                UserId = doctorId,
                CaseStatus = MedicalCaseStatus.Completed,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert - 非管理员无法编辑已完成医案，抛出UnauthorizedAccessException
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

            // OpenSpec: simplify-medicalcase-dataflow - PrescriptionEnabled已从Consultation移至MedicalCase.NeedsPrescription
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

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

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

            // OpenSpec: simplify-medicalcase-dataflow - PatientId/UserId已从Prescription移除，通过MedicalCase获取
            var prescription = new PrescriptionEntity
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsFreshAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map<PrescriptionEntity>(request))
                .Returns(prescription);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CreatePrescriptionAsync(medicalCaseId, request);

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCaseId.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task CreatePrescriptionAsync_WhenPrescriptionAlreadyExists_ShouldThrowInvalidOperationException()
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

            _repositoryMock.Setup(x => x.GetByIdWithDetailsFreshAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(medicalCaseId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(medicalCaseId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_WhenNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(medicalCaseId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(medicalCaseId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
