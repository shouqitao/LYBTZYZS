using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.Prescriptions.Tests.Services
{
    /// <summary>
    /// PrescriptionService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class PrescriptionServiceTests
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly Mock<IPrescriptionQueryService> _mockQueryService;
        private readonly Mock<IPrescriptionBusinessService> _mockBusinessService;

        public PrescriptionServiceTests()
        {
            _mockQueryService = new Mock<IPrescriptionQueryService>();
            _mockBusinessService = new Mock<IPrescriptionBusinessService>();
            _prescriptionService = new PrescriptionService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new PrescriptionService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new PrescriptionService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new PrescriptionQueryDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<PrescriptionDto>>.Success(new PagedResult<PrescriptionDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(It.IsAny<PrescriptionQueryDto>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(It.Is<PrescriptionQueryDto>(q => q.PageIndex == 1 && q.PageSize == 10)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var prescriptionDto = new PrescriptionDto { Id = prescriptionId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(prescriptionDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(prescriptionId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.GetByIdAsync(prescriptionId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(prescriptionId), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new PrescriptionCreateDto { ConsultationId = Guid.NewGuid() };
            var createdPrescription = new PrescriptionDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(createdPrescription);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var updateDto = new PrescriptionEditDto { Id = prescriptionId };
            var updatedPrescription = new PrescriptionDto { Id = prescriptionId };
            var expectedResult = ServiceResult<PrescriptionDto>.Success(updatedPrescription);

            _mockBusinessService.Setup(x => x.UpdateAsync(prescriptionId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _prescriptionService.UpdateAsync(prescriptionId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(prescriptionId, updateDto), Times.Once);
        }

        #endregion

        // Note: 处方项操作和兼容性检查方法在当前接口中不存在，已移除相关测试

        #region 边界值测试

        [Fact]
        public void PrescriptionService_Should_Implement_IPrescriptionService()
        {
            _prescriptionService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IPrescriptionService>();
        }

        #endregion
    }
}