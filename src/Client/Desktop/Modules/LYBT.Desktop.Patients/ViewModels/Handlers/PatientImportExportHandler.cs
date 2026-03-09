using System.IO;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Utilities.Excel;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Handlers;

/// <summary>
/// 患者导入导出处理实现
/// </summary>
public class PatientImportExportHandler : IPatientImportExportHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMasterDetailServices<PatientListDto, PatientDetailModel> _masterDetailServices;
    private readonly ICommonDialogService _commonDialogService;
    private readonly ILogger<PatientImportExportHandler> _logger;

    public PatientImportExportHandler(
        IPatientRepository patientRepository,
        IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
        ICommonDialogService commonDialogService,
        ILogger<PatientImportExportHandler> logger)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
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
                filter: "Excel文件|*.xlsx",
                title: "选择患者导入文件");
            if (string.IsNullOrEmpty(filePath)) return false;

            using var fileStream = File.OpenRead(filePath);
            var patients = await ExcelHelper.ParseAsync<PatientInputDto>(fileStream, hasHeader: true);
            if (patients == null || patients.Count == 0)
            {
                await _commonDialogService.ShowErrorAsync("文件中没有有效的患者数据", "导入患者");
                return false;
            }

            var request = new PatientBatchImportInputDto
            {
                Patients = patients,
                Strategy = DuplicateStrategy.Skip
            };
            var result = await _patientRepository.BatchImportAsync(request);
            if (result == null)
            {
                await _commonDialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入患者");
                return false;
            }

            var message = $"导入完成！\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n成功率：{result.SuccessRate:F1}%";
            if (result.FailureCount > 0)
            {
                message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：";
                foreach (var f in result.Failures.Take(3))
                    message += $"\n第{f.OriginalRowNumber}行：{f.FailureReason}";
            }
            await _commonDialogService.ShowInfoAsync(message, "导入结果");

            return result.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入患者失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("导入患者失败", "操作失败");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task ExportAsync(string? searchText)
    {
        try
        {
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "导出患者数据",
                defaultFileName: $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var allPatients = await _patientRepository.SearchAsync(searchText ?? string.Empty);
            if (allPatients == null || allPatients.Count == 0)
            {
                await _commonDialogService.ShowErrorAsync("没有可导出的数据", "导出患者");
                return;
            }

            await ExcelHelper.ExportAsync(allPatients, filePath, "患者数据");
            await _commonDialogService.ShowInfoAsync($"成功导出{allPatients.Count}条患者数据到：\n{filePath}", "导出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出患者失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("导出患者失败", "操作失败");
        }
    }

    /// <inheritdoc/>
    public async Task DownloadTemplateAsync()
    {
        try
        {
            var filePath = await _commonDialogService.ShowSaveFileDialogAsync(
                filter: "Excel文件|*.xlsx",
                title: "保存患者导入模板",
                defaultFileName: $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            var sampleData = new List<PatientInputDto>
            {
                new() { Name = "张三", Gender = Gender.Male, BirthDate = new DateTime(1980, 1, 1), PhoneNumber = "13800138000", Address = "北京市朝阳区" },
                new() { Name = "李四", Gender = Gender.Female, BirthDate = new DateTime(1990, 5, 15), PhoneNumber = "13800138001", Address = "上海市浦东新区" }
            };
            await ExcelHelper.GenerateTemplateAsync(filePath, "患者导入模板", sampleData);
            await _commonDialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入患者」功能导入。", "下载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载模板失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("下载模板失败", "操作失败");
        }
    }
}
