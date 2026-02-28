using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Extensions;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Clinical.Handlers;

/// <summary>
/// 处方导入处理器
/// 负责验方导入、历史处方复制、药材清空等操作
/// OpenSpec: refactor-workspace-srp - 从MedicalCaseWorkspaceViewModel提取
/// </summary>
public class PrescriptionImportHandler
{
    #region 字段

    private readonly IDialogService? _dialogService;
    private readonly ILogger<PrescriptionImportHandler> _logger;

    #endregion

    #region 属性

    /// <summary>
    /// 获取弹窗服务（从ViewModel回调获取）
    /// </summary>
    public Func<ICommonDialogService?>? GetCommonDialogService { get; set; }

    /// <summary>
    /// 设置忙碌状态的回调
    /// </summary>
    public Action<bool, string?>? SetBusy { get; set; }

    /// <summary>
    /// 显示错误消息的回调
    /// </summary>
    public Func<string, Task>? ShowErrorMessage { get; set; }

    /// <summary>
    /// 显示成功消息的回调
    /// </summary>
    public Func<string, Task>? ShowSuccessMessage { get; set; }

    /// <summary>
    /// 显示确认消息的回调
    /// </summary>
    public Func<string, string, Task<bool>>? ShowConfirmMessage { get; set; }

    /// <summary>
    /// 获取当前患者的回调
    /// </summary>
    public Func<PatientDetailDto?>? GetCurrentPatient { get; set; }

    /// <summary>
    /// 获取处方Item的回调
    /// </summary>
    public Func<PrescriptionItem?>? GetPrescription { get; set; }

