using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using FluentAssertions;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.CardReader.Integration;
using LYBT.Desktop.Infrastructure.CardReader.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Patients.Mappers;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Patients.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// 患者导入流（US-SHELL-021 AC②③⑤）：≤1000 行分批（含 1000 边界）、进度、汇总报告含失败行号。
/// 使用真实 <see cref="PatientExcelService"/> 解析真实 .xlsx（端到端覆盖 Excel → DTO → 分批提交）。
/// </summary>
public class PatientImportFlowTests : DesktopTestBase, IDisposable
{
    private readonly string _importFilePath;
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<PatientListDto, PatientDetailModel> _masterDetailServices;
    private readonly IDialogManager _dialogManager;
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly IPatientStatusHandler _statusHandler = Substitute.For<IPatientStatusHandler>();
    private readonly IDesktopCacheManager _cacheManager = Substitute.For<IDesktopCacheManager>();
    private readonly IFileDialogService _fileDialogService = Substitute.For<IFileDialogService>();
    private readonly PatientExcelService _patientExcelService = new();
    private readonly PatientMapper _patientMapper = new();
    private readonly PatientCardReaderViewModel _cardReaderViewModel;
    private readonly PatientEditorViewModel _patientEditor;

    public PatientImportFlowTests()
    {
        _viewModelServices = CreateViewModelServicesMock();
        _masterDetailServices = CreateMasterDetailServicesMock<PatientListDto, PatientDetailModel>();
        _dialogManager = _masterDetailServices.Dialog;

        _cardReaderViewModel = Substitute.For<PatientCardReaderViewModel>(
            _viewModelServices,
            Substitute.For<ICardReaderService>(),
            Substitute.For<IPatientCardReaderIntegration>(),
            Substitute.For<ILogger<PatientCardReaderViewModel>>());
        _patientEditor = new PatientEditorViewModel(_patientMapper);

        _importFilePath = CreatePatientWorkbook(rowCount: 1001);
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _fileDialogService.ShowOpenFileDialog(Arg.Any<string>(), Arg.Any<string>()).Returns(_importFilePath);
        _patientService
            .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult<PagedResult<PatientListDto>>(
                true, new PagedResult<PatientListDto> { Items = new List<PatientListDto>(), TotalCount = 0 }, null));
    }

    public void Dispose()
    {
        _patientEditor.Dispose();
        if (File.Exists(_importFilePath))
        {
            File.Delete(_importFilePath);
        }
    }

    /// <summary>生成真实 .xlsx：表头 + rowCount 条有效患者行（姓名 + 性别）。</summary>
    private static string CreatePatientWorkbook(int rowCount)
    {
        var path = Path.Combine(Path.GetTempPath(), $"patient-import-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("患者数据");
        sheet.Cell(1, 1).Value = "姓名（必填）";
        sheet.Cell(1, 2).Value = "性别（必填）";
        for (var i = 0; i < rowCount; i++)
        {
            sheet.Cell(i + 2, 1).Value = $"患者{i + 1}";
            sheet.Cell(i + 2, 2).Value = "男";
        }

        workbook.SaveAs(path);
        return path;
    }

    private PatientMasterDetailViewModel CreateSut()
        => new(
            _viewModelServices,
            _masterDetailServices,
            _patientService,
            _statusHandler,
            _cacheManager,
            _patientMapper,
            _patientExcelService,
            _fileDialogService,
            _cardReaderViewModel,
            _patientEditor);

    [Fact]
    public async Task ImportPatientsCommand_SplitsIntoBatchesOfAtMost1000_AndAggregatesReportWithFileRowNumbers()
    {
        // Arrange：1001 行 → 2 批（1000 + 1，验证 1000 边界）；第 2 批（单行）失败
        var batchSizes = new List<int>();
        _patientService.BatchImportAsync(Arg.Any<PatientBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var request = ci.Arg<PatientBatchImportInputDto>();
                batchSizes.Add(request.Patients.Count);

                var result = new PatientBatchImportResultDto
                {
                    TotalCount = request.Patients.Count,
                    SuccessCount = request.Patients.Count
                };

                if (batchSizes.Count == 2)
                {
                    result.SuccessCount = 0;
                    result.FailureCount = 1;
                    result.Failures.Add(new PatientImportFailureDto
                    {
                        OriginalRowNumber = 2,
                        FailureReason = "患者姓名重复",
                        FieldName = "Name",
                        OriginalValue = "患者1001",
                        DataSnapshot = request.Patients[0]
                    });
                }

                return Task.FromResult(new CommandResult<PatientBatchImportResultDto>(true, result, null));
            });

        var sut = CreateSut();
        sut.ImportDuplicateStrategy = DuplicateStrategy.Skip;

        // Act
        await sut.ImportPatientsCommand.ExecuteAsync(null);

        // AC③：1000 行整数边界 → 恰好 2 批
        batchSizes.Should().Equal(1000, 1);

        // AC②：所选重复策略随每个批次请求下发
        await _patientService.Received(2).BatchImportAsync(
            Arg.Is<PatientBatchImportInputDto>(r => r.Strategy == DuplicateStrategy.Skip),
            Arg.Any<CancellationToken>());

        // AC⑤：汇总报告 + 文件行号（批次偏移 1000 + 服务端相对行号 2）
        sut.HasImportReport.Should().BeTrue();
        var report = sut.ImportReport!;
        report.BatchCount.Should().Be(2);
        report.TotalCount.Should().Be(1001);
        report.SuccessCount.Should().Be(1000);
        report.FailureCount.Should().Be(1);
        report.SkippedCount.Should().Be(0);
        report.Failures.Should().ContainSingle();
        report.Failures[0].RowNumber.Should().Be(1002);
        report.Failures[0].Identifier.Should().Be("患者1001");
        report.Failures[0].Reason.Should().Contain("姓名重复");

        // AC③：进度可跟踪
        sut.ImportProgress.TotalCount.Should().Be(1001);
        sut.ImportProgress.ProcessedCount.Should().Be(1001);
        sut.ImportProgress.PercentComplete.Should().Be(100);
        sut.ImportProgress.Message.Should().Be("已导入 1001/1001 行");

        _cacheManager.Received(1).InvalidatePatientCaches();
    }

    [Fact]
    public async Task ImportPatientsCommand_AbortsRemainingBatches_WhenBatchRequestFails()
    {
        var callCount = 0;
        _patientService.BatchImportAsync(Arg.Any<PatientBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? Task.FromResult(CommandResult<PatientBatchImportResultDto>.Failed("服务端异常"))
                    : Task.FromResult(new CommandResult<PatientBatchImportResultDto>(
                        true, new PatientBatchImportResultDto { TotalCount = 1, SuccessCount = 1 }, null));
            });

        var sut = CreateSut();

        await sut.ImportPatientsCommand.ExecuteAsync(null);

        callCount.Should().Be(1, "首批失败后不再提交后续批次（该批已由服务端回滚）");
        var report = sut.ImportReport!;
        report.IsAborted.Should().BeTrue();
        report.TotalCount.Should().Be(1000);
        report.FailureCount.Should().Be(1000);
        report.Failures.Should().ContainSingle();
        report.Failures[0].RowNumber.Should().Be(2);
        report.Summary.Should().Contain("已中止");
        sut.ImportProgress.ProcessedCount.Should().Be(0);
    }
}
