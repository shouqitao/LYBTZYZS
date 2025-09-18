using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests
{
    /// <summary>
    /// PatientService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimplePatientServiceTests
    {
        private readonly PatientService _patientService;
        private readonly Mock<IPatientQueryService> _mockQueryService;
        private readonly Mock<IPatientBusinessService> _mockBusinessService;

        public SimplePatientServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IPatientQueryService>();
            _mockBusinessService = new Mock<IPatientBusinessService>();

            // 创建 PatientService 实例 (主Service委托模式)
            _patientService = new PatientService(
                _mockQueryService.Object,
                _mockBusinessService.Object
            );
        }

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_Should_Return_Success_When_BusinessService_Succeeds()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "测试患者",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30)
            };

            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30),
                Status = CommonStatus.Enabled
            });

            _mockBusinessService
                .Setup(x => x.CreateAsync(It.IsAny<PatientCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("测试患者");
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Patient_When_Exists()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto
            {
                Id = patientId,
                Name = "测试患者",
                Status = CommonStatus.Enabled
            });

            _mockQueryService
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(patientId);
            result.Data.Name.Should().Be("测试患者");
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Empty_Result_When_No_Patients()
        {
            // Arrange
            var query = new PatientPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            var expectedResult = ServiceResult<PagedResult<PatientDto>>.Success(new PagedResult<PatientDto>
            {
                Items = new List<PatientDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(It.IsAny<PagedQueryBaseDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_Should_Return_Success_When_BusinessService_Succeeds()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            _mockBusinessService
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync(ServiceResult<PatientDto>.Success(new PatientDto { Id = patientId, Name = "已删除患者" }));

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_List_When_No_Match()
        {
            // Arrange
            var keyword = "不存在的关键字";
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion

        #region 异常分支和边界值测试

        [Fact]
        public async Task CreateAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var dto = new PatientCreateDto
            {
                Name = "测试患者",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30)
            };

            var failureResult = ServiceResult<PatientDto>.Failure("创建患者失败");

            _mockBusinessService
                .Setup(x => x.CreateAsync(It.IsAny<PatientCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failureResult);

            // Act
            var result = await _patientService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("创建患者失败");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Patient_Not_Found()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var failureResult = ServiceResult<PatientDto>.Failure("患者不存在");

            _mockQueryService
                .Setup(x => x.GetByIdAsync(patientId))
                .ReturnsAsync(failureResult);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("患者不存在");
        }

        [Fact]
        public async Task CreateAsync_With_Empty_Guid_Should_Still_Work()
        {
            // Arrange - 边界值测试：空GUID
            var dto = new PatientCreateDto
            {
                Name = "边界测试患者",
                Gender = Gender.Female,
                BirthDate = DateTime.Now.AddYears(-1) // 1岁患者
            };

            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto
            {
                Id = Guid.Empty, // 边界值：空GUID
                Name = "边界测试患者",
                Gender = Gender.Female,
                Status = CommonStatus.Enabled
            });

            _mockBusinessService
                .Setup(x => x.CreateAsync(It.IsAny<PatientCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task GetPagedAsync_With_Large_PageSize_Should_Handle_Gracefully()
        {
            // Arrange - 边界值测试：大页面大小
            var query = new PatientPagedQueryDto
            {
                PageIndex = 1,
                PageSize = int.MaxValue // 极端值
            };

            var expectedResult = ServiceResult<PagedResult<PatientDto>>.Success(new PagedResult<PatientDto>
            {
                Items = new List<PatientDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = int.MaxValue
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(It.IsAny<PagedQueryBaseDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.PageSize.Should().Be(int.MaxValue);
        }

        [Fact]
        public async Task SearchAsync_With_Empty_Keyword_Should_Return_Empty_List()
        {
            // Arrange - 边界值测试：空关键字
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 异常分支：删除失败
            var patientId = Guid.NewGuid();
            var failureResult = ServiceResult<PatientDto>.Failure("删除失败，患者可能有关联数据");

            _mockBusinessService
                .Setup(x => x.DeleteAsync(It.IsAny<Guid>()))
                .ReturnsAsync(failureResult);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeFalse();
        }

        #endregion

    }
}