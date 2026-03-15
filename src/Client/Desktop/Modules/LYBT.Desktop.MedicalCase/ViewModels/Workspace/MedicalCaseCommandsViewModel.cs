using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Extensions;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Components;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
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
    private readonly PrescriptionPrintHandler _printHandler;
    private readonly IDialogService? _dialogService;
    private readonly ConsultationMapper _consultationMapper = new();

    #region Data provider delegates (set by parent after construction)

    public Func<ConsultationInputDto?>? GetConsultationData { get; set; }
    public Func<PrescriptionInputDto?>? GetPrescriptionData { get; set; }
    public Func<IValidatable>? GetConsultationValidator { get; set; }
    public Func<IValidatable>? GetPrescriptionValidator { get; set; }
    public Func<IDataProvider?>? GetPrescriptionProvider { get; set; }
    public Func<ConsultationItem?>? GetConsultationItem { get; set; }
    public Func<PrescriptionItem?>? GetPrescriptionItem { get; set; }
    public Func<IEnumerable<HerbListDto>?>? GetAllHerbs { get; set; }

    #endregion

    #region State accessors from parent (set by parent)

    public Func<string>? GetRemark { get; set; }
    public Func<string>? GetEditReason { get; set; }
    public Func<bool>? GetIsPrescriptionEnabled { get; set; }

    #endregion

    #region Commands

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand SuspendCommand { get; }
    public DelegateCommand CompleteCommand { get; }
    public DelegateCommand PrintCommand { get; }
    public DelegateCommand ExportPdfCommand { get; }
    public DelegateCommand EnterEditModeCommand { get; }
    public DelegateCommand ImportFormulaCommand { get; }
    public DelegateCommand CopyHistoryCommand { get; }
    public DelegateCommand ClearHerbsCommand { get; }

    #endregion

    public MedicalCaseCommandsViewModel(
        IMedicalCaseWorkspaceContext context,
        IWorkspaceHost host,
        ILoggerFactory loggerFactory,
        IMedicalCaseService medicalCaseService,
        PrescriptionPrintHandler printHandler,
        IDialogService? dialogService = null)
        : base(host, loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _printHandler = printHandler ?? throw new ArgumentNullException(nameof(printHandler));
        _dialogService = dialogService;

        SaveCommand = new DelegateCommand(ExecuteSave, CanSave);
        SuspendCommand = new DelegateCommand(ExecuteSuspend, CanSuspend);
        CompleteCommand = new DelegateCommand(ExecuteComplete, CanComplete);
        PrintCommand = new DelegateCommand(ExecutePrint, CanPrint);
        ExportPdfCommand = new DelegateCommand(ExecuteExportPdf, CanPrint);
        EnterEditModeCommand = new DelegateCommand(ExecuteEnterEditMode, CanEnterEditMode);
        ImportFormulaCommand = new DelegateCommand(ExecuteImportFormula);
        CopyHistoryCommand = new DelegateCommand(ExecuteCopyHistory);
        ClearHerbsCommand = new DelegateCommand(ExecuteClearHerbs);
    }

    /// <summary>
    /// Called by parent when State changes to update CanExecute for all commands.
    /// DelegateCommand.ObservesProperty does NOT work across child VM boundaries.
    /// </summary>
    public void RefreshCanExecute()
    {
        SaveCommand.RaiseCanExecuteChanged();
        SuspendCommand.RaiseCanExecuteChanged();
        CompleteCommand.RaiseCanExecuteChanged();
        PrintCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();
        EnterEditModeCommand.RaiseCanExecuteChanged();
    }

    #region CanExecute

    private bool CanSave() => _context.State.IsEditing;
    private bool CanSuspend() => _context.State.ShowSuspendButton;
    private bool CanComplete() => _context.State.ShowCompleteButton && _context.State.CanComplete;
    private bool CanPrint() => _context.State.CanPrint;
    private bool CanEnterEditMode() => _context.State.ShowEditButton || _context.State.ShowEditButtonTopRight;

    #endregion

    #region Core Command Implementations

    private async void ExecuteSave()
    {
        try
        {
            Host.SetBusy(true, "正在保存...");
            var result = await _medicalCaseService.AggregateSaveAsync(
                _context.MedicalCaseId,
                GetConsultationData?.Invoke(),
                GetPrescriptionData?.Invoke(),
                GetRemark?.Invoke() ?? "",
                GetEditReason?.Invoke() ?? "");

            if (result.Success)
                await Host.ShowSuccessAsync("保存成功");
            else
                await Host.ShowErrorAsync(result.Error ?? "保存失败");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存医案数据失败");
            await Host.ShowErrorAsync("保存失败");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async void ExecuteSuspend()
    {
        try
        {
            Host.SetBusy(true, "正在挂起...");
            var result = await _medicalCaseService.SaveAndSuspendAsync(
                _context.MedicalCaseId,
                GetConsultationData?.Invoke(),
                GetPrescriptionData?.Invoke(),
                GetRemark?.Invoke() ?? "");

            if (result.Success)
            {
                Host.NotifyStateChanged();
                await Host.ShowSuccessAsync("医案已暂存");
            }
            else
            {
                await Host.ShowErrorAsync(result.Error ?? "暂存失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "暂存医案失败");
            await Host.ShowErrorAsync("暂存失败");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async void ExecuteComplete()
    {
        try
        {
            Host.SetBusy(true, "正在完成医案...");
            var result = await _medicalCaseService.SaveAndCompleteAsync(
                _context.MedicalCaseId,
                GetConsultationData?.Invoke(),
                GetPrescriptionData?.Invoke(),
                GetConsultationValidator?.Invoke(),
                GetPrescriptionValidator?.Invoke(),
                GetRemark?.Invoke() ?? "",
                GetIsPrescriptionEnabled?.Invoke() ?? false);

            if (result.Success)
            {
                Host.NotifyStateChanged();
                await Host.ShowSuccessAsync("医案已完成");
            }
            else
            {
                await Host.ShowErrorAsync(result.Error ?? "完成失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "完成医案失败");
            await Host.ShowErrorAsync("完成失败");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async void ExecutePrint()
    {
        try
        {
            Host.SetBusy(true, "正在准备预览...");

            var consultationItem = GetConsultationItem?.Invoke();
            var consultationData = consultationItem != null
                ? _consultationMapper.ToInputDto(consultationItem)
                : null;

            var result = await _printHandler.PrintPreviewAsync(
                _context.MedicalCaseId,
                GetPrescriptionProvider?.Invoke(),
                _context.CurrentPatient,
                consultationData);

            if (!result.IsSuccess)
            {
                await Host.ShowErrorAsync(result.ErrorMessage ?? "打印失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "打印处方笺失败");
            await Host.ShowErrorAsync("打印失败");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    /// <summary>
    /// D1: 导出处方笺为 PDF
    /// </summary>
    private async void ExecuteExportPdf()
    {
        try
        {
            Host.SetBusy(true, "正在导出PDF...");

            var consultationItem = GetConsultationItem?.Invoke();
            var consultationData = consultationItem != null
                ? _consultationMapper.ToInputDto(consultationItem)
                : null;

            var result = await _printHandler.ExportPdfAsync(
                _context.MedicalCaseId,
                GetPrescriptionProvider?.Invoke(),
                _context.CurrentPatient,
                consultationData);

            if (!result.IsSuccess)
            {
                await Host.ShowErrorAsync(result.ErrorMessage ?? "导出失败");
            }
            else
            {
                await Host.ShowSuccessAsync("PDF导出成功");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导出PDF失败");
            await Host.ShowErrorAsync("导出失败");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private void ExecuteEnterEditMode()
    {
        Host.NotifyStateChanged();
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

    private async void ExecuteClearHerbs()
    {
        var prescription = GetPrescriptionItem?.Invoke();
        if (prescription == null)
        {
            Logger.LogWarning("处方数据为空，无法清空药材");
            return;
        }

        var validItemCount = prescription.Items.Count(h => h.HerbId != Guid.Empty);
        if (validItemCount == 0)
        {
            await Host.ShowSuccessAsync("当前没有可清空的药材");
            return;
        }

        var confirmed = await Host.ShowConfirmAsync($"确定要清空当前所有药材（共{validItemCount}项）吗？", "清空药材");
        if (!confirmed) return;

        prescription.Items.Clear();
        Logger.LogInformation("已清空处方药材，共{Count}项", validItemCount);
        await Host.ShowSuccessAsync($"已清空{validItemCount}项药材");
    }

    private async Task HandleFormulaImportResultAsync(IDialogParameters parameters)
    {
        try
        {
            Host.SetBusy(true, "正在导入验方...");

            if (!parameters.TryGetValue<FormulaDetailDto>("SelectedFormula", out var formula) || formula == null)
                return;

            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            {
                await Host.ShowErrorAsync("验方无药材信息");
                return;
            }

            var prescription = GetPrescriptionItem?.Invoke();
            if (prescription == null)
            {
                Logger.LogWarning("处方数据为空，无法导入验方");
                return;
            }

            var herbPrices = BuildHerbPriceLookup();
            var herbItems = FilterDisabledHerbs(formula.ToPrescriptionItemDtos(herbs, herbPrices), "验方导入");
            if (!herbItems.Any())
            {
                await Host.ShowErrorAsync("验方无有效药材");
                return;
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

            await Host.ShowSuccessAsync($"已导入验方「{formula.Name}」，共 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理验方导入结果异常");
            await Host.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入", ex));
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    private async Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
    {
        try
        {
            Host.SetBusy(true, "正在复制处方...");

            if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items?.Any() != true)
            {
                await Host.ShowErrorAsync("历史处方无药材记录");
                return;
            }

            var prescription = GetPrescriptionItem?.Invoke();
            if (prescription == null)
            {
                Logger.LogWarning("处方数据为空，无法复制历史处方");
                return;
            }

            // T5-P2-21: Filter disabled herbs
            // CODE-08: 复制历史处方时刷新为当前药材价格
            var herbPrices = BuildHerbPriceLookup();
            var herbItems = FilterDisabledHerbs(items.ToPrescriptionItemDtos(herbPrices), "历史复制");
            if (!herbItems.Any())
            {
                await Host.ShowErrorAsync("历史处方无有效药材");
                return;
            }

            foreach (var item in herbItems)
                prescription.Items.Add(item);

            // T5-P2-23 + T5-P3-09: Copy source info and prescription-level fields
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

            await Host.ShowSuccessAsync($"已复制 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理历史复制结果异常");
            await Host.ShowErrorAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制", ex));
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    /// <summary>
    /// CODE-08: 从 AllHerbs 构建 HerbId -> 当前价格 查找表
    /// </summary>
    private IReadOnlyDictionary<Guid, decimal>? BuildHerbPriceLookup()
    {
        var allHerbs = GetAllHerbs?.Invoke();
        if (allHerbs == null) return null;

        return allHerbs
            .Where(h => h.Status == CommonStatus.Enabled)
            .ToDictionary(h => h.Id, h => h.Price);
    }

    /// <summary>
    /// Filter disabled herbs from import source. T5-P2-19, T5-P2-21.
    /// </summary>
    private IReadOnlyList<PrescriptionItemDto> FilterDisabledHerbs(
        IReadOnlyList<PrescriptionItemDto> items, string source)
    {
        var allHerbs = GetAllHerbs?.Invoke();
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
