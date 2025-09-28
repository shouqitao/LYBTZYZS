using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.UnitTests.Core.Services
{
    /// <summary>
    /// ConsultationService服务层单元测试
    /// </summary>
    public class ConsultationServiceTests : IDisposable
    {
        private readonly Mock<IConsultationRepository> _repositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ConsultationService>> _loggerMock;
        private readonly ConsultationService _service;

        public ConsultationServiceTests()
        {
            _repositoryMock = new Mock<IConsultationRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ConsultationService>>();

            // 创建服务实例
            _service = new ConsultationService(_repositoryMock.Object, _mapperMock.Object, _loggerMock.Object);
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
            result.Data.Should().HaveCount(1);
            result.Data[0].MedicalCaseId.Should().Be(medicalCaseId);
            result.Data[0].ChiefComplaint.Should().Be("测试主诉");
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithNoConsultation_ShouldReturnEmptyList()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            
            _repositoryMock.Setup(x => x.GetByMedicalCaseIdAsync(medicalCaseId))
                .ReturnsAsync((Consultation)null);

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region Create Tests (Obsolete)

        [Fact]
        public async Task CreateAsync_ShouldReturnFailure_BecauseObsolete()
        {
            // Arrange
            var createDto = new ConsultationCreateDto
            {
                MedicalCaseId = Guid.NewGuid(),
                ChiefComplaint = "测试主诉"
            };

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("必须通过医疗案例(MedicalCase)创建");
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
            result.Data.Id.Should().Be(consultationId);
            result.Data.ChiefComplaint.Should().Be("测试主诉");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnFailure()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            
            _repositoryMock.Setup(x => x.GetByIdWithDetailsAsync(consultationId))
                .ReturnsAsync((Consultation)null);

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

            var updateDto = new ConsultationUpdateDto
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
            result.Data.ChiefComplaint.Should().Be("更新后的主诉");
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
            result.Data.Should().HaveCount(1);
            result.Data[0].ChiefComplaint.Should().Contain(keyword);
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
            result.Data.Items.Should().HaveCount(1);
            result.Data.TotalCount.Should().Be(1);
        }

        #endregion

        public void Dispose()
        {
            // Clean up any resources if needed
        }
    }
}