    /// <summary>
    /// 获取全部药材列表（用于过滤禁用药材）
    /// T5-P2-19, T5-P2-21
    /// </summary>
    public Func<IEnumerable<HerbListDto>?>? GetAllHerbs { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public PrescriptionImportHandler(
        IDialogService? dialogService,
        ILoggerFactory loggerFactory)
    {
        _dialogService = dialogService;
        _logger = loggerFactory.CreateLogger<PrescriptionImportHandler>();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 打开验方导入对话框
    /// </summary>
    public void OpenFormulaImportDialog()
    {
        if (_dialogService == null)
        {
            _logger.LogWarning("DialogService为空，无法打开验方导入对话框");
            return;
        }

        _dialogService.ShowDialog("FormulaImportDialog", null, async r =>
        {
            if (r.Result == ButtonResult.OK)
                await HandleFormulaImportResultAsync(r.Parameters);
        });
    }

    /// <summary>
    /// 打开历史处方复制对话框
    /// </summary>
    public void OpenHistoryCopyDialog()
    {
        if (_dialogService == null)
        {
            _logger.LogWarning("DialogService为空，无法打开历史复制对话框");
            return;
        }

        var currentPatient = GetCurrentPatient?.Invoke();
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

    /// <summary>
    /// 清空药材列表
    /// </summary>
    public async Task ClearHerbItemsAsync()
    {
        var prescription = GetPrescription?.Invoke();
        if (prescription == null)
        {
            _logger.LogWarning("处方数据为空，无法清空药材");
            return;
        }

        // 检查有效药材数量
        var validItemCount = prescription.Items.Count(h => h.HerbId != Guid.Empty);
        if (validItemCount == 0)
        {
            if (ShowSuccessMessage != null)
                await ShowSuccessMessage.Invoke("当前没有可清空的药材");
            return;
        }

        // 确认清空
        if (ShowConfirmMessage != null)
        {
            var confirmed = await ShowConfirmMessage.Invoke($"确定要清空当前所有药材（共{validItemCount}项）吗？", "清空药材");
            if (!confirmed)
                return;
        }

        prescription.Items.Clear();
        _logger.LogInformation("已清空处方药材，共{Count}项", validItemCount);
        if (ShowSuccessMessage != null)
            await ShowSuccessMessage.Invoke($"已清空{validItemCount}项药材");
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 过滤禁用药材
    /// T5-P2-19, T5-P2-21
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
        {
            _logger.LogInformation("{Source}跳过 {Count} 味已禁用药材", source, skippedCount);
        }
        return filtered;
    }

    /// <summary>
    /// 处理验方导入结果
    /// </summary>
    private async Task HandleFormulaImportResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetBusy?.Invoke(true, "正在导入验方...");

            if (!parameters.TryGetValue<FormulaDetailDto>("SelectedFormula", out var formula) || formula == null)
                return;

            if (!parameters.TryGetValue<List<FormulaHerbItemDto>>("SelectedHerbs", out var herbs) || herbs?.Any() != true)
            {
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("验方无药材信息");
                return;
            }

            var prescription = GetPrescription?.Invoke();
            if (prescription == null)
            {
                _logger.LogWarning("处方数据为空，无法导入验方");
                return;
            }

            // 转换为PrescriptionItemDto并添加
            var herbItems = FilterDisabledHerbs(formula.ToPrescriptionItemDtos(herbs), "验方导入");
            if (!herbItems.Any())
            {
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("验方无有效药材");
                return;
            }

            foreach (var item in herbItems)
            {
                prescription.Items.Add(item);
            }

            // 记录引用的验方名称
            if (!string.IsNullOrEmpty(formula.Name))
            {
                if (string.IsNullOrEmpty(prescription.ReferencedFormulas))
                    prescription.ReferencedFormulas = formula.Name;
                else if (!prescription.ReferencedFormulas.Contains(formula.Name))
                    prescription.ReferencedFormulas = $"{prescription.ReferencedFormulas}, {formula.Name}";
            }

            if (ShowSuccessMessage != null)
                await ShowSuccessMessage.Invoke($"已导入验方「{formula.Name}」，共 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理验方导入结果异常");
            if (ShowErrorMessage != null)
                await ShowErrorMessage.Invoke(ClientErrorMessageMapper.GetSafeOperationFailureMessage("导入", ex));
        }
        finally
        {
            SetBusy?.Invoke(false, null);
        }
    }

    /// <summary>
    /// 处理历史处方复制结果
    /// </summary>
    private async Task HandleHistoryCopyResultAsync(IDialogParameters parameters)
    {
        try
        {
            SetBusy?.Invoke(true, "正在复制处方...");

            if (!parameters.TryGetValue<List<PrescriptionItemDto>>("SelectedItems", out var items) || items?.Any() != true)
            {
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("历史处方无药材记录");
                return;
            }

            var prescription = GetPrescription?.Invoke();
            if (prescription == null)
            {
                _logger.LogWarning("处方数据为空，无法复制历史处方");
                return;
            }

            // 转换为PrescriptionItemDto并添加
            // T5-P2-21: 过滤禁用药材
            var herbItems = FilterDisabledHerbs(items.ToPrescriptionItemDtos(), "历史复制");
            if (!herbItems.Any())
            {
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("历史处方无有效药材");
                return;
            }

            foreach (var item in herbItems)
            {
                prescription.Items.Add(item);
            }

            // T5-P2-23 + T5-P3-09: 从历史医案复制来源信息和处方级字段
            if (parameters.TryGetValue<MedicalCaseDetailDto>("SelectedCase", out var selectedCase) && selectedCase != null)
            {
                // T5-P2-23: 记录历史复制来源
                var sourceRef = !string.IsNullOrEmpty(selectedCase.CaseNumber)
                    ? $"复制自{selectedCase.CaseNumber}"
                    : "复制自历史医案";

                if (string.IsNullOrEmpty(prescription.ReferencedFormulas))
                    prescription.ReferencedFormulas = sourceRef;
                else if (!prescription.ReferencedFormulas.Contains(sourceRef))
                    prescription.ReferencedFormulas = $"{prescription.ReferencedFormulas}, {sourceRef}";

                // T5-P3-09: 复制处方级别字段
                if (selectedCase.Prescription != null)
                {
                    if (selectedCase.Prescription.DosageCount > 0 && prescription.DosageCount == 0)
                        prescription.DosageCount = selectedCase.Prescription.DosageCount;
                    if (selectedCase.Prescription.Discount > 0 && prescription.Discount == 0)
                        prescription.Discount = selectedCase.Prescription.Discount;
                }
            }

            if (ShowSuccessMessage != null)
                await ShowSuccessMessage.Invoke($"已复制 {herbItems.Count} 味药材");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理历史复制结果异常");
            if (ShowErrorMessage != null)
                await ShowErrorMessage.Invoke(ClientErrorMessageMapper.GetSafeOperationFailureMessage("复制", ex));
        }
        finally
        {
            SetBusy?.Invoke(false, null);
        }
    }

    #endregion
}
