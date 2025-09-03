using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// PrescriptionService核心功能测试 - UltraThink设计
    /// 职责单一：专注于PrescriptionService的核心功能测试
    /// 代码干净：清晰的测试结构，AAA模式
    /// 性能出色：使用Mock对象，快速执行
    /// </summary>
    public class PrescriptionServiceTests : IDisposable
    {
        private readonly PrescriptionService _service;
        private readonly Mock<IPrescriptionRepository> _repositoryMock;
        private readonly Mock<IUnifiedLogService> _logServiceMock;
        private readonly IMapper _mapper;
        private readonly MockFactory _mockFactory;
        private readonly PrescriptionTestDataBuilder _builder;

        public PrescriptionServiceTests()
        {
            _mockFactory = new MockFactory();
            _builder = new PrescriptionTestDataBuilder();
            _repositoryMock = new Mock<IPrescriptionRepository>();
            _logServiceMock = new Mock<IUnifiedLogService>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<PrescriptionModel, PrescriptionDto>();
                cfg.CreateMap<PrescriptionModel, PrescriptionDetailDto>();
                cfg.CreateMap<PrescriptionCreateDto, PrescriptionModel>();
                cfg.CreateMap<PrescriptionEditDto, PrescriptionModel>();
                cfg.CreateMap<PrescriptionItemModel, PrescriptionItemDto>();
                cfg.CreateMap<PrescriptionItemCreateDto, PrescriptionItemModel>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _service = new PrescriptionService(
                _repositoryMock.Object,
                _logServiceMock.Object,
                _mapper);
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllPrescriptions()
        {
            // Arrange
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().Build(),
                _builder.AsCompletedPrescription().Build(),
                _builder.AsDispensedPrescription().Build()
            };
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            _repositoryMock.Verify(r => r.GetListAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(new List<PrescriptionModel>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidRequest_ReturnsPaginatedResult()
        {
            // Arrange
            var prescriptions = new List<PrescriptionModel>();
            for (int i = 0; i < 25; i++)
            {
                prescriptions.Add(_builder.AsValidPrescription().Build());
            }
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            var request = new PaginationRequest
            {
                CurrentPage = 2,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(2, result.CurrentPage);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearchKeyword_FiltersResults()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().WithId(id1).Build(),
                _builder.AsValidPrescription().WithId(id2).Build(),
                _builder.AsValidPrescription().Build()
            };
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = id1.ToString().Substring(0, 8)
            };

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            Assert.Single(result.Items);
            Assert.Contains(result.Items, p => p.Id == id1);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDetailDto()
        {
            // Arrange
            var prescription = _builder.AsCompletedPrescription().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);

            // Act
            var result = await _service.GetByIdAsync(prescription.Id.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(prescription.Id, result.Id);
            Assert.Equal(prescription.Diagnosis, result.Diagnosis);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((PrescriptionModel)null);

            // Act
            var result = await _service.GetByIdAsync(id.ToString());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_InvalidGuid_ReturnsNull()
        {
            // Act
            var result = await _service.GetByIdAsync("invalid-guid");

            // Assert
            Assert.Null(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidDto_CreatesNewPrescription()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "风寒感冒",
                DosageCount = 7,
                Advice = "忌食生冷",
                Items = new List<PrescriptionItemCreateDto>
                {
                    new PrescriptionItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "麻黄",
                        Quantity = 9,
                        Unit = "g",
                        UnitPrice = 2.5m
                    }
                }
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync(true);

            var operatorId = Guid.NewGuid();
            var operatorName = "张医生";

            // Act
            var result = await _service.CreateAsync(createDto, operatorId, operatorName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.PatientId, result.PatientId);
            Assert.Equal(createDto.Diagnosis, result.Diagnosis);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<PrescriptionModel>()), Times.Once);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_RepositoryFails_ReturnsNull()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "测试诊断"
            };

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync(false);

            // Act
            var result = await _service.CreateAsync(createDto, Guid.NewGuid(), "操作员");

            // Assert
            Assert.Null(result);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Never);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingPrescription_UpdatesSuccessfully()
        {
            // Arrange
            var prescription = _builder.AsValidPrescription().Build();
            var editDto = new PrescriptionEditDto
            {
                Id = prescription.Id,
                Diagnosis = "更新后的诊断",
                Advice = "更新后的医嘱"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateAsync(editDto, Guid.NewGuid(), "操作员");

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()), Times.Once);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingPrescription_ReturnsFalse()
        {
            // Arrange
            var editDto = new PrescriptionEditDto
            {
                Id = Guid.NewGuid(),
                Diagnosis = "更新诊断"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(editDto.Id))
                .ReturnsAsync((PrescriptionModel)null);

            // Act
            var result = await _service.UpdateAsync(editDto, Guid.NewGuid(), "操作员");

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<PrescriptionModel>()), Times.Never);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Never);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingPrescription_DeletesSuccessfully()
        {
            // Arrange
            var prescription = _builder.AsValidPrescription().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _repositoryMock.Setup(r => r.DeleteAsync(prescription.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(prescription.Id.ToString(), Guid.NewGuid(), "操作员");

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.DeleteAsync(prescription.Id), Times.Once);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingPrescription_ReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((PrescriptionModel)null);

            // Act
            var result = await _service.DeleteAsync(id.ToString(), Guid.NewGuid(), "操作员");

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_InvalidGuid_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteAsync("invalid-guid", Guid.NewGuid(), "操作员");

            // Assert
            Assert.False(result);
            _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region CancelAsync Tests

        [Fact]
        public async Task CancelAsync_ExistingPrescription_CancelsSuccessfully()
        {
            // Arrange
            var prescription = _builder.AsValidPrescription().Build();
            _repositoryMock.Setup(r => r.GetByIdAsync(prescription.Id))
                .ReturnsAsync(prescription);
            _repositoryMock.Setup(r => r.CancelAsync(prescription.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CancelAsync(prescription.Id.ToString(), Guid.NewGuid(), "操作员");

            // Assert
            Assert.True(result);
            _repositoryMock.Verify(r => r.CancelAsync(prescription.Id), Times.Once);
            _logServiceMock.Verify(l => l.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Once);
        }

        #endregion

        #region GetPatientHistoryAsync Tests

        [Fact]
        public async Task GetPatientHistoryAsync_ReturnsPatientPrescriptions()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionModel>
            {
                _builder.AsValidPrescription().WithPatientId(patientId).CreatedDaysAgo(1).Build(),
                _builder.AsValidPrescription().WithPatientId(patientId).CreatedDaysAgo(7).Build(),
                _builder.AsValidPrescription().WithPatientId(patientId).CreatedDaysAgo(14).Build(),
                _builder.AsValidPrescription().WithPatientId(Guid.NewGuid()).Build() // 其他患者
            };
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId, 10);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, p => Assert.Equal(patientId, p.PatientId));
            // 验证按时间降序排列
            Assert.True(result[0].CreateTime > result[1].CreateTime);
            Assert.True(result[1].CreateTime > result[2].CreateTime);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WithLimit_RespectsLimit()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var prescriptions = _builder.BuildPatientHistory(patientId, 20);
            _repositoryMock.Setup(r => r.GetListAsync())
                .ReturnsAsync(prescriptions.ToList());

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId, 5);

            // Assert
            Assert.Equal(5, result.Count);
        }

        #endregion

        public void Dispose()
        {
            _mockFactory?.ClearCache();
        }
    }
}