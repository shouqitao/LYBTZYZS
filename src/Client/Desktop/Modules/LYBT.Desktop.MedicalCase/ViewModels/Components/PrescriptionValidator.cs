using System.Collections.ObjectModel;
using LYBT.Desktop.Prescriptions.Models.Items;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方验证器
/// 负责处方数据验证、重复药材检测
/// OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanelViewModel拆分
/// </summary>
public class PrescriptionValidator
{
    #region 字段

    private readonly ILogger<PrescriptionValidator> _logger;

    #endregion

    #region 构造函数

    public PrescriptionValidator(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PrescriptionValidator>();
    }

    #endregion

    #region 重复药材检测

    /// <summary>
    /// 检查重复药材
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <returns>验证结果，包含是否有重复和重复药材名称</returns>
    public DuplicateHerbsCheckResult CheckDuplicateHerbs(ObservableCollection<PrescriptionHerbItem> herbItems)
    {
        if (herbItems == null || herbItems.Count == 0)
        {
            return DuplicateHerbsCheckResult.NoDuplicates();
        }

        var herbIds = new List<Guid>();
        var duplicates = new List<string>();

        foreach (var item in herbItems)
        {
            if (item.HerbId != Guid.Empty)
            {
                if (herbIds.Contains(item.HerbId))
                {
                    duplicates.Add(item.HerbName);
                }
                else
                {
                    herbIds.Add(item.HerbId);
                }
            }
        }

        if (duplicates.Any())
        {
            var distinctDuplicates = duplicates.Distinct().ToList();
            var warningText = $"发现重复药材：{string.Join("、", distinctDuplicates)}";

            _logger.LogWarning("处方验证: {WarningText}", warningText);

            return DuplicateHerbsCheckResult.WithDuplicates(warningText, distinctDuplicates);
        }

        return DuplicateHerbsCheckResult.NoDuplicates();
    }

    #endregion

    #region 处方完整性验证

    /// <summary>
    /// 验证处方是否有效（至少有一个药材）
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <returns>是否有效</returns>
    public bool ValidateHasItems(ObservableCollection<PrescriptionHerbItem> herbItems)
    {
        if (herbItems == null)
        {
            return false;
        }

        return herbItems.Any(h => h.HerbId != Guid.Empty);
    }

    /// <summary>
    /// 验证剂数是否有效
    /// </summary>
    /// <param name="dosageCount">剂数</param>
    /// <returns>是否有效</returns>
    public bool ValidateDosageCount(int dosageCount)
    {
        return dosageCount > 0 && dosageCount <= 365; // 最多一年的量
    }

    /// <summary>
    /// 完整验证处方
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="dosageCount">剂数</param>
    /// <returns>验证结果</returns>
    public PrescriptionValidationResult Validate(
        ObservableCollection<PrescriptionHerbItem> herbItems,
        int dosageCount)
    {
        var errors = new List<string>();

        if (!ValidateHasItems(herbItems))
        {
            errors.Add("处方至少需要一个药材");
        }

        if (!ValidateDosageCount(dosageCount))
        {
            errors.Add("剂数必须在1-365之间");
        }

        var duplicateCheck = CheckDuplicateHerbs(herbItems);
        if (duplicateCheck.HasDuplicates)
        {
            // 重复药材是警告，不是错误
            _logger.LogWarning("处方包含重复药材: {Duplicates}",
                string.Join(", ", duplicateCheck.DuplicateNames));
        }

        return new PrescriptionValidationResult(
            isValid: errors.Count == 0,
            errors: errors,
            duplicateCheck: duplicateCheck);
    }

    #endregion
}

#region 结果类型

/// <summary>
/// 重复药材检查结果
/// </summary>
public class DuplicateHerbsCheckResult
{
    public bool HasDuplicates { get; }
    public string WarningText { get; }
    public IReadOnlyList<string> DuplicateNames { get; }

    private DuplicateHerbsCheckResult(bool hasDuplicates, string warningText, IReadOnlyList<string> duplicateNames)
    {
        HasDuplicates = hasDuplicates;
        WarningText = warningText;
        DuplicateNames = duplicateNames;
    }

    public static DuplicateHerbsCheckResult NoDuplicates()
        => new(false, string.Empty, Array.Empty<string>());

    public static DuplicateHerbsCheckResult WithDuplicates(string warningText, IReadOnlyList<string> duplicateNames)
        => new(true, warningText, duplicateNames);
}

/// <summary>
/// 处方验证结果
/// </summary>
public class PrescriptionValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }
    public DuplicateHerbsCheckResult DuplicateCheck { get; }

    public PrescriptionValidationResult(
        bool isValid,
        IReadOnlyList<string> errors,
        DuplicateHerbsCheckResult duplicateCheck)
    {
        IsValid = isValid;
        Errors = errors;
        DuplicateCheck = duplicateCheck;
    }
}

#endregion
