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
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Infrastructure;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// 药材导入流（US-SHELL-021 AC②③⑤）：≤1000 行分批、进度可跟踪、汇总报告含失败行号定位。
/// </summary>
public class HerbImportFlowTests : DesktopTestBase, IDisposable
{
    private readonly string _importFilePath;
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<HerbListDto, HerbDetailModel> _masterDetailServices;
    private readonly IDialogManager _dialogManager;
    private readonly IHerbService _herbService = Substitute.For<IHerbService>();
    private readonly IHerbStatusHandler _statusHandler = Substitute.For<IHerbStatusHandler>();
    private readonly IDesktopCacheManager _cacheManager = Substitute.For<IDesktopCacheManager>();
    private readonly IFileDialogService _fileDialogService = Substitute.For<IFileDialogService>();
    private readonly IHerbExcelService _herbExcelService = Substitute.For<IHerbExcelService>();
    private readonly HerbEditorViewModel _herbEditor;

    public HerbImportFlowTests()
    {
        _viewModelServices = CreateViewModelServicesMock();
        _masterDetailServices = CreateMasterDetailServicesMock<HerbListDto, HerbDetailModel>();
        _dialogManager = _masterDetailServices.Dialog;
        _herbEditor = new HerbEditorViewModel(new HerbDetailModelMapper());

        // 文件内容无关紧要——解析由 IHerbExcelService 桩承担，仅需真实文件供 VM 打开流
        _importFilePath = Path.Combine(Path.GetTempPath(), $"herb-import-{Guid.NewGuid():N}.xlsx");
        File.WriteAllBytes(_importFilePath, new byte[] { 0x50, 0x4B });

        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _fileDialogService.ShowOpenFileDialog(Arg.Any<string>(), Arg.Any<string>()).Returns(_importFilePath);
        _herbService
            .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandResult<PagedResult<HerbListDto>>(
                true, new PagedResult<HerbListDto> { Items = new List<HerbListDto>(), TotalCount = 0 }, null)));
    }

    public void Dispose()
    {
        _herbEditor.Dispose();
        if (File.Exists(_importFilePath))
        {
            File.Delete(_importFilePath);
        }
    }

    private HerbMasterDetailViewModel CreateSut()
        => new(
            _viewModelServices,
            _masterDetailServices,
            _herbService,
            _statusHandler,
            _cacheManager,
            new HerbDetailModelMapper(),
            _fileDialogService,
            _herbExcelService,
            _herbEditor);

    private void StubParsedRows(int rowCount)
    {
        var rows = new List<HerbInputDto>(rowCount);
        for (var i = 0; i < rowCount; i++)
        {
            rows.Add(new HerbInputDto { Name = $"药材{i + 1}", Unit = "g", Price = 1m });
        }

        _herbExcelService.ParseImportFile(Arg.Any<Stream>())
            .Returns(new HerbBatchImportInputDto { Herbs = rows });
    }

    private static Task<CommandResult<HerbBatchImportResultDto>> SuccessResult(int rows, int settled)
        => Task.FromResult(new CommandResult<HerbBatchImportResultDto>(
            true, new HerbBatchImportResultDto { TotalCount = rows, SuccessCount = rows - settled, SkippedCount = settled }, null));

    [Fact]
    public async Task ImportHerbsCommand_SplitsIntoBatchesOfAtMost1000_AndAggregatesReportWithFileRowNumbers()
    {
        // Arrange：2500 行 → 3 批（1000/1000/500）；第 2 批第 2 行（服务端相对行号 3）失败
        StubParsedRows(2500);
        var batchSizes = new List<int>();
        _herbService.BatchImportAsync(Arg.Any<HerbBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var request = ci.Arg<HerbBatchImportInputDto>();
                batchSizes.Add(request.Herbs.Count);

                var result = new HerbBatchImportResultDto
                {
                    TotalCount = request.Herbs.Count,
                    SuccessCount = request.Herbs.Count
                };

                if (batchSizes.Count == 2)
                {
                    result.SuccessCount -= 1;
                    result.FailureCount = 1;
                    result.Failures.Add(new HerbImportFailureDto
                    {
                        RowNumber = 3,
                        HerbName = "坏药材",
                        Reason = "校验失败",
                        ErrorDetails = new List<string> { "名称非法" }
                    });
                }

                return Task.FromResult(new CommandResult<HerbBatchImportResultDto>(true, result, null));
            });

        var sut = CreateSut();
        sut.ImportDuplicateStrategy = DuplicateStrategy.Update;

        // Act
        await sut.ImportHerbsCommand.ExecuteAsync(null);

        // AC③：每批 ≤1000 行、顺序提交
        batchSizes.Should().Equal(1000, 1000, 500);

        // AC②：所选重复策略随每个批次请求下发
        await _herbService.Received(3).BatchImportAsync(
            Arg.Is<HerbBatchImportInputDto>(r => r.Strategy == DuplicateStrategy.Update),
            Arg.Any<CancellationToken>());

        // AC②：界面可选三种策略（跳过/更新/报错）
        sut.ImportDuplicateStrategyOptions.Select(o => o.Value)
            .Should().BeEquivalentTo(new[] { DuplicateStrategy.Skip, DuplicateStrategy.Update, DuplicateStrategy.Error });

        // AC⑤：汇总报告（总数/成功/失败/跳过 + 失败行号还原为文件行号）
        sut.HasImportReport.Should().BeTrue();
        var report = sut.ImportReport!;
        report.BatchCount.Should().Be(3);
        report.TotalCount.Should().Be(2500);
        report.SuccessCount.Should().Be(2499);
        report.FailureCount.Should().Be(1);
        report.SkippedCount.Should().Be(0);
        report.IsAborted.Should().BeFalse();
        report.Failures.Should().ContainSingle();
        report.Failures[0].RowNumber.Should().Be(1003, "批次偏移 1000 + 服务端相对行号 3");
        report.Failures[0].Identifier.Should().Be("坏药材");
        report.Failures[0].Reason.Should().Contain("名称非法");

        // AC③：进度可跟踪（「已导入 X/Y 行」）
        sut.ImportProgress.TotalCount.Should().Be(2500);
        sut.ImportProgress.ProcessedCount.Should().Be(2500);
        sut.ImportProgress.PercentComplete.Should().Be(100);
        sut.ImportProgress.Message.Should().Be("已导入 2500/2500 行");

        _cacheManager.Received(1).InvalidateHerbCaches();
    }

    [Fact]
    public async Task ImportHerbsCommand_AbortsRemainingBatches_WhenBatchRequestFails()
    {
        StubParsedRows(2500);
        var callCount = 0;
        _herbService.BatchImportAsync(Arg.Any<HerbBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 2
                    ? Task.FromResult(CommandResult<HerbBatchImportResultDto>.Failed("服务端异常"))
                    : SuccessResult(1000, 0);
            });

        var sut = CreateSut();

        await sut.ImportHerbsCommand.ExecuteAsync(null);

        callCount.Should().Be(2, "整批失败后不再提交后续批次（该批已由服务端回滚）");
        var report = sut.ImportReport!;
        report.IsAborted.Should().BeTrue();
        report.TotalCount.Should().Be(2000);
        report.SuccessCount.Should().Be(1000);
        report.FailureCount.Should().Be(1000);
        report.Failures.Should().ContainSingle();
        report.Failures[0].RowNumber.Should().Be(1002, "失败批首行 = 批次偏移 1000 + 首数据行 2");
        report.Summary.Should().Contain("已中止");
        sut.ImportProgress.ProcessedCount.Should().Be(1000);
        _cacheManager.Received(1).InvalidateHerbCaches();
    }

    [Fact]
    public async Task ImportHerbsCommand_ReportsFileFormatError_AndSkipsBatchImport_WhenParserRejectsFile()
    {
        _herbExcelService.ParseImportFile(Arg.Any<Stream>())
            .Returns(_ => throw new InvalidDataException("第 3 行「名称」值无效"));
        var sut = CreateSut();

        await sut.ImportHerbsCommand.ExecuteAsync(null);

        sut.HasImportReport.Should().BeFalse();
        await _herbService.DidNotReceive().BatchImportAsync(Arg.Any<HerbBatchImportInputDto>(), Arg.Any<CancellationToken>());
        await _dialogManager.Received(1).ShowErrorAsync(
            Arg.Is<string>(m => m.Contains("第 3 行")), Arg.Any<string>());
    }

    [Fact]
    public async Task ImportHerbsCommand_DoesNotImport_WhenUserCancelsConfirmation()
    {
        StubParsedRows(10);
        _dialogManager.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var sut = CreateSut();

        await sut.ImportHerbsCommand.ExecuteAsync(null);

        await _herbService.DidNotReceive().BatchImportAsync(Arg.Any<HerbBatchImportInputDto>(), Arg.Any<CancellationToken>());
        sut.HasImportReport.Should().BeFalse();
    }

    [Fact]
    public async Task CloseImportReportCommand_HidesReportPanel()
    {
        StubParsedRows(2);
        _herbService.BatchImportAsync(Arg.Any<HerbBatchImportInputDto>(), Arg.Any<CancellationToken>())
            .Returns(SuccessResult(2, 0));
        var sut = CreateSut();
        await sut.ImportHerbsCommand.ExecuteAsync(null);
        sut.HasImportReport.Should().BeTrue();

        sut.CloseImportReportCommand.Execute(null);

        sut.HasImportReport.Should().BeFalse();
        sut.ImportReport.Should().BeNull();
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenExcelServiceIsNull()
    {
        Action act = () => new HerbMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _herbService,
            _statusHandler,
            _cacheManager,
            new HerbDetailModelMapper(),
            _fileDialogService,
            null!,
            _herbEditor);

        act.Should().Throw<ArgumentNullException>().WithParameterName("herbExcelService");
    }
}
