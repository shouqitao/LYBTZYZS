using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests
{
    /// <summary>
    /// PrescriptionService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimplePrescriptionServiceTests
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly Mock<IPrescriptionQueryService> _mockQueryService;
        private readonly Mock<IPrescriptionBusinessService> _mockBusinessService;

        public SimplePrescriptionServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IPrescriptionQueryService>();
            _mockBusinessService = new Mock<IPrescriptionBusinessService>();

            // 创建 PrescriptionService 实例 (主Service委托模式)
            _prescriptionService = new PrescriptionService(
                _mockQueryService.Object,
                _mockBusinessService.Object);
        }

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Prescription_When_Exists()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedResult = ServiceResult<PrescriptionDto>.Success(new PrescriptionDto
            {
                Id = prescriptionId,
                PatientId = Guid.NewGuid(),
                Status = CommonStatus.Enabled
            });

            _mockQueryService
                .Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(prescriptionId);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Not_Found()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedResult = ServiceResult<PrescriptionDto>.Failure("处方不存在");

            _mockQueryService
                .Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("处方不存在");
        }

        #endregion

        #region GetByPatientIdAsync 测试

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_Patient_Prescriptions()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var prescriptions = new List<PrescriptionDto>
            {
                new() { Id = Guid.NewGuid(), PatientId = patientId },
                new() { Id = Guid.NewGuid(), PatientId = patientId }
            };

            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().OnlyContain(p => p.PatientId == patientId);
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paged_Result()
        {
            // Arrange
            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            var prescriptions = new List<PrescriptionDto>
            {
                new() { Id = Guid.NewGuid(), }, // Name字段已删除
                new() { Id = Guid.NewGuid(), } // Name字段已删除
            };

            var expectedResult = ServiceResult<PagedResult<PrescriptionDto>>.Success(new PagedResult<PrescriptionDto>
            {
                Items = prescriptions,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Matching_Prescriptions()
        {
            // Arrange
            var keyword = "测试";
            var prescriptions = new List<PrescriptionDto>
            {
                new() { Id = Guid.NewGuid(), }, // Name字段已删除
                new() { Id = Guid.NewGuid(), } // Name字段已删除
            };

            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_No_Match()
        {
            // Arrange
            var keyword = "不存在";
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion

        #region GetAllAsync 测试

        [Fact]
        public async Task GetAllAsync_Should_Return_All_Prescriptions()
        {
            // Arrange
            var prescriptions = new List<PrescriptionDto>
            {
                new() { Id = Guid.NewGuid(), }, // Name字段已删除
                new() { Id = Guid.NewGuid(), }, // Name字段已删除
                new() { Id = Guid.NewGuid(), } // Name字段已删除
            };

            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(prescriptions);

            _mockQueryService
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().OnlyContain(p => p.Id != Guid.Empty);
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Empty_When_QueryService_Fails()
        {
            // Arrange
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Failure("查询失败");

            _mockQueryService
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region 异常分支和边界值测试 (成功经验应用)

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Prescription_Not_Found()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var expectedResult = ServiceResult<PrescriptionDto>.Failure("处方不存在");

            _mockQueryService
                .Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("处方不存在");
        }

        [Fact]
        public async Task GetByIdAsync_With_Empty_Guid_Should_Still_Work()
        {
            // Arrange - 边界值：空GUID
            var prescriptionId = Guid.Empty;
            var expectedResult = ServiceResult<PrescriptionDto>.Success(new PrescriptionDto
            {
                Id = Guid.Empty,
                PatientId = Guid.NewGuid(),
                // Name = "边界测试处方" // Name字段已删除
            });

            _mockQueryService
                .Setup(x => x.GetByIdAsync(prescriptionId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task GetPagedAsync_With_Large_PageSize_Should_Handle_Gracefully()
        {
            // Arrange - 极端值测试：大分页尺寸
            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 999999 // 极端大值
            };

            var prescriptions = new List<PrescriptionDto>
            {
                new() { Id = Guid.NewGuid(), }, // Name字段已删除
                new() { Id = Guid.NewGuid(), } // Name字段已删除
            };

            var expectedResult = ServiceResult<PagedResult<PrescriptionDto>>.Success(new PagedResult<PrescriptionDto>
            {
                Items = prescriptions,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 999999
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.PageSize.Should().Be(999999);
        }

        [Fact]
        public async Task SearchAsync_With_Empty_Keyword_Should_Return_Empty_List()
        {
            // Arrange - 空值测试
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 业务失败分支测试
            var keyword = "测试";
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Failure("查询服务异常");

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("查询服务异常");
        }

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 删除/查询失败测试
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<PrescriptionDto>>.Failure("患者处方查询失败");

            _mockQueryService
                .Setup(x => x.GetByPatientIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者处方查询失败");
        }

        #endregion
    }
}
