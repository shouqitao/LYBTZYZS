using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.Patients.Tests.Services
{
    /// <summary>
    /// PatientService UltraThink委托模式测试
    /// 验证纯委托架构的正确性和完整性
    /// </summary>
    public class PatientServiceUltraThinkTests
    {
        private readonly Mock<IPatientQueryService> _mockQueryService;
        private readonly Mock<IPatientBusinessService> _mockBusinessService;
        private readonly PatientService _patientService;

        public PatientServiceUltraThinkTests()
        {
            _mockQueryService = new Mock<IPatientQueryService>();
            _mockBusinessService = new Mock<IPatientBusinessService>();
            _patientService = new PatientService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Arrange & Act
            var service = new PatientService(_mockQueryService.Object, _mockBusinessService.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullQueryService_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new PatientService(null!, _mockBusinessService.Object));
            
            exception.ParamName.Should().Be("queryService");
        }

        [Fact]
        public void Constructor_WithNullBusinessService_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new PatientService(_mockQueryService.Object, null!));
            
            exception.ParamName.Should().Be("businessService");
        }

        #endregion

        #region Query Delegation Tests

        [Fact]
        public async Task GetPagedAsync_DelegatesToQueryService()
        {
            // Arrange
            var query = new PagedQueryBaseDto { CurrentPage = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<PatientDto>>.Success(new PagedResult<PatientDto>());
            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetPagedAsync(query);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockQueryService.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_DelegatesToQueryService()
        {
            // Arrange
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockQueryService.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetAllAsync();

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetActivePatientsAsync_DelegatesToQueryService()
        {
            // Arrange
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockQueryService.Setup(x => x.GetActivePatientsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetActivePatientsAsync();

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetActivePatientsAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DelegatesToQueryService()
        {
            // Arrange
            var keyword = "test";
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task AdvancedSearchAsync_DelegatesToQueryService()
        {
            // Arrange
            var searchDto = new PatientSearchDto();
            var expectedResult = ServiceResult<PagedResult<PatientDto>>.Success(new PagedResult<PatientDto>());
            _mockQueryService.Setup(x => x.AdvancedSearchAsync(searchDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.AdvancedSearchAsync(searchDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.AdvancedSearchAsync(searchDto), Times.Once);
        }

        [Fact]
        public async Task GetByIDNumberAsync_DelegatesToQueryService()
        {
            // Arrange
            var idNumber = "123456789012345678";
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockQueryService.Setup(x => x.GetByIDNumberAsync(idNumber)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIDNumberAsync(idNumber);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIDNumberAsync(idNumber), Times.Once);
        }

        [Fact]
        public async Task GetByPhoneNumberAsync_DelegatesToQueryService()
        {
            // Arrange
            var phoneNumber = "13800138000";
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockQueryService.Setup(x => x.GetByPhoneNumberAsync(phoneNumber)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByPhoneNumberAsync(phoneNumber);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        [Fact]
        public async Task GetByIdCardAsync_DelegatesToQueryService()
        {
            // Arrange
            var idCard = "123456789012345678";
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockQueryService.Setup(x => x.GetByIdCardAsync(idCard)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdCardAsync(idCard);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdCardAsync(idCard), Times.Once);
        }

        [Fact]
        public async Task GetByPhoneAsync_DelegatesToQueryService()
        {
            // Arrange
            var phone = "13800138000";
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockQueryService.Setup(x => x.GetByPhoneAsync(phone)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByPhoneAsync(phone);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByPhoneAsync(phone), Times.Once);
        }

        [Fact]
        public async Task CheckDuplicatePatientsAsync_DelegatesToQueryService()
        {
            // Arrange
            var createDto = new PatientCreateDto();
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockQueryService.Setup(x => x.CheckDuplicatePatientsAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CheckDuplicatePatientsAsync(createDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.CheckDuplicatePatientsAsync(createDto), Times.Once);
        }

        #endregion

        #region Business Delegation Tests

        [Fact]
        public async Task CreateAsync_DelegatesToBusinessService()
        {
            // Arrange
            var createDto = new PatientCreateDto();
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.CreateAsync(createDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_DelegatesToBusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var updateDto = new PatientUpdateDto();
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockBusinessService.Setup(x => x.UpdateAsync(patientId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.UpdateAsync(patientId, updateDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(patientId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithSingleId_DelegatesToBusinessService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<PatientDto>.Success(new PatientDto());
            _mockBusinessService.Setup(x => x.DeleteAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DeleteAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithMultipleIds_DelegatesToBusinessService()
        {
            // Arrange
            var patientIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.DeleteAsync(patientIds)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DeleteAsync(patientIds);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(patientIds), Times.Once);
        }

        [Fact]
        public async Task SetStatusAsync_DelegatesToBusinessService()
        {
            // Arrange
            var patientIds = new List<Guid> { Guid.NewGuid() };
            var status = "Active";
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.SetStatusAsync(patientIds, status)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.SetStatusAsync(patientIds, status);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.SetStatusAsync(patientIds, status), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_DelegatesToBusinessService()
        {
            // Arrange
            var patientIds = new List<Guid> { Guid.NewGuid() };
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.EnableAsync(patientIds)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.EnableAsync(patientIds);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.EnableAsync(patientIds), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_DelegatesToBusinessService()
        {
            // Arrange
            var patientIds = new List<Guid> { Guid.NewGuid() };
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.DisableAsync(patientIds)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.DisableAsync(patientIds);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.DisableAsync(patientIds), Times.Once);
        }

        [Fact]
        public async Task ImportPatientsAsync_DelegatesToBusinessService()
        {
            // Arrange
            var importDtos = new List<PatientImportDto>();
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockBusinessService.Setup(x => x.ImportPatientsAsync(importDtos)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ImportPatientsAsync(importDtos);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ImportPatientsAsync(importDtos), Times.Once);
        }

        [Fact]
        public async Task ExportPatientsAsync_DelegatesToBusinessService()
        {
            // Arrange
            var exportDto = new PatientExportDto();
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockBusinessService.Setup(x => x.ExportPatientsAsync(exportDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ExportPatientsAsync(exportDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ExportPatientsAsync(exportDto), Times.Once);
        }

        [Fact]
        public async Task ValidatePatientAsync_DelegatesToBusinessService()
        {
            // Arrange
            var createDto = new PatientCreateDto();
            var expectedResult = ServiceResult<List<string>>.Success(new List<string>());
            _mockBusinessService.Setup(x => x.ValidatePatientAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.ValidatePatientAsync(createDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ValidatePatientAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task GetImportTemplateAsync_DelegatesToBusinessService()
        {
            // Arrange
            var expectedResult = ServiceResult<object>.Success(new object());
            _mockBusinessService.Setup(x => x.GetImportTemplateAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetImportTemplateAsync();

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.GetImportTemplateAsync(), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetByIdAsync_WithEmptyGuid_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.Empty;
            var expectedResult = ServiceResult<PatientDto>.Failure("无效的患者ID");
            _mockQueryService.Setup(x => x.GetByIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.GetByIdAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_DelegatesToQueryService()
        {
            // Arrange
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        #endregion

        #region Architecture Compliance Tests

        [Fact]
        public void Service_ShouldImplementIPatientService()
        {
            // Assert
            _patientService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IPatientService>();
        }

        [Fact]
        public void Service_ShouldOnlyDependOnInterfaces()
        {
            // Arrange
            var constructor = typeof(PatientService).GetConstructors()[0];
            var parameters = constructor.GetParameters();

            // Assert
            parameters.Should().HaveCount(2);
            parameters[0].ParameterType.Should().Be<IPatientQueryService>();
            parameters[1].ParameterType.Should().Be<IPatientBusinessService>();
        }

        [Fact]
        public void Service_ShouldNotContainBusinessLogic()
        {
            // Arrange & Act
            var methods = typeof(PatientService).GetMethods();
            var publicMethods = methods.Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == typeof(PatientService));

            // Assert - 所有方法都应该是简单的委托，不包含复杂业务逻辑
            foreach (var method in publicMethods)
            {
                method.Name.Should().NotContain("Validate");
                method.Name.Should().NotContain("Calculate");
                method.Name.Should().NotContain("Process");
            }
        }

        [Fact]
        public void Service_ShouldFollowNamingConventions()
        {
            // Assert
            typeof(PatientService).Name.Should().EndWith("Service");
            typeof(PatientService).Namespace.Should().Be("LYBT.Module.Patients.Services");
        }

        #endregion

        #region Integration Pattern Tests

        [Fact]
        public async Task CompletePatientWorkflow_ShouldDelegateCorrectly()
        {
            // Arrange
            var createDto = new PatientCreateDto { Name = "Test Patient" };
            var createdPatient = new PatientDto { Id = Guid.NewGuid(), Name = "Test Patient" };
            var updateDto = new PatientUpdateDto { Name = "Updated Patient" };
            var updatedPatient = new PatientDto { Id = createdPatient.Id, Name = "Updated Patient" };

            _mockBusinessService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(ServiceResult<PatientDto>.Success(createdPatient));
            _mockQueryService.Setup(x => x.GetByIdAsync(createdPatient.Id))
                .ReturnsAsync(ServiceResult<PatientDto>.Success(createdPatient));
            _mockBusinessService.Setup(x => x.UpdateAsync(createdPatient.Id, updateDto))
                .ReturnsAsync(ServiceResult<PatientDto>.Success(updatedPatient));

            // Act
            var createResult = await _patientService.CreateAsync(createDto);
            var getResult = await _patientService.GetByIdAsync(createdPatient.Id);
            var updateResult = await _patientService.UpdateAsync(createdPatient.Id, updateDto);

            // Assert
            createResult.Success.Should().BeTrue();
            getResult.Success.Should().BeTrue();
            updateResult.Success.Should().BeTrue();
            updateResult.Data.Name.Should().Be("Updated Patient");

            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
            _mockQueryService.Verify(x => x.GetByIdAsync(createdPatient.Id), Times.Once);
            _mockBusinessService.Verify(x => x.UpdateAsync(createdPatient.Id, updateDto), Times.Once);
        }

        #endregion
    }
}