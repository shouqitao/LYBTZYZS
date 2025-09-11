using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Common;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    /// <summary>
    /// MedicalCaseService UltraThink委托模式测试
    /// 验证纯委托架构的正确性和完整性
    /// </summary>
    public class MedicalCaseServiceUltraThinkTests
    {
        private readonly Mock<IMedicalCaseQueryService> _mockQueryService;
        private readonly Mock<IMedicalCaseBusinessService> _mockBusinessService;
        private readonly MedicalCaseService _medicalCaseService;

        public MedicalCaseServiceUltraThinkTests()
        {
            _mockQueryService = new Mock<IMedicalCaseQueryService>();
            _mockBusinessService = new Mock<IMedicalCaseBusinessService>();
            _medicalCaseService = new MedicalCaseService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidDependencies_ShouldInitialize()
        {
            // Arrange & Act
            var service = new MedicalCaseService(_mockQueryService.Object, _mockBusinessService.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullQueryService_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new MedicalCaseService(null!, _mockBusinessService.Object));
            
            exception.ParamName.Should().Be("queryService");
        }

        [Fact]
        public void Constructor_WithNullBusinessService_ShouldThrowArgumentNullException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new MedicalCaseService(_mockQueryService.Object, null!));
            
            exception.ParamName.Should().Be("businessService");
        }

        #endregion

        #region Query Delegation Tests

        [Fact]
        public async Task GetByIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(new MedicalCaseDto());
            _mockQueryService.Setup(x => x.GetByIdAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_DelegatesToQueryService()
        {
            // Arrange
            var query = new PagedQueryBaseDto { CurrentPage = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<MedicalCaseDto>>.Success(new PagedResult<MedicalCaseDto>());
            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetPagedAsync(query);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByPatientIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            _mockQueryService.Setup(x => x.GetByPatientIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetActiveByPatientIdAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(new MedicalCaseDto());
            _mockQueryService.Setup(x => x.GetActiveByPatientIdAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetActiveByPatientIdAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetActiveByPatientIdAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_DelegatesToQueryService()
        {
            // Arrange
            var keyword = "test";
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task GetHistoryAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            _mockQueryService.Setup(x => x.GetHistoryAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetHistoryAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetHistoryAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task HasActiveCaseAsync_DelegatesToQueryService()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockQueryService.Setup(x => x.HasActiveCaseAsync(patientId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.HasActiveCaseAsync(patientId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.HasActiveCaseAsync(patientId), Times.Once);
        }

        [Fact]
        public async Task GetStatisticsAsync_DelegatesToQueryService()
        {
            // Arrange
            var expectedResult = ServiceResult<object>.Success(new object());
            _mockQueryService.Setup(x => x.GetStatisticsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetStatisticsAsync();

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetStatisticsAsync(), Times.Once);
        }

        #endregion

        #region Business Delegation Tests

        [Fact]
        public async Task CreateAsync_DelegatesToBusinessService()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto();
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(new MedicalCaseDto());
            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.CreateAsync(createDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var updateDto = new MedicalCaseUpdateDto();
            var expectedResult = ServiceResult<MedicalCaseDto>.Success(new MedicalCaseDto());
            _mockBusinessService.Setup(x => x.UpdateAsync(caseId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.UpdateAsync(caseId, updateDto);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(caseId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.DeleteAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.DeleteAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task CompleteAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.CompleteAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.CompleteAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.CompleteAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task SuspendAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.SuspendAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SuspendAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.SuspendAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task ResumeAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.ResumeAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.ResumeAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ResumeAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task ArchiveAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.ArchiveAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.ArchiveAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ArchiveAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var status = "Completed";
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.UpdateStatusAsync(caseId, status)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.UpdateStatusAsync(caseId, status);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateStatusAsync(caseId, status), Times.Once);
        }

        [Fact]
        public async Task CancelConsultationAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.CancelConsultationAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.CancelConsultationAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.CancelConsultationAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var status = "Completed";
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.BatchUpdateStatusAsync(caseIds, status)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.BatchUpdateStatusAsync(caseIds, status);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.BatchUpdateStatusAsync(caseIds, status), Times.Once);
        }

        [Fact]
        public async Task PrintMedicalRecordAsync_DelegatesToBusinessService()
        {
            // Arrange
            var caseId = Guid.NewGuid();
            var printOptions = new object();
            var expectedResult = ServiceResult<object>.Success(new object());
            _mockBusinessService.Setup(x => x.PrintMedicalRecordAsync(caseId, printOptions)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.PrintMedicalRecordAsync(caseId, printOptions);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.PrintMedicalRecordAsync(caseId, printOptions), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetByIdAsync_WithEmptyGuid_DelegatesToQueryService()
        {
            // Arrange
            var caseId = Guid.Empty;
            var expectedResult = ServiceResult<MedicalCaseDto>.Failure("无效的案例ID");
            _mockQueryService.Setup(x => x.GetByIdAsync(caseId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.GetByIdAsync(caseId);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(caseId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_DelegatesToQueryService()
        {
            // Arrange
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _medicalCaseService.SearchAsync(keyword);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        #endregion

        #region Architecture Compliance Tests

        [Fact]
        public void Service_ShouldImplementIMedicalCaseService()
        {
            // Assert
            _medicalCaseService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IMedicalCaseService>();
        }

        [Fact]
        public void Service_ShouldOnlyDependOnInterfaces()
        {
            // Arrange
            var constructor = typeof(MedicalCaseService).GetConstructors()[0];
            var parameters = constructor.GetParameters();

            // Assert
            parameters.Should().HaveCount(2);
            parameters[0].ParameterType.Should().Be<IMedicalCaseQueryService>();
            parameters[1].ParameterType.Should().Be<IMedicalCaseBusinessService>();
        }

        [Fact]
        public void Service_ShouldNotContainBusinessLogic()
        {
            // Arrange & Act
            var methods = typeof(MedicalCaseService).GetMethods();
            var publicMethods = methods.Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == typeof(MedicalCaseService));

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
            typeof(MedicalCaseService).Name.Should().EndWith("Service");
            typeof(MedicalCaseService).Namespace.Should().Be("LYBT.Module.MedicalCase.Services");
        }

        #endregion

        #region Integration Pattern Tests

        [Fact]
        public async Task CompleteMedicalCaseWorkflow_ShouldDelegateCorrectly()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var createDto = new MedicalCaseCreateDto { PatientId = patientId };
            var createdCase = new MedicalCaseDto { Id = Guid.NewGuid(), PatientId = patientId };
            var updateDto = new MedicalCaseUpdateDto();
            var updatedCase = new MedicalCaseDto { Id = createdCase.Id, PatientId = patientId };

            _mockBusinessService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(ServiceResult<MedicalCaseDto>.Success(createdCase));
            _mockQueryService.Setup(x => x.GetByIdAsync(createdCase.Id))
                .ReturnsAsync(ServiceResult<MedicalCaseDto>.Success(createdCase));
            _mockBusinessService.Setup(x => x.UpdateAsync(createdCase.Id, updateDto))
                .ReturnsAsync(ServiceResult<MedicalCaseDto>.Success(updatedCase));
            _mockBusinessService.Setup(x => x.CompleteAsync(createdCase.Id))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var createResult = await _medicalCaseService.CreateAsync(createDto);
            var getResult = await _medicalCaseService.GetByIdAsync(createdCase.Id);
            var updateResult = await _medicalCaseService.UpdateAsync(createdCase.Id, updateDto);
            var completeResult = await _medicalCaseService.CompleteAsync(createdCase.Id);

            // Assert
            createResult.Success.Should().BeTrue();
            getResult.Success.Should().BeTrue();
            updateResult.Success.Should().BeTrue();
            completeResult.Success.Should().BeTrue();

            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
            _mockQueryService.Verify(x => x.GetByIdAsync(createdCase.Id), Times.Once);
            _mockBusinessService.Verify(x => x.UpdateAsync(createdCase.Id, updateDto), Times.Once);
            _mockBusinessService.Verify(x => x.CompleteAsync(createdCase.Id), Times.Once);
        }

        [Fact]
        public async Task MedicalCaseLifecycleManagement_ShouldDelegateCorrectly()
        {
            // Arrange
            var caseId = Guid.NewGuid();

            _mockBusinessService.Setup(x => x.SuspendAsync(caseId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));
            _mockBusinessService.Setup(x => x.ResumeAsync(caseId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));
            _mockBusinessService.Setup(x => x.CompleteAsync(caseId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));
            _mockBusinessService.Setup(x => x.ArchiveAsync(caseId))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var suspendResult = await _medicalCaseService.SuspendAsync(caseId);
            var resumeResult = await _medicalCaseService.ResumeAsync(caseId);
            var completeResult = await _medicalCaseService.CompleteAsync(caseId);
            var archiveResult = await _medicalCaseService.ArchiveAsync(caseId);

            // Assert
            suspendResult.Success.Should().BeTrue();
            resumeResult.Success.Should().BeTrue();
            completeResult.Success.Should().BeTrue();
            archiveResult.Success.Should().BeTrue();

            _mockBusinessService.Verify(x => x.SuspendAsync(caseId), Times.Once);
            _mockBusinessService.Verify(x => x.ResumeAsync(caseId), Times.Once);
            _mockBusinessService.Verify(x => x.CompleteAsync(caseId), Times.Once);
            _mockBusinessService.Verify(x => x.ArchiveAsync(caseId), Times.Once);
        }

        #endregion
    }
}