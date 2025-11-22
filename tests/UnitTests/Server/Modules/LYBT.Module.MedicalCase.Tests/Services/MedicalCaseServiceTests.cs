using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Dtos; // SetPrescriptionFlagRequest (模块专用)
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
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
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;

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
        private readonly Mock<IPatientRepository> _patientRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<MedicalCaseService>> _loggerMock;

        public MedicalCaseServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _patientRepositoryMock = CreateMock<IPatientRepository>();
            _userRepositoryMock = CreateMock<IUserRepository>();
            _mapperMock = CreateMock<IMapper>();
            _loggerMock = CreateLoggerMock<MedicalCaseService>();

            _service = new MedicalCaseService(
                _repositoryMock.Object,
                _patientRepositoryMock.Object,
                _userRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Write Layer Tests - CreateAsync

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
                DoctorId = doctorId,
                DoctorName = "李医生",
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = Guid.NewGuid() }
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(createdMedicalCase);

            // Act
            var result = await _service.CreateAsync(patientId, visitDate, doctorId);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(patientId);
            result.DoctorId.Should().Be(doctorId);
            result.PatientName.Should().Be("张三");
            result.DoctorName.Should().Be("李医生");
            result.Status.Should().Be(MedicalCaseStatus.Active);
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
                Status = MedicalCaseStatus.Active
            };

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity> { existingActiveCase });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(patientId, visitDate, doctorId));
        }

        /// <summary>
        /// Epic #2210 Issue #2215: 验证DoctorId为Guid.Empty时抛出ArgumentException
        /// </summary>
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

        /// <summary>
        /// Epic #2210 Issue #2215: 验证Patient不存在时抛出InvalidOperationException
        /// </summary>
        [Fact]
        public async Task CreateAsync_WhenPatientNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            _patientRepositoryMock.Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync((Patient?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(patientId, visitDate, doctorId));

            exception.Message.Should().Contain("患者不存在");
            exception.Message.Should().Contain(patientId.ToString());
        }

        /// <summary>
        /// Epic #2210 Issue #2215: 验证Doctor不存在时抛出InvalidOperationException
        /// </summary>
        [Fact]
        public async Task CreateAsync_WhenDoctorNotFound_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            var patient = new Patient { Id = patientId, Name = "张三" };

            _patientRepositoryMock.Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _userRepositoryMock.Setup(x => x.GetByIdAsync(doctorId))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(patientId, visitDate, doctorId));

            exception.Message.Should().Contain("医生不存在");
            exception.Message.Should().Contain(doctorId.ToString());
        }

        /// <summary>
        /// Epic #2210 Issue #2215: 验证PatientName和DoctorName正确设置
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldSetPatientNameAndDoctorNameCorrectly()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var visitDate = DateTime.Now;

            var patient = new Patient { Id = patientId, Name = "王五" };
            var doctor = new User { Id = doctorId, RealName = "赵医生" };

            _patientRepositoryMock.Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(patient);

            _userRepositoryMock.Setup(x => x.GetByIdAsync(doctorId))
                .ReturnsAsync(doctor);

            _repositoryMock.Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(new List<MedicalCaseEntity>());

            MedicalCaseEntity? capturedEntity = null;
            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<MedicalCaseEntity>()))
                .Callback<MedicalCaseEntity>(entity => capturedEntity = entity)
                .ReturnsAsync((MedicalCaseEntity entity) => entity);

            // Act
            var result = await _service.CreateAsync(patientId, visitDate, doctorId);

            // Assert
            capturedEntity.Should().NotBeNull();
            capturedEntity!.PatientName.Should().Be("王五");
            capturedEntity.DoctorName.Should().Be("赵医生");
            capturedEntity.DoctorId.Should().Be(doctorId);

            // 验证Repository方法被正确调用
            _patientRepositoryMock.Verify(x => x.GetByIdAsync(patientId), Times.Once);
            _userRepositoryMock.Verify(x => x.GetByIdAsync(doctorId), Times.Once);
        }

        #endregion

        #region Write Layer Tests - UpdateConsultationAsync

        [Fact]
        public async Task UpdateConsultationAsync_WithValidRequest_ShouldUpdateConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new ConsultationInputDto
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
            var result = await _service.UpdateConsultationAsync(medicalCaseId, request, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().NotBeNull();
            result!.Consultation!.Step1CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateConsultationAsync_WhenStatusNotActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new ConsultationInputDto();

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
                () => _service.UpdateConsultationAsync(medicalCaseId, request, Guid.NewGuid(), isAdmin: true));
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
            var result = await _service.SetPrescriptionFlagAsync(medicalCaseId, needsPrescription, Guid.NewGuid(), isAdmin: true);

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
                () => _service.SetPrescriptionFlagAsync(medicalCaseId, needsPrescription, Guid.NewGuid(), isAdmin: true));
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证首次标记时自动设置Step2CompletedAt
        /// </summary>
        [Fact]
        public async Task SetPrescriptionFlagAsync_FirstTimeMarking_ShouldSetStep2CompletedAt()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var needsPrescription = true;

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = null, // 未标记状态
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = null, // Step2未完成
                    PrescriptionEnabled = false
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
            result.Consultation!.Step2CompletedAt.Should().NotBeNull();
            result.Consultation.PrescriptionEnabled.Should().BeTrue();
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证重复标记时不改变Step2CompletedAt
        /// </summary>
        [Fact]
        public async Task SetPrescriptionFlagAsync_SecondTimeMarking_ShouldNotChangeStep2CompletedAt()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var originalStep2Time = DateTime.Now.AddMinutes(-10);

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now.AddMinutes(-15),
                    Step2CompletedAt = originalStep2Time, // 已标记
                    PrescriptionEnabled = true
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act - 修改为false
            var result = await _service.SetPrescriptionFlagAsync(medicalCaseId, false, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().NotBeNull();
            result!.NeedsPrescription.Should().BeFalse();
            result.Consultation!.Step2CompletedAt.Should().Be(originalStep2Time); // 时间戳不变
            result.Consultation.PrescriptionEnabled.Should().BeFalse();
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证NeedsPrescription三态语义 - 设置为false
        /// </summary>
        [Fact]
        public async Task SetPrescriptionFlagAsync_SetToFalse_ShouldMarkAsNotNeeded()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = null, // 未标记
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = null
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act - 明确标记为"不需要开处方"
            var result = await _service.SetPrescriptionFlagAsync(medicalCaseId, false, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().NotBeNull();
            result!.NeedsPrescription.Should().BeFalse(); // false = 明确不需要
            result.Consultation!.Step2CompletedAt.Should().NotBeNull();
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
            var request = new PrescriptionCreateDto
            {                Items = new List<PrescriptionItemInputDto>()
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
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now // Epic #2175 BF-002: Step2完成标记
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
            var request = new PrescriptionCreateDto();

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
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now // Epic #2175 BF-002: Step2完成标记
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证Step2未完成时抛出异常
        /// </summary>
        [Fact]
        public async Task CreatePrescriptionAsync_WhenStep2NotCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new PrescriptionCreateDto();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = null // Step2未完成
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));

            exception.Message.Should().Contain("Step2");
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证NeedsPrescription为null时抛出异常
        /// </summary>
        [Fact]
        public async Task CreatePrescriptionAsync_WhenNeedsPrescriptionIsNull_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new PrescriptionCreateDto();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = null, // 未标记状态
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));

            exception.Message.Should().Contain("未标记需要开处方");
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证NeedsPrescription为false时抛出异常
        /// </summary>
        [Fact]
        public async Task CreatePrescriptionAsync_WhenNeedsPrescriptionIsFalse_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var request = new PrescriptionCreateDto();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = false, // 明确标记不需要
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePrescriptionAsync(medicalCaseId, request));

            exception.Message.Should().Contain("未标记需要开处方");
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证完整的BF-002流程 - Step1+Step2都完成
        /// </summary>
        [Fact]
        public async Task CreatePrescriptionAsync_WithCompleteBF002Flow_ShouldSucceed()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var request = new PrescriptionCreateDto
            {
                Items = new List<PrescriptionItemInputDto>()
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = patientId,
                DoctorId = doctorId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true, // Step2标记为需要
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now.AddMinutes(-10), // Step1已完成
                    Step2CompletedAt = DateTime.Now.AddMinutes(-5)  // Step2已完成
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

        #endregion

        #region Helper Layer Tests

        #region Write Layer Tests - UpdatePrescriptionAsync (补充)

        [Fact]
        public async Task UpdatePrescriptionAsync_WithValidRequest_ShouldUpdatePrescription()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var request = new PrescriptionEditDto
            {                Items = new List<PrescriptionItemInputDto>()
            };

            var prescription = new PrescriptionEntity
            {
                Id = prescriptionId,
                IsPrinted = false,
                Indication = "原始主治"
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = prescription
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map(request, prescription));

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.UpdatePrescriptionAsync(medicalCaseId, prescriptionId, request, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(prescriptionId);
        }

        [Fact]
        public async Task UpdatePrescriptionAsync_WhenMedicalCaseNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var request = new PrescriptionEditDto();

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.UpdatePrescriptionAsync(medicalCaseId, prescriptionId, request, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePrescriptionAsync_WhenPrescriptionNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var request = new PrescriptionEditDto();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = null
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.UpdatePrescriptionAsync(medicalCaseId, prescriptionId, request, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePrescriptionAsync_WhenPrescriptionIdMismatch_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var differentPrescriptionId = Guid.NewGuid();
            var request = new PrescriptionEditDto();

            var prescription = new PrescriptionEntity
            {
                Id = differentPrescriptionId,
                IsPrinted = false
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = prescription
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.UpdatePrescriptionAsync(medicalCaseId, prescriptionId, request, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePrescriptionAsync_WhenPrescriptionIsPrinted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var request = new PrescriptionEditDto();

            var prescription = new PrescriptionEntity
            {
                Id = prescriptionId,
                IsPrinted = true
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = prescription
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdatePrescriptionAsync(medicalCaseId, prescriptionId, request, Guid.NewGuid(), isAdmin: true));
        }

        #endregion

        #region Write Layer Tests - DeletePrescriptionAsync (补充)

        [Fact]
        public async Task DeletePrescriptionAsync_WithValidRequest_ShouldDeletePrescription()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            var prescription = new PrescriptionEntity
            {
                Id = prescriptionId,
                IsPrinted = false,
                IsDeleted = false
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = prescription
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.DeletePrescriptionAsync(medicalCaseId, prescriptionId, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeTrue();
            prescription.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeletePrescriptionAsync_WhenMedicalCaseNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.DeletePrescriptionAsync(medicalCaseId, prescriptionId, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeletePrescriptionAsync_WhenPrescriptionNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = null
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.DeletePrescriptionAsync(medicalCaseId, prescriptionId, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeletePrescriptionAsync_WhenPrescriptionIdMismatch_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();
            var differentPrescriptionId = Guid.NewGuid();

            var prescription = new PrescriptionEntity
            {
                Id = differentPrescriptionId,
                IsPrinted = false
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = prescription
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.DeletePrescriptionAsync(medicalCaseId, prescriptionId, Guid.NewGuid(), isAdmin: true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeletePrescriptionAsync_WhenPrescriptionIsPrinted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            var prescription = new PrescriptionEntity
            {
                Id = prescriptionId,
                IsPrinted = true
            };

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Prescription = prescription
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeletePrescriptionAsync(medicalCaseId, prescriptionId, Guid.NewGuid(), isAdmin: true));
        }

        #endregion

        #region Write Layer Tests - CompleteAsync (补充)

        [Fact]
        public async Task CompleteAsync_WithValidRequest_ShouldCompleteCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = false,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now, // Epic #2175 BF-002: Step2完成标记
                    Step3CompletedAt = null
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
            result!.Status.Should().Be(MedicalCaseStatus.Completed);
            result.Consultation!.Step3CompletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteAsync_WhenMedicalCaseNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.CompleteAsync(medicalCaseId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CompleteAsync_WhenStep1NotCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

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
                () => _service.CompleteAsync(medicalCaseId));
        }

        [Fact]
        public async Task CompleteAsync_WhenNeedsPrescriptionButPrescriptionNotExists_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true,
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now // Epic #2175 BF-002: Step2完成标记
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CompleteAsync(medicalCaseId));
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证NeedsPrescription为null时抛出异常
        /// </summary>
        [Fact]
        public async Task CompleteAsync_WhenNeedsPrescriptionIsNull_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = null, // 未标记状态
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = DateTime.Now // Epic #2175 BF-002: Step2完成标记
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CompleteAsync(medicalCaseId));

            exception.Message.Should().Contain("是否需要开处方");
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证Step2未完成时抛出异常
        /// </summary>
        [Fact]
        public async Task CompleteAsync_WhenStep2NotCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = false, // 已标记不需要
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now,
                    Step2CompletedAt = null // Step2未完成
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CompleteAsync(medicalCaseId));

            exception.Message.Should().Contain("Step2");
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证NeedsPrescription=false时可以完成（不需要处方）
        /// </summary>
        [Fact]
        public async Task CompleteAsync_WhenNeedsPrescriptionIsFalse_ShouldCompleteSuccessfully()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = false, // 明确标记不需要处方
                Prescription = null,
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now.AddMinutes(-10),
                    Step2CompletedAt = DateTime.Now.AddMinutes(-5),
                    Step3CompletedAt = null
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
            result!.Status.Should().Be(MedicalCaseStatus.Completed);
            result.Consultation!.Step3CompletedAt.Should().NotBeNull();
        }

        /// <summary>
        /// Epic #2175 BF-002: 验证完整的三步流程 - 需要处方且处方已开具
        /// </summary>
        [Fact]
        public async Task CompleteAsync_WithCompleteBF002FlowAndPrescription_ShouldSucceed()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                NeedsPrescription = true, // 需要处方
                Prescription = new PrescriptionEntity // 处方已开具
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = false
                },
                Consultation = new ConsultationEntity
                {
                    Id = medicalCaseId,
                    Step1CompletedAt = DateTime.Now.AddMinutes(-15), // Step1完成
                    Step2CompletedAt = DateTime.Now.AddMinutes(-10), // Step2完成
                    Step3CompletedAt = null
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
            result!.Status.Should().Be(MedicalCaseStatus.Completed);
            result.Consultation!.Step3CompletedAt.Should().NotBeNull();
        }

        #endregion

        #region Write Layer Tests - CloseCaseAsync (Epic #1676 Phase 4 Task 4.1)

        [Fact]
        public async Task CloseCaseAsync_WithValidId_ShouldCloseCaseSuccessfully()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert
            result.Should().BeTrue();
            medicalCase.Status.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task CloseCaseAsync_WhenMedicalCaseNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CloseCaseAsync_ShouldNotValidateThreeStepProcess()
        {
            // Arrange - 病案未完成三步流程（Consultation为空）
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active,
                Consultation = null, // 未完成三步流程
                NeedsPrescription = true,
                Prescription = null  // 未开处方
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseEntity>()))
                .ReturnsAsync(medicalCase);

            // Act
            var result = await _service.CloseCaseAsync(medicalCaseId);

            // Assert - 应该直接关闭，不抛出异常
            result.Should().BeTrue();
            medicalCase.Status.Should().Be(MedicalCaseStatus.Completed);
        }

        #endregion

        #region Read Layer Tests - GetByIdAsync, GetListAsync

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnEntity()
        {
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientId = Guid.NewGuid(),
                PatientName = "Test Patient",
                DoctorId = Guid.NewGuid(),
                DoctorName = "Test Doctor",
                Status = MedicalCaseStatus.Active
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            var result = await _service.GetByIdAsync(medicalCaseId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(medicalCaseId);
            result.PatientName.Should().Be("Test Patient");
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
        {
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            var result = await _service.GetByIdAsync(medicalCaseId);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetListAsync_WithValidRequest_ShouldReturnPagedResult()
        {
            var entities = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientName = "Patient1", Status = MedicalCaseStatus.Active },
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientName = "Patient2", Status = MedicalCaseStatus.Active }
            };

            var pagedResult = new PagedResult<MedicalCaseEntity>
            {
                Items = entities,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(1, 10, null))
                .ReturnsAsync(pagedResult);

            var result = await _service.GetListAsync(null, null, 1, 10);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetConsultationListAsync_WithValidMedicalCaseId_ShouldReturnList()
        {
            var medicalCaseId = Guid.NewGuid();
            var consultation = new ConsultationEntity { Id = medicalCaseId, TCMDiagnosis = "Test Diagnosis" };
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Consultation = consultation
            };

            var consultationDto = new ConsultationDto { Id = medicalCaseId, TCMDiagnosis = "Test Diagnosis" };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map<ConsultationDto>(consultation))
                .Returns(consultationDto);

            var result = await _service.GetConsultationListAsync(medicalCaseId);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().TCMDiagnosis.Should().Be("Test Diagnosis");
        }

        [Fact]
        public async Task GetPrescriptionListAsync_WithValidMedicalCaseId_ShouldReturnList()
        {
            var medicalCaseId = Guid.NewGuid();
            var prescription = new PrescriptionEntity { Id = Guid.NewGuid(), Indication = "Test Indication" };
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Prescription = prescription
            };

            var prescriptionDto = new MedicalCasePrescriptionDto { Id = prescription.Id, Indication = "Test Indication" };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _mapperMock.Setup(x => x.Map<MedicalCasePrescriptionDto>(prescription))
                .Returns(prescriptionDto);

            var result = await _service.GetPrescriptionListAsync(medicalCaseId);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Indication.Should().Be("Test Indication");
        }

        [Fact]
        public async Task GetUnfinishedCaseByPatientIdAsync_WithActiveCase_ShouldReturnCase()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var activeCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Status = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = Guid.NewGuid() }
            };

            _repositoryMock.Setup(x => x.GetUnfinishedCaseByPatientIdAsync(patientId))
                .ReturnsAsync(activeCase);

            // Act
            var result = await _service.GetUnfinishedCaseByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(patientId);
            result.Status.Should().Be(MedicalCaseStatus.Active);
        }

        [Fact]
        public async Task GetUnfinishedCaseByPatientIdAsync_WithDraftCase_ShouldReturnCase()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var draftCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Status = MedicalCaseStatus.Draft
            };

            _repositoryMock.Setup(x => x.GetUnfinishedCaseByPatientIdAsync(patientId))
                .ReturnsAsync(draftCase);

            // Act
            var result = await _service.GetUnfinishedCaseByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(MedicalCaseStatus.Draft);
        }

        [Fact]
        public async Task GetUnfinishedCaseByPatientIdAsync_WhenNoUnfinishedCase_ShouldReturnNull()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetUnfinishedCaseByPatientIdAsync(patientId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            // Act
            var result = await _service.GetUnfinishedCaseByPatientIdAsync(patientId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateStatusAsync_WithValidTransition_ShouldUpdateStatus()
        {
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Active
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            _repositoryMock.Setup(x => x.UpdateAsync(medicalCase))
                .ReturnsAsync(medicalCase);

            var result = await _service.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Completed);

            result.Should().NotBeNull();
            result!.Status.Should().Be(MedicalCaseStatus.Completed);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenMedicalCaseNotFound_ShouldReturnNull()
        {
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseEntity?)null);

            var result = await _service.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Completed);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateStatusAsync_WithInvalidTransition_ShouldThrowException()
        {
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                Status = MedicalCaseStatus.Completed
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Active));
        }

        #endregion

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
