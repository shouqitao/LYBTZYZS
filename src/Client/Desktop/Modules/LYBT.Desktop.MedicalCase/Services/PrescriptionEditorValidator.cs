using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 处方编辑器验证器 - 负责所有验证逻辑
/// Issue #1790: 从PrescriptionEditorViewModel提取验证逻辑(~150行)
/// </summary>
public class PrescriptionEditorValidator
{
    private readonly IPrescriptionEditorService _prescriptionEditorService;
    private readonly ILogger<PrescriptionEditorValidator> _logger;

    /// <summary>
    /// 验证结果事件
    /// </summary>
    public event EventHandler<ValidationResultEventArgs>? ValidationCompleted;

    public PrescriptionEditorValidator(
        IPrescriptionEditorService prescriptionEditorService,
        ILogger<PrescriptionEditorValidator> logger)
    {
        _prescriptionEditorService = prescriptionEditorService ?? throw new ArgumentNullException(nameof(prescriptionEditorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 验证处方数据
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    public ValidationResult Validate(
        PatientDto? currentPatient,
        Guid medicalCaseId,
        List<PrescriptionItemDto> allItems,
        List<HerbDto> allHerbs)
    {
        var errors = new List<string>();

        ValidateBasicInfo(currentPatient, medicalCaseId, errors);
        ValidateHerbItems(allItems, allHerbs, errors);

        var result = new ValidationResult
        {
            IsValid = !errors.Any(),
            ValidationMessage = errors.Any() ? string.Join("；", errors) : string.Empty,
            ItemCount = allItems.Count
        };

        if (!result.IsValid)
        {
            _logger.LogWarning("处方验证失败：{ValidationMessage}", result.ValidationMessage);
        }
        else
        {
            _logger.LogInformation("处方验证通过，共{ItemCount}味药材", result.ItemCount);
        }

        // 触发事件
        ValidationCompleted?.Invoke(this, new ValidationResultEventArgs { Result = result });

        return result;
    }

    /// <summary>
    /// 验证基本信息（患者、医案ID）
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    private void ValidateBasicInfo(PatientDto? currentPatient, Guid medicalCaseId, List<string> errors)
    {
        if (currentPatient == null)
            errors.Add("请先选择患者");

        if (medicalCaseId == Guid.Empty)
            errors.Add("MedicalCaseId不能为空");
    }

    /// <summary>
    /// 验证药材项列表
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    private void ValidateHerbItems(List<PrescriptionItemDto> allItems, List<HerbDto> allHerbs, List<string> errors)
    {
        if (allItems.Count == 0)
        {
            errors.Add("请至少添加一味药材");
            return;
        }

        foreach (var item in allItems)
        {
            if (!string.IsNullOrWhiteSpace(item.HerbName))
            {
                ValidateSingleHerbItem(item, allHerbs, errors);
            }
        }
    }

    /// <summary>
    /// 验证单个药材项
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    private void ValidateSingleHerbItem(PrescriptionItemDto item, List<HerbDto> allHerbs, List<string> errors)
    {
        var matchedHerb = allHerbs.FirstOrDefault(h =>
            h.Name.Equals(item.HerbName, StringComparison.OrdinalIgnoreCase));

        if (matchedHerb == null)
        {
            errors.Add($"药材 '{item.HerbName}' 在药材库中不存在，请检查名称或添加新药材");
        }
        else if (!matchedHerb.IsEnabled)
        {
            errors.Add($"药材 '{item.HerbName}' 已停用，请选择其他药材");
        }
        else
        {
            if (item.HerbId == Guid.Empty || item.HerbId != matchedHerb.Id)
            {
                item.HerbId = matchedHerb.Id;
                _logger.LogInformation("自动设置药材ID：{HerbName} → {HerbId}", item.HerbName, matchedHerb.Id);
            }

            if (item.Dosage <= 0)
            {
                errors.Add($"药材 '{item.HerbName}' 的用量必须大于0");
            }
        }
    }

    /// <summary>
    /// 验证处方草稿（调用Service层验证）
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    public async Task<bool> ValidateDraftAsync(PrescriptionDto draft)
    {
        var isValid = await _prescriptionEditorService.ValidatePrescriptionAsync(draft);
        if (!isValid)
        {
            _logger.LogWarning("处方草稿验证失败");
        }
        return isValid;
    }

    /// <summary>
    /// 检测重复药材
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    public string CheckDuplicateHerbs(List<PrescriptionItemDto> allItems)
    {
        try
        {
            if (allItems.Count == 0)
            {
                return string.Empty;
            }

            // 按 HerbId 分组，找出重复的药材
            var duplicates = allItems
                .Where(item => item.HerbId != Guid.Empty) // 只检查已关联药材库的药材
                .GroupBy(item => item.HerbId)
                .Where(group => group.Count() > 1)
                .Select(group => new
                {
                    HerbName = group.First().HerbName,
                    Count = group.Count(),
                    TotalDosage = group.Sum(item => item.Dosage)
                })
                .ToList();

            if (duplicates.Any())
            {
                var warningLines = duplicates.Select(d =>
                    $"• {d.HerbName}：重复{d.Count}次，累计用量{d.TotalDosage}g");
                var warningText = string.Join("\n", warningLines);

                _logger.LogWarning("检测到{Count}种重复药材", duplicates.Count);
                return warningText;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检测重复药材失败");
            return string.Empty;
        }
    }
}

/// <summary>
/// 验证结果
/// Issue #1790: 封装验证结果数据
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

/// <summary>
/// 验证完成事件参数
/// Issue #1790: 封装事件数据
/// </summary>
public class ValidationResultEventArgs : EventArgs
{
    public ValidationResult Result { get; set; } = new();
}
