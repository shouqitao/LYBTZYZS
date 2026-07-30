using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Extensions;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels.Workspace;

/// <summary>
/// Child VM for aggregate root commands (save/suspend/complete/print/import/clear).
/// All operations go through the MedicalCase aggregate root via IMedicalCaseService.
/// Import operations (formula/history/clear) are handled directly, replacing PrescriptionImportHandler callbacks.
/// </summary>
/// <remarks>
/// ARCHITECTURE-NOTE: This VM is intentionally kept as a single cohesive unit despite having 9 commands.
/// The commands are highly coupled (sharing _context, _medicalCaseService, delegates from parent)
/// and represent a single responsibility: "Medical Case Lifecycle Commands".
/// Attempting to split would introduce unnecessary complexity and cross-VM coordination overhead.
/// See: Phase 1 Architecture Review 2026-03-15
/// </remarks>
public class MedicalCaseCommandsViewModel : ChildViewModelBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IMedicalCaseDataProvider _dataProvider;
    private readonly PrescriptionPrintHandler _printHandler;
    private readonly IDialogService? _dialogService;
    private readonly IToastService _toastService;
    private readonly ConsultationMapper _consultationMapper = new();

    #region Commands

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand SuspendCommand { get; }
    public IRelayCommand CompleteCommand { get; }
    public IRelayCommand PrintCommand { get; }
    public IRelayCommand ExportPdfCommand { get; }
    public IRelayCommand EnterEditModeCommand { get; }
    public IRelayCommand ImportFormulaCommand { get; }
    public IRelayCommand CopyHistoryCommand { get; }
    public IRelayCommand ClearHerbsCommand { get; }

    #endregion

    public MedicalCaseCommandsViewModel(
        IMedicalCaseWorkspaceContext context,
        IWorkspaceHost host,
        ILoggerFactory loggerFactory,
        IMedicalCaseService medicalCaseService,
        IMedicalCaseDataProvider dataProvider,
        PrescriptionPrintHandler printHandler,
        IToastService toastService,
        IDialogService? dialogService = null)
        : base(host, loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _printHandler = printHandler ?? throw new ArgumentNullException(nameof(printHandler));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _dialogService = dialogService;

        SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, () => CanSave);
        SuspendCommand = new AsyncRelayCommand(ExecuteSuspendAsync, () => CanSuspend);
        CompleteCommand = new AsyncRelayCommand(ExecuteCompleteAsync, () => CanComplete);
        PrintCommand = new AsyncRelayCommand(ExecutePrintAsync, () => CanPrint);
        ExportPdfCommand = new AsyncRelayCommand(ExecuteExportPdfAsync, () => CanPrint);
        EnterEditModeCommand = new RelayCommand(ExecuteEnterEditMode, () => CanEnterEditMode);
        ImportFormulaCommand = new RelayCommand(ExecuteImportFormula);
        CopyHistoryCommand = new RelayCommand(ExecuteCopyHistory);
        ClearHerbsCommand = new AsyncRelayCommand(ExecuteClearHerbsAsync);
    }

    /// <summary>
    /// Called by parent when State changes to update CanExecute for all commands.
    /// CommunityToolkit IRelayCommand.NotifyCanExecuteChanged() replaces Prism's RaiseCanExecuteChanged().
    /// </summary>
    public void RefreshCanExecute()
    {
        SaveCommand.NotifyCanExecuteChanged();
        SuspendCommand.NotifyCanExecuteChanged();
        CompleteCommand.NotifyCanExecuteChanged();
        PrintCommand.NotifyCanExecuteChanged();
        ExportPdfCommand.NotifyCanExecuteChanged();
        EnterEditModeCommand.NotifyCanExecuteChanged();
    }

    #region CanExecute

    private bool CanSave => _context.State.IsEditing;
    private bool CanSuspend => _context.State.ShowSuspendButton;
    private bool CanComplete => _context.State.ShowCompleteButton && _context.State.CanComplete;
    private bool CanPrint => _context.State.CanPrint;
    private bool CanEnterEditMode => _context.State.ShowEditButton || _context.State.ShowEditButtonTopRight;

    #endregion

    #region Core Command Implementations

    private async Task ExecuteSaveAsync()
    {
        try
        {
            Host.SetBusy(true, "正在保存医案...");
            var result = await _medicalCaseService.AggregateSaveAsync(
                _context.MedicalCaseId,
                _dataProvider.GetConsultationData(),
                _dataProvider.GetPrescriptionData(),
                _dataProvider.GetRemark() ?? "",
                _dataProvider.GetEditReason() ?? "");

            if (result.Success)
            {
                _toastService.Show("医案已保存", ToastType.Success, 5000);
                Logger.LogInformation("医案保存成功, MedicalCaseId={MedicalCaseId}", _context.MedicalCaseId);
            }
            else
            {
                _toastService.Show($"保存失败：{result.Error ?? "未知错误"}", ToastType.Error, 4000);
                Logger.LogWarning("医案保存失败, Error={Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案数据失败");
            _toastService.Show("保存失败，请稍后重试", ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async Task ExecuteSuspendAsync()
    {
        try
        {
            Host.SetBusy(true, "正在暂存医案...");
            var result = await _medicalCaseService.SaveAndSuspendAsync(
                _context.MedicalCaseId,
                _dataProvider.GetConsultationData(),
                _dataProvider.GetPrescriptionData(),
                _dataProvider.GetRemark() ?? "");

            if (result.Success)
            {
                Host.NotifyStateChanged();
                _toastService.Show("医案已暂存，可稍后继续", ToastType.Info, 5000);
                Logger.LogInformation("医案已暂存, MedicalCaseId={MedicalCaseId}", _context.MedicalCaseId);
            }
            else
            {
                _toastService.Show($"暂存失败：{result.Error ?? "未知错误"}", ToastType.Error, 4000);
                Logger.LogWarning("医案暂存失败, Error={Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "暂存医案失败");
            _toastService.Show("暂存失败，请稍后重试", ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async Task ExecuteCompleteAsync()
    {
        try
        {
            Host.SetBusy(true, "正在完成看诊并归档...");
            var result = await _medicalCaseService.SaveAndCompleteAsync(
                _context.MedicalCaseId,
                _dataProvider.GetConsultationData(),
                _dataProvider.GetPrescriptionData(),
                _dataProvider.GetConsultationValidator(),
                _dataProvider.GetPrescriptionValidator(),
                _dataProvider.GetRemark() ?? "",
                _dataProvider.GetIsPrescriptionEnabled());

            if (result.Success)
            {
                Host.NotifyStateChanged();
                _toastService.Show("看诊完成，医案已归档", ToastType.Success, 5000);
                Logger.LogInformation("医案完成, MedicalCaseId={MedicalCaseId}", _context.MedicalCaseId);
            }
            else
            {
                _toastService.Show($"完成失败：{result.Error ?? "未知错误"}", ToastType.Error, 4000);
                Logger.LogWarning("医案完成失败, Error={Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "完成医案失败");
            _toastService.Show("完成失败，请稍后重试", ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async Task ExecutePrintAsync()
    {
        try
        {
            Host.SetBusy(true, "正在准备打印预览...");

            var consultationItem = _dataProvider.GetConsultationItem();
            var consultationData = consultationItem != null
                ? _consultationMapper.ToInputDto(consultationItem)
                : null;

            var result = await _printHandler.PrintPreviewAsync(
                _context.MedicalCaseId,
                _dataProvider.GetPrescriptionProvider(),
                _context.CurrentPatient,
                consultationData);

            if (!result.IsSuccess)
            {
                _toastService.Show($"打印失败：{result.ErrorMessage ?? "未知错误"}", ToastType.Error, 4000);
                Logger.LogWarning("打印失败, Error={Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "打印处方笺失败");
            _toastService.Show("打印失败，请稍后重试", ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async Task ExecuteExportPdfAsync()
    {
        try
        {
            Host.SetBusy(true, "正在生成PDF文件...");

            var consultationItem = _dataProvider.GetConsultationItem();
            var consultationData = consultationItem != null
                ? _consultationMapper.ToInputDto(consultationItem)
                : null;

            var result = await _printHandler.ExportPdfAsync(
                _context.MedicalCaseId,
                _dataProvider.GetPrescriptionProvider(),
                _context.CurrentPatient,
                consultationData);

            if (!result.IsSuccess)
            {
                _toastService.Show($"导出失败：{result.ErrorMessage ?? "未知错误"}", ToastType.Error, 4000);
                Logger.LogWarning("PDF导出失败, Error={Error}", result.ErrorMessage);
            }
            else
            {
                _toastService.Show("PDF导出成功，文件已保存", ToastType.Success, 5000);
                Logger.LogInformation("PDF导出成功, MedicalCaseId={MedicalCaseId}", _context.MedicalCaseId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导出PDF失败");
            _toastService.Show("导出失败，请稍后重试", ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private void ExecuteEnterEditMode()
    {
        Host.RequestEnterEditMode();
    }

    #endregion

    #region Import Operations (migrated from PrescriptionImportHandler)

    private void ExecuteImportFormula()
    {
        if (_dialogService == null)
        {
            Logger.LogWarning("DialogService为空，无法打开验方导入对话框");
            return;
        }

        _dialogService.ShowDialog("FormulaImportDialog", null, async r =>
        {
            if (r.Result == ButtonResult.OK)
                await HandleFormulaImportResultAsync(r.Parameters);
        });
    }

    private void ExecuteCopyHistory()
    {
        if (_dialogService == null)
        {
            Logger.LogWarning("DialogService为空，无法打开历史复制对话框");
            return;
        }

        var currentPatient = _context.CurrentPatient;
        var parameters = new DialogParameters
        {
            { "PatientId", currentPatient?.Id ?? Guid.Empty },
            { "PatientName", currentPatient?.Name ?? string.Empty }
        };

        _dialogService.ShowDialog("HistoryCopyDialog", parameters, async r =>
        {
            if (r.Result == ButtonResult.OK)
                await HandleHistoryCopyResultAsync(r.Parameters);
        });
    }

    private async Task ExecuteClearHerbsAsync()
    {
        var prescription = _dataProvider.GetPrescriptionItem();
        if (prescription == null)
        {
            Logger.LogWarning("处方数据为空，无法清空药材");
            return;
        }

        var validItemCount = prescription.Items.Count(h => h.HerbId != Guid.Empty);
        if (validItemCount == 0)
        {
            _toastService.ShowInfo("当前没有可清空的药材");
            return;
        }

        var confirmed = await Host.ShowConfirmAsync($"确定要清空当前所有药材（共{validItemCount}项）吗？", "清空药材");
        if (!confirmed) return;

        prescription.Items.Clear();
        Logger.LogInformation("已清空处方药材，共{Count}项", validItemCount);
        _toastService.Show($"已清空所有药材（共{validItemCount}味）", ToastType.Warning, 4000);
    }

    private Task HandleFormulaImportResultAsync(IDialogParameters parameters)
    {
        try
        {
            Host.SetBusy(true, "正在导入验方药材...");

            if (!parameters.TryGetValue<FormulaDetailDto>("SelectedFormula", out var formula) || formula == null)
                return Task.CompletedTask;

            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            {
                _toastService.Show("验方无药材信息", ToastType.Error, 4000);
                return Task.CompletedTask;
            }

            var prescription = _dataProvider.GetPrescriptionItem();
            if (prescription == null)
            {
                Logger.LogWarning("处方数据为空，无法导入验方");
                return Task.CompletedTask;
            }

            var herbPrices = BuildHerbPriceLookup();
            var herbItems = FilterDisabledHerbs(formula.ToPrescriptionItemDtos(herbs, herbPrices), "验方导入");
            if (!herbItems.Any())
            {
                _toastService.Show("验方无有效药材", ToastType.Error, 4000);
                return Task.CompletedTask;
            }

            foreach (var item in herbItems)
                prescription.Items.Add(item);

            // Record referenced formula name
            if (!string.IsNullOrEmpty(formula.Name))
            {
                if (string.IsNullOrEmpty(prescription.ReferencedFormulas))
                    prescription.ReferencedFormulas = formula.Name;
                else if (!prescription.ReferencedFormulas.Contains(formula.Name))
                    prescription.ReferencedFormulas = $"{prescription.ReferencedFormulas}, {formula.Name}";
            }

            _toastService.Show($"已导入验方「{formula.Name}」，共{herbItems.Count}味药材", ToastType.Success, 5000);
            Logger.LogInformation("验方导入成功, FormulaName={FormulaName}, HerbCount={Count}", formula.Name, herbItems.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理验方导入结果异常");
            _toastService.Show(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入", ex), ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
        return Task.CompletedTask;
    }

    private Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
    {
        try
        {
            Host.SetBusy(true, "正在复制历史处方...");

            if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items?.Any() != true)
            {
                _toastService.Show("历史处方无药材记录", ToastType.Error, 4000);
                return Task.CompletedTask;
            }

            var prescription = _dataProvider.GetPrescriptionItem();
            if (prescription == null)
            {
                Logger.LogWarning("处方数据为空，无法复制历史处方");
                return Task.CompletedTask;
            }

            var herbPrices = BuildHerbPriceLookup();
            var herbItems = FilterDisabledHerbs(items.ToPrescriptionItemDtos(herbPrices), "历史复制");
            if (!herbItems.Any())
            {
                _toastService.Show("历史处方无有效药材", ToastType.Error, 4000);
                return Task.CompletedTask;
            }

            foreach (var item in herbItems)
                prescription.Items.Add(item);

            if (parameters.TryGetValue<MedicalCaseDetailDto>("SelectedCase", out var selectedCase) && selectedCase != null)
            {
                var sourceRef = !string.IsNullOrEmpty(selectedCase.CaseNumber)
                    ? $"复制自{selectedCase.CaseNumber}"
                    : "复制自历史医案";

                if (string.IsNullOrEmpty(prescription.ReferencedFormulas))
                    prescription.ReferencedFormulas = sourceRef;
                else if (!prescription.ReferencedFormulas.Contains(sourceRef))
                    prescription.ReferencedFormulas = $"{prescription.ReferencedFormulas}, {sourceRef}";

                if (selectedCase.Prescription != null)
                {
                    if (selectedCase.Prescription.DosageCount > 0 && prescription.DosageCount == 0)
                        prescription.DosageCount = selectedCase.Prescription.DosageCount;
                    if (selectedCase.Prescription.Discount > 0 && prescription.Discount == 0)
                        prescription.Discount = selectedCase.Prescription.Discount;
                }
            }

            _toastService.Show($"已复制历史处方，共{herbItems.Count}味药材", ToastType.Success, 5000);
            Logger.LogInformation("历史处方复制成功, HerbCount={Count}", herbItems.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理历史复制结果异常");
            _toastService.Show(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制", ex), ToastType.Error, 4000);
        }
        finally
        {
            Host.SetBusy(false);
        }
        return Task.CompletedTask;
    }

    private IReadOnlyDictionary<Guid, decimal>? BuildHerbPriceLookup()
    {
        var allHerbs = _dataProvider.GetAllHerbs();
        if (allHerbs == null) return null;

        return allHerbs
            .Where(h => h.Status == CommonStatus.Enabled)
            .ToDictionary(h => h.Id, h => h.Price);
    }

    private IReadOnlyList<PrescriptionItemDto> FilterDisabledHerbs(
        IReadOnlyList<PrescriptionItemDto> items, string source)
    {
        var allHerbs = _dataProvider.GetAllHerbs();
        if (allHerbs == null) return items;

        var disabledHerbIds = new HashSet<Guid>(
            allHerbs.Where(h => h.Status != CommonStatus.Enabled).Select(h => h.Id));

        var filtered = items.Where(h => !disabledHerbIds.Contains(h.HerbId)).ToList();
        var skippedCount = items.Count - filtered.Count;
        if (skippedCount > 0)
            Logger.LogInformation("{Source}跳过 {Count} 味已禁用药材", source, skippedCount);

        return filtered;
    }

    #endregion
}
