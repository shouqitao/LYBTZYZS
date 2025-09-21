using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests.Services
{
    /// <summary>
    /// PatientService 完整单元测试 - UltraThink双层架构
    /// 主Service委托模式测试，验证所有委托调用的正确�?
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

        #region 构造函数测�?

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
            var patientDto = new PatientDto { IdNumber = idCard, Name = "张三" };
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
                new() { PhoneNumber = phone, Name = "张三" }
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
                IdNumber = "110101199001011234"
            };
            var createdPatient = new PatientDto { Id = Guid.NewGuid(), Name = "张三" };
            var expectedResult = ServiceResult<PatientDto>.Success(createdPatient);

            _mockBusinessService.Setup(x => x.CreateAsync(It.IsAny<PatientCreateDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientUpdateDto
            {
                Id = patientId,
                Name = "张三",
                PhoneNumber = "13800138001" // 正确的属性名
            };
            var updatedPatient = new PatientDto { Id = patientId, Name = "张三" };
            var expectedResult = ServiceResult<PatientDto>.Success(updatedPatient);

            _mockBusinessService.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<PatientUpdateDto>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(patientId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var deletedPatient = new PatientDto { Id = patientId, Name = "已删除患者" };
            var expectedResult = ServiceResult<PatientDto>.Success(deletedPatient);

            _mockBusinessService.Setup(x => x.DeleteAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockBusinessService.Verify(x => x.DeleteAsync(patientId), Times.Once);
        }

        #endregion

        #region 状态操作测试

        [Fact]
        public async Task EnableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult.Success();

            _mockBusinessService.Setup(x => x.EnableAsync(It.IsAny<List<Guid>>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.EnableAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockBusinessService.Verify(x => x.EnableAsync(It.Is<List<Guid>>(list =>
                list.Count == 1 && list[0] == patientId)), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult.Success();

            _mockBusinessService.Setup(x => x.DisableAsync(It.IsAny<List<Guid>>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DisableAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockBusinessService.Verify(x => x.DisableAsync(It.Is<List<Guid>>(list =>
                list.Count == 1 && list[0] == patientId)), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult.Failure("启用失败");

            _mockBusinessService.Setup(x => x.EnableAsync(It.IsAny<List<Guid>>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.EnableAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("启用失败");
        }

        [Fact]
        public async Task DisableAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult.Failure("禁用失败");

            _mockBusinessService.Setup(x => x.DisableAsync(It.IsAny<List<Guid>>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DisableAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("禁用失败");
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task ImportPatientsAsync_Should_Convert_And_Delegate_To_BusinessService()
        {
            // Arrange
            var patients = new List<PatientCreateDto>
            {
                new PatientCreateDto
                {
                    Name = "张三",
                    Gender = Gender.Male,
                    BirthDate = DateTime.Parse("1990-01-01"),
                    PhoneNumber = "13800138000",
                    IdNumber = "110101199001011234",
                    Address = "北京市朝阳区",
                    EmergencyContactName = "张四",
                    EmergencyContactPhone = "13800138001",
                    AllergyHistory = "青霉素过敏"
                }
            };

            var importedPatients = new List<PatientDto>
            {
                new PatientDto { Id = Guid.NewGuid(), Name = "张三" }
            };
            var expectedResult = ServiceResult<List<PatientDto>>.Success(importedPatients);

            _mockBusinessService.Setup(x => x.ImportPatientsAsync(It.IsAny<List<PatientImportDto>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ImportPatientsAsync(patients);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            _mockBusinessService.Verify(x => x.ImportPatientsAsync(It.Is<List<PatientImportDto>>(list =>
                list.Count == 1 &&
                list[0].Name == "张三" &&
                list[0].GenderText == "男" &&
                list[0].BirthDateText == "1990-01-01" &&
                list[0].PhoneNumber == "13800138000" &&
                list[0].IdCardNumber == "110101199001011234")), Times.Once);
        }

        [Fact]
        public async Task ImportPatientsAsync_Should_Handle_Female_Gender()
        {
            // Arrange
            var patients = new List<PatientCreateDto>
            {
                new PatientCreateDto
                {
                    Name = "李四",
                    Gender = Gender.Female,
                    BirthDate = DateTime.Parse("1995-05-05")
                }
            };

            var importedPatients = new List<PatientDto>
            {
                new PatientDto { Id = Guid.NewGuid(), Name = "李四" }
            };
            var expectedResult = ServiceResult<List<PatientDto>>.Success(importedPatients);

            _mockBusinessService.Setup(x => x.ImportPatientsAsync(It.IsAny<List<PatientImportDto>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ImportPatientsAsync(patients);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _mockBusinessService.Verify(x => x.ImportPatientsAsync(It.Is<List<PatientImportDto>>(list =>
                list.Count == 1 &&
                list[0].GenderText == "女")), Times.Once);
        }

        [Fact]
        public async Task ImportPatientsAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var patients = new List<PatientCreateDto>
            {
                new PatientCreateDto { Name = "测试患者" }
            };

            var expectedResult = ServiceResult<List<PatientDto>>.Failure("导入失败");

            _mockBusinessService.Setup(x => x.ImportPatientsAsync(It.IsAny<List<PatientImportDto>>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ImportPatientsAsync(patients);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("导入失败");
        }

        [Fact]
        public async Task ExportPatientsAsync_Should_Convert_To_CSV_Bytes()
        {
            // Arrange
            var query = new PagedQueryBaseDto
            {
                Keyword = "张",
                PageIndex = 1,
                PageSize = 10
            };

            var patients = new List<PatientDto>
            {
                new PatientDto
                {
                    Id = Guid.NewGuid(),
                    Name = "张三",
                    Gender = Gender.Male,
                    BirthDate = DateTime.Parse("1990-01-01"),
                    PhoneNumber = "13800138000",
                    IdNumber = "110101199001011234",
                    Address = "北京市朝阳区"
                },
                new PatientDto
                {
                    Id = Guid.NewGuid(),
                    Name = "张四",
                    Gender = Gender.Female,
                    BirthDate = DateTime.Parse("1995-05-05"),
                    PhoneNumber = "13800138001",
                    IdNumber = "110101199505051234",
                    Address = "上海市浦东区"
                }
            };

            var expectedResult = ServiceResult<List<PatientDto>>.Success(patients);

            _mockBusinessService.Setup(x => x.ExportPatientsAsync(It.IsAny<PatientExportDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ExportPatientsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var csvContent = System.Text.Encoding.UTF8.GetString(result.Data!);
            csvContent.Should().Contain("姓名,性别,出生日期,手机号码,身份证号,地址");
            csvContent.Should().Contain("张三,男,1990-01-01,13800138000,110101199001011234,北京市朝阳区");
            csvContent.Should().Contain("张四,女,1995-05-05,13800138001,110101199505051234,上海市浦东区");

            _mockBusinessService.Verify(x => x.ExportPatientsAsync(It.Is<PatientExportDto>(dto =>
                dto.Name == "张")), Times.Once);
        }

        [Fact]
        public async Task ExportPatientsAsync_Should_Handle_Empty_Result()
        {
            // Arrange
            var query = new PagedQueryBaseDto { Keyword = "不存在" };
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());

            _mockBusinessService.Setup(x => x.ExportPatientsAsync(It.IsAny<PatientExportDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ExportPatientsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var csvContent = System.Text.Encoding.UTF8.GetString(result.Data!);
            csvContent.Should().Be("姓名,性别,出生日期,手机号码,身份证号,地址\n");
        }

        [Fact]
        public async Task ExportPatientsAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var query = new PagedQueryBaseDto { Keyword = "test" };
            var expectedResult = ServiceResult<List<PatientDto>>.Failure("导出失败");

            _mockBusinessService.Setup(x => x.ExportPatientsAsync(It.IsAny<PatientExportDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ExportPatientsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("导出失败");
        }

        [Fact]
        public async Task ExportPatientsAsync_Should_Handle_Null_Keyword()
        {
            // Arrange
            var query = new PagedQueryBaseDto { Keyword = null };
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());

            _mockBusinessService.Setup(x => x.ExportPatientsAsync(It.IsAny<PatientExportDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ExportPatientsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _mockBusinessService.Verify(x => x.ExportPatientsAsync(It.Is<PatientExportDto>(dto =>
                dto.Name == string.Empty)), Times.Once);
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

            _mockBusinessService.Setup(x => x.CreateAsync(It.IsAny<PatientCreateDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

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
            _patientService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IPatientService>();
        }

        #endregion
    }
}
