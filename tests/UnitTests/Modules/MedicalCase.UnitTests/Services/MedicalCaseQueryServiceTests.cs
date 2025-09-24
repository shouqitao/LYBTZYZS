using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Services
{
    public class MedicalCaseQueryServiceTests
    {
        private readonly MedicalCaseQueryService _service;
        private readonly Mock<IMedicalCaseReadRepository> _mockReadRepository;
        private readonly Mock<ILogger<MedicalCaseQueryService>> _mockLogger;

        public MedicalCaseQueryServiceTests()
        {
            _mockReadRepository = new Mock<IMedicalCaseReadRepository>();
            _mockLogger = new Mock<ILogger<MedicalCaseQueryService>>();

            _service = new MedicalCaseQueryService(
                _mockReadRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Not_Found()
        {
            // Act
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("病历不存在");
        }

        #region GetByPatientIdAsync Tests

        [Fact]
        public async Task GetByPatientIdAsync_Should_Return_PatientCases()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedCases = new List<MedicalCaseDto>
            {
                new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    PatientName = "测试患者",
                    DoctorName = "医生1",
                    CaseStatus = MedicalCaseStatus.Active,
                    ConsultationDate = DateTime.Now.AddDays(-2)
                },
                new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    PatientName = "测试患者",
                    DoctorName = "医生2",
                    CaseStatus = MedicalCaseStatus.Closed,
                    ConsultationDate = DateTime.Now.AddDays(-1)
                }
            };

            _mockReadRepository
                .Setup(x => x.GetMedicalCaseDtosByPatientIdAsync(patientId))
                .ReturnsAsync(expectedCases);

            // Act
            var result = await _service.GetByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region GetByDoctorIdAsync Tests

        [Fact]
        public async Task GetByDoctorIdAsync_Should_Return_DoctorCases()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedCases = new List<MedicalCaseDto>
            {
                new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorId = doctorId,
                    DoctorName = "测试医生",
                    CaseStatus = MedicalCaseStatus.Active
                },
                new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorId = doctorId,
                    DoctorName = "测试医生",
                    CaseStatus = MedicalCaseStatus.Active
                }
            };

            _mockReadRepository
                .Setup(x => x.GetMedicalCaseDtosByDoctorIdAsync(doctorId))
                .ReturnsAsync(expectedCases);

            // Act
            var result = await _service.GetByDoctorIdAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region GetActiveByPatientIdAsync Tests

        [Fact]
        public async Task GetActiveByPatientIdAsync_Should_Return_Active_Case()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var expectedActiveCase = new MedicalCaseDto
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "测试患者",
                DoctorName = "测试医生",
                CaseStatus = MedicalCaseStatus.Active
            };

            _mockReadRepository
                .Setup(x => x.GetActiveMedicalCaseDtoByPatientIdAsync(patientId))
                .ReturnsAsync(expectedActiveCase);

            // Act
            var result = await _service.GetActiveByPatientIdAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.CaseStatus.Should().Be(MedicalCaseStatus.Active);
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_Should_Filter_By_Status()
        {
            // Arrange
            var keyword = "患者";
            var expectedResult = new List<MedicalCaseDto>
            {
                new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientName = "患者1",
                    DoctorName = "医生1",
                    CaseStatus = MedicalCaseStatus.Closed
                }
            };

            _mockReadRepository
                .Setup(x => x.SearchMedicalCaseDtosAsync(keyword, 50))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data!.First().CaseStatus.Should().Be(MedicalCaseStatus.Closed);
        }

        [Fact]
        public async Task SearchAsync_Should_Filter_By_DateRange()
        {
            // Arrange
            var keyword = "患者";
            var expectedResult = new List<MedicalCaseDto>
            {
                new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientName = "患者2",
                    DoctorName = "医生2",
                    CaseStatus = MedicalCaseStatus.Active,
                    ConsultationDate = DateTime.Now.AddDays(-1)
                }
            };

            _mockReadRepository
                .Setup(x => x.SearchMedicalCaseDtosAsync(keyword, 50))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data!.First().ConsultationDate.Should().BeAfter(DateTime.Now.AddDays(-5));
        }

        #endregion

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_Should_Return_Correct_Statistics()
        {
            // Arrange
            var expectedStats = new MedicalCaseStatisticsDto
            {
                TotalCount = 3,
                InProgressCount = 2,
                CompletedCount = 1,
                CancelledCount = 0,
                AverageCompletionDays = 5.5,
                DoctorCaseDistribution = new Dictionary<string, int> { { "医生1", 2 }, { "医生2", 1 } }
            };

            _mockReadRepository
                .Setup(x => x.GetMedicalCaseStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            var stats = result.Data as MedicalCaseStatisticsDto;
            stats!.TotalCount.Should().Be(3);
            stats.InProgressCount.Should().Be(2);
            stats.CompletedCount.Should().Be(1);
        }

        #endregion
    }
}