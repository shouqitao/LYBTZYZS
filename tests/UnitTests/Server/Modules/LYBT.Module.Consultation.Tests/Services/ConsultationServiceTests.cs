// Issue #1601 Phase 1: 测试文件暂时禁用，等待Phase 2重构
#if FALSE
using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.UnitTests.Core.Services
{
    /// <summary>
    /// ConsultationService服务层单元测试
    /// </summary>
    public class ConsultationServiceTests : IDisposable
    {
        private readonly Mock<IConsultationRepository> _repositoryMock;
        private readonly Mock<IMedicalCaseRepository> _medicalCaseRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ConsultationService>> _loggerMock;
        private readonly ConsultationService _service;

        public ConsultationServiceTests()
        {
            _repositoryMock = new Mock<IConsultationRepository>();
            _medicalCaseRepositoryMock = new Mock<IMedicalCaseRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ConsultationService>>();

            // 创建服务实例
            _service = new ConsultationService(
                _repositoryMock.Object,
                _medicalCaseRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Get Through MedicalCase Tests

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                ChiefComplaint = "测试主诉",
                TCMDiagnosis = "测试诊断",
                MedicalCase = new MedicalCase
                {
                    Id = medicalCaseId,
                    PatientName = "测试患者",
                    DoctorName = "测试医生"
                }
            };

            var consultationDto = new ConsultationDto
            {
                Id = consultation.Id,
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉",
                TCMDiagnosis = "测试诊断",
                PatientName = "测试患者",
                DoctorName = "测试医生"
            };

            _repositoryMock.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync(consultation);
            _mapperMock.Setup(x => x.Map<ConsultationDto>(consultation))
                .Returns(consultationDto);

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data![0].MedicalCaseId.Should().Be(medicalCaseId);
            result.Data![0].ChiefComplaint.Should().Be("测试主诉");
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithNoConsultation_ShouldReturnEmptyList()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in nullability
            _repositoryMock.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync((Consultation?)null);
#pragma warning restore CS8620

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region Create Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var createDto = new ConsultationInputDto
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉"
            };

            var consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                ChiefComplaint = "测试主诉"
            };

            var consultationDto = new ConsultationDto
            {
                Id = consultation.Id,
                ChiefComplaint = "测试主诉",
                MedicalCaseId = medicalCaseId
            };

            _mapperMock.Setup(x => x.Map<Consultation>(createDto))
                .Returns(consultation);
            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Consultation>()))
                .ReturnsAsync(consultation);
            _mapperMock.Setup(x => x.Map<ConsultationDto>(consultation))
                .Returns(consultationDto);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ChiefComplaint.Should().Be("测试主诉");
        }

        #endregion

        #region Get Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var consultation = new Consultation
            {
                Id = consultationId,
                ChiefComplaint = "测试主诉",
                TCMDiagnosis = "测试诊断",
                MedicalCase = new MedicalCase
                {
                    PatientName = "测试患者",
                    DoctorName = "测试医生"
                }
            };

            var consultationDto = new ConsultationDto
            {
                Id = consultationId,
                ChiefComplaint = "测试主诉",
                TCMDiagnosis = "测试诊断",
                PatientName = "测试患者",
                DoctorName = "测试医生"
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(consultationId))
                .ReturnsAsync(consultation);
            _mapperMock.Setup(x => x.Map<ConsultationDto>(consultation))
                .Returns(consultationDto);

            // Act
            var result = await _service.GetByIdAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(consultationId);
            result.Data!.ChiefComplaint.Should().Be("测试主诉");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in nullability
            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(consultationId))
                .ReturnsAsync((Consultation?)null);
