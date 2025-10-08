using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
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
    /// MedicalCaseService单元测试
    /// Issue #1053 - 重写以匹配实际API
    /// </summary>
    public class MedicalCaseServiceTests : TestBase
    {
        private readonly MedicalCaseService _service;
        private readonly Mock<IMedicalCaseRepository> _repositoryMock;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;

        public MedicalCaseServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _loggerMock = CreateLoggerMock<MedicalCaseService>();

            _service = new MedicalCaseService(
                _repositoryMock.Object,
                Mapper,
                _loggerMock.Object
            );
        }

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var medicalCases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者A",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生A",
                    ConsultationDate = DateTime.Now,
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                }
            };

            var pagedResult = new PagedResult<MedicalCaseEntity>
            {
                Items = medicalCases,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .Setup(x => x.GetPagedWithDetailsAsync(1, 20, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPagedAsync(1, 20);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);
            result.Data!.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            _repositoryMock
                .Setup(x => x.GetPagedWithDetailsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetPagedAsync(1, 20);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取医疗案例列表失败");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnMedicalCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid()
            };

            _repositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.GetByIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(nonExistentId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("医疗案例不存在");
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateMedicalCase()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Remark = "测试备注"
            };

            var createdEntity = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                PatientName = "患者",
                DoctorName = "医生",
                Remark = createDto.Remark,
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid()
            };

            _repositoryMock
                .Setup(x => x.GetByPatientIdAsync(createDto.PatientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithExistingMedicalCase_ShouldUpdateSuccessfully()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            var existingEntity = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "患者",
                DoctorId = doctorId,
                DoctorName = "医生",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow, // 今天创建，可以编辑
                CreatedBy = Guid.NewGuid()
            };

            var updateDto = new MedicalCaseUpdateDto
            {
                Id = medicalCaseId,
                PatientId = existingEntity.PatientId,
                DoctorId = existingEntity.DoctorId,
                Remark = "更新后的备注"
            };

            var updatedEntity = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = existingEntity.PatientId,
                PatientName = existingEntity.PatientName,
                DoctorId = existingEntity.DoctorId,
                DoctorName = existingEntity.DoctorName,
                Remark = updateDto.Remark,
                ConsultationDate = existingEntity.ConsultationDate,
                Status = existingEntity.Status,
                CreatedAt = existingEntity.CreatedAt,
                CreatedBy = existingEntity.CreatedBy
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(existingEntity);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(updatedEntity);

            // Act
            var result = await _service.UpdateAsync(medicalCaseId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentMedicalCase_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto
            {
                Id = nonExistentId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.UpdateAsync(nonExistentId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("医疗案例不存在");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithExistingMedicalCase_ShouldDeleteSuccessfully()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            var existingEntity = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "患者",
                DoctorId = doctorId,
                DoctorName = "医生",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow, // 今天创建，可以删除
                CreatedBy = Guid.NewGuid()
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(existingEntity);

            _repositoryMock
                .Setup(x => x.DeleteAsync(medicalCaseId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentMedicalCase_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(nonExistentId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.DeleteAsync(nonExistentId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("医疗案例不存在");
        }

        #endregion

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_WithExistingPatientId_ShouldReturnMedicalCases()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var medicalCases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    PatientName = "患者A",
                    DoctorId = Guid.NewGuid(),
                    DoctorName = "医生A",
                    ConsultationDate = DateTime.Now,
                    Status = MedicalCaseStatus.Active,
                    CreatedBy = Guid.NewGuid()
                }
            };

            _repositoryMock
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(medicalCases);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
        }

        #endregion

        #region CreateWithDetailsAsync Tests

        [Fact]
        public async Task CreateWithDetailsAsync_WithValidData_ShouldCreateCompleteAggregate()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            var caseDto = new MedicalCaseCreateDto
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Remark = "测试病案"
            };

            var consultationDto = new ConsultationCreateDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = patientId,
                UserId = doctorId,
                ChiefComplaint = "头痛发热",
                PresentIllness = "患者3天前开始头痛"
            };

            var prescriptionDto = new PrescriptionCreateDto
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Quantity = 7,
                Usage = "水煎服",
                TotalAmount = 168.50m
            };

            var createdEntity = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "患者",
                DoctorId = doctorId,
                DoctorName = "医生",
                Remark = caseDto.Remark,
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedBy = doctorId,
                Consultation = new ConsultationEntity
                {
                    Id = Guid.NewGuid(),
                    ChiefComplaint = consultationDto.ChiefComplaint,
                    PresentIllness = consultationDto.PresentIllness,
                    CreatedBy = doctorId
                },
                Prescription = new PrescriptionEntity
                {
                    Id = Guid.NewGuid(),
                    DosageCount = prescriptionDto.Quantity,
                    CreatedBy = doctorId
                }
            };

            _repositoryMock
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _service.CreateWithDetailsAsync(caseDto, consultationDto, prescriptionDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateWithDetailsAsync_WithNullPrescription_ShouldCreateWithoutPrescription()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            var caseDto = new MedicalCaseCreateDto
            {
                PatientId = patientId,
                DoctorId = doctorId
            };

            var consultationDto = new ConsultationCreateDto
            {
                MedicalCaseId = Guid.NewGuid(),
                PatientId = patientId,
                UserId = doctorId,
                ChiefComplaint = "测试主诉"
            };

            var createdEntity = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "患者",
                DoctorId = doctorId,
                DoctorName = "医生",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedBy = doctorId,
                Consultation = new ConsultationEntity
                {
                    Id = Guid.NewGuid(),
                    ChiefComplaint = consultationDto.ChiefComplaint,
                    CreatedBy = doctorId
                }
            };

            _repositoryMock
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(createdEntity);

            // Act
            var result = await _service.CreateWithDetailsAsync(caseDto, consultationDto, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region GetByIdWithDetailsAsync Tests

        [Fact]
        public async Task GetByIdWithDetailsAsync_WithExistingId_ShouldReturnCompleteDetails()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "测试患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "测试医生",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                CreatedBy = Guid.NewGuid(),
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    ChiefComplaint = "主诉",
                    CreatedBy = Guid.NewGuid()
                }
            };

            _repositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.GetByIdWithDetailsAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdWithDetailsAsync(nonExistentId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.GetByIdWithDetailsAsync(nonExistentId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("医疗案例不存在");
        }

        #endregion
    }
}
