using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests.Services
{
    /// <summary>
    /// PatientService 完整单元测试 - UltraThink双层架构
    /// 主Service委托模式测试，验证所有委托调用的正确性
    /// </summary>
    public class PatientServiceTests
    {
        private readonly PatientService _patientService;
        private readonly Mock<IPatientQueryService> _mockQueryService;
        private readonly Mock<IPatientBusinessService> _mockBusinessService;

        public PatientServiceTests()
        {
            _mockQueryService = new Mock<IPatientQueryService>();
            _mockBusinessService = new Mock<IPatientBusinessService>();
            _patientService = new PatientService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            // Act & Assert
            var action = () => new PatientService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            // Act & Assert
            var action = () => new PatientService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService_With_Correct_Mapping()
        {
            // Arrange
            var query = new PatientSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Keyword = "test"
            };
            var expectedResult = ServiceResult<PagedResult<PatientDto>>.Success(new PagedResult<PatientDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(It.IsAny<PagedQueryBaseDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(It.Is<PagedQueryBaseDto>(q =>
                q.PageIndex == query.PageIndex &&
                q.PageSize == query.PageSize &&
                q.Keyword == query.Keyword)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var patientDto = new PatientDto { Id = patientId, Name = "张三" };
            var expectedResult = ServiceResult<PatientDto>.Success(patientDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetByIdCardAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var idCard = "110101199001011234";
            var patientDto = new PatientDto { IdCard = idCard, Name = "张三" };
            var expectedResult = ServiceResult<PatientDto>.Success(patientDto);

            _mockQueryService.Setup(x => x.GetByIdCardAsync(idCard)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdCardAsync(idCard);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdCardAsync(idCard), Times.Once);
        }

        [Fact]
        public async Task GetByPhoneAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var phone = "13800138000";
            var patients = new List<PatientDto>
            {
                new() { Phone = phone, Name = "张三" }
            };
            var expectedResult = ServiceResult<List<PatientDto>>.Success(patients);

            _mockQueryService.Setup(x => x.GetByPhoneAsync(phone)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByPhoneAsync(phone);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByPhoneAsync(phone), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "张";
            var patients = new List<PatientDto>
            {
                new() { Name = "张三" },
                new() { Name = "张四" }
            };
            var expectedResult = ServiceResult<List<PatientDto>>.Success(patients);

            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "张三",
                Gender = Gender.Male,
                IdCard = "110101199001011234"
            };
            var createdPatient = new PatientDto { Id = Guid.NewGuid(), Name = "张三" };
            var expectedResult = ServiceResult<PatientDto>.Success(createdPatient);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientUpdateDto
            {
                Id = patientId,
                Name = "张三三",
                Phone = "13800138001"
            };
            var updatedPatient = new PatientDto { Id = patientId, Name = "张三三" };
            var expectedResult = ServiceResult<PatientDto>.Success(updatedPatient);

            _mockBusinessService.Setup(x => x.UpdateAsync(patientId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.UpdateAsync(updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(patientId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.DeleteAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(patientId), Times.Once);
        }

        #endregion

        #region 边界值和异常测试

        [Fact]
        public async Task Query_Methods_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<PatientDto>.Failure("查询失败");

            _mockQueryService.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Business_Methods_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var createDto = new PatientCreateDto { Name = "张三" };
            var expectedResult = ServiceResult<PatientDto>.Failure("创建失败");

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void PatientService_Should_Implement_IPatientService()
        {
            // Assert
            _patientService.Should().BeAssignableTo<IPatientService>();
        }

        #endregion
    }
}