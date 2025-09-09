using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Transactions;
// Removed: using LYBT.Infrastructure.Transactions.Steps; - DatabaseTransactionStep now in main namespace
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Transactions.Steps
{
    /// <summary>
    /// 验证药材配伍安全性事务步骤
    /// 负责检查处方中药材的配伍禁忌和安全性
    /// </summary>
    public class ValidateCompatibilityStep : DatabaseTransactionStep<PrescriptionTransactionContext>
    {
        /// <inheritdoc />
        public override string StepName => "ValidateCompatibility";

        /// <inheritdoc />
        public override int Order => 4;

        /// <inheritdoc />
        public override bool SupportsCompensation => false; // 验证步骤无需补偿

        /// <inheritdoc />
        public override TimeSpan Timeout => TimeSpan.FromSeconds(45);

        public ValidateCompatibilityStep(AppDbContext dbContext, ILogger<ValidateCompatibilityStep> logger)
            : base(dbContext, logger)
        {
        }

        /// <inheritdoc />
        public override async Task<bool> CanExecuteAsync(PrescriptionTransactionContext context, CancellationToken cancellationToken = default)
        {
            // 检查基础条件
            if (!await base.CanExecuteAsync(context, cancellationToken))
                return false;

            try
            {
                // 必须已经创建处方和添加药材项目
                if (!context.PrescriptionId.HasValue)
                {
                    context.LogError("Cannot validate compatibility without prescription ID");
                    return false;
                }

                // 检查是否需要配伍检查
                if (!context.RequireCompatibilityCheck)
                {
                    context.LogInformation("Compatibility check skipped as per configuration");
                    context.SetValidationResult("CompatibilityCheckRequired", false);
                    return false; // 跳过此步骤
                }

                // 验证是否有药材项目需要检查
                if (context.Items == null || context.Items.Count == 0)
                {
                    context.LogError("No prescription items to validate");
                    return false;
                }

                if (context.Items.Count == 1)
                {
                    context.LogInformation("Single herb prescription, compatibility check not needed");
                    context.SetValidationResult("SingleHerbPrescription", true);
                    return false; // 单味药无需配伍检查
                }

                context.SetValidationResult("CompatibilityCheckRequired", true);
                return true;
            }
            catch (Exception ex)
            {
                context.LogError(ex, "Failed to validate compatibility check conditions");
                return false;
            }
        }

        /// <inheritdoc />
        protected override async Task<TransactionStepResult> ExecuteDatabaseOperationAsync(
            PrescriptionTransactionContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var validationResults = new List<string>();
                var warnings = new List<string>();
                var errors = new List<string>();

                // 1. 基础配伍检查
                var basicCheck = await PerformBasicCompatibilityCheckAsync(context, cancellationToken);
                validationResults.Add($"BasicCompatibilityCheck:Result={basicCheck.IsValid}:Warnings={basicCheck.Warnings.Count}");
                warnings.AddRange(basicCheck.Warnings);
                if (!basicCheck.IsValid)
                {
                    errors.AddRange(basicCheck.Errors);
                }

                // 2. 十八反检查（相反药物配伍禁忌）
                var eighteenContraryCheck = await CheckEighteenContraryAsync(context, cancellationToken);
                validationResults.Add($"EighteenContraryCheck:Result={eighteenContraryCheck.IsValid}:Issues={eighteenContraryCheck.Issues.Count}");
                if (!eighteenContraryCheck.IsValid)
                {
                    errors.AddRange(eighteenContraryCheck.Issues);
                }

                // 3. 十九畏检查（相畏药物配伍注意）
                var nineteenFearCheck = await CheckNineteenFearAsync(context, cancellationToken);
                validationResults.Add($"NineteenFearCheck:Result={nineteenFearCheck.IsValid}:Issues={nineteenFearCheck.Issues.Count}");
                warnings.AddRange(nineteenFearCheck.Issues);

                // 4. 妊娠用药禁忌检查（如果患者信息包含性别和年龄）
                var pregnancyCheck = await CheckPregnancyCompatibilityAsync(context, cancellationToken);
                validationResults.Add($"PregnancyCheck:Result={pregnancyCheck.IsValid}:Issues={pregnancyCheck.Issues.Count}");
                if (!pregnancyCheck.IsValid)
                {
                    errors.AddRange(pregnancyCheck.Issues);
                }

                // 5. 剂量安全性检查
                var dosageCheck = await CheckDosageSafetyAsync(context, cancellationToken);
                validationResults.Add($"DosageCheck:Result={dosageCheck.IsValid}:Issues={dosageCheck.Issues.Count}");
                warnings.AddRange(dosageCheck.Issues);
                if (!dosageCheck.IsValid)
                {
                    errors.AddRange(dosageCheck.Errors);
                }

                // 6. 记录检查历史
                await RecordCompatibilityCheckHistoryAsync(context, validationResults, warnings, errors, cancellationToken);

                // 判断整体结果
                bool overallValid = errors.Count == 0;
                var severityLevel = DetermineSeverityLevel(errors, warnings);

                Logger.LogInformation(
                    "Compatibility validation completed: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}, Severity={Severity}",
                    overallValid, errors.Count, warnings.Count, severityLevel);

                // 根据配置决定是否阻止事务继续
                if (!overallValid && severityLevel == "Critical")
                {
                    var criticalErrorMessage = string.Join("; ", errors);
                    Logger.LogError("Critical compatibility issues found: {Errors}", criticalErrorMessage);
                    return CreateFailureResult(new InvalidOperationException($"存在严重配伍禁忌：{criticalErrorMessage}"));
                }

                // 返回成功结果（包含警告信息）
                return CreateSuccessResult(new Dictionary<string, object>
                {
                    ["CompatibilityValid"] = overallValid,
                    ["SeverityLevel"] = severityLevel,
                    ["ErrorCount"] = errors.Count,
                    ["WarningCount"] = warnings.Count,
                    ["Errors"] = errors,
                    ["Warnings"] = warnings,
                    ["ValidationResults"] = validationResults,
                    ["CheckedItemCount"] = context.Items.Count,
                    ["Timestamp"] = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to validate prescription compatibility");
                throw;
            }
        }

        /// <summary>
        /// 执行基础配伍检查
        /// </summary>
        private async Task<(bool IsValid, List<string> Warnings, List<string> Errors)> PerformBasicCompatibilityCheckAsync(
            PrescriptionTransactionContext context, CancellationToken cancellationToken)
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            try
            {
                // 检查重复药材
                var herbGroups = context.Items.GroupBy(item => item.HerbId).Where(g => g.Count() > 1);
                foreach (var group in herbGroups)
                {
                    var herbName = group.First().HerbName;
                    var totalQuantity = group.Sum(item => item.Quantity);
                    warnings.Add($"重复用药：{herbName}，总用量：{totalQuantity}克");
                }

                // 检查药材名称一致性
                foreach (var item in context.Items)
                {
                    var herb = await FindEntityAsync<LYBT.Entities.Herbs.Herb>(item.HerbId, cancellationToken);
                    if (herb != null && herb.Name != item.HerbName)
                    {
                        warnings.Add($"药材名称不一致：{item.HerbName} != {herb.Name}");
                    }
                }

                return (errors.Count == 0, warnings, errors);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Basic compatibility check failed");
                errors.Add("基础配伍检查过程中发生错误");
                return (false, warnings, errors);
            }
        }

        /// <summary>
        /// 十八反检查（相反药物配伍禁忌）
        /// </summary>
        private async Task<(bool IsValid, List<string> Issues)> CheckEighteenContraryAsync(
            PrescriptionTransactionContext context, CancellationToken cancellationToken)
        {
            var issues = new List<string>();

            try
            {
                // 十八反配伍禁忌表（简化版）
                var contraryPairs = new Dictionary<string, List<string>>
                {
                    ["甘草"] = new List<string> { "甘遂", "大戟", "海藻", "芫花" },
                    ["乌头"] = new List<string> { "贝母", "瓜蒌", "半夏", "白蔹", "白及" },
                    ["藜芦"] = new List<string> { "人参", "沙参", "丹参", "玄参", "细辛", "芍药" }
                };

                var herbNames = context.Items.Select(item => item.HerbName.Trim()).ToList();

                foreach (var kvp in contraryPairs)
                {
                    var primaryHerb = kvp.Key;
                    var contraryHerbs = kvp.Value;

                    if (herbNames.Contains(primaryHerb))
                    {
                        var foundContrary = contraryHerbs.Where(contrary => herbNames.Contains(contrary)).ToList();
                        if (foundContrary.Any())
                        {
                            issues.Add($"十八反配伍禁忌：{primaryHerb} 与 {string.Join("、", foundContrary)} 相反");
                        }
                    }
                }

                return (issues.Count == 0, issues);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Eighteen contrary check failed");
                issues.Add("十八反检查过程中发生错误");
                return (false, issues);
            }
        }

        /// <summary>
        /// 十九畏检查（相畏药物配伍注意）
        /// </summary>
        private async Task<(bool IsValid, List<string> Issues)> CheckNineteenFearAsync(
            PrescriptionTransactionContext context, CancellationToken cancellationToken)
        {
            var issues = new List<string>();

            try
            {
                // 十九畏配伍注意表（简化版）
                var fearPairs = new Dictionary<string, List<string>>
                {
                    ["硫黄"] = new List<string> { "朴硝" },
                    ["水银"] = new List<string> { "砒霜" },
                    ["狼毒"] = new List<string> { "密陀僧" },
                    ["巴豆"] = new List<string> { "牵牛子" },
                    ["丁香"] = new List<string> { "郁金" },
                    ["川乌"] = new List<string> { "犀角" },
                    ["牙硝"] = new List<string> { "三棱" },
                    ["官桂"] = new List<string> { "石脂" },
                    ["人参"] = new List<string> { "五灵脂" }
                };

                var herbNames = context.Items.Select(item => item.HerbName.Trim()).ToList();

                foreach (var kvp in fearPairs)
                {
                    var primaryHerb = kvp.Key;
                    var fearHerbs = kvp.Value;

                    if (herbNames.Contains(primaryHerb))
                    {
                        var foundFear = fearHerbs.Where(fear => herbNames.Contains(fear)).ToList();
                        if (foundFear.Any())
                        {
                            issues.Add($"十九畏配伍注意：{primaryHerb} 畏 {string.Join("、", foundFear)}，需谨慎使用");
                        }
                    }
                }

                return (true, issues); // 十九畏只是注意事项，不阻止使用
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Nineteen fear check failed");
                issues.Add("十九畏检查过程中发生错误");
                return (true, issues);
            }
        }

        /// <summary>
        /// 妊娠用药禁忌检查
        /// </summary>
        private async Task<(bool IsValid, List<string> Issues)> CheckPregnancyCompatibilityAsync(
            PrescriptionTransactionContext context, CancellationToken cancellationToken)
        {
            var issues = new List<string>();

            try
            {
                // 获取患者信息
                var patient = await FindEntityAsync<LYBT.Entities.Patients.Patient>(context.PatientId, cancellationToken);

                // 妊娠禁用药物表（简化版）
                var pregnancyForbiddenHerbs = new List<string>
                {
                    "麝香", "牛黄", "巴豆", "牵牛子", "大戟", "芫花", "甘遂", "商陆", "斑蝥", "蜈蚣",
                    "水蛭", "虻虫", "干漆", "瞿麦", "滑石", "代赭石", "芒硝", "巴豆霜", "轻粉", "雄黄"
                };

                var pregnancyCautionHerbs = new List<string>
                {
                    "桃仁", "红花", "川芎", "牛膝", "薏苡仁", "车前子", "王不留行", "穿山甲", "皂角刺", "枳实"
                };

                // 简化判断：如果是育龄女性（18-45岁），给出妊娠用药提醒
                if (patient != null && patient.Gender == Shared.Models.Enums.Gender.Female)
                {
                    var age = DateTime.Now.Year - patient.BirthDate?.Year;
                    if (age.HasValue && age >= 18 && age <= 45)
                    {
                        var herbNames = context.Items.Select(item => item.HerbName.Trim()).ToList();

                        var forbiddenFound = pregnancyForbiddenHerbs.Where(forbidden => herbNames.Contains(forbidden)).ToList();
                        if (forbiddenFound.Any())
                        {
                            issues.Add($"妊娠禁用药物：{string.Join("、", forbiddenFound)}，请确认患者无妊娠");
                        }

                        var cautionFound = pregnancyCautionHerbs.Where(caution => herbNames.Contains(caution)).ToList();
                        if (cautionFound.Any())
                        {
                            issues.Add($"妊娠慎用药物：{string.Join("、", cautionFound)}，请确认患者无妊娠或谨慎使用");
                        }
                    }
                }

                // 如果发现禁用药物，返回无效；如果只是慎用，返回有效但有提醒
                bool isValid = !context.Items.Any(item => pregnancyForbiddenHerbs.Contains(item.HerbName.Trim()));

                return (isValid, issues);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Pregnancy compatibility check failed");
                issues.Add("妊娠用药检查过程中发生错误");
                return (true, issues);
            }
        }

        /// <summary>
        /// 剂量安全性检查
        /// </summary>
        private async Task<(bool IsValid, List<string> Issues, List<string> Errors)> CheckDosageSafetyAsync(
            PrescriptionTransactionContext context, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            var errors = new List<string>();

            try
            {
                // 药材剂量限制表（简化版）
                var dosageLimits = new Dictionary<string, (decimal Max, decimal Caution)>
                {
                    ["附子"] = (15m, 10m),
                    ["川乌"] = (6m, 3m),
                    ["草乌"] = (6m, 3m),
                    ["雄黄"] = (1.5m, 0.6m),
                    ["朱砂"] = (3m, 1m),
                    ["轻粉"] = (0.3m, 0.1m),
                    ["巴豆"] = (1m, 0.3m),
                    ["斑蝥"] = (0.6m, 0.3m),
                    ["蜈蚣"] = (3m, 1m),
                    ["全蝎"] = (6m, 3m)
                };

                foreach (var item in context.Items)
                {
                    if (dosageLimits.TryGetValue(item.HerbName.Trim(), out var limits))
                    {
                        if (item.Quantity > limits.Max)
                        {
                            errors.Add($"{item.HerbName}用量{item.Quantity}克超过最大安全剂量{limits.Max}克");
                        }
                        else if (item.Quantity > limits.Caution)
                        {
                            issues.Add($"{item.HerbName}用量{item.Quantity}克超过建议剂量{limits.Caution}克，请注意安全");
                        }
                    }

                    // 检查极端剂量
                    if (item.Quantity <= 0)
                    {
                        errors.Add($"{item.HerbName}用量不能为0或负数");
                    }
                    else if (item.Quantity > 100m)
                    {
                        issues.Add($"{item.HerbName}用量{item.Quantity}克过大，请核实");
                    }
                }

                return (errors.Count == 0, issues, errors);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Dosage safety check failed");
                errors.Add("剂量安全检查过程中发生错误");
                return (false, issues, errors);
            }
        }

        /// <summary>
        /// 记录配伍检查历史
        /// </summary>
        private async Task RecordCompatibilityCheckHistoryAsync(
            PrescriptionTransactionContext context,
            List<string> validationResults,
            List<string> warnings,
            List<string> errors,
            CancellationToken cancellationToken)
        {
            try
            {
                context.PrescriptionMetadata["CompatibilityCheck"] = new
                {
                    CheckedAt = DateTime.UtcNow,
                    PrescriptionId = context.PrescriptionId,
                    ValidationResults = validationResults,
                    Warnings = warnings,
                    Errors = errors,
                    WarningCount = warnings.Count,
                    ErrorCount = errors.Count,
                    CheckedItemCount = context.Items.Count
                };

                Logger.LogDebug("Recorded compatibility check history for prescription {PrescriptionId}", context.PrescriptionId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to record compatibility check history");

                // 不抛出异常，因为这不是关键操作
            }
        }

        /// <summary>
        /// 确定严重程度级别
        /// </summary>
        private string DetermineSeverityLevel(List<string> errors, List<string> warnings)
        {
            if (errors.Any(e => e.Contains("十八反") || e.Contains("妊娠禁用") || e.Contains("超过最大安全剂量")))
            {
                return "Critical";
            }
            else if (errors.Count > 0)
            {
                return "High";
            }
            else if (warnings.Count > 3)
            {
                return "Medium";
            }
            else if (warnings.Count > 0)
            {
                return "Low";
            }
            else
            {
                return "None";
            }
        }
    }
}
