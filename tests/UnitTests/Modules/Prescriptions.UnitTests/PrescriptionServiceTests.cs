using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests
{
    /// <summary>
    /// 处方服务单元测试
    /// </summary>
    public class PrescriptionServiceTests
    {
        private readonly Mock<IPrescriptionRepository> _mockRepository;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly PrescriptionService _service;

        private readonly Guid _testOperatorId = Guid.NewGuid();
        private readonly string _testOperatorName = "测试医生";

        public PrescriptionServiceTests()
        {
            _mockRepository = new Mock<IPrescriptionRepository>();
            _mockLogService = new Mock<IUnifiedLogService>();
            _mockMapper = new Mock<IMapper>();
            _service = new PrescriptionService(_mockRepository.Object, _mockLogService.Object, _mockMapper.Object);
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithData_ReturnsAllPrescriptions()
        {
            // Arrange
            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), Diagnosis = "感冒" },
                new PrescriptionModel { Id = Guid.NewGuid(), Diagnosis = "胃痛" }
            };
            var expectedDtos = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = prescriptions[0].Id, Diagnosis = "感冒" },
                new PrescriptionDto { Id = prescriptions[1].Id, Diagnosis = "胃痛" }
            };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(prescriptions)).Returns(expectedDtos);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Diagnosis.Should().Be("感冒");
            result[1].Diagnosis.Should().Be("胃痛");
        }

        [Fact]
        public async Task GetAllAsync_WithEmptyData_ReturnsEmptyList()
        {
            // Arrange
            var emptyPrescriptions = new List<PrescriptionModel>();
            var emptyDtos = new List<PrescriptionDto>();

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(emptyPrescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(emptyPrescriptions)).Returns(emptyDtos);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithSearchKeyword_FiltersResults()
        {
            // Arrange
            var query = new PaginationRequest { CurrentPage = 1, PageSize = 10, SearchKeyword = "123" };
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), PatientId = patientId, UserId = doctorId },
                new PrescriptionModel { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), UserId = Guid.NewGuid() }
            };
            var dtos = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = prescriptions[0].Id, PatientId = patientId, DoctorId = doctorId },
                new PrescriptionDto { Id = prescriptions[1].Id, PatientId = Guid.NewGuid(), DoctorId = Guid.NewGuid() }
            };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(prescriptions)).Returns(dtos);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeNull();
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var query = new PaginationRequest { CurrentPage = 2, PageSize = 1 };
            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), Diagnosis = "第一个" },
                new PrescriptionModel { Id = Guid.NewGuid(), Diagnosis = "第二个" }
            };
            var dtos = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = prescriptions[0].Id, Diagnosis = "第一个" },
                new PrescriptionDto { Id = prescriptions[1].Id, Diagnosis = "第二个" }
            };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(prescriptions)).Returns(dtos);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.CurrentPage.Should().Be(2);
            result.PageSize.Should().Be(1);
            result.TotalCount.Should().Be(2);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsDetail()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new PrescriptionModel { Id = prescriptionId, Diagnosis = "感冒" };
            var expectedDto = new PrescriptionDetailDto { Id = prescriptionId, Diagnosis = "感冒" };

            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync(prescription);
            _mockMapper.Setup(x => x.Map<PrescriptionDetailDto>(prescription)).Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId.ToString());

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(prescriptionId);
            result.Diagnosis.Should().Be("感冒");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _service.GetByIdAsync("invalid-guid");

            // Assert
            result.Should().BeNull();
            _mockRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync((PrescriptionModel?)null);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId.ToString());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ReturnsCreatedPrescription()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "感冒",
                DosageCount = 7
            };
            var model = new PrescriptionModel
            {
                PatientId = createDto.PatientId,
                UserId = createDto.DoctorId,
                Diagnosis = createDto.Diagnosis,
                DosageCount = createDto.DosageCount
            };
            var resultDto = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                Diagnosis = createDto.Diagnosis
            };

            _mockMapper.Setup(x => x.Map<PrescriptionModel>(createDto)).Returns(model);
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<PrescriptionModel>())).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<PrescriptionDto>(It.IsAny<PrescriptionModel>())).Returns(resultDto);
            _mockLogService.Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>())).Returns(Task.FromResult(true));

            // Act
            var result = await _service.CreateAsync(createDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(createDto.PatientId);
            result.Diagnosis.Should().Be("感冒");
            _mockLogService.Verify(x => x.CreateLogAsync(It.Is<LogCreateDto>(log => 
                log.ActionType == ActionType.Create && 
                log.ObjectType == ObjectType.Prescription)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenRepositoryFails_ReturnsNull()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto { PatientId = Guid.NewGuid(), Diagnosis = "测试" };
            var model = new PrescriptionModel();

            _mockMapper.Setup(x => x.Map<PrescriptionModel>(createDto)).Returns(model);
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<PrescriptionModel>())).ReturnsAsync(false);

            // Act
            var result = await _service.CreateAsync(createDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeNull();
            _mockLogService.Verify(x => x.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Never);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var updateDto = new PrescriptionEditDto { Id = Guid.NewGuid(), Diagnosis = "更新诊断" };
            var oldModel = new PrescriptionModel { Id = updateDto.Id, Diagnosis = "原诊断" };
            var updatedModel = new PrescriptionModel { Id = updateDto.Id, Diagnosis = "更新诊断" };

            _mockRepository.Setup(x => x.GetByIdAsync(updateDto.Id)).ReturnsAsync(oldModel);
            _mockMapper.Setup(x => x.Map(updateDto, oldModel)).Returns(updatedModel);
            _mockRepository.Setup(x => x.UpdateAsync(updatedModel)).ReturnsAsync(true);
            _mockLogService.Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>())).Returns(Task.FromResult(true));

            // Act
            var result = await _service.UpdateAsync(updateDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeTrue();
            _mockLogService.Verify(x => x.CreateLogAsync(It.Is<LogCreateDto>(log => 
                log.ActionType == ActionType.Edit && 
                log.ObjectType == ObjectType.Prescription)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var updateDto = new PrescriptionEditDto { Id = Guid.NewGuid(), Diagnosis = "更新" };
            _mockRepository.Setup(x => x.GetByIdAsync(updateDto.Id)).ReturnsAsync((PrescriptionModel?)null);

            // Act
            var result = await _service.UpdateAsync(updateDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<PrescriptionModel>()), Times.Never);
            _mockLogService.Verify(x => x.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Never);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new PrescriptionModel { Id = prescriptionId, Diagnosis = "待删除" };

            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync(prescription);
            _mockRepository.Setup(x => x.DeleteAsync(prescriptionId)).ReturnsAsync(true);
            _mockLogService.Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>())).Returns(Task.FromResult(true));

            // Act
            var result = await _service.DeleteAsync(prescriptionId.ToString(), _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeTrue();
            _mockLogService.Verify(x => x.CreateLogAsync(It.Is<LogCreateDto>(log => 
                log.ActionType == ActionType.Other && 
                log.Content == "删除处方")), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteAsync("invalid-guid", _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync((PrescriptionModel?)null);

            // Act
            var result = await _service.DeleteAsync(prescriptionId.ToString(), _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeFalse();
            _mockLogService.Verify(x => x.CreateLogAsync(It.IsAny<LogCreateDto>()), Times.Never);
        }

        #endregion

        #region CancelAsync Tests

        [Fact]
        public async Task CancelAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescription = new PrescriptionModel { Id = prescriptionId, Diagnosis = "待作废" };

            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync(prescription);
            _mockRepository.Setup(x => x.CancelAsync(prescriptionId)).ReturnsAsync(true);
            _mockLogService.Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>())).Returns(Task.FromResult(true));

            // Act
            var result = await _service.CancelAsync(prescriptionId.ToString(), _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeTrue();
            _mockLogService.Verify(x => x.CreateLogAsync(It.Is<LogCreateDto>(log => 
                log.ActionType == ActionType.Edit && 
                log.Content == "作废处方")), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = await _service.CancelAsync("invalid-guid", _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetPatientHistoryAsync Tests

        [Fact]
        public async Task GetPatientHistoryAsync_WithValidPatientId_ReturnsOrderedHistory()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var otherPatientId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), PatientId = patientId, CreateTime = DateTime.Now.AddDays(-2) },
                new PrescriptionModel { Id = Guid.NewGuid(), PatientId = patientId, CreateTime = DateTime.Now.AddDays(-1) },
                new PrescriptionModel { Id = Guid.NewGuid(), PatientId = otherPatientId, CreateTime = DateTime.Now }
            };
            var expectedDtos = new List<PrescriptionDto>
            {
                new PrescriptionDto { Id = prescriptions[1].Id, PatientId = patientId },
                new PrescriptionDto { Id = prescriptions[0].Id, PatientId = patientId }
            };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(It.IsAny<List<PrescriptionModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId, 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(p => p.PatientId == patientId).Should().BeTrue();
        }

        [Fact]
        public async Task GetPatientHistoryAsync_WithLimit_ReturnsLimitedResults()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var prescriptions = Enumerable.Range(1, 5)
                .Select(i => new PrescriptionModel { Id = Guid.NewGuid(), PatientId = patientId, CreateTime = DateTime.Now.AddDays(-i) })
                .ToList();
            var dtos = prescriptions.Select(p => new PrescriptionDto { Id = p.Id, PatientId = p.PatientId }).ToList();

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(It.IsAny<List<PrescriptionModel>>())).Returns(dtos.Take(3).ToList());

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId, 3);

            // Assert
            result.Should().HaveCount(3);
        }

        #endregion

        #region GetDoctorTodayPrescriptionsAsync Tests

        [Fact]
        public async Task GetDoctorTodayPrescriptionsAsync_WithTodayPrescriptions_ReturnsOnlyToday()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var today = DateTime.Today;
            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), UserId = doctorId, CreateTime = today.AddHours(10) },
                new PrescriptionModel { Id = Guid.NewGuid(), UserId = doctorId, CreateTime = today.AddDays(-1) },
                new PrescriptionModel { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CreateTime = today.AddHours(14) }
            };
            var expectedDto = new PrescriptionDto { Id = prescriptions[0].Id, DoctorId = doctorId };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(It.IsAny<List<PrescriptionModel>>()))
                      .Returns(new List<PrescriptionDto> { expectedDto });

            // Act
            var result = await _service.GetDoctorTodayPrescriptionsAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].DoctorId.Should().Be(doctorId);
        }

        #endregion

        #region CopyLastPrescriptionAsync Tests

        [Fact]
        public async Task CopyLastPrescriptionAsync_WithPatientHistory_CreatesCopy()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var lastPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Diagnosis = "原诊断",
                DosageCount = 7,
                Advice = "饭后服用",
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "金银花",
                        Quantity = 10,
                        Unit = "g",
                        UnitPrice = 5.0m,
                        Remark = "清热"
                    }
                }
            };

            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), PatientId = patientId, CreateTime = DateTime.Now }
            };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(It.IsAny<List<PrescriptionModel>>()))
                      .Returns(new List<PrescriptionDto> { lastPrescription });

            var newPrescription = new PrescriptionDto { Id = Guid.NewGuid(), PatientId = patientId, DoctorId = doctorId };
            var model = new PrescriptionModel();
            _mockMapper.Setup(x => x.Map<PrescriptionModel>(It.IsAny<PrescriptionCreateDto>())).Returns(model);
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<PrescriptionModel>())).ReturnsAsync(true);
            _mockMapper.Setup(x => x.Map<PrescriptionDto>(It.IsAny<PrescriptionModel>())).Returns(newPrescription);
            _mockLogService.Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>())).Returns(Task.FromResult(true));

            // Act
            var result = await _service.CopyLastPrescriptionAsync(patientId, doctorId, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(patientId);
            result.DoctorId.Should().Be(doctorId);
        }

        [Fact]
        public async Task CopyLastPrescriptionAsync_WithNoHistory_ReturnsNull()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(new List<PrescriptionModel>());
            _mockMapper.Setup(x => x.Map<List<PrescriptionDto>>(It.IsAny<List<PrescriptionModel>>()))
                      .Returns(new List<PrescriptionDto>());

            // Act
            var result = await _service.CopyLastPrescriptionAsync(patientId, doctorId, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_WithData_ReturnsCorrectStatistics()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel { Id = Guid.NewGuid(), UserId = doctorId, Status = PrescriptionStatus.Draft, CreateTime = DateTime.Now },
                new PrescriptionModel { Id = Guid.NewGuid(), UserId = doctorId, Status = PrescriptionStatus.Completed, CreateTime = DateTime.Now },
                new PrescriptionModel { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = PrescriptionStatus.Draft, CreateTime = DateTime.Now }
            };

            _mockRepository.Setup(x => x.GetListAsync()).ReturnsAsync(prescriptions);

            // Act
            var result = await _service.GetStatisticsAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2); // Only doctor's prescriptions
            result.DraftCount.Should().Be(1);
            result.CompletedCount.Should().Be(1);
        }

        #endregion
    }
}