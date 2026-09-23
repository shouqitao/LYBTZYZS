using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 配置导入导出视图模型（US-SHELL-016）。
/// </summary>
/// <remarks>
/// <para>导出：选择保存位置 → 生成 JSON 配置包（<c>IConfigurationPackageService.ExportAsync</c>）。</para>
/// <para>导入：选择配置文件 → 二次确认 → 校验并应用（格式错误直接拒绝），
/// 结果以「已应用 / 已跳过 / 说明」报告行呈现，并提示是否需要重启。</para>
/// <para>提示一律走 <see cref="IToastService"/>（禁止 MessageBox）。</para>
/// </remarks>
public partial class ConfigExportImportViewModel : NavigableViewModelBase
{
    /// <summary>配置文件对话框过滤器（导出/导入共用）</summary>
    private const string JsonFilter = "JSON 文件 (*.json)|*.json";

    private readonly IConfigurationPackageService _packageService;
    private readonly IFileDialogService _fileDialogService;

    /// <summary>导出目标路径（可由「选择保存位置…」填充，也可手工输入）</summary>
    [ObservableProperty]
    private string _exportPath = string.Empty;

    /// <summary>待导入的配置包路径（可由「选择配置文件…」填充，也可手工输入）</summary>
    [ObservableProperty]
    private string _importPath = string.Empty;

    /// <summary>是否已有导入报告可展示</summary>
    [ObservableProperty]
    private bool _hasReport;

    /// <summary>导入后是否需要重启应用才能生效（连接设置/本地数据库变更）</summary>
    [ObservableProperty]
    private bool _requiresRestart;

    /// <summary>导入报告行（已应用 / 已跳过 / 说明）</summary>
    public ObservableCollection<string> ReportLines { get; } = new();

    /// <summary>构造配置导入导出视图模型</summary>
    public ConfigExportImportViewModel(
        IViewModelServices services,
        IConfigurationPackageService packageService,
        IFileDialogService fileDialogService)
        : base(services)
    {
        _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
        _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        PageTitle = "配置导入导出";
    }

    #region 导出

    /// <summary>选择配置包保存位置（默认文件名含时间戳）</summary>
    [RelayCommand]
    private void SelectExportPath()
    {
        var path = _fileDialogService.ShowSaveFileDialog(
            JsonFilter,
            ".json",
            $"lybt-config-{DateTime.Now:yyyyMMddHHmmss}.json");

        if (!string.IsNullOrWhiteSpace(path))
            ExportPath = path;
    }

    /// <summary>导出配置包</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(ExportPath))
        {
            SetError("请先选择配置包的保存位置");
            ToastService.ShowWarning(ErrorMessage);
            return;
        }

        ClearError();
        SetBusy(true, "正在导出配置包…");
        string? successMessage = null;

        try
        {
            var result = await _packageService.ExportAsync(ExportPath);
            if (result.Success && result.Data != null)
            {
                var summary = result.Data;
                successMessage =
                    $"已导出配置包（{summary.Sections.Count} 个节，{FormatSize(summary.FileSizeBytes)}）：{summary.FilePath}";
            }
            else
            {
                SetError(result.Error ?? "导出配置包失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CFG-PKG-VM] 导出配置包异常");
            SetError($"导出配置包失败：{ex.Message}");
        }
        finally
        {
            SetBusy(false);
            if (successMessage != null)
            {
                StatusMessage = successMessage;
                ToastService.ShowSuccess("配置包导出成功");
            }
            else if (HasError)
            {
                ToastService.ShowError(ErrorMessage);
            }
        }
    }

    #endregion

    #region 导入

    /// <summary>选择待导入的配置包</summary>
    [RelayCommand]
    private void SelectImportPath()
    {
        var path = _fileDialogService.ShowOpenFileDialog(JsonFilter, ".json");
        if (!string.IsNullOrWhiteSpace(path))
            ImportPath = path;
    }

    /// <summary>导入配置包（先二次确认，再校验并应用）</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(ImportPath))
        {
            SetError("请先选择待导入的配置文件");
            ToastService.ShowWarning(ErrorMessage);
            return;
        }

        ClearError();

        var confirmed = await CommonDialogService.ShowConfirmAsync(
            "导入将覆盖当前配置（安全项除外），是否继续？",
            "确认导入配置");
        if (!confirmed)
        {
            StatusMessage = "已取消导入";
            return;
        }

        SetBusy(true, "正在导入配置包…");
        string? successMessage = null;

        try
        {
            var result = await _packageService.ImportAsync(ImportPath);
            if (result.Success && result.Data != null)
                successMessage = BuildImportReport(result.Data);
            else
                SetError(result.Error ?? "导入配置包失败");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[CFG-PKG-VM] 导入配置包异常");
            SetError($"导入配置包失败：{ex.Message}");
        }
        finally
        {
            SetBusy(false);
            if (successMessage != null)
            {
                StatusMessage = successMessage;
                ToastService.ShowSuccess("配置包导入完成");
            }
            else if (HasError)
            {
                ToastService.ShowError(ErrorMessage);
            }
        }
    }

    /// <summary>把导入报告铺开为可读行，并返回状态栏摘要</summary>
    private string BuildImportReport(ConfigurationPackageResult report)
    {
        ReportLines.Clear();

        foreach (var section in report.AppliedSections)
            ReportLines.Add($"已应用：{section}");

        foreach (var skip in report.SkippedItems)
            ReportLines.Add($"已跳过：{skip.Section} —— {skip.Reason}");

        foreach (var note in report.Notes)
            ReportLines.Add($"说明：{note}");

        HasReport = ReportLines.Count > 0;
        RequiresRestart = report.RequiresRestart;

        return report.RequiresRestart
            ? $"导入完成：应用 {report.AppliedSections.Count} 节，跳过 {report.SkippedItems.Count} 项；连接设置变更需重启生效"
            : $"导入完成：应用 {report.AppliedSections.Count} 节，跳过 {report.SkippedItems.Count} 项";
    }

    #endregion

    #region 辅助

    /// <summary>文件大小的人类可读文本</summary>
    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }

    #endregion
}
