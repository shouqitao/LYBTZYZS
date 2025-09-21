using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services
{
    /// <summary>
    /// ConsultationService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class ConsultationServiceTests
    {
        private readonly ConsultationService _consultationService;
        private readonly Mock<IConsultationQueryService> _mockQueryService;
        private readonly Mock<IConsultationBusinessService> _mockBusinessService;

        public ConsultationServiceTests()
        {
            _mockQueryService = new Mock<IConsultationQueryService>();
            _mockBusinessService = new Mock<IConsultationBusinessService>();
            _consultationService = new ConsultationService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new ConsultationService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new ConsultationService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new PagedQueryBaseDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<ConsultationDto>>.Success(new PagedResult<ConsultationDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var consultationDetailDto = new ConsultationDetailDto { Id = consultationId };
            var expectedResult = ServiceResult<ConsultationDetailDto>.Success(consultationDetailDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(consultationId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.GetByIdAsync(consultationId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(consultationId), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task StartAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var startDto = new ConsultationStartDto { MedicalCaseId = Guid.NewGuid() };
            var createdConsultation = new ConsultationDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<ConsultationDto>.Success(createdConsultation);

            _mockBusinessService.Setup(x => x.StartAsync(startDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.StartAsync(startDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.StartAsync(startDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var consultationId = Guid.NewGuid();
            var updateDto = new ConsultationDetailDto { Id = consultationId };
            var updatedConsultation = new ConsultationDto { Id = consultationId };
            var expectedResult = ServiceResult<ConsultationDto>.Success(updatedConsultation);

            _mockBusinessService.Setup(x => x.UpdateAsync(consultationId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _consultationService.UpdateAsync(consultationId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(consultationId, updateDto), Times.Once);
        }

        #endregion

        #region TCM诊断测试

        // TCM诊断相关方法已在新架构中整合到UpdateAsync方法中
        // 这些专门的更新方法不再需要单独测试

        #endregion

        #region 边界值测试

        [Fact]
        public void ConsultationService_Should_Implement_IConsultationService()
        {
            _consultationService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IConsultationService>();
        }

        #endregion
    }
}