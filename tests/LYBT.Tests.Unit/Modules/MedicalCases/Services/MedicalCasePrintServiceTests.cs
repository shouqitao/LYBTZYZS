using FluentAssertions;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Services;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;
using MedicalCaseEntity = LYBT.Entities.MedicalCases.MedicalCase;

namespace LYBT.Tests.Unit.Modules.MedicalCases.Services
{
    /// <summary>
    /// MedicalCasePrintService 单元测试
    /// 从 MedicalCaseCommandService 拆分，覆盖打印回写和打印日志功能
    /// </summary>
    public class MedicalCasePrintServiceTests : TestBase
    {
        private readonly MedicalCasePrintService _service;
        private readonly IMedicalCaseRepository _repositoryMock;
        private readonly ILogger<MedicalCasePrintService> _loggerMock;

        public MedicalCasePrintServiceTests()
        {
            _repositoryMock = CreateMock<IMedicalCaseRepository>();
            _loggerMock = CreateLoggerMock<MedicalCasePrintService>();

            _service = new MedicalCasePrintService(
                _repositoryMock,
                _loggerMock);
        }

        #region RecordPrintCompletedAsync Tests

        [Fact]
        public async Task RecordPrintCompletedAsync_WithValidCase_ShouldUpdatePrintFields()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var printedBy = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                IsPrinted = false,
                PrintCount = 0,
                PrintVersion = 0,
                PrintLogs = new List<LYBT.Entities.MedicalCases.MedicalCasePrintLog>()
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);
            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.RecordPrintCompletedAsync(
                medicalCaseId, PrintType.Prescription, printedBy, "李医生", "HP-001");

            // Assert
            result.Should().NotBeNull();
            result!.IsPrinted.Should().BeTrue();
            result.PrintCount.Should().Be(1);
            result.PrintVersion.Should().Be(1);
            result.LastPrintedAt.Should().NotBeNull();
            result.PrintLogs.Should().HaveCount(1);
            result.PrintLogs.First().IsSuccess.Should().BeTrue();
            result.PrintLogs.First().PrintedByName.Should().Be("李医生");
            result.PrintLogs.First().PrinterName.Should().Be("HP-001");
        }

        [Fact]
        public async Task RecordPrintCompletedAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .ReturnsNull();

            // Act
            var result = await _service.RecordPrintCompletedAsync(
                medicalCaseId, PrintType.Prescription, Guid.NewGuid(), "李医生");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RecordPrintCompletedAsync_MultiplePrints_ShouldIncrementCountAndVersion()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                IsPrinted = true,
                PrintCount = 2,
                PrintVersion = 3,
                PrintLogs = new List<LYBT.Entities.MedicalCases.MedicalCasePrintLog>()
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);
            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.RecordPrintCompletedAsync(
                medicalCaseId, PrintType.Prescription, Guid.NewGuid(), "张医生");

            // Assert
            result.Should().NotBeNull();
            result!.PrintCount.Should().Be(3);
            result.PrintVersion.Should().Be(4);
        }

        #endregion

        #region AddPrintLogAsync Tests

        [Fact]
        public async Task AddPrintLogAsync_WhenSuccess_ShouldUpdateFieldsAndCreateLog()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                IsPrinted = false,
                PrintCount = 0,
                PrintVersion = 0,
                PrintLogs = new List<LYBT.Entities.MedicalCases.MedicalCasePrintLog>()
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);
            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.AddPrintLogAsync(
                medicalCaseId, PrintType.Prescription, isSuccess: true,
                Guid.NewGuid(), "李医生", "HP-001");

            // Assert
            result.Should().BeTrue();
            medicalCase.IsPrinted.Should().BeTrue();
            medicalCase.PrintCount.Should().Be(1);
            medicalCase.PrintVersion.Should().Be(1);
            medicalCase.PrintLogs.Should().HaveCount(1);
            medicalCase.PrintLogs.First().IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task AddPrintLogAsync_WhenFailed_ShouldNotUpdateFieldsButCreateLog()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var medicalCase = new MedicalCaseEntity
            {
                Id = medicalCaseId,
                IsPrinted = false,
                PrintCount = 0,
                PrintVersion = 0,
                PrintLogs = new List<LYBT.Entities.MedicalCases.MedicalCasePrintLog>()
            };

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .Returns(medicalCase);
            _repositoryMock.UpdateAsync(medicalCase)
                .Returns(medicalCase);

            // Act
            var result = await _service.AddPrintLogAsync(
                medicalCaseId, PrintType.Prescription, isSuccess: false,
                Guid.NewGuid(), "李医生", errorMessage: "打印机离线");

            // Assert
            result.Should().BeTrue();
            medicalCase.IsPrinted.Should().BeFalse();
            medicalCase.PrintCount.Should().Be(0);
            medicalCase.PrintLogs.Should().HaveCount(1);
            medicalCase.PrintLogs.First().IsSuccess.Should().BeFalse();
            medicalCase.PrintLogs.First().ErrorMessage.Should().Be("打印机离线");
        }

        [Fact]
        public async Task AddPrintLogAsync_WhenNotFound_ShouldReturnFalse()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            _repositoryMock.GetByIdWithDetailsAsync(medicalCaseId)
                .ReturnsNull();

            // Act
            var result = await _service.AddPrintLogAsync(
                medicalCaseId, PrintType.Prescription, isSuccess: true,
                Guid.NewGuid(), "李医生");

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
