using System.IO;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Utilities.Excel;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Herbs.Interfaces;

namespace LYBT.Desktop.Herbs.ViewModels.Handlers;

/// <summary>
/// 药材导入导出处理实现
/// </summary>
public class HerbImportExportHandler : IHerbImportExportHandler
{
    private readonly IHerbService _herbService;
    private readonly IMasterDetailServices<HerbListDto, HerbDetailModel> _masterDetailServices;
    private readonly ICommonDialogService _commonDialogService;
    private readonly ILogger<HerbImportExportHandler> _logger;

    public HerbImportExportHandler(
        IHerbService herbService,
        IMasterDetailServices<HerbListDto, HerbDetailModel> masterDetailServices,
        ICommonDialogService commonDialogService,
        ILogger<HerbImportExportHandler> logger)
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _commonDialogService = commonDialogService ?? throw new ArgumentNullException(nameof(commonDialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> ImportAsync()
    {
        try
        {
            var filePath = await _commonDialogService.ShowOpenFileDialogAsync(
                filter: "Excel文件|*.xlsx;*.xls",
                title: "选择药材导入文件");

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var needsRefresh = false;
            await _masterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
            {
                await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var herbs = await ExcelHelper.ParseAsync<HerbInputDto>(fileStream, hasHeader: true);
                if (herbs == null || herbs.Count == 0)
                {
                    await _masterDetailServices.Dialog.ShowWarningAsync(
                        "没有导入任何记录，请检查文件格式", "导入提示");
                    return;
                }

                var useOverwrite = await _masterDetailServices.Dialog.ShowConfirmAsync(
                    "检测到导入数据中可能包含重复药材。\n\n选择「是」覆盖已有记录\n选择「否」跳过重复记录",
                    "重复处理策略");
                var request = new HerbBatchImportInputDto
                {
                    Herbs = herbs,
                    Strategy = useOverwrite ? DuplicateStrategy.Update : DuplicateStrategy.Skip
                };
                var result = await _herbService.BatchImportAsync(request);

                if (result.Success && result.Data != null && result.Data.SuccessCount > 0)
                {
                    await _masterDetailServices.Dialog.ShowSuccessAsync(
                        $"成功导入 {result.Data.SuccessCount} 条药材记录", "导入成功");
                    needsRefresh = true;
                }
                else
                {
                    await _masterDetailServices.Dialog.ShowWarningAsync(
                        "没有导入任何记录，请检查文件格式", "导入提示");
                }
            }, "导入药材");

            return needsRefresh;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入药材失败");
            await _masterDetailServices.Dialog.ShowErrorAsync(
                "导入药材失败，请检查文件格式后重试", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task ExportAsync(string? searchText)
    {
        try
        {
            var defaultFileName = $"药材导出_{DateTime.Now:yyyyMMdd}.xlsx";
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "导出药材数据",
                defaultFileName: defaultFileName);

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            await _masterDetailServices.Loading.ExecuteWithLoadingAsync(async () =>
            {
                _logger.LogInformation("导出药材数据，关键词：{Keyword}", searchText);
                var result = await _herbService.ExportHerbsAsync(searchText);

                if (!result.Success || result.Data == null || result.Data.Length == 0)
                {
                    await _masterDetailServices.Dialog.ShowErrorAsync(
                        "导出失败，没有数据可导出", "导出药材");
                    return;
                }

                await File.WriteAllBytesAsync(filePath, result.Data);
                await _masterDetailServices.Dialog.ShowSuccessAsync(
                    $"药材数据已导出到：{filePath}", "导出成功");
            }, "导出药材");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出药材失败");
            await _masterDetailServices.Dialog.ShowErrorAsync(
                "导出药材失败，请稍后重试", "操作失败");
        }
    }
}
