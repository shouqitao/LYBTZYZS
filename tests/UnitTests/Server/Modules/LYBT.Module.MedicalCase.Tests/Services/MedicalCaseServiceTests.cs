using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Dtos;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// Epic #1612：MedicalCaseService单元测试
    /// 测试范围：14个新方法（Write Layer 8 + Read Layer 4 + Helper Layer 2）
    /// 业务规则：
    /// - BR-001: 单患者仅一条未完成病案
    /// - BF-002: 三步流程验证（辨证→开方标记→处方）
    /// - AR-003: 一诊一方约束
    /// </summary>
    public class MedicalCaseServiceTests : TestBase
    {
        private readonly MedicalCaseService _service;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;

        public MedicalCaseServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _mapperMock = CreateMock<IMapper>();
            _loggerMock = CreateLoggerMock<MedicalCaseService>();

            _service = new MedicalCaseService(
                _repositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Write Layer Tests - CreateAsync

        [Fact]
        public async Task CreateAsync_WithValidPatient_ShouldCreateMedicalCaseAndConsultation()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var visitDate = DateTime.Now;
            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());

            var createdMedicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = Guid.NewGuid() }
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(createdMedicalCase);

            // Act
            var result = await _service.CreateAsync(patientId, visitDate);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(patientId);
            result.Status.Should().Be(MedicalCaseStatus.Active);
        }

        [Fact]
        public async Task CreateAsync_WhenPatientHasActiveCase_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var visitDate = DateTime.Now;
            var existingActiveCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Status = MedicalCaseStatus.Active
            };

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity> { existingActiveCase });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(patientId, visitDate));
        }

        #endregion

        #region Write Layer Tests - UpdateConsultationAsync

        [Fact]
        public async Task UpdateConsultationAsync_WithValidRequest_ShouldUpdateConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new UpdateConsultationRequest
            {
                ChiefComplaint = "头痛",
                TCMDiagnosis = "风寒感冒"
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = null
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map(request, medicalCase.Consultation));

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.UpdateConsultationAsync(medicalCaseId, request);

            // Assert
            result.Should().NotBeNull();
            result!.Consultation!.Step1CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateConsultationAsync_WhenStatusNotActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new UpdateConsultationRequest();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Completed,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateConsultationAsync(medicalCaseId, request));
        }

        #endregion

        #region Write Layer Tests - SetPrescriptionFlagAsync

        [Fact]
        public async Task SetPrescriptionFlagAsync_WithValidRequest_ShouldUpdateFlag()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var needsPrescription = true;

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = false,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    PrescriptionEnabled = false
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.SetPrescriptionFlagAsync(medicalCaseId, needsPrescription);

            // Assert
            result.Should().NotBeNull();
            result!.NeedsPrescription.Should().BeTrue();
        }

        [Fact]
        public async Task SetPrescriptionFlagAsync_WhenStep1NotCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var needsPrescription = true;

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = null
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SetPrescriptionFlagAsync(medicalCaseId, needsPrescription));
        }

        #endregion

        #region Write Layer Tests - CreatePrescriptionAsync

        [Fact]
        public async Task CreatePrescriptionAsync_WithValidRequest_ShouldCreatePrescription()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var request = new CreatePrescriptionRequest
            {
                Indication = "感冒",
                Items = new List<PrescriptionItemDto>()
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = patientId,
                DoctorId = doctorId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now
                }
            };

            var prescription = new PrescriptionEntity
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId,
                PatientId = patientId,
                UserId = doctorId,
                Status = PrescriptionStatus.Draft
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
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
            var request = new CreatePrescriptionRequest();

            var existingPrescription = new PrescriptionEntity
            {
                Id = Guid.NewGuid(),
                IsDeleted = false
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Prescription = existingPrescription,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));
        }

        #endregion

        #region Helper Layer Tests

        [Fact]
        public async Task CanEditAsync_WhenStatusActive_ShouldReturnTrue()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CanEditAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.CanEdit.Should().BeTrue();
        }

        [Fact]
        public async Task CanEditAsync_WhenStatusCompleted_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Completed
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CanEditAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.CanEdit.Should().BeFalse();
        }

        #endregion
    }
}
