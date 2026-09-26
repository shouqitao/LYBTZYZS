using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Desktop.Catalog.Mappers;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.Services;
using LYBT.Desktop.Catalog.ViewModels;
using LYBT.Desktop.Catalog.ViewModels.Handlers;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Infrastructure;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// 验方导入流（US-SHELL-021 AC②③⑤）：≤1000 行分批、进度、汇总报告（含药材匹配数与失败行号）。
/// </summary>
public class FormulaImportFlowTests : DesktopTestBase, IDisposable
{
    private readonly string _importFilePath;
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<FormulaListDto, FormulaDetailModel> _masterDetailServices;
    private readonly IDialogManager _dialogManager;
    private readonly IFormulaService _formulaService = Substitute.For<IFormulaService>();
    private readonly IFormulaStatusHandler _statusHandler = Substitute.For<IFormulaStatusHandler>();
    private readonly IHerbSearchProvider _herbSearchProvider = Substitute.For<IHerbSearchProvider>();
    private readonly IDesktopCacheManager _cacheManager = Substitute.For<IDesktopCacheManager>();
    private readonly IFileDialogService _fileDialogService = Substitute.For<IFileDialogService>();
    private readonly IFormulaExcelService _formulaExcelService = Substitute.For<IFormulaExcelService>();
    private readonly FormulaEditorViewModel _formulaEditor;

    public FormulaImportFlowTests()
    {
        _viewModelServices = CreateViewModelServicesMock();
        _masterDetailServices = CreateMasterDetailServicesMock<FormulaListDto, FormulaDetailModel>();
        _dialogManager = _masterDetailServices.Dialog;
        _formulaEditor = new FormulaEditorViewModel(new FormulaDetailModelMapper());

        _importFilePath = Path.Combine(Path.GetTempPath(), $"formula-import-{Guid.NewGuid():N}.xlsx");
        File.WriteAllBytes(_importFilePath, new byte[] { 0x50, 0x4B });

        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _fileDialogService.ShowOpenFileDialog(Arg.Any<string>(), Arg.Any<string>()).Returns(_importFilePath);
        _herbSearchProvider.GetAllHerbsAsync()
            .Returns(Task.FromResult<IReadOnlyList<HerbListDto>>(Array.Empty<HerbListDto>()));
        _formulaService
            .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult<PagedResult<FormulaListDto>>(
                true, new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }, null));
    }

    public void Dispose()
    {
        _formulaEditor.Dispose();
        if (File.Exists(_importFilePath))
        {
            File.Delete(_importFilePath);
        }
    }

    private FormulaMasterDetailViewModel CreateSut()
        => new(
            _viewModelServices,
            _masterDetailServices,
            _formulaService,
            _statusHandler,
            _herbSearchProvider,
            _cacheManager,
            _fileDialogService,
            _formulaExcelService,
            _formulaEditor);

    private void StubParsedRows(int rowCount)
    {
        var rows = new List<FormulaImportItemDto>(rowCount);
        for (var i = 0; i < rowCount; i++)
        {
            rows.Add(new FormulaImportItemDto { Name = $"验方{i + 1}" });
        }

        _formulaExcelService.ParseImportFile(Arg.Any<Stream>())
            .Returns(new FormulaBatchImportInputDto { Formulas = rows });
    }

    [Fact]
    public async Task ImportFormulasCommand_SplitsIntoBatchesOfAtMost1000_AndAggregatesReportWithMatchedHerbsAndFileRowNumbers()
    {
        // Arrange：2500 行 → 3 批（1000/1000/500）；第 2 批第 2 行（服务端相对行号 3）失败
        StubParsedRows(2500);
        var batchSizes = new List<int>();
        _formulaService.BatchImportAsync(Arg.Any<FormulaBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var request = ci.Arg<FormulaBatchImportInputDto>();
                batchSizes.Add(request.Formulas.Count);

                var result = new FormulaBatchImportResultDto
                {
                    TotalCount = request.Formulas.Count,
                    SuccessCount = request.Formulas.Count,
                    MatchedHerbsCount = 2,
                    UnmatchedHerbsCount = 1
                };

                if (batchSizes.Count == 2)
                {
                    result.SuccessCount -= 1;
                    result.FailureCount = 1;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        RowIndex = 3,
                        FormulaName = "坏验方",
                        ErrorMessage = "验方名称已存在"
                    });
                }

                return Task.FromResult(new CommandResult<FormulaBatchImportResultDto>(true, result, null));
            });

        var sut = CreateSut();
        sut.ImportDuplicateStrategy = DuplicateStrategy.Error;

        // Act
        await sut.ImportFormulasCommand.ExecuteAsync(null);

        // AC③：每批 ≤1000 行、顺序提交
        batchSizes.Should().Equal(1000, 1000, 500);

        // AC②：所选重复策略与文件名随每个批次请求下发
        await _formulaService.Received(3).BatchImportAsync(
            Arg.Is<FormulaBatchImportInputDto>(r =>
                r.Strategy == DuplicateStrategy.Error
                && r.FileName == Path.GetFileName(_importFilePath)),
            Arg.Any<CancellationToken>());

        // AC⑤：汇总报告
        sut.HasImportReport.Should().BeTrue();
        var report = sut.ImportReport!;
        report.BatchCount.Should().Be(3);
        report.TotalCount.Should().Be(2500);
        report.SuccessCount.Should().Be(2499);
        report.FailureCount.Should().Be(1);
        report.Failures.Should().ContainSingle();
        report.Failures[0].RowNumber.Should().Be(1003, "批次偏移 1000 + 服务端相对行号 3");
        report.Failures[0].Identifier.Should().Be("坏验方");
        report.Summary.Should().Contain("匹配药材 6 味", "各批匹配数累加（3 批 × 2 味）");
        report.Summary.Should().Contain("未匹配 3 味");

        // AC③：进度可跟踪
        sut.ImportProgress.ProcessedCount.Should().Be(2500);
        sut.ImportProgress.PercentComplete.Should().Be(100);
        sut.ImportProgress.Message.Should().Be("已导入 2500/2500 行");

        _cacheManager.Received(1).InvalidateFormulaCaches();
    }

    [Fact]
    public async Task ImportFormulasCommand_AbortsRemainingBatches_WhenBatchRequestFails()
    {
        StubParsedRows(1500);
        var callCount = 0;
        _formulaService.BatchImportAsync(Arg.Any<FormulaBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 2
                    ? Task.FromResult(CommandResult<FormulaBatchImportResultDto>.Failed("服务端异常"))
                    : Task.FromResult(new CommandResult<FormulaBatchImportResultDto>(
                        true, new FormulaBatchImportResultDto { TotalCount = 1000, SuccessCount = 1000 }, null));
            });

        var sut = CreateSut();

        await sut.ImportFormulasCommand.ExecuteAsync(null);

        callCount.Should().Be(2, "整批失败后不再提交后续批次（该批已由服务端回滚）");
        var report = sut.ImportReport!;
        report.IsAborted.Should().BeTrue();
        report.TotalCount.Should().Be(1500);
        report.SuccessCount.Should().Be(1000);
        report.FailureCount.Should().Be(500);
        report.Failures.Should().ContainSingle();
        report.Failures[0].RowNumber.Should().Be(1002, "失败批首行 = 批次偏移 1000 + 首数据行 2");
        sut.ImportProgress.ProcessedCount.Should().Be(1000);
    }
}
