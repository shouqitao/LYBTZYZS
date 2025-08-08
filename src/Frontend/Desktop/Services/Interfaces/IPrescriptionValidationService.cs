using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Prescriptions;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 处方质量控制服务接口
    /// </summary>
    public interface IPrescriptionValidationService
    {
        /// <summary>
        /// 验证处方质量
        /// </summary>
        /// <param name="prescriptionItems">处方项目列表</param>
        /// <param name="patientInfo">患者信息</param>
        /// <param name="diagnosis">诊断信息</param>
        /// <returns>验证结果</returns>
        Task<PrescriptionValidationResult> ValidatePrescriptionAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems, 
            PatientValidationInfo patientInfo,
            string diagnosis = "");

        /// <summary>
        /// 检查药物配伍禁忌
        /// </summary>
        /// <param name="prescriptionItems">处方项目</param>
        /// <returns>配伍禁忌检查结果</returns>
        Task<List<DrugInteractionWarning>> CheckDrugInteractionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems);

        /// <summary>
        /// 检查用药剂量合理性
        /// </summary>
        /// <param name="prescriptionItems">处方项目</param>
        /// <param name="patientAge">患者年龄</param>
        /// <param name="patientWeight">患者体重（可选）</param>
        /// <returns>剂量检查结果</returns>
        Task<List<DosageWarning>> CheckDosageAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            int patientAge,
            double patientWeight = 0);

        /// <summary>
        /// 检查特殊人群用药安全性
        /// </summary>
        /// <param name="prescriptionItems">处方项目</param>
        /// <param name="patientInfo">患者信息</param>
        /// <returns>特殊人群用药检查结果</returns>
        Task<List<SpecialPopulationWarning>> CheckSpecialPopulationSafetyAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            PatientValidationInfo patientInfo);

        /// <summary>
        /// 获取处方改进建议
        /// </summary>
        /// <param name="prescriptionItems">处方项目</param>
        /// <param name="diagnosis">诊断信息</param>
        /// <returns>改进建议</returns>
        Task<List<PrescriptionSuggestion>> GetImprovementSuggestionsAsync(
            IEnumerable<PrescriptionItemInfo> prescriptionItems,
            string diagnosis);
    }

    /// <summary>
    /// 处方验证结果
    /// </summary>
    public class PrescriptionValidationResult
    {
        /// <summary>总体质量等级</summary>
        public PrescriptionQualityLevel QualityLevel { get; set; }

        /// <summary>质量评分 (0-100)</summary>
        public int QualityScore { get; set; }

        /// <summary>错误警告列表</summary>
        public List<ValidationWarning> Errors { get; set; } = new();

        /// <summary>警告提醒列表</summary>
        public List<ValidationWarning> Warnings { get; set; } = new();

        /// <summary>信息提示列表</summary>
        public List<ValidationWarning> Infos { get; set; } = new();

        /// <summary>改进建议</summary>
        public List<PrescriptionSuggestion> Suggestions { get; set; } = new();

        /// <summary>是否可以开具处方</summary>
        public bool CanPrescribe => Errors.Count == 0;

        /// <summary>验证总结</summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方质量等级
    /// </summary>
    public enum PrescriptionQualityLevel
    {
        /// <summary>优秀 (90-100分)</summary>
        Excellent = 5,
        /// <summary>良好 (80-89分)</summary>
        Good = 4,
        /// <summary>一般 (70-79分)</summary>
        Fair = 3,
        /// <summary>需改进 (60-69分)</summary>
        NeedsImprovement = 2,
        /// <summary>不合格 (0-59分)</summary>
        Poor = 1
    }

    /// <summary>
    /// 验证警告信息
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>警告类型</summary>
        public ValidationWarningType Type { get; set; }

        /// <summary>警告级别</summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>警告消息</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>相关药材名称</summary>
        public string? HerbName { get; set; }

        /// <summary>建议处理方式</summary>
        public string? Suggestion { get; set; }

        /// <summary>相关规则或依据</summary>
        public string? Reference { get; set; }
    }

    /// <summary>
    /// 验证警告类型
    /// </summary>
    public enum ValidationWarningType
    {
        /// <summary>配伍禁忌</summary>
        DrugInteraction,
        /// <summary>剂量异常</summary>
        DosageIssue,
        /// <summary>特殊人群</summary>
        SpecialPopulation,
        /// <summary>药材质量</summary>
        HerbQuality,
        /// <summary>处方合理性</summary>
        PrescriptionRationality,
        /// <summary>其他</summary>
        Other
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>严重错误 - 必须修正</summary>
        Error = 3,
        /// <summary>警告 - 建议关注</summary>
        Warning = 2,
        /// <summary>信息 - 仅供参考</summary>
        Info = 1
    }

    /// <summary>
    /// 药物相互作用警告
    /// </summary>
    public class DrugInteractionWarning
    {
        /// <summary>相互作用的药材组合</summary>
        public List<string> InteractingHerbs { get; set; } = new();

        /// <summary>相互作用类型</summary>
        public InteractionType Type { get; set; }

        /// <summary>严重程度</summary>
        public InteractionSeverity Severity { get; set; }

        /// <summary>描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>临床意义</summary>
        public string ClinicalSignificance { get; set; } = string.Empty;

        /// <summary>处理建议</summary>
        public string ManagementAdvice { get; set; } = string.Empty;
    }

    /// <summary>
    /// 相互作用类型
    /// </summary>
    public enum InteractionType
    {
        /// <summary>十八反</summary>
        EighteenAntagonisms,
        /// <summary>十九畏</summary>
        NineteenFears,
        /// <summary>妊娠禁忌</summary>
        PregnancyContraindication,
        /// <summary>功效冲突</summary>
        EffectConflict,
        /// <summary>毒性协同</summary>
        ToxicitySynergy,
        /// <summary>其他</summary>
        Other
    }

    /// <summary>
    /// 相互作用严重程度
    /// </summary>
    public enum InteractionSeverity
    {
        /// <summary>严重 - 禁止配伍</summary>
        Severe,
        /// <summary>中等 - 谨慎使用</summary>
        Moderate,
        /// <summary>轻微 - 注意观察</summary>
        Minor
    }

    /// <summary>
    /// 剂量警告
    /// </summary>
    public class DosageWarning
    {
        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>当前剂量</summary>
        public decimal CurrentDosage { get; set; }

        /// <summary>推荐剂量范围</summary>
        public DosageRange RecommendedRange { get; set; } = new();

        /// <summary>警告类型</summary>
        public DosageWarningType Type { get; set; }

        /// <summary>警告描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>风险说明</summary>
        public string RiskDescription { get; set; } = string.Empty;

        /// <summary>调整建议</summary>
        public string AdjustmentAdvice { get; set; } = string.Empty;
    }

    /// <summary>
    /// 剂量警告类型
    /// </summary>
    public enum DosageWarningType
    {
        /// <summary>剂量过高</summary>
        ExcessiveDose,
        /// <summary>剂量过低</summary>
        InsufficientDose,
        /// <summary>有毒药材超量</summary>
        ToxicOverdose,
        /// <summary>儿童剂量不当</summary>
        PediatricDosageIssue,
        /// <summary>老年人剂量不当</summary>
        GeriatricDosageIssue
    }

    /// <summary>
    /// 剂量范围
    /// </summary>
    public class DosageRange
    {
        /// <summary>最小剂量</summary>
        public decimal MinDose { get; set; }

        /// <summary>最大剂量</summary>
        public decimal MaxDose { get; set; }

        /// <summary>常用剂量</summary>
        public decimal TypicalDose { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";
    }

    /// <summary>
    /// 特殊人群用药警告
    /// </summary>
    public class SpecialPopulationWarning
    {
        /// <summary>人群类型</summary>
        public SpecialPopulationType PopulationType { get; set; }

        /// <summary>相关药材</summary>
        public List<string> AffectedHerbs { get; set; } = new();

        /// <summary>风险等级</summary>
        public RiskLevel RiskLevel { get; set; }

        /// <summary>风险描述</summary>
        public string RiskDescription { get; set; } = string.Empty;

        /// <summary>处理建议</summary>
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// 特殊人群类型
    /// </summary>
    public enum SpecialPopulationType
    {
        /// <summary>孕妇</summary>
        Pregnant,
        /// <summary>哺乳期</summary>
        Lactating,
        /// <summary>儿童</summary>
        Pediatric,
        /// <summary>老年人</summary>
        Geriatric,
        /// <summary>肝功能不全</summary>
        HepaticImpairment,
        /// <summary>肾功能不全</summary>
        RenalImpairment,
        /// <summary>心功能不全</summary>
        CardiacImpairment
    }

    /// <summary>
    /// 风险等级
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>高风险 - 禁用</summary>
        High,
        /// <summary>中等风险 - 慎用</summary>
        Medium,
        /// <summary>低风险 - 注意</summary>
        Low
    }

    /// <summary>
    /// 处方改进建议
    /// </summary>
    public class PrescriptionSuggestion
    {
        /// <summary>建议类型</summary>
        public SuggestionType Type { get; set; }

        /// <summary>优先级</summary>
        public SuggestionPriority Priority { get; set; }

        /// <summary>建议内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>理由说明</summary>
        public string Rationale { get; set; } = string.Empty;

        /// <summary>预期效果</summary>
        public string ExpectedOutcome { get; set; } = string.Empty;
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum SuggestionType
    {
        /// <summary>增加药材</summary>
        AddHerb,
        /// <summary>减少药材</summary>
        RemoveHerb,
        /// <summary>调整剂量</summary>
        AdjustDosage,
        /// <summary>更换药材</summary>
        ReplaceHerb,
        /// <summary>优化配伍</summary>
        OptimizeCombination,
        /// <summary>改进用法</summary>
        ImproveUsage
    }

    /// <summary>
    /// 建议优先级
    /// </summary>
    public enum SuggestionPriority
    {
        /// <summary>高优先级</summary>
        High = 3,
        /// <summary>中优先级</summary>
        Medium = 2,
        /// <summary>低优先级</summary>
        Low = 1
    }

    /// <summary>
    /// 患者验证信息
    /// </summary>
    public class PatientValidationInfo
    {
        /// <summary>年龄</summary>
        public int Age { get; set; }

        /// <summary>性别</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>体重(kg)</summary>
        public double? Weight { get; set; }

        /// <summary>是否怀孕</summary>
        public bool IsPregnant { get; set; }

        /// <summary>是否哺乳期</summary>
        public bool IsLactating { get; set; }

        /// <summary>过敏史</summary>
        public List<string> Allergies { get; set; } = new();

        /// <summary>既往病史</summary>
        public List<string> MedicalHistory { get; set; } = new();

        /// <summary>当前用药</summary>
        public List<string> CurrentMedications { get; set; } = new();

        /// <summary>肝功能状态</summary>
        public OrganFunctionStatus? LiverFunction { get; set; }

        /// <summary>肾功能状态</summary>
        public OrganFunctionStatus? KidneyFunction { get; set; }
    }

    /// <summary>
    /// 脏器功能状态
    /// </summary>
    public enum OrganFunctionStatus
    {
        /// <summary>正常</summary>
        Normal,
        /// <summary>轻度异常</summary>
        MildImpairment,
        /// <summary>中度异常</summary>
        ModerateImpairment,
        /// <summary>重度异常</summary>
        SevereImpairment
    }
}