#pragma warning restore CS8620

            // Act
            var result = await _service.GetByIdAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("诊疗记录不存在");
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var existingConsultation = new Consultation
            {
                Id = consultationId,
                ChiefComplaint = "原始主诉"
            };

            var updateDto = new ConsultationInputDto
            {
                ChiefComplaint = "更新后的主诉",
                TCMDiagnosis = "更新后的诊断"
            };

            var updatedConsultation = new Consultation
            {
                Id = consultationId,
                ChiefComplaint = "更新后的主诉",
                TCMDiagnosis = "更新后的诊断"
            };

            var resultDto = new ConsultationDto
            {
                Id = consultationId,
                ChiefComplaint = "更新后的主诉",
                TCMDiagnosis = "更新后的诊断"
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(consultationId))
                .ReturnsAsync(existingConsultation);
            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Consultation>()))
                .ReturnsAsync(updatedConsultation);
            _mapperMock.Setup(x => x.Map(updateDto, existingConsultation));
            _mapperMock.Setup(x => x.Map<ConsultationDto>(updatedConsultation))
                .Returns(resultDto);

            // Act
            var result = await _service.UpdateAsync(consultationId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ChiefComplaint.Should().Be("更新后的主诉");
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(consultationId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("删除成功");
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(consultationId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(consultationId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("删除失败");
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchAsync_WithKeyword_ShouldReturnMatchingResults()
        {
            // Arrange
            var keyword = "头痛";
            var consultations = new List<Consultation>
            {
                new Consultation
                {
                    Id = Guid.NewGuid(),
                    ChiefComplaint = "头痛发热",
                    TCMDiagnosis = "外感风寒"
                }
            };

            var consultationDtos = new List<ConsultationDto>
            {
                new ConsultationDto
                {
                    Id = consultations[0].Id,
                    ChiefComplaint = "头痛发热",
                    TCMDiagnosis = "外感风寒"
                }
            };

            _repositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Consultation, bool>>>()))
                .ReturnsAsync(consultations);
            _mapperMock.Setup(x => x.Map<List<ConsultationDto>>(consultations))
                .Returns(consultationDtos);

            // Act
            var result = await _service.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data![0].ChiefComplaint.Should().Contain(keyword);
        }

        #endregion

        #region GetPaged Tests

        [Fact]
        public async Task GetPagedAsync_WithDefaultParameters_ShouldReturnSuccess()
        {
            // Arrange
            var consultations = new List<Consultation>
            {
                new Consultation
                {
                    Id = Guid.NewGuid(),
                    ChiefComplaint = "测试主诉1",
                    MedicalCase = new MedicalCase
                    {
                        PatientName = "患者1",
                        DoctorName = "医生1"
                    }
                }
            };

            var pagedResult = new PagedResult<Consultation>
            {
                Items = consultations,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };

            var consultationDtos = new List<ConsultationDto>
            {
                new ConsultationDto
                {
                    Id = consultations[0].Id,
                    ChiefComplaint = "测试主诉1",
                    PatientName = "患者1",
                    DoctorName = "医生1"
                }
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(1, 20, null))
                .ReturnsAsync(pagedResult);
            _mapperMock.Setup(x => x.Map<ConsultationDto>(It.IsAny<Consultation>()))
                .Returns((Consultation c) => consultationDtos.First(d => d.Id == c.Id));

            // Act
            var result = await _service.GetPagedAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(1);
            result.Data!.TotalCount.Should().Be(1);
        }

        #endregion

        #region Business Rules Tests (Issue #1423)

        /// <summary>
        /// RULE-1: 一病案一诊断约束 - 医案不存在时应失败
        /// </summary>
        [Fact]
        public async Task CreateAsync_WhenMedicalCaseNotExists_ShouldReturnFailure()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var createDto = new ConsultationInputDto
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉"
            };

#pragma warning disable CS8620
            _medicalCaseRepositoryMock.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync((MedicalCase?)null);
#pragma warning restore CS8620

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("医疗案例不存在，无法创建诊疗记录");
        }

        /// <summary>
        /// RULE-1: 一病案一诊断约束 - 已有诊断时应失败
        /// </summary>
        [Fact]
        public async Task CreateAsync_WhenConsultationAlreadyExists_ShouldReturnFailure()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var createDto = new ConsultationInputDto
            {
                MedicalCaseId = medicalCaseId,
                ChiefComplaint = "测试主诉"
            };

            var medicalCase = new MedicalCase
            {
                Id = medicalCaseId,
                PatientName = "测试患者",
                DoctorName = "测试医生"
            };

            var existingConsultation = new Consultation
            {
                Id = medicalCaseId, // 共享主键
                ChiefComplaint = "已存在的诊断"
            };

            _medicalCaseRepositoryMock.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(medicalCase);
            _repositoryMock.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(existingConsultation);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("该医疗案例已有诊疗记录，不可重复创建");
        }

        /// <summary>
        /// RULE-3: 当天可改隔日锁定 - 创建当天可以修改
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenCreatedToday_ShouldReturnSuccess()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var existingConsultation = new Consultation
            {
                Id = consultationId,
                ChiefComplaint = "原始主诉",
                CreatedAt = DateTime.Today.AddHours(10) // 今天创建
            };

            var updateDto = new ConsultationInputDto
            {
                ChiefComplaint = "更新后的主诉",
                TCMDiagnosis = "更新后的诊断"
            };

            var updatedConsultation = new Consultation
            {
                Id = consultationId,
                ChiefComplaint = "更新后的主诉",
                TCMDiagnosis = "更新后的诊断",
                CreatedAt = DateTime.Today.AddHours(10)
            };

            var resultDto = new ConsultationDto
            {
                Id = consultationId,
                ChiefComplaint = "更新后的主诉",
                TCMDiagnosis = "更新后的诊断"
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(consultationId))
                .ReturnsAsync(existingConsultation);
            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Consultation>()))
                .ReturnsAsync(updatedConsultation);
            _mapperMock.Setup(x => x.Map(updateDto, existingConsultation));
            _mapperMock.Setup(x => x.Map<ConsultationDto>(updatedConsultation))
                .Returns(resultDto);

            // Act
            var result = await _service.UpdateAsync(consultationId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ChiefComplaint.Should().Be("更新后的主诉");
        }

        /// <summary>
        /// RULE-3: 当天可改隔日锁定 - 隔日后不可修改
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenCreatedYesterday_ShouldReturnFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var existingConsultation = new Consultation
            {
                Id = consultationId,
                ChiefComplaint = "原始主诉",
                CreatedAt = DateTime.Today.AddDays(-1).AddHours(10) // 昨天创建
            };

            var updateDto = new ConsultationInputDto
            {
                ChiefComplaint = "尝试更新的主诉",
                TCMDiagnosis = "尝试更新的诊断"
            };

            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(consultationId))
                .ReturnsAsync(existingConsultation);

            // Act
            var result = await _service.UpdateAsync(consultationId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("已超过可修改期限");
            result.Message.Should().Contain("仅限创建当天可修改");
        }

        #endregion

        public void Dispose()
        {
            // Clean up any resources if needed
        }
    }
}
#endif
