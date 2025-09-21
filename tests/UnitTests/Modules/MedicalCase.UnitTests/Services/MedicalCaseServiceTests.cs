using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class MedicalCaseServiceTests
    {
        private readonly MedicalCaseService _medicalCaseService;
        private readonly Mock<IMedicalCaseQueryService> _mockQueryService;
        private readonly Mock<IMedicalCaseBusinessService> _mockBusinessService;

        public MedicalCaseServiceTests()
        {
            _mockQueryService = new Mock<IMedicalCaseQueryService>();
            _mockBusinessService = new Mock<IMedicalCaseBusinessService>();
            _medicalCaseService = new MedicalCaseService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new MedicalCaseService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new MedicalCaseService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new MedicalCaseSearchDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<MedicalCaseDto>>.Success(new PagedResult<MedicalCaseDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var caseDto = new MedicalCaseDto { Id = caseId };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(caseDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(caseId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(caseId), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto { PatientId = Guid.NewGuid() };
            var createdCase = new MedicalCaseDto { Id = Guid.NewGuid() };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(createdCase);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto { Id = caseId };
            var updatedCase = new MedicalCaseDto { Id = caseId };
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(updatedCase);

            _mockBusinessService.Setup(x => x.UpdateAsync(caseId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.UpdateAsync(caseId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(caseId, updateDto), Times.Once);
        }

        #endregion

        #region 边界值测试

        [Fact]
        public void MedicalCaseService_Should_Implement_IMedicalCaseService()
        {
            _medicalCaseService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IMedicalCaseService>();
        }

        #endregion
    }
}