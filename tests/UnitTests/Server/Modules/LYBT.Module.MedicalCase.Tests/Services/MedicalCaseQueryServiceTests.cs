using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultations.Consultation;

namespace LYBT.Module.MedicalCases.Tests.Services
{
    /// <summary>
    /// Phase 3: MedicalCaseQueryService单元测试
    /// 测试范围：Query Service（读操作）
    /// </summary>
    public class MedicalCaseQueryServiceTests : TestBase
    {
        private readonly MedicalCaseQueryService _service;
        private readonly IMedicalCaseRepository _repositoryMock;
        private readonly ILogger<MedicalCaseQueryService> _loggerMock;

        public MedicalCaseQueryServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _loggerMock = CreateLoggerMock<MedicalCaseQueryService>();

            _service = new MedicalCaseQueryService(
                _repositoryMock,
                _loggerMock);
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnMedicalCase()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                PatientName = "张三",
                CaseStatus = MedicalCaseStatus.Active,
                Consultation = new ConsultationEntity { Id = medicalCaseId }
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);

            // Act
            var result = await _service.GetByIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(medicalCaseId);
            result.PatientName.Should().Be("张三");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns((MedicalCaseEntity?)null);

            // Act
            var result = await _service.GetByIdAsync(medicalCaseId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetListAsync Tests

        [Fact]
        public async Task GetListAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var status = MedicalCaseStatus.Active;
            var patientId = Guid.NewGuid();
            var page = 1;
            var pageSize = 10;

            var medicalCases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientId = patientId, CaseStatus = MedicalCaseStatus.Active },
                new MedicalCaseEntity { Id = Guid.NewGuid(), PatientId = patientId, CaseStatus = MedicalCaseStatus.Active }
            };

            var pagedResult = new PagedResult<MedicalCaseEntity>
            {
                Items = medicalCases,
                TotalCount = 2,
                CurrentPage = page,
                PageSize = pageSize
            };

            _repositoryMock.GetPagedWithDetailsAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<string?>())
                .Returns(pagedResult);

            // Act
            var result = await _service.GetListAsync(status, patientId, page, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetListAsync_WithNoFilters_ShouldReturnAllActiveCases()
        {
            // Arrange
            var page = 1;
            var pageSize = 10;

            var medicalCases = new List<MedicalCaseEntity>
            {
                new MedicalCaseEntity { Id = Guid.NewGuid(), CaseStatus = MedicalCaseStatus.Active },
                new MedicalCaseEntity { Id = Guid.NewGuid(), CaseStatus = MedicalCaseStatus.Completed }
            };

            var pagedResult = new PagedResult<MedicalCaseEntity>
            {
                Items = medicalCases,
                TotalCount = 2,
                CurrentPage = page,
                PageSize = pageSize
            };

            _repositoryMock.GetPagedWithDetailsAsync(page, pageSize, Arg.Any<string?>())
                .Returns(pagedResult);

            // Act
            var result = await _service.GetListAsync(null, null, page, pageSize);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
        }

        #endregion

        #region GetUnfinishedCaseByPatientIdAsync Tests

        [Fact]
        public async Task GetUnfinishedCaseByPatientIdAsync_WithActiveCase_ShouldReturnCase()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                UserId = doctorId,
                CaseStatus = MedicalCaseStatus.Active
            };

            _repositoryMock.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId)
                .Returns(medicalCase);

            // Act
            var result = await _service.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);

            // Assert
            result.Should().NotBeNull();
            result!.PatientId.Should().Be(patientId);
            result.CaseStatus.Should().Be(MedicalCaseStatus.Active);
        }

        [Fact]
        public async Task GetUnfinishedCaseByPatientIdAsync_WithNoActiveCase_ShouldReturnNull()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var doctorId = Guid.NewGuid();

            _repositoryMock.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId)
                .Returns((MedicalCaseEntity?)null);

            // Act
            var result = await _service.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetPendingCasesAsync Tests

        [Fact]
        public async Task GetPendingCasesAsync_WithValidDoctorId_ShouldReturnPendingCases()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var pendingCases = new List<PendingMedicalCaseDto>
            {
                new PendingMedicalCaseDto { PatientId = Guid.NewGuid(), PatientName = "张三" },
                new PendingMedicalCaseDto { PatientId = Guid.NewGuid(), PatientName = "李四" }
            };

            _repositoryMock.GetPendingCasesAsync(doctorId, null)
                .Returns(pendingCases);

            // Act
            var result = await _service.GetPendingCasesAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPendingCasesAsync_WithNoPendingCases_ShouldReturnEmptyList()
        {
            // Arrange
            var doctorId = Guid.NewGuid();

            _repositoryMock.GetPendingCasesAsync(doctorId, null)
                .Returns(new List<PendingMedicalCaseDto>());

            // Act
            var result = await _service.GetPendingCasesAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// OpenSpec: unify-pending-query-api - 按患者筛选待看诊医案
        /// </summary>
        [Fact]
        public async Task GetPendingCasesAsync_WithPatientId_ShouldFilterByPatient()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var patientId = Guid.NewGuid();
            var pendingCases = new List<PendingMedicalCaseDto>
            {
                new PendingMedicalCaseDto { PatientId = patientId, PatientName = "张三" }
            };

            _repositoryMock.GetPendingCasesAsync(doctorId, patientId)
                .Returns(pendingCases);

            // Act
            var result = await _service.GetPendingCasesAsync(doctorId, patientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].PatientId.Should().Be(patientId);
        }

        #endregion

        #region GetAllPendingCasesAsync Tests

        [Fact]
        public async Task GetAllPendingCasesAsync_ShouldReturnAllPendingCases()
        {
            // Arrange
            var pendingCases = new List<PendingMedicalCaseDto>
            {
                new PendingMedicalCaseDto { PatientId = Guid.NewGuid(), PatientName = "张三" },
                new PendingMedicalCaseDto { PatientId = Guid.NewGuid(), PatientName = "李四" },
                new PendingMedicalCaseDto { PatientId = Guid.NewGuid(), PatientName = "王五" }
            };

            _repositoryMock.GetAllPendingCasesAsync()
                .Returns(pendingCases);

            // Act
            var result = await _service.GetAllPendingCasesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
        }

        #endregion
    }
}
