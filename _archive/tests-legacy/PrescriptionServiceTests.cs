using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Dtos;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Services
{
    /// <summary>
    /// 处方服务层单元测试
    /// 测试新创建的服务层功能
    /// </summary>
    public class PrescriptionServiceTests
    {
        private readonly Mock<IPrescriptionRepository> _mockRepository;
        private readonly Mock<ILogger<PrescriptionService>> _mockLogger;
        private readonly PrescriptionService _service;

        public PrescriptionServiceTests()
        {
            _mockRepository = new Mock<IPrescriptionRepository>();
            _mockLogger = new Mock<ILogger<PrescriptionService>>();
            _service = new PrescriptionService(_mockRepository.Object, _mockLogger.Object);
        }

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResults()
        {
            // Arrange
            var expectedData = new PaginatedResult<PrescriptionDto>
            {
                Items = new List<PrescriptionDto>
                {
                    new PrescriptionDto
                    {
                        Id = Guid.NewGuid(),
                        PatientId = Guid.NewGuid(),
                        DoctorId = Guid.NewGuid(),
                        Diagnosis = "感冒",
                        Status = PrescriptionStatus.Completed,
                        TotalPrice = 150.00m,
                        CreateTime = DateTime.Now.AddHours(-1)
                    },
                    new PrescriptionDto
                    {
                        Id = Guid.NewGuid(),
                        PatientId = Guid.NewGuid(),
                        DoctorId = Guid.NewGuid(),
                        Diagnosis = "胃病",
                        Status = PrescriptionStatus.Draft,
                        TotalPrice = 200.00m,
                        CreateTime = DateTime.Now.AddHours(-2)
                    }
                },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _service.GetPagedAsync(1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_WithDateRangeFilter_CallsRepositoryWithCorrectDates()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;

            _mockRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                startDate,
                endDate,
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ReturnsAsync(new PaginatedResult<PrescriptionDto>());

            // Act
            await _service.GetPagedAsync(1, 10, null, startDate, endDate);

            // Assert
            _mockRepository.Verify(x => x.GetPagedAsync(
                1, 10, null, startDate, endDate, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WithPatientFilter_ReturnsOnlyPatientPrescriptions()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedData = new PaginatedResult<PrescriptionDto>
            {
                Items = new List<PrescriptionDto>
                {
                    new PrescriptionDto { Id = Guid.NewGuid(), PatientId = patientId }
                },
                TotalCount = 1
            };

            _mockRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                patientId,
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _service.GetPagedAsync(1, 10, patientId: patientId);

            // Assert
            result.Items.Should().OnlyContain(p => p.PatientId == patientId);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ReturnsPrescriptionDetail()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedPrescription = new PrescriptionDetailDto
            {
                Id = prescriptionId,
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "感冒",
                Status = PrescriptionStatus.Completed,
                Items = new List<PrescriptionItemDto>
                {
                    new PrescriptionItemDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "金银花",
                        Quantity = 10,
                        Unit = "g",
                        Price = 5.00m
                    },
                    new PrescriptionItemDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "连翘",
                        Quantity = 15,
                        Unit = "g",
                        Price = 3.00m
                    }
                },
                TotalPrice = 95.00m,
                CreateTime = DateTime.Now.AddHours(-1)
            };

            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(expectedPrescription);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(prescriptionId);
            result.Items.Should().HaveCount(2);
            result.TotalPrice.Should().Be(95.00m);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            _mockRepository.Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync((PrescriptionDetailDto?)null);

            // Act
            var result = await _service.GetByIdAsync(prescriptionId);

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
                Diagnosis = "胃炎",
                Items = new List<PrescriptionItemCreateDto>
                {
                    new PrescriptionItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 20,
                        Price = 5.00m
                    }
                },
                Remark = "饭后服用"
            };

            var createdPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                DoctorId = createDto.DoctorId,
                Diagnosis = createDto.Diagnosis,
                Status = PrescriptionStatus.Draft,
                TotalPrice = 100.00m,
                CreateTime = DateTime.Now
            };

            _mockRepository.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(createdPrescription);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Diagnosis.Should().Be("胃炎");
            result.Status.Should().Be(PrescriptionStatus.Draft);
        }

        [Fact]
        public async Task CreateAsync_WithEmptyItems_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "胃炎",
                Items = new List<PrescriptionItemCreateDto>() // 空的药材列表
            };

            _mockRepository.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new ArgumentException("处方必须包含至少一种药材"));

            // Act
            var act = async () => await _service.CreateAsync(createDto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("处方必须包含至少一种药材");
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ReturnsUpdatedPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var updateDto = new PrescriptionEditDto
            {
                Id = prescriptionId,
                Diagnosis = "更新后的诊断",
                Items = new List<PrescriptionItemEditDto>
                {
                    new PrescriptionItemEditDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 25,
                        Price = 6.00m
                    }
                },
                Remark = "更新后的备注"
            };

            var updatedPrescription = new PrescriptionDto
            {
                Id = prescriptionId,
                Diagnosis = updateDto.Diagnosis,
                TotalPrice = 150.00m,
                UpdateTime = DateTime.Now
            };

            _mockRepository.Setup(x => x.UpdateAsync(updateDto))
                .ReturnsAsync(updatedPrescription);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Diagnosis.Should().Be("更新后的诊断");
        }

        [Fact]
        public async Task UpdateAsync_CompletedPrescription_ThrowsInvalidOperationException()
        {
            // Arrange
            var updateDto = new PrescriptionEditDto
            {
                Id = Guid.NewGuid(),
                Diagnosis = "尝试更新"
            };

            _mockRepository.Setup(x => x.UpdateAsync(updateDto))
                .ThrowsAsync(new InvalidOperationException("已完成的处方不能修改"));

            // Act
            var act = async () => await _service.UpdateAsync(updateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("已完成的处方不能修改");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_DraftPrescription_ReturnsTrue()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            _mockRepository.Setup(x => x.DeleteAsync(prescriptionId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteAsync(prescriptionId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_CompletedPrescription_ReturnsFalse()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            _mockRepository.Setup(x => x.DeleteAsync(prescriptionId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteAsync(prescriptionId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CompleteAsync Tests

        [Fact]
        public async Task CompleteAsync_DraftPrescription_ReturnsCompletedPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var completedPrescription = new PrescriptionDto
            {
                Id = prescriptionId,
                Status = PrescriptionStatus.Completed,
                CompleteTime = DateTime.Now
            };

            _mockRepository.Setup(x => x.CompleteAsync(prescriptionId))
                .ReturnsAsync(completedPrescription);

            // Act
            var result = await _service.CompleteAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PrescriptionStatus.Completed);
            result.CompleteTime.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteAsync_AlreadyCompletedPrescription_ThrowsInvalidOperationException()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            _mockRepository.Setup(x => x.CompleteAsync(prescriptionId))
                .ThrowsAsync(new InvalidOperationException("处方已经完成"));

            // Act
            var act = async () => await _service.CompleteAsync(prescriptionId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("处方已经完成");
        }

        #endregion

        #region CancelAsync Tests

        [Fact]
        public async Task CancelAsync_DraftPrescription_ReturnsCancelledPrescription()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var cancelledPrescription = new PrescriptionDto
            {
                Id = prescriptionId,
                Status = PrescriptionStatus.Cancelled,
                CancelTime = DateTime.Now,
                CancelReason = "患者取消"
            };

            _mockRepository.Setup(x => x.CancelAsync(prescriptionId))
                .ReturnsAsync(cancelledPrescription);

            // Act
            var result = await _service.CancelAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PrescriptionStatus.Cancelled);
            result.CancelTime.Should().NotBeNull();
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task GetPagedAsync_RepositoryThrowsException_PropagatesException()
        {
            // Arrange
            _mockRepository.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<PrescriptionStatus?>())
            ).ThrowsAsync(new Exception("数据库连接失败"));

            // Act
            var act = async () => await _service.GetPagedAsync(1, 10);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("数据库连接失败");
        }

        [Fact]
        public async Task CreateAsync_CalculatesTotalPrice_ReturnsCorrectTotal()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Diagnosis = "测试",
                Items = new List<PrescriptionItemCreateDto>
                {
                    new PrescriptionItemCreateDto { HerbId = Guid.NewGuid(), Quantity = 10, Price = 5.00m }, // 50
                    new PrescriptionItemCreateDto { HerbId = Guid.NewGuid(), Quantity = 20, Price = 3.00m }, // 60
                    new PrescriptionItemCreateDto { HerbId = Guid.NewGuid(), Quantity = 5, Price = 10.00m }  // 50
                    // Total: 160
                }
            };

            var createdPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                TotalPrice = 160.00m // 正确计算的总价
            };

            _mockRepository.Setup(x => x.CreateAsync(It.IsAny<PrescriptionCreateDto>()))
                .ReturnsAsync(createdPrescription);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.TotalPrice.Should().Be(160.00m);
        }

        #endregion
    }
}