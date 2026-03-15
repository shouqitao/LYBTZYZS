using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Handlers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 患者导入导出功能 ViewModel (Child VM)
/// 从 PatientMasterDetailViewModel 拆分出来的导入导出功能
/// </summary>
public partial class PatientImportExportViewModel : CoreViewModelBase
{
    private readonly IPatientImportExportHandler _importExportHandler;
    private readonly ILogger<PatientImportExportViewModel> _logger;

    public PatientImportExportViewModel(
        IViewModelServices viewModelServices,
        IPatientImportExportHandler importExportHandler,
        ILogger<PatientImportExportViewModel> logger) : base(viewModelServices)
    {
        _importExportHandler = importExportHandler ?? throw new ArgumentNullException(nameof(importExportHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 导入患者命令
    /// 返回是否成功导入
    /// </summary>
    [RelayCommand]
    public async Task<bool> ImportAsync()
    {
        var result = await _importExportHandler.ImportAsync();
        if (result)
        {
            _logger.LogInformation("患者导入成功");
        }
        return result;
    }

    /// <summary>
    /// 导出患者命令
    /// </summary>
    [RelayCommand]
    public async Task ExportAsync(string? searchText = null)
    {
        await _importExportHandler.ExportAsync(searchText);
    }

    /// <summary>
    /// 下载模板命令
    /// </summary>
    [RelayCommand]
    public async Task DownloadTemplateAsync()
    {
        await _importExportHandler.DownloadTemplateAsync();
    }
}
